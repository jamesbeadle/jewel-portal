#!/usr/bin/env bash
#
# GROUND TRUTH: read the actual categories on inbox messages directly — no $filter, no eventual
# consistency, no index. Whatever this prints is exactly what's on the message right now. Shows every
# message carrying any 'JPMS' category, with its FULL category list. GET only — writes nothing.
#
#   export MAILBOX_TENANT_ID="..."; export MAILBOX_CLIENT_ID="..."; export MAILBOX_CLIENT_SECRET="..."
#   bash scripts/probe-categories-raw.sh

: "${MAILBOX_TENANT_ID:?}"; : "${MAILBOX_CLIENT_ID:?}"; : "${MAILBOX_CLIENT_SECRET:?}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}"

echo "=== Raw categories on the 60 most recent inbox messages (no filter / no eventual) ==="
echo "    Listing only messages that carry at least one 'JPMS' category:"
curl -s -G "$BASE/mailFolders/inbox/messages" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode '$select=subject,categories' \
  --data-urlencode '$orderby=receivedDateTime desc' \
  --data-urlencode '$top=60' | python3 -c "
import sys,json
d=json.load(sys.stdin)
if 'error' in d:
    print('  ERROR:', d['error'].get('code'),'-',(d['error'].get('message') or '')[:140]); sys.exit()
rows=d.get('value',[])
hit=False
for m in rows:
    cats=m.get('categories') or []
    jpms=[c for c in cats if c=='JPMS' or c.startswith('JPMS')]
    if jpms:
        hit=True
        print('  -', (m.get('subject') or '(no subject)')[:55])
        print('      ALL categories:', cats)
if not hit:
    print('  (no message in the 60 most recent carries any JPMS category)')
print()
print('  scanned', len(rows), 'messages')
"

echo
echo "=== Cross-check: exact filter the app's Discarded tab uses (eq 'JPMS/Discarded', eventual) ==="
curl -s -G "$BASE/mailFolders/inbox/messages" \
  -H "Authorization: Bearer $TOKEN" -H "ConsistencyLevel: eventual" \
  --data-urlencode "\$filter=categories/any(c:c eq 'JPMS/Discarded')" \
  --data-urlencode '$select=subject,categories' --data-urlencode '$top=10' --data-urlencode '$count=true' | python3 -c "
import sys,json
d=json.load(sys.stdin)
if 'error' in d: print('  ERROR:', d['error'].get('code')); sys.exit()
print('  count:', d.get('@odata.count','-'))
for m in d.get('value',[]):
    print('  -', (m.get('subject') or '')[:55], '->', m.get('categories'))
"
