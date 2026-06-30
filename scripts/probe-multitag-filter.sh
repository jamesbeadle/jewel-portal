#!/usr/bin/env bash
#
# Read-only probe: does Graph accept an OR of several category filters (multi-tag filtering), with
# $count + $orderby + $skip + eventual consistency — i.e. does it paginate like the single-tag filter?
# This decides whether the Tagged tab's multi-select filter can be done server-side. GET only.
#
#   export MAILBOX_TENANT_ID="..."; export MAILBOX_CLIENT_ID="..."; export MAILBOX_CLIENT_SECRET="..."
#   bash scripts/probe-multitag-filter.sh

: "${MAILBOX_TENANT_ID:?}"; : "${MAILBOX_CLIENT_ID:?}"; : "${MAILBOX_CLIENT_SECRET:?}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}/mailFolders/inbox/messages"
EV=(-H "Authorization: Bearer $TOKEN" -H "ConsistencyLevel: eventual")

run() { # $1 = label, $2 = filter, $3 = skip
  echo "=== $1 ==="
  curl -s -G "$BASE" "${EV[@]}" \
    --data-urlencode "\$filter=$2" \
    --data-urlencode '$orderby=receivedDateTime desc' \
    --data-urlencode '$select=subject,categories' \
    --data-urlencode '$top=25' --data-urlencode "\$skip=$3" --data-urlencode '$count=true' | python3 -c "
import sys,json
try: d=json.load(sys.stdin)
except: print('  (non-JSON / empty)'); sys.exit()
if isinstance(d,dict) and 'error' in d:
    print('  ERROR:', d['error'].get('code'),'-',(d['error'].get('message') or '')[:140])
else:
    print('  status: OK | count:', d.get('@odata.count','-'), '| items this page:', len(d.get('value',[])))
"
  echo
}

run "Baseline single tag: JPMS/RFI-001" "categories/any(c:c eq 'JPMS/RFI-001')" 0
run "OR of two tags (RFI-001 OR Discarded), page 1" "categories/any(c:c eq 'JPMS/RFI-001') or categories/any(c:c eq 'JPMS/Discarded')" 0
run "OR of two tags, page 2 (skip 25) — confirms paging is accepted" "categories/any(c:c eq 'JPMS/RFI-001') or categories/any(c:c eq 'JPMS/Discarded')" 25
run "OR of three tags (adds the bare marker)" "categories/any(c:c eq 'JPMS/RFI-001') or categories/any(c:c eq 'JPMS/Discarded') or categories/any(c:c eq 'JPMS')" 0

echo "If every section says 'status: OK' (counts can be small/zero — you only have a few tagged emails),"
echo "the OR filter works and I'll build the multi-select dropdown on it. Any 'ERROR' means we adjust."
