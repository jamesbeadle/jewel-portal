#!/usr/bin/env bash
#
# Find EVERY message in the whole mailbox (all folders, not just Inbox) that carries a JPMS tag, and
# show its subject, sender, received time and folder. Settles "the app shows 1 but Outlook shows 2":
# if this returns 1, Outlook is drawing a phantom pill; if it returns 2+, a tagged email is sitting in
# another folder. GET only — writes nothing.
#
#   export MAILBOX_TENANT_ID="..."; export MAILBOX_CLIENT_ID="..."; export MAILBOX_CLIENT_SECRET="..."
#   bash scripts/probe-tagged-anywhere.sh

: "${MAILBOX_TENANT_ID:?}"; : "${MAILBOX_CLIENT_ID:?}"; : "${MAILBOX_CLIENT_SECRET:?}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}"
EV=(-H "Authorization: Bearer $TOKEN" -H "ConsistencyLevel: eventual")

# Build a folder-id -> name map so we can show WHERE each tagged message lives.
FOLDERS=$(curl -s -G "$BASE/mailFolders" "${EV[@]}" \
  --data-urlencode '$top=200' --data-urlencode '$select=id,displayName' | python3 -c "
import sys,json
d=json.load(sys.stdin);
print(json.dumps({f['id']:f.get('displayName','?') for f in d.get('value',[])}))
" 2>/dev/null)

show() { python3 -c "
import sys,json
folders=json.loads('''$FOLDERS''') if '''$FOLDERS''' else {}
d=json.load(sys.stdin)
if 'error' in d: print('  ERROR:', d['error'].get('code'),'-',(d['error'].get('message') or '')[:120]); sys.exit()
print('  count:', d.get('@odata.count','-'))
for m in d.get('value',[]):
    frm=((m.get('from') or {}).get('emailAddress') or {}).get('address','?')
    fid=m.get('parentFolderId','')
    print('  -', (m.get('subject') or '')[:40], '| from', frm, '| folder:', folders.get(fid,'(id '+fid[:8]+'…)'))
    print('      categories:', m.get('categories'), '| received', m.get('receivedDateTime'))
"; }

echo "=== Whole mailbox: messages tagged 'JPMS/Discarded' (all folders) ==="
curl -s -G "$BASE/messages" "${EV[@]}" \
  --data-urlencode "\$filter=categories/any(c:c eq 'JPMS/Discarded')" \
  --data-urlencode '$select=subject,from,receivedDateTime,categories,parentFolderId' \
  --data-urlencode '$top=25' --data-urlencode '$count=true' | show

echo
echo "=== Whole mailbox: messages with the 'JPMS' marker (all folders) ==="
curl -s -G "$BASE/messages" "${EV[@]}" \
  --data-urlencode "\$filter=categories/any(c:c eq 'JPMS')" \
  --data-urlencode '$select=subject,from,receivedDateTime,categories,parentFolderId' \
  --data-urlencode '$top=25' --data-urlencode '$count=true' | show
