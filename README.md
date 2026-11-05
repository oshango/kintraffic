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
