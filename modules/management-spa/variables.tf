variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "bucket_name_prefix" {
  description = "Prefix for the S3 bucket name (e.g. 'my-sites-management-spa')"
  type        = string
}

variable "force_destroy" {
  description = "Allow terraform destroy to delete non-empty bucket (true for staging, false for prod)"
  type        = bool
  default     = false
}

variable "cloudfront_price_class" {
  description = "CloudFront distribution price class"
  type        = string
  default     = "PriceClass_100"
}

variable "tags" {
  description = "Common tags applied to all resources"
  type        = map(string)
  default     = {}
}
