#!/usr/bin/env bash
#
# Read-only verification for the tagging model (slice 1). GET only — writes nothing.
#   1. Queue count   = Inbox WITHOUT the "JPMS" marker  (what triage shows).
#   2. Tagged count  = Inbox WITH the "JPMS" marker      (what the Tagged tab will show).
#   3. For each tagged email: its JPMS categories — confirms the marker sits alongside a workflow tag.
#   4. The mailbox master category list — confirms JPMS categories were registered (coloured in Outlook).
#
# Run after deploying, then tag an email in the app (discard / assign) and run again to watch it move.
#   export MAILBOX_TENANT_ID="..."; export MAILBOX_CLIENT_ID="..."; export MAILBOX_CLIENT_SECRET="..."
#   bash scripts/probe-tagging.sh

: "${MAILBOX_TENANT_ID:?}"; : "${MAILBOX_CLIENT_ID:?}"; : "${MAILBOX_CLIENT_SECRET:?}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"
MARKER="JPMS"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}"
EV=(-H "Authorization: Bearer $TOKEN" -H "ConsistencyLevel: eventual")

count() { python3 -c "
import sys,json
try: d=json.load(sys.stdin)
except: print('  (non-JSON / empty)'); sys.exit()
if isinstance(d,dict) and 'error' in d: print('  ERROR:', d['error'].get('code'),'-',(d['error'].get('message') or '')[:120])
else: print('  count:', d.get('@odata.count','-'))
"; }

echo "=== 1. Triage queue: Inbox WITHOUT '$MARKER' marker ==="
curl -s -G "$BASE/mailFolders/inbox/messages" "${EV[@]}" \
  --data-urlencode "\$filter=not categories/any(c:c eq '$MARKER')" \
  --data-urlencode '$top=1' --data-urlencode '$count=true' | count

echo
echo "=== 2. Tagged: Inbox WITH '$MARKER' marker ==="
curl -s -G "$BASE/mailFolders/inbox/messages" "${EV[@]}" \
  --data-urlencode "\$filter=categories/any(c:c eq '$MARKER')" \
  --data-urlencode '$top=1' --data-urlencode '$count=true' | count

echo
echo "=== 3. Tagged emails and their JPMS categories (marker should sit beside a workflow tag) ==="
curl -s -G "$BASE/mailFolders/inbox/messages" "${EV[@]}" \
  --data-urlencode "\$filter=categories/any(c:c eq '$MARKER')" \
  --data-urlencode '$orderby=receivedDateTime desc' \
  --data-urlencode '$select=subject,categories' --data-urlencode '$top=15' | python3 -c "
import sys,json
d=json.load(sys.stdin)
if 'error' in d: print('  ERROR:', d['error'].get('code')); sys.exit()
rows=d.get('value',[])
if not rows: print('  (none tagged yet)')
for m in rows:
    jpms=[c for c in m.get('categories',[]) if c=='JPMS' or c.startswith('JPMS/')]
    print('  -', (m.get('subject') or '(no subject)')[:55], '->', jpms)
"

echo
echo "=== 4. JPMS categories registered in the mailbox master list (coloured in Outlook) ==="
curl -s -G "$BASE/outlook/masterCategories" "${EV[@]}" | python3 -c "
import sys,json
d=json.load(sys.stdin)
if 'error' in d: print('  ERROR:', d['error'].get('code'),'-',(d['error'].get('message') or '')[:120]); sys.exit()
jpms=[(c.get('displayName'),c.get('color')) for c in d.get('value',[]) if (c.get('displayName') or '').startswith('JPMS')]
if not jpms: print('  (no JPMS categories registered yet — tag one email to create them)')
for n,col in jpms: print('  -', n, '('+str(col)+')')
"
