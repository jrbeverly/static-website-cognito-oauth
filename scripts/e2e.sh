#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

tf() { terraform -chdir=env/my-sites/staging output -raw "$1"; }
api=$(tf api_endpoint)
cognito=$(tf cognito_domain)
client_id=$(tf automation_client_id)
client_secret=$(tf automation_client_secret)

marker="e2e-$(date +%s)"
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
echo "<h1>$marker</h1>" > "$work/index.html"
(cd "$work" && zip -q site.zip index.html)

echo "GET /sites without a token"
code=$(curl -sS -o /dev/null -w '%{http_code}' "$api/sites")
[[ $code == 401 ]] || { echo "expected 401, got $code"; exit 1; }
echo "  $code"

echo "POST /oauth2/token (client_credentials)"
token=$(curl -sS "https://$cognito/oauth2/token" \
  -d grant_type=client_credentials -d "client_id=$client_id" -d "client_secret=$client_secret" \
  --data-urlencode 'scope=sites/read sites/write' | jq -r .access_token)
auth="Authorization: Bearer $token"
echo "  ${token:0:12}..."

echo "POST /sites"
site_id=$(curl -sS -X POST "$api/sites" -H "$auth" -H 'Content-Type: application/json' \
  -d "{\"siteName\":\"$marker\"}" | jq -r .siteId)
echo "  $site_id"

echo "PUT /sites/$site_id/content"
site_url=$(curl -sS -X PUT "$api/sites/$site_id/content" -H "$auth" -F "file=@$work/site.zip" | jq -r .siteUrl)
echo "  $site_url"

echo "GET $site_url"
curl -sS "$site_url" | grep -q "$marker"
echo "  served $marker"

echo "DELETE /sites/$site_id"
code=$(curl -sS -o /dev/null -w '%{http_code}' -X DELETE "$api/sites/$site_id" -H "$auth")
[[ $code == 204 ]] || { echo "expected 204, got $code"; exit 1; }
code=$(curl -sS -o /dev/null -w '%{http_code}' "$api/sites/$site_id" -H "$auth")
[[ $code == 404 ]] || { echo "expected 404, got $code"; exit 1; }
echo "  204, then GET is 404"

echo "e2e passed"
