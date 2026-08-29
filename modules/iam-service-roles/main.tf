# IAM Service Roles
#
# IAM roles for the PublishingApi Lambda. All policies are scoped to
# specific resources — no wildcard actions. The publishing-api role
# explicitly denies dynamodb:Scan.

data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

locals {
  create_cicd_role = var.cicd_assume_role_policy != ""
}

# ==============================================================================
# Publishing API Lambda execution role
# ==============================================================================
resource "aws_iam_role" "publishing_api_lambda" {
  name = "publishing-api-lambda-${var.environment}"
  path = "/service-roles/"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })

  tags = merge(var.tags, {
    Component = "API"
  })
}

# CloudWatch Logs — scoped to the publishing-api log group
resource "aws_iam_role_policy" "publishing_api_logs" {
  name = "publishing-api-logs-${var.environment}"
  role = aws_iam_role.publishing_api_lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/publishing-api-${var.environment}:*"
      }
    ]
  })
}

# DynamoDB — scoped to the sites table, explicitly denies Scan
resource "aws_iam_role_policy" "publishing_api_dynamodb" {
  name = "publishing-api-dynamodb-${var.environment}"
  role = aws_iam_role.publishing_api_lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:Query",
          "dynamodb:PutItem",
          "dynamodb:UpdateItem",
          "dynamodb:DeleteItem"
        ]
        Resource = var.dynamodb_table_arn
      },
      {
        Effect   = "Deny"
        Action   = "dynamodb:Scan"
        Resource = var.dynamodb_table_arn
      }
    ]
  })
}

# S3 — scoped to the content bucket
resource "aws_iam_role_policy" "publishing_api_s3" {
  name = "publishing-api-s3-${var.environment}"
  role = aws_iam_role.publishing_api_lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Resource = [
          var.s3_bucket_arn,
          "${var.s3_bucket_arn}/*"
        ]
      }
    ]
  })
}

# CloudFront invalidation — scoped to distribution ARN when provided
resource "aws_iam_role_policy" "publishing_api_cloudfront" {
  name = "publishing-api-cloudfront-${var.environment}"
  role = aws_iam_role.publishing_api_lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = "cloudfront:CreateInvalidation"
        Resource = var.cloudfront_distribution_arn
      }
    ]
  })
}

# ==============================================================================
# CI/CD role — secrets retrieval for automation pipelines
# ==============================================================================
resource "aws_iam_role" "cicd" {
  count = local.create_cicd_role ? 1 : 0

  name = "my-sites-cicd-${var.environment}"
  path = "/service-roles/"

  assume_role_policy = var.cicd_assume_role_policy

  tags = merge(var.tags, {
    Component = "CICD"
  })
}

resource "aws_iam_role_policy" "cicd_secrets" {
  count = local.create_cicd_role ? 1 : 0

  name = "cicd-secrets-${var.environment}"
  role = aws_iam_role.cicd[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = "secretsmanager:GetSecretValue"
        Resource = var.secrets_manager_secret_arn
      }
    ]
  })

  lifecycle {
    precondition {
      condition     = var.secrets_manager_secret_arn != ""
      error_message = "secrets_manager_secret_arn must be provided when cicd_assume_role_policy is configured."
    }
  }
}

