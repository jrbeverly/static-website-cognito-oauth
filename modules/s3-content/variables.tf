variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "bucket_name_prefix" {
  description = "Prefix for the S3 bucket name (e.g. 'my-sites-content')"
  type        = string
}

variable "force_destroy" {
  description = "Allow terraform destroy to delete non-empty bucket (true for staging, false for prod)"
  type        = bool
  default     = false
}

variable "cloudfront_distribution_arn" {
  description = "CloudFront distribution ARN for OAC bucket policy"
  type        = string
}
