#!/usr/bin/env bash
#
# Read-only triage reconciliation probe. Answers: does the queue (untagged) + the tagged emails add
# up to the whole Inbox, and which emails are tagged? Uses the same app-only identity as the SWA.
# GET requests only — writes nothing.
#
# Run:
#   export MAILBOX_TENANT_ID="<tenant id>"
#   export MAILBOX_CLIENT_ID="<app client id>"
#   export MAILBOX_CLIENT_SECRET="<app client secret>"
#   bash scripts/probe-triage-counts.sh

: "${MAILBOX_TENANT_ID:?set MAILBOX_TENANT_ID}"
: "${MAILBOX_CLIENT_ID:?set MAILBOX_CLIENT_ID}"
: "${MAILBOX_CLIENT_SECRET:?set MAILBOX_CLIENT_SECRET}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"
TRIAGED="${TRIAGED:-JPMS/Triaged}"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" \
  --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" \
  --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}/mailFolders/inbox/messages"
H=(-H "Authorization: Bearer ${TOKEN}" -H "ConsistencyLevel: eventual")

count() { # $1 = filter (or empty)
  if [ -z "$1" ]; then
    curl -s -G "$BASE" "${H[@]}" --data-urlencode '$count=true' --data-urlencode '$top=1' \
      | python3 -c "import sys,json;print(json.load(sys.stdin).get('@odata.count','?'))"
  else
    curl -s -G "$BASE" "${H[@]}" --data-urlencode "\$filter=$1" --data-urlencode '$count=true' --data-urlencode '$top=1' \
      | python3 -c "import sys,json;print(json.load(sys.stdin).get('@odata.count','?'))"
  fi
}

echo "Inbox total:                 $(count '')"
echo "Untagged (the triage queue): $(count "not categories/any(c:c eq '${TRIAGED}')")"
echo "Tagged ${TRIAGED}:           $(count "categories/any(c:c eq '${TRIAGED}')")"

echo
echo "=== Emails currently carrying a JPMS tag (subject -> categories) ==="
curl -s -G "$BASE" "${H[@]}" \
  --data-urlencode "\$filter=categories/any(c:c eq '${TRIAGED}')" \
  --data-urlencode '$select=subject,categories' \
  --data-urlencode '$top=50' \
  | python3 -c "
import sys, json
d = json.load(sys.stdin)
v = d.get('value', [])
if not v:
    print('  (none — no emails are tagged)')
for m in v:
    print('  %-55s -> %s' % ((m.get('subject') or '(no subject)')[:55], ', '.join(m.get('categories') or [])))
"
