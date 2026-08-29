variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "dynamodb_table_arn" {
  description = "DynamoDB sites table ARN for scoped IAM policy"
  type        = string
}

variable "s3_bucket_arn" {
  description = "S3 content bucket ARN for scoped IAM policy"
  type        = string
}

variable "cloudfront_distribution_arn" {
  description = "CloudFront distribution ARN for invalidation permission"
  type        = string
}

variable "secrets_manager_secret_arn" {
  description = "Secrets Manager secret ARN for the CI/CD role to retrieve (leave empty to skip CI/CD role creation)"
  type        = string
  default     = ""
}

variable "cicd_assume_role_policy" {
  description = "Assume role policy for the my-sites-cicd role"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Common tags applied to all resources"
  type        = map(string)
  default     = {}
}
