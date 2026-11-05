variable "deployment_principal_object_id" {
  description = "Object ID of the GitHub deployment service principal. Leave null until Azure identity setup is complete."
  type        = string
  default     = null
  nullable    = true
}
