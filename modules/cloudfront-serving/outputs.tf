output "distribution_id" {
  description = "CloudFront distribution ID for invalidation references (used by the publishing API)"
  value       = aws_cloudfront_distribution.content.id
}

output "distribution_domain_name" {
  description = "CloudFront distribution domain name"
  value       = aws_cloudfront_distribution.content.domain_name
}

output "distribution_arn" {
  description = "CloudFront distribution ARN"
  value       = aws_cloudfront_distribution.content.arn
}
