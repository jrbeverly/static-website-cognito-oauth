output "bucket_id" {
  description = "S3 bucket ID (name) for the content bucket"
  value       = aws_s3_bucket.content.id
}

output "bucket_arn" {
  description = "S3 bucket ARN for IAM policy attachment"
  value       = aws_s3_bucket.content.arn
}

output "bucket_regional_domain_name" {
  description = "S3 bucket regional domain name for CloudFront origin configuration"
  value       = aws_s3_bucket.content.bucket_regional_domain_name
}
