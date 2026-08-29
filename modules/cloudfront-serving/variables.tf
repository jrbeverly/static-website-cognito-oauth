variable "environment" {
  description = "Deployment environment (staging, prod)"
  type        = string
}

variable "s3_bucket_regional_domain" {
  description = "S3 bucket regional domain name for CloudFront origin configuration"
  type        = string
}

variable "aliases" {
  description = "Custom domain aliases for the CloudFront distribution (requires ACM cert in us-east-1)"
  type        = list(string)
  default     = []
}

variable "acm_cert_arn" {
  description = "ACM certificate ARN in us-east-1 for custom domain (null = use default CloudFront cert)"
  type        = string
  default     = null
}

variable "price_class" {
  description = "CloudFront distribution price class"
  type        = string
  default     = "PriceClass_100"
}

variable "tags" {
  description = "Common tags applied to all resources"
  type        = map(string)
  default     = {}
}
