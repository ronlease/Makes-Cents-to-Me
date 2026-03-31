# Institutions

Manages financial institutions (banks, credit unions, credit card issuers). Each institution serves as a parent container for one or more accounts.

## Endpoints

All routes are under `/api/v1/institutions`.

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/` | Create a new institution |
| GET | `/` | List all institutions |
| GET | `/{id}` | Get an institution by ID |
| PUT | `/{id}` | Update an institution |
| DELETE | `/{id}` | Delete an institution (fails if accounts exist) |

## Key Types

- **CreateInstitutionRequest** -- `Name` (string)
- **UpdateInstitutionRequest** -- `Name` (string)
- **InstitutionResponse** -- `Id` (Guid), `Name` (string), `AccountCount` (int)
- **IInstitutionService** -- service interface with `CreateAsync`, `DeleteAsync`, `GetByIdAsync`, `ListAsync`, `UpdateAsync`
