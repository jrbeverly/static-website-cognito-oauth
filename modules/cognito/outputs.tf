output "user_pool_id" {
  description = "Cognito User Pool ID"
  value       = aws_cognito_user_pool.main.id
}

output "user_pool_arn" {
  description = "Cognito User Pool ARN"
  value       = aws_cognito_user_pool.main.arn
}

output "user_pool_endpoint" {
  description = "Cognito User Pool endpoint URL"
  value       = aws_cognito_user_pool.main.endpoint
}

output "user_pool_domain" {
  description = "Hosted UI domain (Cognito default or custom)"
  value       = var.domain != "" ? "${aws_cognito_user_pool_domain.main[0].domain}.auth.${data.aws_region.current.name}.amazoncognito.com" : null
}

output "spa_client_id" {
  description = "Management SPA app client ID"
  value       = aws_cognito_user_pool_client.spa.id
}

output "automation_client_id" {
  description = "Automation (machine-to-machine) app client ID"
  value       = aws_cognito_user_pool_client.automation.id
}

output "automation_client_secret" {
  description = "Automation app client secret (sensitive — use with Secrets Manager, never expose in plaintext)"
  value       = aws_cognito_user_pool_client.automation.client_secret
  sensitive   = true
}

output "oauth_domain" {
  description = "Cognito hosted UI domain for OAuth 2.0 flows"
  value       = var.domain != "" ? "${aws_cognito_user_pool_domain.main[0].domain}.auth.${data.aws_region.current.name}.amazoncognito.com" : null
}

output "token_endpoint" {
  description = "OAuth 2.0 token endpoint URL"
  value       = var.domain != "" ? "https://${aws_cognito_user_pool_domain.main[0].domain}.auth.${data.aws_region.current.name}.amazoncognito.com/oauth2/token" : null
}

output "jwks_uri" {
  description = "JWKS URI for the user pool's public signing keys"
  value       = "https://cognito-idp.${data.aws_region.current.name}.amazonaws.com/${aws_cognito_user_pool.main.id}/.well-known/jwks.json"
}
