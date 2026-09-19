provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "kintraffic-rg"
  location = "uksouth"
}

resource "azurerm_log_analytics_workspace" "api" {
  name                = "kintraffic-api-logs"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_registry" "api" {
  name                = "kintrafficacr"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = false
}

resource "azurerm_user_assigned_identity" "api" {
  name                = "kintraffic-api-identity"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_container_app_environment" "api" {
  name                       = "kintraffic-api-env"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.api.id
}

resource "azurerm_container_app" "api" {
  name                         = "kintraffic-api"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.api.id
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  registry {
    server   = azurerm_container_registry.api.login_server
    identity = azurerm_user_assigned_identity.api.id
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "kintraffic-api"
      image  = "mcr.microsoft.com/dotnet/aspnet:10.0"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080"
      }

      env {
        name  = "Cors__AllowedOrigins__0"
        value = "https://salmon-bush-0eb5c280f.6.azurestaticapps.net"
      }

      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/api/Health"
      }

      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/api/Health"
      }
    }
  }

  lifecycle {
    ignore_changes = [template[0].container[0].image]
  }
}

resource "azurerm_role_assignment" "deployment" {
  count                = var.deployment_principal_object_id == null ? 0 : 1
  scope                = azurerm_container_app.api.id
  role_definition_name = "Container Apps Contributor"
  principal_id         = var.deployment_principal_object_id
}

resource "azurerm_role_assignment" "registry_push" {
  count                = var.deployment_principal_object_id == null ? 0 : 1
  scope                = azurerm_container_registry.api.id
  role_definition_name = "AcrPush"
  principal_id         = var.deployment_principal_object_id
}

resource "azurerm_role_assignment" "registry_pull" {
  scope                = azurerm_container_registry.api.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.api.principal_id
}
