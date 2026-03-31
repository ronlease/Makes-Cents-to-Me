# Makes Cents To Me — Claude Code Orchestration

## Project Overview
Makes Cents To Me is a personal spending intelligence and trend analysis tool. It ingests
CSV exports from financial institutions, normalizes and categorizes transactions via Claude,
and surfaces spending patterns, personal inflation rates, trend projections, and anomaly
alerts through a Mint-style dashboard. Single-user, local deployment only.

## Tech Stack
- **API:** ASP.NET Core 10 Web API, Entity Framework Core 10, PostgreSQL
- **Frontend:** Angular 21, standalone components, Angular Material, ApexCharts
- **Auth:** None — single user, local deployment
- **AI:** Claude API (vendor normalization, categorization, dedup algorithm derivation)
- **Testing:** xUnit, Gherkin-style naming, FluentAssertions, Moq
- **Documentation:** Swashbuckle (OpenAPI/Swagger), PlantUML (C4 models)
- **Infrastructure:** Docker Compose (Windows and Fedora)
- **Secrets:** dotnet user-secrets (local)

## Repository Structure
```
MakesCentsToMe/
  src/
    MakesCentsToMe.Api/
      Common/                     # Shared types
      Features/
        Accounts/
        Alerts/
        Categories/
        Import/                   # CSV upload, parsers, pipeline, review queue
        LearnedRules/
        Recurring/
        Spending/
        Transactions/
      Infrastructure/
        Claude/                   # Claude API client
        Data/                     # EF Core DbContext, migrations
      Models/
        Entities/                 # EF Core entity classes
      Program.cs
    MakesCentsToMe.Web/           # Angular 21 frontend
  tests/
    MakesCentsToMe.Unit/          # xUnit unit tests, mirroring Features/ structure
    MakesCentsToMe.Integration/   # xUnit integration tests (EF Core in-memory)
  docs/
    backlog.md                    # Owned by Product Owner agent
    c4/                           # PlantUML C4 model files
  docker-compose.yml
  .claude/
    agents/
```

## Agent Roster
| Agent | File | Responsibility |
|---|---|---|
| Product Owner | `product-owner.md` | Backlog, business problems, acceptance criteria |
| Architect | `architect.md` | Swashbuckle OpenAPI, PlantUML C4 models |
| Backend Engineer | `backend-engineer.md` | .NET 10 API, import pipeline, Claude integration |
| Frontend Engineer | `frontend-engineer.md` | Angular 21, dashboards, ApexCharts |
| QA Engineer | `qa-engineer.md` | Gherkin scenarios, xUnit tests |

## Workflow
- Workflow is fluid. Any agent may be invoked at any time.
- Agents may edit files directly without asking for approval.
- The Backend Engineer and QA Engineer work alongside each other:
  the Engineer implements a feature, QA immediately writes tests for it before moving on.
- The Architect generates and updates OpenAPI specs and C4 models after API changes.
- The Product Owner owns `docs/backlog.md` exclusively.
- Commit locally freely as work progresses. Only push to origin or open/update PRs when explicitly asked.
- **Never commit directly to main.** Always create a feature branch and commit there.

## Routing Rules
- "backlog", "story", "feature request", "business problem" → Product Owner
- "C4", "diagram", "architecture", "swagger", "openapi" → Architect
- "implement", "endpoint", "controller", "service", "repository", "EF", "migration", "parser", "import", "dedup" → Backend Engineer
- "component", "angular", "frontend", "UI", "page", "route", "chart", "dashboard" → Frontend Engineer
- "test", "gherkin", "scenario", "given/when/then", "coverage" → QA Engineer

## Conventions
- C# follows Microsoft conventions. Use `var` where type is obvious.
- All API endpoints are versioned under `/api/v1/`.
- All secrets go through `dotnet user-secrets` locally. Never hardcode credentials.
- EF Core migrations are explicit. Auto-migration on startup is allowed.
- Angular uses standalone components. No NgModules.
- All new features require a backlog entry before implementation.
- **All fields, properties, methods, and variables within a class must be declared in alphabetical order.** Applies to both C# and TypeScript. Enforced to ease diffs and code review.
- **Avoid abbreviations in naming.** Use full names in both C# and TypeScript.
- Never collapse raw institution data — always preserve originals verbatim.

## Pre-PR Checklist
Before any PR is opened, verify the following:
1. All README files in the repo are up-to-date
2. Every vertical slice feature folder has a `README.md`
3. All PlantUML C4 diagrams in `docs/c4/` are up-to-date
4. All Swagger/OpenAPI docs are up-to-date (new endpoints documented, descriptions accurate)
5. All projects build successfully (`dotnet build`, `ng build`)
6. All tests pass (`dotnet test`)
7. Code coverage is at least 90% (excluding EF migrations, generated code, property-only DTOs, and Program.cs)
8. Run `dotnet format` and fix all violations
9. Delete any leftover `coverage-*/` and `**/TestResults/` directories before committing
10. Update `docs/backlog.md` — mark completed items as `Done`, verify no stale statuses

## Domain Concepts
- **Institution:** A financial institution (credit union, bank, credit card issuer)
- **Account:** A checking, savings, or credit card account belonging to an institution
- **Transaction:** A single financial event belonging to an account
- **Raw Description:** The original vendor string from the CSV export — never modified after import
- **Normalized Vendor:** Claude-derived clean merchant name mapped from the raw description
- **Canonical Category:** One of ~15 user-defined categories all transactions map into
- **Learned Rule:** A raw description pattern → normalized vendor + category mapping promoted from a user-accepted correction
- **Review Queue:** Post-import list of Claude-analyzed transactions pending user acceptance or override
- **Recurring Transaction:** A transaction Claude or the system identifies as a regular bill or subscription

## Import Pipeline
1. User uploads CSV via frontend file picker
2. Backend identifies institution and account type, selects the correct parser
3. Parser normalizes raw rows into a common internal transaction schema
4. Dedup check runs against existing transactions using the derived composite key
5. Claude analyzes each new transaction: suggests normalized vendor and canonical category
6. Transactions enter the Review Queue with Claude's suggestions and confidence levels
7. User accepts or overrides each row; accepted corrections promote to Learned Rules
8. Committed transactions become part of reporting data
