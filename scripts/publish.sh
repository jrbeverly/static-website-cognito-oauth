#!/usr/bin/env bash
# publish.sh — Reference automation script for the My Sites publishing platform.
#
# Acquires a Cognito OAuth 2.0 token (Client Credentials grant), finds or creates
# a site by name, packages a local directory as a ZIP, and publishes it.
#
# Dependencies: bash 4+, curl, jq, zip
#
# Usage:
#   scripts/publish.sh \
#     --site-name "My Report" \
#     --source-dir ./dist \
#     --api-url https://api-gateway.execute-api.ca-central-1.amazonaws.com \
#     --cognito-domain my-sites.auth.ca-central-1.amazoncognito.com \
#     --client-id YOUR_CLIENT_ID \
#     --client-secret-env MY_CLIENT_SECRET
#
#   export MY_SITES_API_URL=...
#   export MY_SITES_COGNITO_DOMAIN=...
#   export MY_SITES_CLIENT_ID=...
#   export MY_SITES_CLIENT_SECRET=...
#   export MY_SITES_SITE_NAME="My Report"
#   export MY_SITES_SOURCE_DIR=./dist
#   scripts/publish.sh
#
# Flags take precedence over environment variables.
# The client secret must not be passed directly on the command line;
# use --client-secret-env to name the environment variable that holds it.

set -euo pipefail

# ─── helpers ──────────────────────────────────────────────────────────────────

die() {
  echo "✗ $*" >&2
  exit 1
}

usage() {
  cat <<'EOF'
Usage: publish.sh [OPTIONS]

OPTIONS (each has an environment variable fallback):

  --site-name NAME            Site display name ($MY_SITES_SITE_NAME)
  --source-dir DIR            Local directory to publish ($MY_SITES_SOURCE_DIR)
  --api-url URL               Publishing API Gateway URL ($MY_SITES_API_URL)
  --cognito-domain DOMAIN     Cognito hosted UI domain ($MY_SITES_COGNITO_DOMAIN)
  --client-id ID              Automation app client ID ($MY_SITES_CLIENT_ID)
  --client-secret-env VAR     Name of env var holding the client secret
                              ($MY_SITES_CLIENT_SECRET is used directly if set)
  --help                      Show this message

The client secret itself is never accepted as a command-line argument.
Store it in an environment variable and pass the variable name via
--client-secret-env, or set MY_SITES_CLIENT_SECRET directly.
EOF
  exit 0
}

# ─── argument parsing ─────────────────────────────────────────────────────────

SITE_NAME="${MY_SITES_SITE_NAME:-}"
SOURCE_DIR="${MY_SITES_SOURCE_DIR:-}"
API_URL="${MY_SITES_API_URL:-}"
COGNITO_DOMAIN="${MY_SITES_COGNITO_DOMAIN:-}"
CLIENT_ID="${MY_SITES_CLIENT_ID:-}"
CLIENT_SECRET_ENV="${MY_SITES_CLIENT_SECRET:+MY_SITES_CLIENT_SECRET}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --site-name)        SITE_NAME="$2";        shift 2 ;;
    --source-dir)       SOURCE_DIR="$2";        shift 2 ;;
    --api-url)          API_URL="$2";           shift 2 ;;
    --cognito-domain)   COGNITO_DOMAIN="$2";    shift 2 ;;
    --client-id)        CLIENT_ID="$2";         shift 2 ;;
    --client-secret-env) CLIENT_SECRET_ENV="$2"; shift 2 ;;
    --help)             usage ;;
    *) die "Unknown option: $1" ;;
  esac
done

# ─── validation ────────────────────────────────────────────────────────────────

[[ -n "${SITE_NAME}" ]]       || die "Missing required option: --site-name (or set MY_SITES_SITE_NAME)"
[[ -n "${SOURCE_DIR}" ]]      || die "Missing required option: --source-dir (or set MY_SITES_SOURCE_DIR)"
[[ -n "${API_URL}" ]]         || die "Missing required option: --api-url (or set MY_SITES_API_URL)"
[[ -n "${COGNITO_DOMAIN}" ]]  || die "Missing required option: --cognito-domain (or set MY_SITES_COGNITO_DOMAIN)"
[[ -n "${CLIENT_ID}" ]]       || die "Missing required option: --client-id (or set MY_SITES_CLIENT_ID)"
[[ -n "${CLIENT_SECRET_ENV}" ]] || die "Missing required option: --client-secret-env (or set MY_SITES_CLIENT_SECRET)"
[[ -d "${SOURCE_DIR}" ]]      || die "Source directory does not exist: ${SOURCE_DIR}"

CLIENT_SECRET="${!CLIENT_SECRET_ENV:-}"
[[ -n "${CLIENT_SECRET}" ]] || die "Environment variable ${CLIENT_SECRET_ENV} is empty or not set"

# --- strip trailing slash from API_URL (API Gateway normalises, but be safe) ---
API_URL="${API_URL%/}"

# ─── token acquisition ────────────────────────────────────────────────────────

TOKEN=$(curl -sS -X POST \
  "https://${COGNITO_DOMAIN}/oauth2/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=${CLIENT_ID}&client_secret=${CLIENT_SECRET}&scope=sites%2Fread%20sites%2Fwrite" \
  | jq -r '.access_token')

if [[ -z "${TOKEN}" || "${TOKEN}" == "null" ]]; then
  die "Failed to acquire token. Check client credentials."
fi

# ─── find or create site ──────────────────────────────────────────────────────

SITES_JSON=$(curl -sS "${API_URL}/sites" \
  -H "Authorization: Bearer ${TOKEN}")

SITE_ID=$(echo "${SITES_JSON}" | jq -r --arg name "${SITE_NAME}" \
  '.[] | select(.siteName == $name) | .siteId' | head -1)

if [[ -z "${SITE_ID}" ]]; then
  CREATE_RESPONSE=$(curl -sS -X POST "${API_URL}/sites" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json" \
    -d "$(jq -n --arg name "${SITE_NAME}" '{siteName: $name}')")

  SITE_ID=$(echo "${CREATE_RESPONSE}" | jq -r '.siteId')
  if [[ -z "${SITE_ID}" || "${SITE_ID}" == "null" ]]; then
    die "Failed to create site."
  fi
  echo "Created site: ${SITE_ID}"
else
  echo "Found existing site: ${SITE_ID}"
fi

# ─── zip source directory ─────────────────────────────────────────────────────

TMPZIP=$(mktemp /tmp/publish-XXXXX.zip)
rm -f "${TMPZIP}"
trap "rm -f ${TMPZIP}" EXIT

(cd "${SOURCE_DIR}" && zip -qr "${TMPZIP}" .) || die "ZIP creation failed."

# ─── upload ───────────────────────────────────────────────────────────────────

HTTP_RESPONSE=$(curl -sS -w "\n%{http_code}" -X PUT \
  "${API_URL}/sites/${SITE_ID}/content" \
  -H "Authorization: Bearer ${TOKEN}" \
  -F "file=@${TMPZIP}")

HTTP_CODE=$(echo "${HTTP_RESPONSE}" | tail -n1)
BODY=$(echo "${HTTP_RESPONSE}" | sed '$d')

case "${HTTP_CODE}" in
  200)
    SITE_URL=$(echo "${BODY}" | jq -r '.siteUrl')
    echo "✓ Published successfully: ${SITE_URL}"
    ;;
  401)
    die "Authentication failed. Check client credentials and scopes."
    ;;
  413)
    die "ZIP too large. Maximum size is 50 MB."
    ;;
  *)
    die "Publish failed (HTTP ${HTTP_CODE}): ${BODY}"
    ;;
esac
