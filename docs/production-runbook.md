# KinTraffic Production Runbook

This runbook configures the GitHub `production` Environment and Azure access for `oshango/kintraffic`.

## 1. Create the GitHub Environment

In GitHub, open:

`Settings` -> `Environments` -> `New environment`

Create an environment named exactly `production`.

Configure these protection rules:

- Required reviewers: at least one trusted reviewer.
- Deployment branches: `Selected branches and tags`, containing only `main`.
- Optional wait timer: enable a short delay if you want a cancellation window.

Add these Environment variables:

| Name | Value |
| --- | --- |
| `PRODUCTION_API_URL` | `https://<api-default-hostname>` |
| `PRODUCTION_UI_URL` | `https://<ui-default-hostname>` |

Add these Environment secrets:

| Name | Purpose |
| --- | --- |
| `AZURE_CLIENT_ID` | Client ID of the API deployment app registration |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Static Web Apps deployment token |

The API workflow reads the first three secrets through Azure OIDC. The UI workflow currently uses the Static Web Apps token because `Azure/static-web-apps-deploy` does not use the same OIDC login path.

The workflow uses the non-secret registry name defined in Terraform: `kintrafficacr`.

## 2. Select the Azure Subscription

Run these commands locally from an elevated development account with permission to create resource groups, applications, role assignments, and storage resources. Do not put the values into source control.

```powershell
az login
az account list --output table
az account set --subscription "<subscription-id>"

$env:ARM_SUBSCRIPTION_ID = az account show --query id --output tsv
$env:ARM_TENANT_ID = az account show --query tenantId --output tsv
```

Record the subscription ID and tenant ID in the GitHub Environment secrets.

## 3. Create the API Deployment Identity

This identity is used only by the API deployment workflow.

```powershell
$apiAppClientId = az ad app create --display-name "kintraffic-github-api-deploy" --query appId --output tsv
$apiAppObjectId = az ad app show --id $apiAppClientId --query id --output tsv
az ad sp create --id $apiAppClientId | Out-Null
$apiPrincipalObjectId = az ad sp show --id $apiAppClientId --query id --output tsv

$federatedCredential = @'
{
  "name": "github-main-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:oshango/kintraffic:ref:refs/heads/main",
  "description": "KinTraffic production API deployment from main",
  "audiences": ["api://AzureADTokenExchange"]
}
'@

az ad app federated-credential create --id $apiAppObjectId --parameters $federatedCredential | Out-Null
```

After the API Web App exists, grant only the Web App scope:

```powershell
$apiResourceId = az webapp show --resource-group kintraffic-rg --name kintraffic-api --query id --output tsv
az role assignment create `
  --assignee-object-id $apiPrincipalObjectId `
  --assignee-principal-type ServicePrincipal `
  --role "Website Contributor" `
  --scope $apiResourceId
```

Set `AZURE_CLIENT_ID` to `$apiAppClientId`.

## 4. Bootstrap Remote Terraform State

Use a separate Terraform identity for infrastructure administration. The bootstrap itself is intentionally run once with your local Azure identity because it creates the state store.

Create the Terraform app registration and service principal using the same pattern as the API identity, but name it `kintraffic-github-terraform`. Create a federated credential for the `main` branch and record its service-principal object ID.

Then run:

```powershell
terraform -chdir=infra/terraform/bootstrap init
terraform -chdir=infra/terraform/bootstrap apply `
  -var="state_storage_account_name=<globally-unique-lowercase-name>" `
  -var="terraform_principal_object_id=<terraform-service-principal-object-id>" `
  -var="bootstrap_principal_object_id=<local-user-object-id>"
```

The state storage account has private access, TLS 1.2, blob versioning, and a private state container. Granting `Storage Blob Data Contributor` to the Terraform principal allows remote state access without storage keys.

Initialize the API and UI states using separate keys:

```powershell
terraform -chdir=infra/terraform/api init `
  -backend-config="resource_group_name=kintraffic-tfstate-rg" `
  -backend-config="storage_account_name=<state-storage-account>" `
  -backend-config="container_name=tfstate" `
  -backend-config="key=api.tfstate" `
  -backend-config="use_azuread_auth=true" `
  -backend-config="use_cli=true"

terraform -chdir=infra/terraform/ui init `
  -backend-config="resource_group_name=kintraffic-tfstate-rg" `
  -backend-config="storage_account_name=<state-storage-account>" `
  -backend-config="container_name=tfstate" `
  -backend-config="key=ui.tfstate" `
  -backend-config="use_azuread_auth=true" `
  -backend-config="use_cli=true"
```

For GitHub Actions, use `use_oidc=true` instead of `use_cli=true` and provide the Terraform identity's OIDC environment variables.

## 5. Apply Application Infrastructure

Create the application resources from your local authenticated session first. Pass the API deployment principal object ID so Terraform can create the scoped role assignment:

```powershell
terraform -chdir=infra/terraform/api plan `
  -var="deployment_principal_object_id=$apiPrincipalObjectId"

terraform -chdir=infra/terraform/api apply `
  -var="deployment_principal_object_id=$apiPrincipalObjectId"

terraform -chdir=infra/terraform/api output api_default_hostname
```

After the resource group exists, grant the Terraform service principal access for future plans and applies:

```powershell
az role assignment create `
  --assignee-object-id <terraform-service-principal-object-id> `
  --assignee-principal-type ServicePrincipal `
  --role Contributor `
  --scope "/subscriptions/$env:ARM_SUBSCRIPTION_ID/resourceGroups/kintraffic-rg"
```

The API stack now creates an Azure Container Apps Consumption environment, a Basic Azure Container Registry, and a user-assigned runtime identity. The initial Terraform image is a public .NET runtime image; the GitHub workflow replaces it with the tested image from the registry by immutable digest.

The API deployment principal requires `Container Apps Contributor` on the Container App and `AcrPush` on the registry. The runtime identity requires `AcrPull` on the registry. These assignments are managed by the API Terraform stack.

Set `PRODUCTION_API_URL` to `https://` followed by the Container App hostname.

The Basic Azure Container Registry is a paid resource. Review the pricing estimate and budget alert before applying the API stack.

Create the Static Web App and configure its deployment token in the GitHub Environment. The current subscription-eligible deployment region is `eastus2`; `uksouth` and `westeurope` were rejected for this subscription. Set `PRODUCTION_UI_URL` to the Static Web App hostname shown by Azure.

Retrieve the deployment token without printing it in shared logs:

```powershell
terraform -chdir=infra/terraform/ui output -raw ui_api_key | Set-Clipboard
```

Paste the clipboard contents into the GitHub `AZURE_STATIC_WEB_APPS_API_TOKEN` Environment secret.

## 6. First Deployment and Verification

Push the branch through a pull request into `main`. The sequence should be:

1. API and UI tests/builds pass.
2. Artifacts are produced once and downloaded by deployment jobs.
3. GitHub pauses at the protected `production` Environment.
4. An approved deployment authenticates to Azure using OIDC for the API.
5. The API health check verifies `/api/Health`.
6. The UI health check verifies the configured production URL.

Do not bypass the environment approval for the first deployment. Confirm the API and UI URLs manually after both health checks pass.

## Security Rules

- Never commit subscription IDs, tenant IDs, client secrets, publish profiles, storage keys, or state files.
- Keep Terraform administration and application deployment identities separate.
- Restrict federated credentials to this repository and the `main` branch.
- Keep production deployment behind the GitHub Environment approval.
- Use separate Terraform state keys for each stack.

## Current Provisioning Blocker

The free subscription currently has an App Service B1 VM quota of `0` in the selected region. Request an App Service quota increase to at least `1` for B1 VMs in the Azure portal before applying `infra/terraform/api` again. The resource group exists, but the App Service plan, API Web App, and API deployment role are not yet created.