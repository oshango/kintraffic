variable "location" {
  description = "Azure region for Terraform state resources."
  type        = string
  default     = "uksouth"
}

variable "state_resource_group_name" {
  description = "Resource group containing Terraform state."
  type        = string
  default     = "kintraffic-tfstate-rg"
}

variable "state_storage_account_name" {
  description = "Globally unique lowercase storage account name for Terraform state."
  type        = string
}

variable "terraform_principal_object_id" {
  description = "Object ID of the GitHub Terraform service principal."
  type        = string
}

variable "bootstrap_principal_object_id" {
  description = "Object ID of the local identity running the bootstrap. Used only for state storage data-plane access."
  type        = string
}