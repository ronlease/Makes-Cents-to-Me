# Review Queue

Manages the post-import review workflow where users accept or override Claude's
transaction analysis suggestions.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/review?accountId={optional}` | List transactions pending review |
| PUT | `/api/v1/review/{transactionId}/accept` | Accept Claude's suggestion |
| PUT | `/api/v1/review/{transactionId}/override` | Override with user-provided values |
| POST | `/api/v1/review/accept-all?accountId={optional}` | Bulk accept all pending transactions |

## Key Types

- `ReviewTransactionResponse` — full transaction details with Claude suggestions and status
- `OverrideTransactionRequest` — user-provided normalized vendor and category ID
- `IReviewService` — service interface for review operations
