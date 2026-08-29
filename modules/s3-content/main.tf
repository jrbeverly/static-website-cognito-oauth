# S3 Content Bucket
#
# Stores all user-published static files with no public access, AES-256
# encryption at rest, and a lifecycle rule to abort incomplete multipart
# uploads after 7 days. Implements the storage model.
#
# Public access is blocked on all fronts. Objects are only served through
# CloudFront with Origin Access Control — never directly from S3.
#
# KMS upgrade path: replace aws_s3_bucket_server_side_encryption_configuration
# with a KMS-managed key (aws_kms_key + sse_algorithm = "aws:kms") in the
# production environment override.

resource "aws_s3_bucket" "content" {
  bucket        = "${var.bucket_name_prefix}-${var.environment}"
  force_destroy = var.force_destroy

  tags = {
    Environment = var.environment
    Project     = "my-sites"
    ManagedBy   = "terraform"
  }
}

resource "aws_s3_bucket_public_access_block" "content" {
  bucket = aws_s3_bucket.content.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "content" {
  bucket = aws_s3_bucket.content.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_versioning" "content" {
  bucket = aws_s3_bucket.content.id

  versioning_configuration {
    status = "Disabled"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "content" {
  bucket = aws_s3_bucket.content.id

  rule {
    id     = "abort-incomplete-multipart-uploads"
    status = "Enabled"

    filter {}

    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }
}

resource "aws_s3_bucket_ownership_controls" "content" {
  bucket = aws_s3_bucket.content.id

  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

# ---------------------------------------------------------------------------
# Bucket policy — allow CloudFront OAC to read objects
# ---------------------------------------------------------------------------
data "aws_iam_policy_document" "cloudfront_oac" {
  statement {
    sid    = "AllowCloudFrontOACRead"
    effect = "Allow"

    principals {
      type        = "Service"
      identifiers = ["cloudfront.amazonaws.com"]
    }

    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.content.arn}/*"]

    condition {
      test     = "StringEquals"
      variable = "AWS:SourceArn"
      values   = [var.cloudfront_distribution_arn]
    }
  }
}

resource "aws_s3_bucket_policy" "cloudfront_oac" {
  bucket = aws_s3_bucket.content.id
  policy = data.aws_iam_policy_document.cloudfront_oac.json
}

resource "aws_s3_object" "not_found" {
  bucket       = aws_s3_bucket.content.id
  key          = "404.html"
  content      = "<!DOCTYPE html><html><body><h1>Not Found</h1></body></html>"
  content_type = "text/html"
}
