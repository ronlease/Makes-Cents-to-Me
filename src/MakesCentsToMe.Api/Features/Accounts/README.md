# Accounts

Manages accounts (checking, savings, credit card) scoped to a parent institution. Each account can have an associated import profile for CSV processing.

## Endpoints

All routes are under `/api/v1/institutions/{institutionId}/accounts`.

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/` | Create a new account within an institution |
| GET | `/` | List all accounts for an institution |
| GET | `/{id}` | Get an account by ID |
| PUT | `/{id}` | Update an account |
| DELETE | `/{id}` | Delete an account (fails if transactions exist) |

## Key Types

- **CreateAccountRequest** -- `Name` (string), `AccountType` (enum: Checking, Savings, CreditCard)
- **UpdateAccountRequest** -- `Name` (string), `AccountType` (enum)
- **AccountResponse** -- `Id` (Guid), `Name` (string), `AccountType` (enum), `HasImportProfile` (bool)
- **IAccountService** -- service interface with `CreateAsync`, `DeleteAsync`, `GetByIdAsync`, `ListAsync`, `UpdateAsync`
