# kintraffic
Traffic Infrastructure Management Platform for monitoring, maintenance and operations of traffic signal networks.

## Technology

- ASP.NET Core .NET 10
- Angular
- SQL Server / Azure SQL
- SignalR
- Azure Container Apps
- Azure Static Web Apps
- Azure Container Registry
- GitHub Actions

## Modules

- Authentication
- Junction Management
- Device Management
- Telemetry
- Fault Management
- Work Orders
- Technicians
- Inventory
- Contracts
- Invoices
- Reporting

## Persistence

The API requires the `ConnectionStrings:KinTraffic` SQL Server connection string; there is no in-memory fallback. Docker Compose supplies it from the git-ignored root `.env` file. When running the API outside Compose, set `ConnectionStrings__KinTraffic` through a local environment variable or user-secrets store; do not commit credentials.

EF Core migrations in `api/KinTraffic.Api/Migrations` are the executable schema source and are applied when the API starts. `infra/database/schema.sql` is a reference copy only and must not be executed against a migration-managed database. Operational IDs remain strings to preserve existing API identifiers; RoadSegment and TrafficState IDs remain integers. SQL `datetime2` defaults use UTC. Only the pre-existing TrafficSignal sample rows are migration-seeded; other tables start empty.

## Local Docker Development

Run the API, Angular UI, and SQL Server database from the repository root:

```powershell
docker compose up --build
```

The Docker UI is a production Angular build served by Nginx at `http://localhost:4200`; Nginx forwards `/api` requests to the API service. The API is available at `http://localhost:8080`, and SQL Server at `localhost:1433`. SQL Server is capped at 2GB for local Docker resource usage and stores data in the `kintraffic-sql-data-v3` Docker volume. The API applies EF Core migrations when it starts; traffic signal sample rows are inserted by the initial migration. For source hot reload, run the Angular development server separately at `http://localhost:4300`.

For stable local development, allocate at least 2 CPUs and 4GB memory to Docker Desktop. If the optional kind cluster is also running and Docker is resource-constrained, pause its container before starting the app with `docker stop kintraffic-control-plane`; resume it later with `docker start kintraffic-control-plane`. This pauses Kubernetes without deleting the cluster. Compose services use restart policies and bounded memory; SQL startup has a 90-second health-check grace period.

Compose requires `MSSQL_SA_PASSWORD`; there is no password default. The fresh-database launcher creates a strong local password in the git-ignored root `.env` file if one is not already set there. To set your own password instead, put `MSSQL_SA_PASSWORD=<your-strong-local-password>` in that file before starting Compose:

```powershell
$env:MSSQL_SA_PASSWORD = "<your-strong-local-password>"
docker compose up --build
```

Never use your local password or the SQL Server Developer edition in a production deployment. Tables other than TrafficSignals start empty; current read-only list endpoints return empty collections until approved data imports or later write workflows populate them.

If the existing Docker database volume has an unknown `sa` password, run `scripts/start-fresh-local-db.ps1 -RotatePassword` from PowerShell. It rotates the exposed local password in the git-ignored root `.env` file, checks that Compose resolves the same password, stops and recreates the Compose containers, and waits for SQL Server health on the new `kintraffic-sql-data-v3` volume. It does not delete the previous `kintraffic_kintraffic-sql-data` or `kintraffic_kintraffic-sql-data-v2` volumes. They remain available for recovery; do not run `docker compose down -v` if you need to preserve them. For SSMS, connect to the Docker database at `localhost,1433` with SQL Server Authentication and the password in `.env`. `(localdb)\\MSSQLLocalDB` is a separate Windows LocalDB instance, not the Docker database.

### Connect with SQL Server Management Studio

With `docker compose up --build` running, create a new connection using:

| Setting | Value |
| --- | --- |
| Server type | Database Engine |
| Server name | `localhost,1433` |
| Authentication | SQL Server Authentication |
| Login | `sa` |
| Password | The value of `MSSQL_SA_PASSWORD` in the root `.env` file |
| Database | `KinTraffic` |
| Encrypt | Mandatory |
| Trust server certificate | Checked |

In SSMS, the server can also be entered as `127.0.0.1,1433`. If the connection dialog only offers Windows Authentication, change Authentication to SQL Server Authentication. The database is created and migrated automatically when the API starts. Do not use the local development password outside your machine.

## Commit Naming Convention

We follow a structured commit naming format:

### Format
<type>(scope): short description

### Types
- feat: new feature
- fix: bug fix
- docs: documentation changes
- style: formatting, no code changes
- refactor: code restructuring
- test: adding or updating tests
- chore: maintenance tasks

### Examples
feat(api): add junctions controller  
fix(database): correct foreign key for devices  
refactor(frontend): simplify dashboard layout  

## PR Title Format
Use the following format for all pull request titles:

<type>: OSH-<number>: <description>

### Examples
feat: OSH-12: implement junctions controller and DTOs  
fix: OSH-34: correct foreign key constraint  

## Azure Deployment Setup

The GitHub deployment workflows build and test artifacts before deployment. Production deployment is protected by the `production` GitHub Environment.

Before the first Azure deployment:

1. Create the `production` environment in repository settings and require an approval reviewer.
2. Create an Entra application or service principal for GitHub Actions with only the required resource-group deployment permissions.
3. Configure GitHub Actions secrets named `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` for OIDC login.
4. Configure the `AZURE_STATIC_WEB_APPS_API_TOKEN` secret for the Static Web App deployment.
5. Add an Azure federated credential restricted to this repository and the `main` branch.
6. Configure remote, locked Terraform state before applying infrastructure. Do not use local state for a shared environment.

The API runs as a non-root .NET 10 container in Azure Container Apps Consumption with scale-to-zero enabled.
