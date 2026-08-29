#!/usr/bin/env bats
# Tests for scripts/publish.sh
# Requires: bats, scripts/publish.sh

setup() {
  SCRIPT_DIR="$(cd "$(dirname "$BATS_TEST_FILENAME")" && pwd)"
  PUBLISH_SH="${SCRIPT_DIR}/../../scripts/publish.sh"

  TEST_DIR="$(mktemp -d)"

  # Valid default env values (individual tests override as needed)
  export MY_SITES_SITE_NAME="My Report"
  export MY_SITES_SOURCE_DIR="${TEST_DIR}/dist"
  export MY_SITES_API_URL="https://api.example.com"
  export MY_SITES_COGNITO_DOMAIN="my-sites.auth.ca-central-1.amazoncognito.com"
  export MY_SITES_CLIENT_ID="test-client-id"
  export MY_SITES_CLIENT_SECRET="test-secret"

  mkdir -p "${MY_SITES_SOURCE_DIR}"
  echo "<h1>Hello</h1>" > "${MY_SITES_SOURCE_DIR}/index.html"
}

teardown() {
  rm -rf "${TEST_DIR}"
}

# Create a mock curl that serves responses from a sequence file in $MOCK_RESPONSES.
# The sequence file has one JSON line per expected curl call.
# The mock uses the URL to decide which keys to read from the JSON.
#
# For each call the JSON may contain:
#   .token           — access_token value (for token endpoint)
#   .sites           — sites JSON array (for GET /sites)
#   .create_site_id  — siteId (for POST /sites)
#   .upload_status   — HTTP status code (for PUT content)
#   .upload_body     — body (for PUT content), defaults to a success body
#
# The mock emulates `curl -w "\n%{http_code}"` by appending upload_status
# as the last line of stdout for the upload call.
stub_curl() {
  local stub_path="${TEST_DIR}/stubs"
  mkdir -p "${stub_path}"

  # Copy the response into the stub dir so the mock script can read it
  cp "${MOCK_RESPONSES}" "${stub_path}/mock-responses.jsonl"

  # Export the responses file path so the mock can find it
  export MOCK_RESPONSES_FILE="${stub_path}/mock-responses.jsonl"

  cat > "${stub_path}/curl" <<'STUB_EOF'
#!/usr/bin/env bash
line=$(head -1 "${MOCK_RESPONSES_FILE}")

url="$*"

if echo "$url" | grep -q "oauth2/token"; then
  tok=$(echo "$line" | jq -r '.token // "tok"')
  echo "{\"access_token\":\"${tok}\",\"expires_in\":3600,\"token_type\":\"Bearer\"}"

elif echo "$url" | grep -q '/sites/.*/content'; then
  status=$(echo "$line" | jq -r '.upload_status // 200')
  body=$(echo "$line" | jq -r '.upload_body // "{\"siteUrl\":\"https://cdn.example.com/sub/site-1/\"}"')
  echo "${body}"
  echo "${status}"

elif echo "$url" | grep -q '\-X POST.*/sites'; then
  sid=$(echo "$line" | jq -r '.create_site_id // "site-1"')
  sn=$(echo "$line" | jq -r '.create_site_name // "My Report"')
  echo "{\"siteId\":\"${sid}\",\"siteName\":\"${sn}\",\"contentPath\":\"sub/${sid}/\"}"

elif echo "$url" | grep -q '/sites'; then
  echo "$line" | jq -r '.sites // "[]"'

else
  echo "{}"
fi
STUB_EOF

  chmod +x "${stub_path}/curl"
  export PATH="${stub_path}:${PATH}"
}

# ─── error: missing --site-name ───────────────────────────────────────────────

@test "missing --site-name causes exit 1" {
  unset MY_SITES_SITE_NAME

  run bash "${PUBLISH_SH}" --source-dir "${TEST_DIR}/dist" \
    --api-url "https://api.example.com" \
    --cognito-domain "example.auth.ca-central-1.amazoncognito.com" \
    --client-id "id" --client-secret-env MY_SITES_CLIENT_SECRET

  [[ "${status}" -eq 1 ]]
  [[ "${output}" == *"site-name"* ]]
}

# ─── error: non-existent source-dir ───────────────────────────────────────────

@test "non-existent --source-dir causes exit 1" {
  run bash "${PUBLISH_SH}" --site-name "Test" --source-dir "${TEST_DIR}/nope" \
    --api-url "https://api.example.com" \
    --cognito-domain "example.auth.ca-central-1.amazoncognito.com" \
    --client-id "id" --client-secret-env MY_SITES_CLIENT_SECRET

  [[ "${status}" -eq 1 ]]
  [[ "${output}" == *"Source directory does not exist"* ]]
}

# ─── error: missing client secret env ─────────────────────────────────────────

@test "missing client secret causes exit 1" {
  run bash "${PUBLISH_SH}" --site-name "Test" --source-dir "${TEST_DIR}/dist" \
    --api-url "https://api.example.com" \
    --cognito-domain "example.auth.ca-central-1.amazoncognito.com" \
    --client-id "id" --client-secret-env NOPE_NOT_SET

  [[ "${status}" -eq 1 ]]
  [[ "${output}" == *"empty or not set"* ]]
}

# ─── error: token acquisition fails ───────────────────────────────────────────

@test "token acquisition failure exits 1" {
  export MOCK_RESPONSES="${TEST_DIR}/responses.jsonl"
  echo '{"token":"null"}' > "${MOCK_RESPONSES}"
  stub_curl

  run bash "${PUBLISH_SH}"

  [[ "${status}" -eq 1 ]]
  [[ "${output}" == *"Failed to acquire token"* ]]
}

# ─── happy path: create site (not found) + publish ────────────────────────────

@test "happy path — creates site and publishes" {
  export MOCK_RESPONSES="${TEST_DIR}/responses.jsonl"
  # Call 1: token   Call 2: GET /sites returns []   Call 3: POST /sites   Call 4: PUT content
  cat > "${MOCK_RESPONSES}" <<'JSONL'
{"token":"tok","sites":"[]","create_site_id":"new-site","upload_status":200,"upload_body":"{\"siteUrl\":\"https://cdn.example.com/sub/new-site/\"}"}
JSONL

  stub_curl

  run bash "${PUBLISH_SH}"

  [[ "${status}" -eq 0 ]]
  [[ "${output}" == *"Created site: new-site"* ]]
  [[ "${output}" == *"Published successfully: https://cdn.example.com/sub/new-site/"* ]]
}

# ─── happy path: find existing site + publish ─────────────────────────────────

@test "happy path — finds existing site by name and publishes" {
  export MOCK_RESPONSES="${TEST_DIR}/responses.jsonl"
  cat > "${MOCK_RESPONSES}" <<'JSONL'
{"token":"tok","sites":"[{\"siteId\":\"existing-1\",\"siteName\":\"My Report\",\"status\":\"active\",\"siteUrl\":null,\"createdAt\":\"2026-01-01T00:00:00Z\"}]","upload_status":200,"upload_body":"{\"siteUrl\":\"https://cdn.example.com/sub/existing-1/\"}"}
JSONL

  stub_curl

  run bash "${PUBLISH_SH}"

  [[ "${status}" -eq 0 ]]
  [[ "${output}" == *"Found existing site: existing-1"* ]]
  [[ "${output}" == *"Published successfully: https://cdn.example.com/sub/existing-1/"* ]]
}

# ─── error: 401 ───────────────────────────────────────────────────────────────

@test "HTTP 401 on upload exits 1 with authentication message" {
  export MOCK_RESPONSES="${TEST_DIR}/responses.jsonl"
  cat > "${MOCK_RESPONSES}" <<'JSONL'
{"token":"tok","sites":"[{\"siteId\":\"s1\",\"siteName\":\"My Report\",\"status\":\"active\",\"siteUrl\":null,\"createdAt\":\"2026-01-01T00:00:00Z\"}]","upload_status":401,"upload_body":"{\"error\":\"Unauthorized\"}"}
JSONL

  stub_curl

  run bash "${PUBLISH_SH}"

  [[ "${status}" -eq 1 ]]
  [[ "${output}" == *"Authentication failed. Check client credentials and scopes."* ]]
}

# ─── error: 413 ───────────────────────────────────────────────────────────────

@test "HTTP 413 on upload exits 1 with size message" {
  export MOCK_RESPONSES="${TEST_DIR}/responses.jsonl"
  cat > "${MOCK_RESPONSES}" <<'JSONL'
{"token":"tok","sites":"[{\"siteId\":\"s1\",\"siteName\":\"My Report\",\"status\":\"active\",\"siteUrl\":null,\"createdAt\":\"2026-01-01T00:00:00Z\"}]","upload_status":413,"upload_body":"{\"detail\":\"ZIP file exceeds 50 MB limit\"}"}
JSONL

  stub_curl

  run bash "${PUBLISH_SH}"

  [[ "${status}" -eq 1 ]]
  [[ "${output}" == *"ZIP too large. Maximum size is 50 MB."* ]]
}

# ─── error: unknown HTTP status ───────────────────────────────────────────────

@test "unknown HTTP status exits 1 with details" {
  export MOCK_RESPONSES="${TEST_DIR}/responses.jsonl"
  cat > "${MOCK_RESPONSES}" <<'JSONL'
{"token":"tok","sites":"[{\"siteId\":\"s1\",\"siteName\":\"My Report\",\"status\":\"active\",\"siteUrl\":null,\"createdAt\":\"2026-01-01T00:00:00Z\"}]","upload_status":500,"upload_body":"Internal error"}
JSONL

  stub_curl

  run bash "${PUBLISH_SH}"

  [[ "${status}" -eq 1 ]]
  [[ "${output}" == *"Publish failed (HTTP 500)"* ]]
}

# ─── help flag ────────────────────────────────────────────────────────────────

@test "--help prints usage and exits 0" {
  run bash "${PUBLISH_SH}" --help

  [[ "${status}" -eq 0 ]]
  [[ "${output}" == *"Usage:"* ]]
}
