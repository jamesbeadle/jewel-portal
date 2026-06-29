#!/usr/bin/env bash
#
# Read-only Microsoft Graph probe (macOS/Linux — uses only curl + python3).
#
# Proves whether we can paginate the projects@ inbox filtered by category, using the SAME app-only
# identity the SWA uses. GET requests only — it never tags, moves, or deletes anything.
#
# Run:
#   export MAILBOX_TENANT_ID="<tenant id>"
#   export MAILBOX_CLIENT_ID="<app client id>"
#   export MAILBOX_CLIENT_SECRET="<app client secret>"
#   bash scripts/probe-graph-categories.sh
#
# The three values are the same MailboxIntake:TenantId / ClientId / ClientSecret the worker/SWA use.
# Don't commit the secret.

: "${MAILBOX_TENANT_ID:?set MAILBOX_TENANT_ID}"
: "${MAILBOX_CLIENT_ID:?set MAILBOX_CLIENT_ID}"
: "${MAILBOX_CLIENT_SECRET:?set MAILBOX_CLIENT_SECRET}"

MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"
CAT="${CATEGORY:-JPMS/Triaged}"

# 1) App-only token (client credentials) — the exact identity production uses.
TOKEN_JSON=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" \
  --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" \
  --data-urlencode "grant_type=client_credentials")

TOKEN=$(printf '%s' "$TOKEN_JSON" | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
if [ -z "$TOKEN" ]; then
  echo "TOKEN ERROR — could not get an app-only token:"
  printf '%s\n' "$TOKEN_JSON"
  exit 1
fi
echo "Token acquired OK."

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}/mailFolders/inbox/messages"
F1="categories/any(c:c eq '${CAT}')"
F2="not categories/any(c:c eq '${CAT}')"
AUTH=(-H "Authorization: Bearer ${TOKEN}")

probe() {
  local label="$1"; shift
  echo
  echo "=== ${label} ==="
  local out code body
  out=$(curl -s -w $'\n%{http_code}' "$@")
  code=$(printf '%s' "$out" | tail -n1)
  body=$(printf '%s' "$out" | sed '$d')
  echo "Status: ${code}"
  printf '%s' "$body" | python3 -c "
import sys, json
try:
    d = json.load(sys.stdin)
except Exception:
    print('  (non-JSON or empty body)'); sys.exit()
if isinstance(d, dict) and 'error' in d:
    print('  ERROR code:', d['error'].get('code'))
    print('  ERROR msg :', d['error'].get('message'))
else:
    print('  Items returned:', len(d.get('value', [])))
    print('  Has nextLink  :', '@odata.nextLink' in d)
"
}

# Query 1 — is filtering on categories valid at all (expect 200; count may be 0, nothing's tagged yet).
probe "Query 1: positive category filter" -G "$BASE" "${AUTH[@]}" \
  --data-urlencode "\$filter=${F1}" \
  --data-urlencode "\$select=subject,categories" \
  --data-urlencode "\$top=10"

# Query 2 — the decisive one: untriaged, newest-first, paged.
probe "Query 2: NOT category + orderby + top" -G "$BASE" "${AUTH[@]}" \
  --data-urlencode "\$filter=${F2}" \
  --data-urlencode "\$orderby=receivedDateTime desc" \
  --data-urlencode "\$select=subject,categories" \
  --data-urlencode "\$top=10"

# Query 2b — same, with advanced-query headers in case the negated filter needs them.
probe "Query 2b: NOT category + orderby + count (ConsistencyLevel: eventual)" -G "$BASE" "${AUTH[@]}" \
  -H "ConsistencyLevel: eventual" \
  --data-urlencode "\$filter=${F2}" \
  --data-urlencode "\$orderby=receivedDateTime desc" \
  --data-urlencode "\$select=subject,categories" \
  --data-urlencode "\$top=10" \
  --data-urlencode "\$count=true"

echo
echo "Done — paste the output above back to interpret it."
