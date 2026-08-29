variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "pool_name" {
  description = "Cognito user pool name"
  type        = string
}

variable "callback_urls" {
  description = "Allowed callback URLs for the hosted UI"
  type        = list(string)
}

variable "logout_urls" {
  description = "Allowed logout URLs for the hosted UI"
  type        = list(string)
}

variable "domain" {
  description = "Cognito domain prefix for the hosted UI (leave empty to skip domain creation)"
  type        = string
  default     = ""
}

variable "certificate_arn" {
  description = "ACM certificate ARN for custom domain (required for production custom domains in us-east-1)"
  type        = string
  default     = ""
}

variable "mfa_configuration" {
  description = "MFA configuration for the user pool (OFF, OPTIONAL, or ON)"
  type        = string
  default     = "OPTIONAL"

  validation {
    condition     = contains(["OFF", "OPTIONAL", "ON"], var.mfa_configuration)
    error_message = "MFA configuration must be OFF, OPTIONAL, or ON."
  }
}

variable "email_sending_account" {
  description = "Email sending account type (COGNITO_DEFAULT or DEVELOPER for SES)"
  type        = string
  default     = "COGNITO_DEFAULT"

  validation {
    condition     = contains(["COGNITO_DEFAULT", "DEVELOPER"], var.email_sending_account)
    error_message = "Email sending account must be COGNITO_DEFAULT or DEVELOPER."
  }
}

variable "email_source_arn" {
  description = "SES identity ARN for email sending (required when email_sending_account is DEVELOPER)"
  type        = string
  default     = ""
}

variable "email_from_address" {
  description = "From email address for email sending (used when email_sending_account is DEVELOPER)"
  type        = string
  default     = ""
}

variable "api_resource_server_identifier" {
  description = "OAuth 2.0 resource server identifier (audience URL for the Publishing API)"
  type        = string
}

variable "log_retention_days" {
  description = "CloudWatch log group retention in days for Cognito advanced security logs"
  type        = number
  default     = 7
}

variable "tags" {
  description = "Common tags applied to all resources"
  type        = map(string)
  default     = {}
}
