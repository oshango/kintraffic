output "ui_default_hostname" {
  value = azurerm_static_web_app.ui.default_host_name
}

output "ui_api_key" {
  value     = azurerm_static_web_app.ui.api_key
  sensitive = true
}
