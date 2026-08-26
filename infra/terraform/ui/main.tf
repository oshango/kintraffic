provider "azurerm" {
  features {}
}

resource "azurerm_static_site" "ui" {
  name                = "kintraffic-ui"
  resource_group_name = "kintraffic-rg"
  location            = "uksouth"
}
