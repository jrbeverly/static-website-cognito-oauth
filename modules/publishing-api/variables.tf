variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "lambda_role_arn" {
  description = "IAM role ARN for the Lambda execution role"
  type        = string
}

variable "lambda_zip_path" {
  description = "Path to the published PublishingApi Lambda ZIP"
  type        = string
}

variable "dynamodb_table" {
  description = "DynamoDB table name for site metadata"
  type        = string
  default     = ""
}

variable "s3_bucket" {
  description = "S3 bucket name for static site content"
  type        = string
  default     = ""
}

variable "cloudfront_domain" {
  description = "CloudFront distribution domain name for content serving"
  type        = string
  default     = ""
}

variable "cloudfront_distribution_id" {
  description = "CloudFront distribution ID for cache invalidation"
  type        = string
  default     = ""
}

variable "lambda_concurrency_limit" {
  description = "Reserved concurrency for the publishing-api Lambda (prevents noisy-neighbour and runaway spend)"
  type        = number
  default     = 10
}

variable "cognito_user_pool_id" {
  description = "Cognito User Pool ID for JWT validation and authorizer"
  type        = string
  default     = ""
}

variable "cognito_client_id" {
  description = "Cognito app client ID used as the token audience in Lambda"
  type        = string
  default     = ""
}

variable "cognito_spa_client_id" {
  description = "Cognito SPA app client ID for API Gateway JWT authorizer audience"
  type        = string
  default     = ""
}

variable "cognito_automation_client_id" {
  description = "Cognito automation app client ID for API Gateway JWT authorizer audience"
  type        = string
  default     = ""
}

variable "log_retention_days" {
  description = "CloudWatch log group retention in days"
  type        = number
  default     = 7
}

variable "tags" {
  description = "Common tags applied to all resources"
  type        = map(string)
  default     = {}
}
