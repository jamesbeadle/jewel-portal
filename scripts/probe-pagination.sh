#!/usr/bin/env bash
#
# Read-only pagination probe. Pulls page 1 of the untriaged inbox, then tries to reach page 2 three
# ways and reports which one actually returns emails. This tests Graph directly (no app code), so the
# result is unambiguous about which paging mechanism to build on. GET only — writes nothing.
#
# Run:
#   export MAILBOX_TENANT_ID="..."; export MAILBOX_CLIENT_ID="..."; export MAILBOX_CLIENT_SECRET="..."
#   bash scripts/probe-pagination.sh

: "${MAILBOX_TENANT_ID:?}"; : "${MAILBOX_CLIENT_ID:?}"; : "${MAILBOX_CLIENT_SECRET:?}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"
FILTER="not categories/any(c:c eq 'JPMS/Triaged')"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}/mailFolders/inbox/messages"
EV=(-H "Authorization: Bearer $TOKEN" -H "ConsistencyLevel: eventual")
PLAIN=(-H "Authorization: Bearer $TOKEN")

summary() { python3 -c "
import sys,json
try: d=json.load(sys.stdin)
except: print('  (non-JSON / empty body)'); sys.exit()
if isinstance(d,dict) and 'error' in d:
    print('  ERROR:', d['error'].get('code'), '-', (d['error'].get('message') or '')[:120])
else:
    print('  items:', len(d.get('value',[])), '| count:', d.get('@odata.count','-'), '| has nextLink:', bool(d.get('@odata.nextLink')))
"; }

echo "=== Page 1: top=25, count, eventual ==="
P1=$(curl -s -G "$BASE" "${EV[@]}" \
  --data-urlencode "\$filter=$FILTER" --data-urlencode '$orderby=receivedDateTime desc' \
  --data-urlencode '$select=subject' --data-urlencode '$top=25' --data-urlencode '$count=true')
printf '%s' "$P1" | summary
NL=$(printf '%s' "$P1" | python3 -c "import sys,json;print(json.load(sys.stdin).get('@odata.nextLink',''))" 2>/dev/null)
echo "  nextLink: ${NL:-(none)}"

echo
echo "=== Page 2 (A): follow the nextLink as-is, with eventual ==="
if [ -n "$NL" ]; then curl -s "${EV[@]}" "$NL" | summary; else echo "  (page 1 returned no nextLink)"; fi

echo
echo "=== Page 2 (B): follow the nextLink as-is, WITHOUT eventual ==="
if [ -n "$NL" ]; then curl -s "${PLAIN[@]}" "$NL" | summary; else echo "  (page 1 returned no nextLink)"; fi

echo
echo "=== Page 2 (C): \$skip=25 + \$top=25, NO eventual, NO count ==="
curl -s -G "$BASE" "${PLAIN[@]}" \
  --data-urlencode "\$filter=$FILTER" --data-urlencode '$orderby=receivedDateTime desc' \
  --data-urlencode '$select=subject' --data-urlencode '$top=25' --data-urlencode '$skip=25' | summary

echo
echo "=== Page 2 (D): \$skip=25 + \$top=25 + count, eventual ==="
curl -s -G "$BASE" "${EV[@]}" \
  --data-urlencode "\$filter=$FILTER" --data-urlencode '$orderby=receivedDateTime desc' \
  --data-urlencode '$select=subject' --data-urlencode '$top=25' --data-urlencode '$skip=25' --data-urlencode '$count=true' | summary

echo
echo "Done. Whichever of A/B/C/D returns ~11 items is the mechanism we build on."
