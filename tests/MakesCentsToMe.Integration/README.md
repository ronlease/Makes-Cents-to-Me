# MakesCentsToMe.Integration

End-to-end tests that host the real API in-process (`WebApplicationFactory<Program>`) against a
throwaway PostgreSQL container started by Testcontainers. They exercise HTTP, routing, validation,
EF Core, migrations, and the import pipeline together.

## Prerequisites

- Docker running, with Linux containers (Docker Desktop on Windows, Docker or Podman on Fedora)
- Ability to pull `postgres:17` (first run only)

## Running

```bash
dotnet test tests/MakesCentsToMe.Integration
```

The container is independent of `docker compose`; the development database on port 5434 is never used.

## Architecture

| Piece | File (in `Infrastructure/`) | Role |
|---|---|---|
| Factory | `IntegrationTestWebApplicationFactory` | Owns the `postgres:17` container, starts the host (so migrations run and failures surface at startup), and overrides configuration and Claude wiring. Use `CreateApiClient()` for an HTTPS client with redirects disabled. |
| Collection | `IntegrationTestCollection` | `[CollectionDefinition("Integration")]` sharing one factory (one container) across all test classes. |
| Base class | `IntegrationTestBase` | Exposes `Client`, `Factory`, `ClaudeRequestRecorder`, `CreateDbContextScope()`; resets the database before each test via `ResetDatabaseAsync()`. |
| Database scope | `DatabaseScope` | A DI scope plus `AppDbContext`, for direct database assertions or seeding. Dispose with `await using`. |
| Claude stub | `StubClaudeMessageHandler`, `ClaudeRequestRecorder` | Replaces the primary HTTP handler of the Claude typed client. The real `ClaudeAnalysisService` runs; the stub returns deterministic suggestions and records every request so tests can assert what Claude was (or was not) asked. |
| JSON helpers | `ApiJson` | `Options` (camelCase, string enums), `ReadApiResponseAsync<T>`, `ReadJsonDocumentAsync`, `AssertApiResponseEnvelope` (top level is exactly `data`, `errors`, `success`). |
| Seeding | `ApiSeeder` | Arranges data through the public API: `CreateInstitutionAsync`, `CreateAccountAsync`, `CreateAccountWithProfileAsync`, `CreateCategoryAsync`, `CreateLearnedRuleAsync`, `GetCategoryIdAsync`, `SaveImportProfileAsync`, `ProcessImportAsync`, `PostProcessImportAsync`. |
| Sample data | `CsvSamples` | Canned CSVs (`FirstImport`, `OverlapImport`, `HeaderOnly`, `NoBalanceImport`) and matching profiles (`StandardProfile`, `NoBalanceProfile`). |

Configuration is injected with `UseSetting` (connection string and a dummy Claude API key) because
`Program.cs` reads the connection string eagerly, before `ConfigureAppConfiguration` callbacks apply.
The host runs in the `IntegrationTesting` environment (Swagger is not mapped).

### Why tests run sequentially

All test classes share one container and one database, and the reset truncates every table. xUnit
runs classes in the same collection sequentially, which is what keeps tests isolated. Do not move
a class out of the `Integration` collection, and do not enable parallel test execution for this
assembly.

## Adding a test

1. Create the class under `Features/<Feature>/`, derive from `IntegrationTestBase`, and add
   `[Collection("Integration")]`. Take `IntegrationTestWebApplicationFactory` in the constructor and pass it to the base.
2. Put the Gherkin scenario as a comment above the class; name methods `Method_Scenario_ExpectedResult`;
   use Arrange/Act/Assert comments and FluentAssertions.
3. Arrange with `ApiSeeder` (through the API). Use `CreateDbContextScope()` only to seed state the API
   cannot create or to assert persisted values (use `AsNoTracking()`).
4. Assert status code, the `ApiResponse` envelope, and data fields. Assert `ClaudeRequestRecorder` when
   the test concerns whether Claude is called.
5. Never call the real Claude API; the stub is always installed.

Members within a class are alphabetical and names are not abbreviated (project conventions).

## Guards against empty test projects

An empty test project reports "No test is available" and can look like a pass. Two guards prevent this:

- `tests/Directory.Build.targets` fails the build of any test project that has no test source files.
- CI runs each test project separately and verifies that every project discovered a non-zero number
  of tests; it also runs a self-test confirming the build guard rejects an empty project.

## Troubleshooting

- `Docker is either not running or misconfigured`: start Docker and confirm `docker info` works.
- Container start timeouts on first run: the `postgres:17` image is still pulling; retry.
- Ryuk (the Testcontainers cleanup container) cannot start in some rootless or restricted setups:
  set `TESTCONTAINERS_RYUK_DISABLED=true`. Containers are then not auto-removed if the run is killed;
  remove leftovers with `docker ps -a` and `docker rm -f`.
- A startup failure in the factory means a migration failed or configuration is missing; the inner
  exception from `InitializeAsync` shows which.
