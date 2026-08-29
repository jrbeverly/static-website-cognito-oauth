# Terraform Modules

Reusable, composable Terraform modules for the My Sites platform.

## Modules

| Module | Purpose |
|--------|---------|
| `cognito/` | Cognito user pool, two app clients (SPA + automation), resource server with OAuth scopes, hosted UI domain |
| `s3-content/` | S3 bucket for published site content with lifecycle rules and block-public-access enforcement |
| `dynamodb-sites/` | DynamoDB table for site metadata (on-demand billing, single-table design) |
| `publishing-api/` | Lambda function (arm64/dotnet8) + API Gateway HTTP API with Cognito JWT authorizer |
| `management-spa/` | S3 bucket for the management SPA + CloudFront distribution with S3 origin |
| `iam-service-roles/` | IAM roles for Lambda execution, CI/CD deployment, and cross-service access |
| `cloudfront-serving/` | CloudFront distribution with S3 OAC, default-index rewrite function, and custom error responses |
| `billing-alerts/` | CloudWatch billing alarm at $10/month threshold + SNS topic for notifications |
| `secrets-manager/` | AWS Secrets Manager entries for automation client ID and secret |

## Usage

Each module is referenced by environment configs in `env/my-sites/`. Example:

```hcl
module "cognito" {
  source = "../../../modules/cognito"

  environment = "staging"
  pool_name   = "my-sites-staging"
  # ...
}
```

Module interfaces are defined via `variables.tf` (inputs) and `outputs.tf` (outputs).
