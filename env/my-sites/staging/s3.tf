module "s3_content" {
  source = "../../../modules/s3-content"

  environment        = "staging"
  bucket_name_prefix = "my-sites-content"
  force_destroy      = true

  cloudfront_distribution_arn = module.cloudfront_serving.distribution_arn
}
