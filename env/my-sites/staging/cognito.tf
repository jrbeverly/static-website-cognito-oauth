module "cognito" {
  source = "../../../modules/cognito"

  environment                    = "staging"
  pool_name                      = "my-sites-staging"
  api_resource_server_identifier = "sites"
  callback_urls                  = ["https://my-sites-staging.example.com/callback", "http://localhost:5173/callback"]
  logout_urls                    = ["https://my-sites-staging.example.com/", "http://localhost:5173/"]
  domain                         = "my-sites-staging"

  tags = {
    Environment = "staging"
    Project     = "my-sites"
    ManagedBy   = "terraform"
  }
}
