using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Models.Entities;

namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// Canned CSV content and the matching import profiles. Unless noted, samples use the header
/// <c>Date,Description,Amount,Balance</c> and the date format <c>MM/dd/yyyy</c>.
/// </summary>
public static class CsvSamples
{
    public const string Header = "Date,Description,Amount,Balance";

    /// <summary>A line whose column count differs from the header; the import skips it.</summary>
    public const string MalformedLine = "01/18/2026,TOO FEW COLUMNS,-1.00";

    public const string PayrollLine = "01/17/2026,PAYROLL DEPOSIT,2000.00,2955.25";

    public const string QuotedCommaLine = "01/15/2026,\"STARBUCKS #123, SEATTLE WA\",-4.75,995.25";

    public const string ShellLine = "01/16/2026,SHELL OIL 5551,-40.00,955.25";

    public const string TraderJoesLine = "01/18/2026,TRADER JOES #55,-23.10,2932.15";

    /// <summary>Three valid rows (one with a quoted comma field, one negative decimal) plus one malformed row.</summary>
    public static string FirstImport { get; } =
        string.Join("\n", Header, QuotedCommaLine, ShellLine, PayrollLine, MalformedLine) + "\n";

    /// <summary>Header-only file: no data rows.</summary>
    public static string HeaderOnly { get; } = Header + "\n";

    /// <summary>Header Date,Description,Amount (no balance column); pair with <see cref="NoBalanceProfile"/>.</summary>
    public static string NoBalanceImport { get; } =
        string.Join("\n", "Date,Description,Amount", "01/15/2026,COFFEE SHOP,-5.00", "01/16/2026,BOOKSTORE,-20.00") + "\n";

    /// <summary>Single amount column, no balance column; imports must supply an opening or closing balance.</summary>
    public static SaveImportProfileRequest NoBalanceProfile { get; } = new(
        AmountType.Single,
        false,
        [
            new ColumnMappingRequest("Amount", "Amount"),
            new ColumnMappingRequest("Date", "Date"),
            new ColumnMappingRequest("Description", "Description"),
        ],
        "MM/dd/yyyy");

    /// <summary>Shares <see cref="ShellLine"/> and <see cref="PayrollLine"/> with <see cref="FirstImport"/> and adds one new row.</summary>
    public static string OverlapImport { get; } =
        string.Join("\n", Header, ShellLine, PayrollLine, TraderJoesLine) + "\n";

    /// <summary>Single amount column, balance column provided.</summary>
    public static SaveImportProfileRequest StandardProfile { get; } = new(
        AmountType.Single,
        true,
        [
            new ColumnMappingRequest("Amount", "Amount"),
            new ColumnMappingRequest("Balance", "Balance"),
            new ColumnMappingRequest("Date", "Date"),
            new ColumnMappingRequest("Description", "Description"),
        ],
        "MM/dd/yyyy");
}
