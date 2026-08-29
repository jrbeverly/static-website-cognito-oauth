output "bucket_id" {
  description = "S3 bucket ID (name) for the SPA static assets"
  value       = aws_s3_bucket.spa.id
}

output "bucket_arn" {
  description = "S3 bucket ARN for IAM policy attachment"
  value       = aws_s3_bucket.spa.arn
}

output "bucket_regional_domain_name" {
  description = "S3 bucket regional domain name for CloudFront origin configuration"
  value       = aws_s3_bucket.spa.bucket_regional_domain_name
}

output "cloudfront_domain_name" {
  description = "CloudFront distribution domain name"
  value       = aws_cloudfront_distribution.spa.domain_name
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID for invalidation references"
  value       = aws_cloudfront_distribution.spa.id
}

output "cloudfront_arn" {
  description = "CloudFront distribution ARN"
  value       = aws_cloudfront_distribution.spa.arn
}
