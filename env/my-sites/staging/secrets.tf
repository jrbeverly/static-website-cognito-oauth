module "secrets" {
  source = "../../../modules/secrets-manager"

  environment   = "staging"
  client_id     = module.cognito.automation_client_id
  client_secret = module.cognito.automation_client_secret

  tags = {
    Environment = "staging"
    Project     = "my-sites"
    ManagedBy   = "terraform"
  }
}
