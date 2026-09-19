output "api_default_hostname" {
  value = azurerm_container_app.api.ingress[0].fqdn
}

output "api_container_registry_name" {
  value = azurerm_container_registry.api.name
}

output "api_container_registry_login_server" {
  value = azurerm_container_registry.api.login_server
}
