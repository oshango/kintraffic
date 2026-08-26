provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "kintraffic-rg"
  location = "uksouth"
}

resource "azurerm_service_plan" "plan" {
  name                = "kintraffic-api-plan"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku_name            = "B1"
}

resource "azurerm_linux_web_app" "api" {
  name                = "kintraffic-api"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  service_plan_id     = azurerm_service_plan.plan.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }
}
