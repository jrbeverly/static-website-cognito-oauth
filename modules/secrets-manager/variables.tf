variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "client_id" {
  description = "Cognito automation app client ID (from cognito module output)"
  type        = string
}

variable "client_secret" {
  description = "Cognito automation app client secret (sensitive — from cognito module output)"
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Common tags applied to all resources"
  type        = map(string)
  default     = {}
}
