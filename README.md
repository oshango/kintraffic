# kintraffic
Traffic Infrastructure Management Platform for monitoring, maintenance and operations of traffic signal networks.

## Technology

- API: ASP.NET Core on .NET 10
- UI: Angular 22, TypeScript, and Node.js 22
- Data access: Entity Framework Core 10 migrations
- Local database: SQL Server 2022 Developer in Docker Compose
- Production database target: Azure SQL
- Local Kubernetes testing: Kubernetes manifests with kind
- Realtime: SignalR (planned; not currently wired into the UI/API)
- Azure deployment: Azure Container Apps, Azure Static Web Apps, and Azure Container Registry
- CI/CD: GitHub Actions

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

## Local Docker Development

Run the API, Angular UI, and SQL Server database from the repository root:

```powershell
docker compose up --build
```

The UI is available at `http://localhost:4200`, the API at `http://localhost:8080`, and SQL Server at `localhost:1433`. The SQL Server Developer container stores its data in the `kintraffic-sql-data` Docker volume. The API applies EF Core migrations when it starts; traffic signal sample rows are inserted by the initial migration.

Compose requires `MSSQL_SA_PASSWORD`; there is no password default. Set it in PowerShell before starting Compose, or put it in a root `.env` file that must not be committed. For example, replace the placeholder below with your own strong local password:

```powershell
$env:MSSQL_SA_PASSWORD = "<your-strong-local-password>"
docker compose up --build
```

Never use your local password or the SQL Server Developer edition in a production deployment. At this stage, only traffic signals are persisted; the other API repositories still use in-memory data.

### Connect with SQL Server Management Studio

With `docker compose up --build` running, create a new connection using:

| Setting | Value |
| --- | --- |
| Server type | Database Engine |
| Server name | `xxxxxx` |
| Authentication | SQL Server Authentication |
| Login | `sa` |
| Password | The value you set for `MSSQL_SA_PASSWORD` |
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
