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
- **ProcessImportResponse** -- `AutoCategorizedCount` (int, transactions matched by a learned rule), `DuplicatesSkipped` (int), `RowsSkipped` (int), `TransactionsCreated` (int, all new transactions)
- **IImportService** -- service interface with `GetProfileAsync`, `ProcessAsync`, `SaveProfileAsync`, `UpdateProfileAsync`, `UploadPreviewAsync`

## Pipeline

1. Parse CSV rows using the account's import profile
2. Dedup against existing transactions (date, description, amount)
3. Save new transactions with `Pending` status
4. Learned rules are applied before Claude; matched transactions are auto-categorized (`IsAutoCategorized`) and counted in `autoCategorizedCount`
5. Only unmatched transactions are sent to Claude for analysis (skipped entirely when all match)
6. Transactions enter the review queue
