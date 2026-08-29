output "table_name" {
  description = "DynamoDB table name for site metadata"
  value       = aws_dynamodb_table.sites.name
}

output "table_arn" {
  description = "DynamoDB table ARN for IAM policy attachment"
  value       = aws_dynamodb_table.sites.arn
}
