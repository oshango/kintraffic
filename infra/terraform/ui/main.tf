provider "azurerm" {
  features {}
}

resource "azurerm_static_web_app" "ui" {
  name                = "kintraffic-ui"
  resource_group_name = "kintraffic-rg"
  location            = var.location
}

resource "azurerm_role_assignment" "deployment" {
  count                = var.deployment_principal_object_id == null ? 0 : 1
  scope                = azurerm_static_web_app.ui.id
  role_definition_name = "Static Web App Contributor"
  principal_id         = var.deployment_principal_object_id
}
