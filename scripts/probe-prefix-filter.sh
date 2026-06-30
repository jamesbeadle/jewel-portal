#!/usr/bin/env bash
#
# Read-only probe: can Graph filter messages by a category *prefix* (any 'JPMS/...' tag)? That's what
# lets "untagged = triage" work server-side with no hidden marker tag. GET only — writes nothing.
#
# Run:
#   export MAILBOX_TENANT_ID="..."; export MAILBOX_CLIENT_ID="..."; export MAILBOX_CLIENT_SECRET="..."
#   bash scripts/probe-prefix-filter.sh

: "${MAILBOX_TENANT_ID:?}"; : "${MAILBOX_CLIENT_ID:?}"; : "${MAILBOX_CLIENT_SECRET:?}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}/mailFolders/inbox/messages"
EV=(-H "Authorization: Bearer $TOKEN" -H "ConsistencyLevel: eventual")

summary() { python3 -c "
import sys,json
try: d=json.load(sys.stdin)
except: print('  (non-JSON / empty)'); sys.exit()
if isinstance(d,dict) and 'error' in d:
    print('  ERROR:', d['error'].get('code'), '-', (d['error'].get('message') or '')[:140])
else:
    print('  status: OK | count:', d.get('@odata.count','-'), '| items:', len(d.get('value',[])))
"; }

echo "=== Triage queue: NO 'JPMS/' tag  ->  not categories/any(c:startsWith(c,'JPMS/')) ==="
curl -s -G "$BASE" "${EV[@]}" \
  --data-urlencode "\$filter=not categories/any(c:startsWith(c,'JPMS/'))" \
  --data-urlencode '$orderby=receivedDateTime desc' --data-urlencode '$select=subject' \
  --data-urlencode '$top=5' --data-urlencode '$count=true' | summary

echo
echo "=== Tagged view: HAS a 'JPMS/' tag  ->  categories/any(c:startsWith(c,'JPMS/')) ==="
curl -s -G "$BASE" "${EV[@]}" \
  --data-urlencode "\$filter=categories/any(c:startsWith(c,'JPMS/'))" \
  --data-urlencode '$orderby=receivedDateTime desc' --data-urlencode '$select=subject,categories' \
  --data-urlencode '$top=5' --data-urlencode '$count=true' | summary

echo
echo "If both say 'status: OK' with sensible counts (the two should add up to the inbox total),"
echo "the prefix filter works and we drop the marker. If either errors, we keep a marker tag instead."
