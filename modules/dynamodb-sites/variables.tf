variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "table_name_prefix" {
  description = "Prefix for the DynamoDB table name (e.g. 'my-sites')"
  type        = string
}

variable "point_in_time_recovery" {
  description = "Enable point-in-time recovery (adds cost; enable for production only)"
  type        = bool
  default     = false
}
