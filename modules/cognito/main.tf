# Cognito User Pool
#
# Implements the Cognito user pool architecture.
# Staging uses the Cognito default domain and default email sender.
# Production swaps to a custom domain with ACM certificate and SES email sender.

data "aws_region" "current" {}

# ---------------------------------------------------------------------------
# User pool
# ---------------------------------------------------------------------------
resource "aws_cognito_user_pool" "main" {
  name = var.pool_name

  # Sign-in: username-based with email as alias
  alias_attributes = ["email"]

  # Self-registration disabled — admin-only user creation
  admin_create_user_config {
    allow_admin_create_user_only = true
  }

  # Password policy: min 12 chars, require uppercase, number, symbol
  password_policy {
    minimum_length                   = 12
    require_lowercase                = true
    require_numbers                  = true
    require_symbols                  = true
    require_uppercase                = true
    temporary_password_validity_days = 7
  }

  # Account recovery via verified email only
  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
  }

  # Email delivery: Cognito default for staging; swap to SES for prod
  email_configuration {
    email_sending_account = var.email_sending_account
    source_arn            = var.email_sending_account == "DEVELOPER" ? var.email_source_arn : null
    from_email_address    = var.email_sending_account == "DEVELOPER" ? var.email_from_address : null
  }

  # Auto-verify email to simplify admin-created user flow
  auto_verified_attributes = ["email"]

  # MFA: parameterised
  #   OFF      → no MFA available (not recommended)
  #   OPTIONAL → users may enrol TOTP (staging default)
  #   ON       → TOTP required for all users (future prod setting)
  mfa_configuration = var.mfa_configuration

  software_token_mfa_configuration {
    enabled = var.mfa_configuration != "OFF"
  }

  # Username: case-insensitive (ease of use)
  username_configuration {
    case_sensitive = false
  }

  # Schema: email as required attribute
  schema {
    name                = "email"
    attribute_data_type = "String"
    required            = true

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  tags = merge(var.tags, {
    Component = "Authentication"
  })
}

# ---------------------------------------------------------------------------
# Hosted UI domain
# ---------------------------------------------------------------------------
# Staging: Cognito default domain (domain prefix only, no ACM cert).
#   Example: my-sites-staging.auth.ca-central-1.amazoncognito.com
# Production: set certificate_arn to an ACM cert in us-east-1 for a custom domain.
#   Example: auth.my-sites.example.com
resource "aws_cognito_user_pool_domain" "main" {
  count = var.domain != "" ? 1 : 0

  domain          = var.domain
  certificate_arn = var.certificate_arn != "" ? var.certificate_arn : null
  user_pool_id    = aws_cognito_user_pool.main.id
}

# ---------------------------------------------------------------------------
# Resource server — OAuth 2.0 scopes for the Publishing API
# ---------------------------------------------------------------------------
resource "aws_cognito_resource_server" "api" {
  user_pool_id = aws_cognito_user_pool.main.id
  identifier   = var.api_resource_server_identifier
  name         = "My Sites API"

  scope {
    scope_name        = "read"
    scope_description = "Read site metadata and listings"
  }

  scope {
    scope_name        = "write"
    scope_description = "Create, publish, and delete sites"
  }
}

# ---------------------------------------------------------------------------
# App client — management SPA (public, Authorization Code + PKCE)
# ---------------------------------------------------------------------------
resource "aws_cognito_user_pool_client" "spa" {
  name         = "management-spa-${var.environment}"
  user_pool_id = aws_cognito_user_pool.main.id

  generate_secret                      = false
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows                  = ["code"]
  allowed_oauth_scopes = [
    "openid",
    "email",
    "profile",
    "${aws_cognito_resource_server.api.identifier}/read",
    "${aws_cognito_resource_server.api.identifier}/write"
  ]
  callback_urls = var.callback_urls
  logout_urls   = var.logout_urls

  access_token_validity  = 60
  id_token_validity      = 60
  refresh_token_validity = 43200

  token_validity_units {
    access_token  = "minutes"
    id_token      = "minutes"
    refresh_token = "minutes"
  }

  supported_identity_providers  = ["COGNITO"]
  prevent_user_existence_errors = "ENABLED"
  enable_token_revocation       = true
}

# ---------------------------------------------------------------------------
# App client — automation (confidential, Client Credentials)
# ---------------------------------------------------------------------------
resource "aws_cognito_user_pool_client" "automation" {
  name         = "automation-${var.environment}"
  user_pool_id = aws_cognito_user_pool.main.id

  generate_secret                      = true
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows                  = ["client_credentials"]
  allowed_oauth_scopes = [
    "${aws_cognito_resource_server.api.identifier}/read",
    "${aws_cognito_resource_server.api.identifier}/write"
  ]

  access_token_validity = 60
  token_validity_units {
    access_token = "minutes"
  }

  enable_token_revocation = true
}
