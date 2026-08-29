# PublishingApi Lambda + API Gateway HTTP API
#
# Deploys a .NET 8 minimal API as a Lambda function behind API Gateway HTTP API (v2).
# Uses the managed dotnet8 runtime — no container image or self-contained binary needed.
# Amazon.Lambda.AspNetCoreServer.Hosting bridges API Gateway requests into the ASP.NET Core pipeline.

data "aws_region" "current" {}

# ---------------------------------------------------------------------------
# Lambda function
# ---------------------------------------------------------------------------
resource "aws_lambda_function" "api" {
  function_name = "publishing-api-${var.environment}"
  description   = "PublishingApi Lambda — ${var.environment}"
  role          = var.lambda_role_arn
  handler       = "PublishingApi.Api"
  runtime       = "dotnet8"
  architectures = ["arm64"]
  memory_size   = 512
  timeout       = 30

  reserved_concurrent_executions = var.lambda_concurrency_limit

  filename         = var.lambda_zip_path
  source_code_hash = filebase64sha256(var.lambda_zip_path)

  environment {
    variables = {
      ENVIRONMENT                = var.environment
      DYNAMODB_TABLE             = var.dynamodb_table
      S3_BUCKET                  = var.s3_bucket
      CLOUDFRONT_DOMAIN          = var.cloudfront_domain
      CLOUDFRONT_DISTRIBUTION_ID = var.cloudfront_distribution_id
      COGNITO_USER_POOL_ID       = var.cognito_user_pool_id
      COGNITO_REGION             = data.aws_region.current.name
      COGNITO_CLIENT_ID          = var.cognito_client_id
    }
  }

  tags = merge(var.tags, {
    Component = "API"
  })
}

resource "aws_cloudwatch_log_group" "api" {
  name              = "/aws/lambda/${aws_lambda_function.api.function_name}"
  retention_in_days = var.log_retention_days
}

# ---------------------------------------------------------------------------
# API Gateway HTTP API (v2) — lower cost and lower latency than REST API
# ---------------------------------------------------------------------------
resource "aws_apigatewayv2_api" "api" {
  name          = "publishing-api-${var.environment}"
  protocol_type = "HTTP"
  description   = "PublishingApi HTTP API — ${var.environment}"

  tags = var.tags
}

resource "aws_apigatewayv2_stage" "api" {
  api_id      = aws_apigatewayv2_api.api.id
  name        = "$default"
  auto_deploy = true

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.api_gateway.arn
    format = jsonencode({
      requestId      = "$context.requestId"
      ip             = "$context.identity.sourceIp"
      requestTime    = "$context.requestTime"
      httpMethod     = "$context.httpMethod"
      routeKey       = "$context.routeKey"
      status         = "$context.status"
      protocol       = "$context.protocol"
      responseLength = "$context.responseLength"
    })
  }

  tags = var.tags
}

resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apigateway/publishing-api-${var.environment}"
  retention_in_days = var.log_retention_days
}

# ---------------------------------------------------------------------------
# JWT authorizer — validates Cognito access tokens at API Gateway (defence in depth layer 1)
# ---------------------------------------------------------------------------
resource "aws_apigatewayv2_authorizer" "cognito_jwt" {
  api_id           = aws_apigatewayv2_api.api.id
  authorizer_type  = "JWT"
  identity_sources = ["$request.header.Authorization"]
  name             = "cognito-jwt-${var.environment}"

  jwt_configuration {
    audience = [var.cognito_spa_client_id, var.cognito_automation_client_id]
    issuer   = "https://cognito-idp.${data.aws_region.current.name}.amazonaws.com/${var.cognito_user_pool_id}"
  }
}

# ---------------------------------------------------------------------------
# Lambda integration + route
# ---------------------------------------------------------------------------
resource "aws_apigatewayv2_integration" "lambda" {
  api_id           = aws_apigatewayv2_api.api.id
  integration_type = "AWS_PROXY"

  integration_uri        = aws_lambda_function.api.invoke_arn
  integration_method     = "POST"
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "health" {
  api_id    = aws_apigatewayv2_api.api.id
  route_key = "GET /health"

  authorization_type = "NONE"
  target             = "integrations/${aws_apigatewayv2_integration.lambda.id}"
}

# Catch-all route for all other endpoints — requires JWT
resource "aws_apigatewayv2_route" "proxy" {
  api_id    = aws_apigatewayv2_api.api.id
  route_key = "$default"

  authorization_type = "JWT"
  authorizer_id      = aws_apigatewayv2_authorizer.cognito_jwt.id
  target             = "integrations/${aws_apigatewayv2_integration.lambda.id}"
}

# ---------------------------------------------------------------------------
# Lambda invoke permission for API Gateway
# ---------------------------------------------------------------------------
resource "aws_lambda_permission" "api_gateway" {
  statement_id  = "AllowAPIGatewayInvoke-${var.environment}"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.api.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.api.execution_arn}/*/*"
}
