using MakesCentsToMe.Api.Models.Entities;

namespace MakesCentsToMe.Api.Features.Import;

public record ColumnMappingRequest(string CsvColumnName, string ApplicationField);

public record ColumnMappingResponse(Guid Id, string CsvColumnName, string ApplicationField);

public record ImportProfileResponse(
    Guid Id,
    Guid AccountId,
    AmountType AmountType,
    bool BalanceProvided,
    string DateFormat,
    IReadOnlyList<ColumnMappingResponse> ColumnMappings);

public record ProcessImportRequest(
    decimal? ClosingBalance,
    decimal? OpeningBalance);

public record ProcessImportResponse(int TransactionsCreated, int RowsSkipped);

public record SaveImportProfileRequest(
    AmountType AmountType,
    bool BalanceProvided,
    IReadOnlyList<ColumnMappingRequest> ColumnMappings,
    string DateFormat);

public record UploadPreviewResponse(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> PreviewRows);
