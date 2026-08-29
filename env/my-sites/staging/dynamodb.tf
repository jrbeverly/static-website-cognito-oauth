module "dynamodb_sites" {
  source = "../../../modules/dynamodb-sites"

  environment            = "staging"
  table_name_prefix      = "my-sites"
  point_in_time_recovery = false
}
