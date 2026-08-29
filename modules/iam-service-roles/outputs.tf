output "lambda_role_arn" {
  description = "Publishing API Lambda execution role ARN"
  value       = aws_iam_role.publishing_api_lambda.arn
}

output "lambda_role_name" {
  description = "Publishing API Lambda execution role name"
  value       = aws_iam_role.publishing_api_lambda.name
}

output "cicd_role_arn" {
  description = "CI/CD role ARN (for Gitea Actions OIDC federation or static credentials)"
  value       = local.create_cicd_role ? aws_iam_role.cicd[0].arn : null
}

output "cicd_role_name" {
  description = "CI/CD role name"
  value       = local.create_cicd_role ? aws_iam_role.cicd[0].name : null
}
