module "iam" {
  source = "../../../modules/iam-service-roles"

  environment                 = "staging"
  dynamodb_table_arn          = module.dynamodb_sites.table_arn
  s3_bucket_arn               = module.s3_content.bucket_arn
  secrets_manager_secret_arn  = module.secrets.automation_client_secret_arn
  cloudfront_distribution_arn = module.cloudfront_serving.distribution_arn

  tags = {
    Environment = "staging"
    Project     = "my-sites"
    ManagedBy   = "terraform"
  }
}
