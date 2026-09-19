provider "azurerm" {
  features {}
  storage_use_azuread = true
}

resource "azurerm_resource_group" "state" {
  name     = var.state_resource_group_name
  location = var.location
}

resource "azurerm_storage_account" "state" {
  name                            = var.state_storage_account_name
  resource_group_name             = azurerm_resource_group.state.name
  location                        = azurerm_resource_group.state.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = false
  blob_properties {
    versioning_enabled = true
  }
}

resource "azurerm_storage_container" "state" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.state.id
  container_access_type = "private"
}

resource "azurerm_role_assignment" "terraform_state" {
  scope                = azurerm_storage_container.state.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.terraform_principal_object_id
}

resource "azurerm_role_assignment" "bootstrap_state" {
  scope                = azurerm_storage_account.state.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.bootstrap_principal_object_id
}