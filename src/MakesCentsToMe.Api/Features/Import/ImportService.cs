using MakesCentsToMe.Api.Common;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Api.Features.Import;

public interface IImportService
{
    Task<ApiResponse<ImportProfileResponse>> GetProfileAsync(Guid accountId);
    Task<ApiResponse<ProcessImportResponse>> ProcessAsync(Guid accountId, Stream csvStream, ProcessImportRequest request);
    Task<ApiResponse<ImportProfileResponse>> SaveProfileAsync(Guid accountId, SaveImportProfileRequest request);
    Task<ApiResponse<UploadPreviewResponse>> UploadPreviewAsync(Guid accountId, Stream csvStream);
    Task<ApiResponse<ImportProfileResponse>> UpdateProfileAsync(Guid accountId, SaveImportProfileRequest request);
}

public class ImportService(AppDbContext dbContext) : IImportService
{
    private const int PreviewRowCount = 5;

    public async Task<ApiResponse<ImportProfileResponse>> GetProfileAsync(Guid accountId)
    {
        var profile = await dbContext.ImportProfiles
            .Include(p => p.ColumnMappings)
            .FirstOrDefaultAsync(p => p.AccountId == accountId);

        if (profile is null)
        {
            return ApiResponse<ImportProfileResponse>.Fail($"No import profile found for account '{accountId}'.");
        }

        return ApiResponse<ImportProfileResponse>.Ok(MapProfileToResponse(profile));
    }

    public async Task<ApiResponse<ProcessImportResponse>> ProcessAsync(
        Guid accountId,
        Stream csvStream,
        ProcessImportRequest request)
    {
        var account = await dbContext.Accounts
            .Include(a => a.ImportProfile)
                .ThenInclude(p => p!.ColumnMappings)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account is null)
        {
            return ApiResponse<ProcessImportResponse>.Fail($"Account '{accountId}' not found.");
        }

        if (account.ImportProfile is null)
        {
            return ApiResponse<ProcessImportResponse>.Fail(
                "No import profile configured for this account. Create a profile before processing.");
        }

        var profile = account.ImportProfile;

        if (!profile.BalanceProvided && request.OpeningBalance is null && request.ClosingBalance is null)
        {
            return ApiResponse<ProcessImportResponse>.Fail(
                "This import profile does not include balance data. Provide either openingBalance or closingBalance.");
        }

        var lines = ReadNonEmptyLines(csvStream);

        if (lines.Count < 2)
        {
            return ApiResponse<ProcessImportResponse>.Ok(new ProcessImportResponse(0, 0));
        }

        var headers = ParseCsvLine(lines[0]);

        var transactions = new List<Transaction>();
        var skippedCount = 0;

        for (var lineIndex = 1; lineIndex < lines.Count; lineIndex++)
        {
            var rawLine = lines[lineIndex];
            var columns = ParseCsvLine(rawLine);

            if (columns.Count != headers.Count)
            {
                skippedCount++;
                continue;
            }

            var rawData = BuildRawData(headers, columns);
            var fieldValues = BuildApplicationFieldLookup(profile.ColumnMappings, headers, columns);

            var dateText = GetMappedValue(fieldValues, "Date");
            var description = GetMappedValue(fieldValues, "Description");

            if (string.IsNullOrWhiteSpace(dateText) || string.IsNullOrWhiteSpace(description))
            {
                skippedCount++;
                continue;
            }

            if (!TryParseDate(dateText, profile.DateFormat, out var date))
            {
                skippedCount++;
                continue;
            }

            decimal principal = 0;
            decimal interest = 0;
            decimal fees = 0;
            decimal amount = 0;

            if (profile.AmountType == AmountType.Split)
            {
                principal = ParseDecimal(GetMappedValue(fieldValues, "Principal"));
                interest = ParseDecimal(GetMappedValue(fieldValues, "Interest"));
                fees = ParseDecimal(GetMappedValue(fieldValues, "Fees"));
                amount = principal + interest + fees;
            }
            else
            {
                amount = ParseDecimal(GetMappedValue(fieldValues, "Amount"));
                principal = amount;
                interest = 0;
                fees = 0;
            }

            decimal? balance = null;
            if (profile.BalanceProvided)
            {
                var balanceText = GetMappedValue(fieldValues, "Balance");
                if (!string.IsNullOrWhiteSpace(balanceText))
                {
                    balance = ParseDecimal(balanceText);
                }
            }

            var category = GetMappedValue(fieldValues, "Category");
            var checkNumber = GetMappedValue(fieldValues, "CheckNumber");

            var transaction = new Transaction
            {
                Account = account,
                AccountId = accountId,
                Amount = amount,
                Balance = balance,
                Category = string.IsNullOrWhiteSpace(category) ? null : category,
                CheckNumber = string.IsNullOrWhiteSpace(checkNumber) ? null : checkNumber,
                Date = date,
                Description = description,
                Fees = fees,
                Id = Guid.NewGuid(),
                Interest = interest,
                Principal = principal,
                RawCsvRow = rawLine,
                RawData = rawData,
            };

            transactions.Add(transaction);
        }

        if (!profile.BalanceProvided && transactions.Count > 0)
        {
            ComputeBalances(transactions, request.OpeningBalance, request.ClosingBalance);
        }

        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync();

        return ApiResponse<ProcessImportResponse>.Ok(
            new ProcessImportResponse(transactions.Count, skippedCount));
    }

    public async Task<ApiResponse<ImportProfileResponse>> SaveProfileAsync(Guid accountId, SaveImportProfileRequest request)
    {
        var accountExists = await dbContext.Accounts.AnyAsync(a => a.Id == accountId);
        if (!accountExists)
        {
            return ApiResponse<ImportProfileResponse>.Fail($"Account '{accountId}' not found.");
        }

        var existingProfile = await dbContext.ImportProfiles
            .AnyAsync(p => p.AccountId == accountId);

        if (existingProfile)
        {
            return ApiResponse<ImportProfileResponse>.Fail(
                "An import profile already exists for this account. Use PUT to update it.");
        }

        var profile = new ImportProfile
        {
            AccountId = accountId,
            AmountType = request.AmountType,
            BalanceProvided = request.BalanceProvided,
            ColumnMappings = request.ColumnMappings
                .Select(m => new ColumnMapping
                {
                    ApplicationField = m.ApplicationField,
                    CsvColumnName = m.CsvColumnName,
                    Id = Guid.NewGuid(),
                })
                .ToList(),
            DateFormat = request.DateFormat,
            Id = Guid.NewGuid(),
        };

        dbContext.ImportProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        return ApiResponse<ImportProfileResponse>.Ok(MapProfileToResponse(profile));
    }

    public async Task<ApiResponse<ImportProfileResponse>> UpdateProfileAsync(Guid accountId, SaveImportProfileRequest request)
    {
        var profile = await dbContext.ImportProfiles
            .Include(p => p.ColumnMappings)
            .FirstOrDefaultAsync(p => p.AccountId == accountId);

        if (profile is null)
        {
            return ApiResponse<ImportProfileResponse>.Fail($"No import profile found for account '{accountId}'.");
        }

        profile.AmountType = request.AmountType;
        profile.BalanceProvided = request.BalanceProvided;
        profile.DateFormat = request.DateFormat;

        dbContext.ColumnMappings.RemoveRange(profile.ColumnMappings);

        profile.ColumnMappings = request.ColumnMappings
            .Select(m => new ColumnMapping
            {
                ApplicationField = m.ApplicationField,
                CsvColumnName = m.CsvColumnName,
                Id = Guid.NewGuid(),
                ImportProfileId = profile.Id,
            })
            .ToList();

        await dbContext.SaveChangesAsync();

        return ApiResponse<ImportProfileResponse>.Ok(MapProfileToResponse(profile));
    }

    public async Task<ApiResponse<UploadPreviewResponse>> UploadPreviewAsync(Guid accountId, Stream csvStream)
    {
        var accountExists = await dbContext.Accounts.AnyAsync(a => a.Id == accountId);
        if (!accountExists)
        {
            return ApiResponse<UploadPreviewResponse>.Fail($"Account '{accountId}' not found.");
        }

        var lines = ReadNonEmptyLines(csvStream);

        if (lines.Count == 0)
        {
            return ApiResponse<UploadPreviewResponse>.Fail("The uploaded CSV file is empty.");
        }

        var headers = ParseCsvLine(lines[0]);

        var previewRows = lines
            .Skip(1)
            .Take(PreviewRowCount)
            .Select(line => (IReadOnlyList<string>)ParseCsvLine(line))
            .ToList();

        return ApiResponse<UploadPreviewResponse>.Ok(new UploadPreviewResponse(headers, previewRows));
    }

    private static Dictionary<string, string> BuildApplicationFieldLookup(
        ICollection<ColumnMapping> mappings,
        List<string> headers,
        List<string> columns)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            var headerIndex = headers.FindIndex(h =>
                string.Equals(h, mapping.CsvColumnName, StringComparison.OrdinalIgnoreCase));

            if (headerIndex >= 0 && headerIndex < columns.Count)
            {
                lookup[mapping.ApplicationField] = columns[headerIndex];
            }
        }

        return lookup;
    }

    private static Dictionary<string, string> BuildRawData(List<string> headers, List<string> columns)
    {
        var rawData = new Dictionary<string, string>();
        for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
        {
            rawData[headers[columnIndex]] = columnIndex < columns.Count ? columns[columnIndex] : string.Empty;
        }

        return rawData;
    }

    private static void ComputeBalances(
        List<Transaction> transactions,
        decimal? openingBalance,
        decimal? closingBalance)
    {
        if (openingBalance.HasValue)
        {
            var runningBalance = openingBalance.Value;
            foreach (var transaction in transactions)
            {
                runningBalance += transaction.Amount;
                transaction.Balance = runningBalance;
            }
        }
        else if (closingBalance.HasValue)
        {
            var runningBalance = closingBalance.Value;
            for (var transactionIndex = transactions.Count - 1; transactionIndex >= 0; transactionIndex--)
            {
                transactions[transactionIndex].Balance = runningBalance;
                runningBalance -= transactions[transactionIndex].Amount;
            }
        }
    }

    private static string GetMappedValue(Dictionary<string, string> applicationFieldLookup, string applicationField)
    {
        return applicationFieldLookup.TryGetValue(applicationField, out var value) ? value : string.Empty;
    }

    private static ImportProfileResponse MapProfileToResponse(ImportProfile profile) =>
        new(
            profile.Id,
            profile.AccountId,
            profile.AmountType,
            profile.BalanceProvided,
            profile.DateFormat,
            profile.ColumnMappings
                .OrderBy(m => m.ApplicationField)
                .Select(m => new ColumnMappingResponse(m.Id, m.CsvColumnName, m.ApplicationField))
                .ToList());

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return decimal.TryParse(value, out var result) ? result : 0;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        var insideQuotes = false;

        for (var characterIndex = 0; characterIndex < line.Length; characterIndex++)
        {
            var character = line[characterIndex];

            if (character == '"')
            {
                if (insideQuotes && characterIndex + 1 < line.Length && line[characterIndex + 1] == '"')
                {
                    currentField.Append('"');
                    characterIndex++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                fields.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(character);
            }
        }

        fields.Add(currentField.ToString().Trim());
        return fields;
    }

    private static List<string> ReadNonEmptyLines(Stream stream)
    {
        var lines = new List<string>();
        using var reader = new StreamReader(stream, leaveOpen: true);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is not null && !string.IsNullOrWhiteSpace(line.Replace(",", "")))
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private static bool TryParseDate(string value, string dateFormat, out DateTime date)
    {
        return DateTime.TryParseExact(
            value,
            dateFormat,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out date);
    }
}
