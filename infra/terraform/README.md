# Terraform Setup

## Bootstrap State Storage

The bootstrap stack is intentionally local-state only because it creates the remote state store used by the other stacks. Run it once from an authenticated Azure session:

```powershell
az login
$env:ARM_SUBSCRIPTION_ID = "<subscription-id>"
terraform -chdir=infra/terraform/bootstrap init
terraform -chdir=infra/terraform/bootstrap apply `
  -var="state_storage_account_name=<globally-unique-name>" `
  -var="terraform_principal_object_id=<service-principal-object-id>" `
  -var="bootstrap_principal_object_id=<local-user-object-id>"
```

The service principal must have permission to create the bootstrap resources. After bootstrap, initialize each application stack with the generated storage account and its own state key:

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

Do not commit subscription IDs, object IDs, storage account names, or credentials.

## GitHub OIDC Roles

Use separate Entra service principals where possible:

- Terraform principal: `Contributor` on the application resource group and `Storage Blob Data Contributor` on the state container.
- API deployment principal: `Container Apps Contributor` scoped to the API Container App and `AcrPush` scoped to the API registry.
- API runtime identity: `AcrPull` scoped to the API registry.
- UI deployment principal: `Static Web App Contributor` scoped to the Static Web App.

Create federated credentials restricted to this repository and the `main` branch. Set the deployment principal object ID through Terraform's `deployment_principal_object_id` variable.

After applying the application stacks, set these variables on the GitHub `production` Environment using the output hostnames:

- `PRODUCTION_API_URL`: `https://<api_default_hostname>`
- `PRODUCTION_UI_URL`: `https://<ui_default_hostname>` (the current deployment uses `eastus2` because `uksouth` and `westeurope` were not eligible for this subscription.)
- `AZURE_STATIC_WEB_APPS_API_TOKEN`: the sensitive `ui_api_key` output from the UI stack.
