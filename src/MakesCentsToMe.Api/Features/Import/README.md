# Import

Handles the CSV import pipeline: uploading files for preview, configuring column-mapping profiles, and processing CSV data into transactions. Each account has at most one import profile that defines how its CSV columns map to application fields.

## Endpoints

All routes are under `/api/v1/accounts/{accountId}/import`.

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/upload` | Upload a CSV file and preview its headers and first rows |
| POST | `/profile` | Save an import profile (column mappings) for an account |
| GET | `/profile` | Get the import profile for an account |
| PUT | `/profile` | Update the import profile for an account |
| POST | `/process` | Process a CSV file using the saved import profile and create transactions |

## Key Types

- **UploadPreviewResponse** -- `Headers` (list of string), `PreviewRows` (list of row arrays)
- **SaveImportProfileRequest** -- `AmountType` (enum: Single, Split), `BalanceProvided` (bool), `ColumnMappings` (list), `DateFormat` (string)
- **ImportProfileResponse** -- `Id`, `AccountId`, `AmountType`, `BalanceProvided`, `DateFormat`, `ColumnMappings`
- **ColumnMappingRequest** / **ColumnMappingResponse** -- maps a `CsvColumnName` to an `ApplicationField`
- **ProcessImportRequest** -- `ClosingBalance` (decimal?), `OpeningBalance` (decimal?)
- **ProcessImportResponse** -- `TransactionsCreated` (int), `RowsSkipped` (int)
- **IImportService** -- service interface with `GetProfileAsync`, `ProcessAsync`, `SaveProfileAsync`, `UpdateProfileAsync`, `UploadPreviewAsync`
