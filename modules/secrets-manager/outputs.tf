output "automation_client_id_arn" {
  description = "ARN of the automation client ID in Secrets Manager"
  value       = aws_secretsmanager_secret.automation_client_id.arn
}

output "automation_client_id_name" {
  description = "Friendly name/path of the automation client ID"
  value       = aws_secretsmanager_secret.automation_client_id.name
}

output "automation_client_secret_arn" {
  description = "ARN of the automation client secret in Secrets Manager"
  value       = aws_secretsmanager_secret.automation_client_secret.arn
}

output "automation_client_secret_name" {
  description = "Friendly name/path of the automation client secret"
  value       = aws_secretsmanager_secret.automation_client_secret.name
}
