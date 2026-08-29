# CloudFront Serving — Content Distribution
#
# CloudFront distribution with S3 Origin Access Control for serving
# user-published static websites.
#
# HTTPS is enforced, compression is enabled, and cost is minimized
# through PriceClass_100 (US, Canada, Europe only).
#
# Cache invalidation is handled by the publishing API at publish time
# via cloudfront:CreateInvalidation on the distribution.

# ---------------------------------------------------------------------------
# CloudFront Function — URI rewriting for sub-path default index
# ---------------------------------------------------------------------------
# CloudFront's default_root_object only applies to the distribution root (/).
# For sub-path default documents (/{sub}/{siteId}/ → /{sub}/{siteId}/index.html),
# a CloudFront Function rewrites viewer-request URIs before forwarding to S3.
resource "aws_cloudfront_function" "rewrite_default_index" {
  name    = "rewrite-default-index-${var.environment}"
  runtime = "cloudfront-js-2.0"
  comment = "Rewrite directory and extensionless paths to serve index.html"
  publish = true
  code    = file("${path.module}/cloudfront-function.js")
}

# ---------------------------------------------------------------------------
# CloudFront Origin Access Control
# ---------------------------------------------------------------------------
resource "aws_cloudfront_origin_access_control" "s3" {
  name                              = "my-sites-${var.environment}"
  description                       = "OAC for my-sites content bucket — ${var.environment}"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# ---------------------------------------------------------------------------
# CloudFront distribution
# ---------------------------------------------------------------------------
resource "aws_cloudfront_distribution" "content" {
  enabled         = true
  is_ipv6_enabled = true
  price_class     = var.price_class

  origin {
    domain_name              = var.s3_bucket_regional_domain
    origin_id                = "s3-content"
    origin_access_control_id = aws_cloudfront_origin_access_control.s3.id
  }

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD", "OPTIONS"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = "s3-content"

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }

    viewer_protocol_policy = "redirect-to-https"
    min_ttl                = 0
    default_ttl            = 3600
    max_ttl                = 86400
    compress               = true

    function_association {
      event_type   = "viewer-request"
      function_arn = aws_cloudfront_function.rewrite_default_index.arn
    }
  }

  custom_error_response {
    error_code            = 403
    response_code         = 404
    response_page_path    = "/404.html"
    error_caching_min_ttl = 10
  }

  custom_error_response {
    error_code            = 404
    response_code         = 404
    response_page_path    = "/404.html"
    error_caching_min_ttl = 10
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = var.acm_cert_arn == null ? true : false
    acm_certificate_arn            = var.acm_cert_arn
    ssl_support_method             = var.acm_cert_arn != null ? "sni-only" : null
    minimum_protocol_version       = var.acm_cert_arn != null ? "TLSv1.2_2021" : null
  }

  tags = merge(var.tags, {
    Component = "ContentServing"
  })
}
