#!/usr/bin/env bash
#
# Diagnose why the triage queue renders empty while untagged mail sits in the Inbox.
#
# Reproduces the EXACT production query MailboxGraphClient.ListInboxAsync sends
# (negated marker filter + $orderby + $skip + $top + $count, ConsistencyLevel eventual,
# Prefer IdType=ImmutableId) and prints the RAW Graph response — production swallows
# errors and shows an empty queue, this script does not.
#
# Read-only. GET requests only — writes nothing.
#
# Run:
#   export MAILBOX_TENANT_ID="<tenant id>"
#   export MAILBOX_CLIENT_ID="<app client id>"
#   export MAILBOX_CLIENT_SECRET="<app client secret>"
#   bash scripts/probe-queue-filter-diagnosis.sh

: "${MAILBOX_TENANT_ID:?set MAILBOX_TENANT_ID}"
: "${MAILBOX_CLIENT_ID:?set MAILBOX_CLIENT_ID}"
: "${MAILBOX_CLIENT_SECRET:?set MAILBOX_CLIENT_SECRET}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"
MARKER="${MARKER:-JPMS}"   # the bare marker, NOT JPMS/Triaged

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" \
  --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" \
  --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

BASE="https://graph.microsoft.com/v1.0/users/${MAILBOX}/mailFolders/inbox/messages"
H=(-H "Authorization: Bearer ${TOKEN}" -H "ConsistencyLevel: eventual" -H 'Prefer: IdType="ImmutableId"')
SUMMARY='id,subject,from,receivedDateTime,categories'

show() { # $1 = label, remaining args = curl query params
  local label="$1"; shift
  echo "=== ${label} ==="
  local body status
  body=$(curl -s -w '\n%{http_code}' -G "$BASE" "${H[@]}" "$@")
  status="${body##*$'\n'}"
  body="${body%$'\n'*}"
  echo "HTTP ${status}"
  echo "$body" | python3 -c "
import sys, json
try: d = json.load(sys.stdin)
except Exception: print(sys.stdin.read()); raise SystemExit
if 'error' in d:
    print(json.dumps(d['error'], indent=2)); raise SystemExit
print('@odata.count:', d.get('@odata.count', '(absent)'))
for m in d.get('value', []):
    frm = ((m.get('from') or {}).get('emailAddress') or {}).get('address', '?')
    print('  %s | %-40s | %s | cats=%s' % (
        (m.get('receivedDateTime') or '?')[:16], (m.get('subject') or '')[:40],
        frm, m.get('categories') or []))
"
  echo
}

# 1. Baseline: inbox total, no filter.
show "1. Inbox total (no filter)" \
  --data-urlencode '$top=1' --data-urlencode '$count=true'

# 2. The probe-style negated filter (what was validated in June: no orderby).
show "2. Negated marker filter, NO orderby (probe shape)" \
  --data-urlencode "\$filter=not categories/any(c:c eq '${MARKER}')" \
  --data-urlencode "\$select=${SUMMARY}" \
  --data-urlencode '$top=10' --data-urlencode '$count=true'

# 3. The EXACT production query (ListInboxAsync): negated filter + orderby + skip.
show "3. Negated marker filter + orderby + skip (PRODUCTION shape)" \
  --data-urlencode "\$filter=not categories/any(c:c eq '${MARKER}')" \
  --data-urlencode '$orderby=receivedDateTime asc' \
  --data-urlencode "\$select=${SUMMARY}" \
  --data-urlencode '$top=10' --data-urlencode '$skip=0' --data-urlencode '$count=true'

# 4. Positive marker filter + orderby (the Tagged tab, known working — the control).
show "4. Positive marker filter + orderby (Tagged tab, control)" \
  --data-urlencode "\$filter=categories/any(c:c eq '${MARKER}')" \
  --data-urlencode '$orderby=receivedDateTime asc' \
  --data-urlencode "\$select=${SUMMARY}" \
  --data-urlencode '$top=10' --data-urlencode '$skip=0' --data-urlencode '$count=true'

# 5. Ground truth by subtraction: page the inbox unfiltered, list untagged client-side.
echo "=== 5. Ground truth: untagged inbox messages via client-side subtraction ==="
curl -s -G "$BASE" "${H[@]}" \
  --data-urlencode "\$select=${SUMMARY}" \
  --data-urlencode '$orderby=receivedDateTime desc' \
  --data-urlencode '$top=100' \
  | python3 -c "
import sys, json
d = json.load(sys.stdin)
if 'error' in d:
    print(json.dumps(d['error'], indent=2)); raise SystemExit
untagged = [m for m in d.get('value', []) if '${MARKER}' not in (m.get('categories') or [])]
print('untagged in newest 100:', len(untagged))
for m in untagged:
    frm = ((m.get('from') or {}).get('emailAddress') or {}).get('address', '?')
    print('  %s | %-50s | %s' % ((m.get('receivedDateTime') or '?')[:16], (m.get('subject') or '')[:50], frm))
"
