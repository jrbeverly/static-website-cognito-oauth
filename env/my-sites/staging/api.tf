module "publishing_api" {
  source = "../../../modules/publishing-api"

  environment                  = "staging"
  lambda_role_arn              = module.iam.lambda_role_arn
  lambda_zip_path              = "${path.module}/../../../build/publishing-api.zip"
  lambda_concurrency_limit     = -1
  dynamodb_table               = module.dynamodb_sites.table_name
  s3_bucket                    = module.s3_content.bucket_id
  cloudfront_domain            = module.cloudfront_serving.distribution_domain_name
  cloudfront_distribution_id   = module.cloudfront_serving.distribution_id
  cognito_user_pool_id         = module.cognito.user_pool_id
  cognito_spa_client_id        = module.cognito.spa_client_id
  cognito_automation_client_id = module.cognito.automation_client_id

  tags = {
    Environment = "staging"
    Project     = "my-sites"
    ManagedBy   = "terraform"
  }
}

output "api_endpoint" {
  value = module.publishing_api.api_endpoint
}

output "cognito_domain" {
  value = module.cognito.oauth_domain
}

output "automation_client_id" {
  value = module.cognito.automation_client_id
}

output "automation_client_secret" {
  value     = module.cognito.automation_client_secret
  sensitive = true
}

output "content_bucket" {
  value = module.s3_content.bucket_id
}

output "distribution_domain" {
  value = module.cloudfront_serving.distribution_domain_name
}
