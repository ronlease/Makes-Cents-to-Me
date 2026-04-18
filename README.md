# Makes Cents To Me

Personal spending intelligence and trend analysis tool. Ingests CSV exports from financial institutions, normalizes transactions via Claude, and surfaces spending patterns, personal inflation rates, trend projections, and anomaly alerts.

## Tech Stack

- **API:** ASP.NET Core 10, Entity Framework Core 10, PostgreSQL 17
- **Frontend:** Angular 21, Angular Material, ApexCharts
- **AI:** Claude API (vendor normalization, categorization)
- **Testing:** xUnit, FluentAssertions, Moq
- **Infrastructure:** Docker Compose

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 22](https://nodejs.org/) with npm
- [Docker](https://www.docker.com/) (for PostgreSQL)

## Getting Started

### 1. Start PostgreSQL

```bash
docker compose up -d
```

This starts PostgreSQL 17 on `localhost:5434`.

### 2. Run the API

```bash
cd src/MakesCentsToMe.Api
dotnet ef database update
dotnet run
```

The API runs at **https://localhost:5010**. Swagger UI is available at `https://localhost:5010/swagger`.

### 3. Run the Frontend

```bash
cd src/MakesCentsToMe.Web
npm install
ng serve
```

The Angular app runs at **http://localhost:4210**.

## Port Configuration

| Service    | URL                        |
|------------|----------------------------|
| API        | https://localhost:5010     |
| Angular    | http://localhost:4210      |
| PostgreSQL | localhost:5434             |

## Project Structure

```
MakesCentsToMe/
  src/
    MakesCentsToMe.Api/          # ASP.NET Core 10 Web API
      Features/                  # Vertical slice feature folders
        Accounts/                # Account CRUD
        Categories/              # Canonical spending categories
        Import/                  # CSV upload, column mapping, parsing, dedup
        Institutions/            # Institution CRUD
        Review/                  # Post-import review queue
      Infrastructure/
        Claude/                  # Claude API client for vendor/category analysis
        Data/                    # EF Core DbContext, migrations, configurations
      Models/
        Entities/                # EF Core entity classes
    MakesCentsToMe.Web/          # Angular 21 frontend
      src/app/
        features/                # Feature components
        services/                # API and theme services
  tests/
    MakesCentsToMe.Unit/         # Unit tests
    MakesCentsToMe.Integration/  # Integration tests
  docs/
    backlog.md                   # Product backlog
    c4/                          # PlantUML C4 architecture diagrams
  docker-compose.yml
```
