# DynamoDB Sites Metadata Table
#
# Implements the single-table design.
# Composite primary key (PK + SK) with no GSI — all required access patterns
# are satisfied by the table key alone.
#
# PK = USER#{cognito-sub}  — partition key scoping all queries to a single user
# SK = SITE#{site-id}      — sort key enabling listing via begins_with("SITE#")

resource "aws_dynamodb_table" "sites" {
  name         = "${var.table_name_prefix}-${var.environment}"
  billing_mode = "PAY_PER_REQUEST"

  hash_key  = "PK"
  range_key = "SK"

  attribute {
    name = "PK"
    type = "S"
  }

  attribute {
    name = "SK"
    type = "S"
  }

  point_in_time_recovery {
    enabled = var.point_in_time_recovery
  }

  tags = {
    Environment = var.environment
    Project     = "my-sites"
    ManagedBy   = "terraform"
  }
}
