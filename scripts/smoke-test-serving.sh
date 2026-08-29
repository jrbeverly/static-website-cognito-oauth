#!/usr/bin/env bash
# smoke-test-serving.sh — End-to-end smoke test for the static site serving layer.
#
# Validates the CloudFront + S3 serving path independently of the publishing API.
# Uploads test files directly to S3 at the expected namespace prefix and verifies
# they are served correctly through CloudFront.
#
# Dependencies: bash 4+, awscli, curl
#
# Usage:
#   scripts/smoke-test-serving.sh \
#     --bucket my-sites-content-staging \
#     --distribution-domain d1234.cloudfront.net \
#     --test-sub test-user-abc123 \
#     --test-site-id test-site-001
#
#   # Or via environment variables:
#   export SMOKE_TEST_BUCKET=my-sites-content-staging
#   export SMOKE_TEST_DOMAIN=d1234.cloudfront.net
#   scripts/smoke-test-serving.sh

set -euo pipefail

# ─── helpers ──────────────────────────────────────────────────────────────────

die() {
  echo "✗ $*" >&2
  exit 1
}

pass()  { echo "  ✓ PASS: $1"; }
fail()  { echo "  ✗ FAIL: $1 — $2"; FAILURES=$((FAILURES + 1)); }

usage() {
  cat <<'EOF'
Usage: smoke-test-serving.sh [OPTIONS]

OPTIONS (each has an environment variable fallback):

  --bucket NAME                S3 content bucket name ($SMOKE_TEST_BUCKET)
  --distribution-domain DOMAIN CloudFront distribution domain ($SMOKE_TEST_DOMAIN)
  --test-sub SUB               Test Cognito sub value ($SMOKE_TEST_SUB)
  --test-site-id ID            Test site ID ($SMOKE_TEST_SITE_ID)
  --help                       Show this message
EOF
  exit 0
}

# ─── argument parsing ─────────────────────────────────────────────────────────

BUCKET="${SMOKE_TEST_BUCKET:-}"
DOMAIN="${SMOKE_TEST_DOMAIN:-}"
TEST_SUB="${SMOKE_TEST_SUB:-test-user-abc123}"
SITE_ID="${SMOKE_TEST_SITE_ID:-test-site-001}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --bucket)                  BUCKET="$2";   shift 2 ;;
    --distribution-domain)     DOMAIN="$2";    shift 2 ;;
    --test-sub)                TEST_SUB="$2";  shift 2 ;;
    --test-site-id)            SITE_ID="$2";   shift 2 ;;
    --help)                    usage ;;
    *) die "Unknown option: $1" ;;
  esac
done

[[ -n "${BUCKET}" ]] || die "Missing required option: --bucket (or set SMOKE_TEST_BUCKET)"
[[ -n "${DOMAIN}" ]] || die "Missing required option: --distribution-domain (or set SMOKE_TEST_DOMAIN)"

FAILURES=0
PREFIX="${TEST_SUB}/${SITE_ID}"
TMPDIR=$(mktemp -d /tmp/smoke-test-XXXXX)
trap "rm -rf ${TMPDIR}" EXIT

echo "=== Serving Layer Smoke Test ==="
echo "Bucket:   ${BUCKET}"
echo "Domain:   ${DOMAIN}"
echo "Prefix:   ${PREFIX}/"
echo ""

# ─── setup: create test files ─────────────────────────────────────────────────

echo "--- Setup: creating test files ---"

echo '<!DOCTYPE html><html><head><title>Test Site</title></head><body><h1>It Works!</h1></body></html>' > "${TMPDIR}/index.html"
echo 'body { font-family: sans-serif; }' > "${TMPDIR}/style.css"

aws s3 cp "${TMPDIR}/index.html" "s3://${BUCKET}/${PREFIX}/index.html" --quiet
echo "  Uploaded: ${PREFIX}/index.html"

aws s3 cp "${TMPDIR}/style.css"  "s3://${BUCKET}/${PREFIX}/assets/style.css" --quiet
echo "  Uploaded: ${PREFIX}/assets/style.css"
echo ""

# ─── T1: directory path → default index ─────────────────────────────────────

echo "--- T1: Directory path returns index.html ---"
RESP=$(curl -sS -o /dev/null -w "%{http_code}" "https://${DOMAIN}/${PREFIX}/")
if [[ "${RESP}" == "200" ]]; then
  BODY=$(curl -sS "https://${DOMAIN}/${PREFIX}/")
  if echo "${BODY}" | grep -q "It Works"; then
    pass "Directory path returned 200 with correct HTML"
  else
    fail "Directory path" "body did not contain expected content"
  fi
else
  fail "Directory path" "expected 200, got ${RESP}"
fi

# ─── T2: explicit file path ─────────────────────────────────────────────────

echo "--- T2: Explicit file path returns 200 ---"
RESP=$(curl -sS -o /dev/null -w "%{http_code}" "https://${DOMAIN}/${PREFIX}/index.html")
if [[ "${RESP}" == "200" ]]; then
  BODY=$(curl -sS "https://${DOMAIN}/${PREFIX}/index.html")
  if echo "${BODY}" | grep -q "It Works"; then
    pass "Explicit index.html returned 200 with correct HTML"
  else
    fail "Explicit index.html" "body did not contain expected content"
  fi
else
  fail "Explicit index.html" "expected 200, got ${RESP}"
fi

# ─── T3: asset file ──────────────────────────────────────────────────────────

echo "--- T3: Asset file returns 200 ---"
RESP=$(curl -sS -o /dev/null -w "%{http_code}" "https://${DOMAIN}/${PREFIX}/assets/style.css")
if [[ "${RESP}" == "200" ]]; then
  BODY=$(curl -sS "https://${DOMAIN}/${PREFIX}/assets/style.css")
  if echo "${BODY}" | grep -q "font-family"; then
    pass "Asset file returned 200 with correct CSS"
  else
    fail "Asset file" "body did not contain expected CSS"
  fi
else
  fail "Asset file" "expected 200, got ${RESP}"
fi

# ─── T4: non-existent file → 404 ────────────────────────────────────────────

echo "--- T4: Non-existent file returns 404 ---"
RESP=$(curl -sS -o /dev/null -w "%{http_code}" "https://${DOMAIN}/${PREFIX}/notfound.html")
if [[ "${RESP}" == "404" ]]; then
  BODY=$(curl -sS "https://${DOMAIN}/${PREFIX}/notfound.html")
  # S3 returns XML error by default; custom error response should override this
  if ! echo "${BODY}" | grep -qi "<?xml"; then
    pass "Non-existent file returned 404 (custom error page, not S3 XML)"
  else
    fail "Non-existent file" "returned 404 but body is S3 XML error"
  fi
else
  fail "Non-existent file" "expected 404, got ${RESP}"
fi

# ─── T5: direct S3 URL → 403 ────────────────────────────────────────────────

echo "--- T5: Direct S3 URL returns 403 ---"
S3_REGION=$(aws s3api get-bucket-location --bucket "${BUCKET}" --query LocationConstraint --output text 2>/dev/null || echo "ca-central-1")
# get-bucket-location returns "null" for us-east-1
[[ "${S3_REGION}" == "null" || -z "${S3_REGION}" ]] && S3_REGION="us-east-1"
S3_URL="https://${BUCKET}.s3.${S3_REGION}.amazonaws.com/${PREFIX}/index.html"
RESP=$(curl -sS -o /dev/null -w "%{http_code}" "${S3_URL}")
if [[ "${RESP}" == "403" ]]; then
  pass "Direct S3 URL returned 403 (public access blocked)"
else
  fail "Direct S3 URL" "expected 403, got ${RESP}"
fi

# ─── T6: another user's namespace → 404 ─────────────────────────────────────

echo "--- T6: Other user namespace returns 404 ---"
RESP=$(curl -sS -o /dev/null -w "%{http_code}" "https://${DOMAIN}/other-user/${SITE_ID}/index.html")
if [[ "${RESP}" == "404" ]]; then
  pass "Other user namespace returned 404"
else
  fail "Other user namespace" "expected 404, got ${RESP}"
fi

# ─── T7: HTTP → HTTPS redirect ──────────────────────────────────────────────

echo "--- T7: HTTP redirects to HTTPS ---"
RESP=$(curl -sS -o /dev/null -w "%{http_code}" -L --max-redirs 0 "http://${DOMAIN}/${PREFIX}/" 2>/dev/null || echo "000")
# CloudFront returns 301 for HTTP→HTTPS redirect. curl --max-redirs 0 on 301 gives exit 47.
# Try with explicit -w to capture the status before curl follows/refuses.
REDIRECT_STATUS=$(curl -sS -o /dev/null -w "%{http_code}" "http://${DOMAIN}/${PREFIX}/")
if [[ "${REDIRECT_STATUS}" == "301" ]]; then
  REDIRECT_URL=$(curl -sS -o /dev/null -w "%{redirect_url}" "http://${DOMAIN}/${PREFIX}/")
  if echo "${REDIRECT_URL}" | grep -q "^https://"; then
    pass "HTTP redirected to HTTPS (301 with HTTPS Location header)"
  else
    fail "HTTP redirect" "redirect URL is not HTTPS: ${REDIRECT_URL}"
  fi
else
  fail "HTTP redirect" "expected 301, got ${REDIRECT_STATUS}"
fi

# ─── cleanup ─────────────────────────────────────────────────────────────────

echo ""
echo "--- Cleanup ---"
aws s3 rm "s3://${BUCKET}/${TEST_SUB}/" --recursive --quiet
echo "  Removed: ${TEST_SUB}/"
echo ""

# ─── result summary ──────────────────────────────────────────────────────────

echo "=== Results ==="
if [[ ${FAILURES} -eq 0 ]]; then
  echo "All tests passed."
else
  echo "${FAILURES} test(s) failed."
  exit 1
fi
