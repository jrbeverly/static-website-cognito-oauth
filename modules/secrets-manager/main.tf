# Secrets Manager — Automation Client Credentials
#
# Stores the Cognito automation app client ID and secret in AWS Secrets Manager so
# CI/CD pipelines and scripts can retrieve them at runtime without hardcoding
# credentials. Both values are sourced from the Cognito module outputs.

resource "aws_secretsmanager_secret" "automation_client_id" {
  name                    = "my-sites/${var.environment}/automation-client-id"
  description             = "Cognito automation app client ID for ${var.environment}"
  recovery_window_in_days = 0

  tags = merge(var.tags, {
    Environment = var.environment
    Project     = "my-sites"
    ManagedBy   = "terraform"
  })
}

resource "aws_secretsmanager_secret_version" "automation_client_id" {
  secret_id     = aws_secretsmanager_secret.automation_client_id.id
  secret_string = var.client_id
}

resource "aws_secretsmanager_secret" "automation_client_secret" {
  name                    = "my-sites/${var.environment}/automation-client-secret"
  description             = "Cognito automation app client secret for ${var.environment}"
  recovery_window_in_days = 0

  tags = merge(var.tags, {
    Environment = var.environment
    Project     = "my-sites"
    ManagedBy   = "terraform"
  })
}

resource "aws_secretsmanager_secret_version" "automation_client_secret" {
  secret_id     = aws_secretsmanager_secret.automation_client_secret.id
  secret_string = var.client_secret
}
