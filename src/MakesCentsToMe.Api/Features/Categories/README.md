# Categories

Manages canonical spending categories used to classify transactions.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/v1/categories` | Create a new category |
| GET | `/api/v1/categories` | List all categories |
| GET | `/api/v1/categories/{id}` | Get a category by ID |
| PUT | `/api/v1/categories/{id}` | Update a category |
| DELETE | `/api/v1/categories/{id}` | Delete a category (only if no assigned transactions) |

## Key Types

- `CategoryResponse` — ID, name, isDefault flag, transaction count
- `CreateCategoryRequest` / `UpdateCategoryRequest` — name only
- `ICategoryService` — service interface for category CRUD
