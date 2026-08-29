module "cloudfront_serving" {
  source = "../../../modules/cloudfront-serving"

  environment               = "staging"
  s3_bucket_regional_domain = module.s3_content.bucket_regional_domain_name
  acm_cert_arn              = null # Default CloudFront certificate for staging

  tags = {
    Environment = "staging"
    Project     = "my-sites"
    ManagedBy   = "terraform"
  }
}
