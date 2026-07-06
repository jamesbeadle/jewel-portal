#!/usr/bin/env bash
#
# Where does a message ACTUALLY live? Searches the whole projects@ mailbox for a subject
# and prints each hit's real folder (resolving the full folder path), categories, and sender.
#
# Read-only. GET requests only — writes nothing.
#
# Run (same three MAILBOX_* exports as probe-queue-filter-diagnosis.sh):
#   SUBJECT="1986_6.07_260626" bash scripts/probe-find-message-folder.sh

: "${MAILBOX_TENANT_ID:?set MAILBOX_TENANT_ID}"
: "${MAILBOX_CLIENT_ID:?set MAILBOX_CLIENT_ID}"
: "${MAILBOX_CLIENT_SECRET:?set MAILBOX_CLIENT_SECRET}"
: "${SUBJECT:?set SUBJECT to (part of) the email subject to find}"
MAILBOX="${MAILBOX:-projects@jewelbb.co.uk}"

TOKEN=$(curl -s -X POST "https://login.microsoftonline.com/${MAILBOX_TENANT_ID}/oauth2/v2.0/token" \
  --data-urlencode "client_id=${MAILBOX_CLIENT_ID}" \
  --data-urlencode "client_secret=${MAILBOX_CLIENT_SECRET}" \
  --data-urlencode "scope=https://graph.microsoft.com/.default" \
  --data-urlencode "grant_type=client_credentials" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('access_token',''))" 2>/dev/null)
[ -z "$TOKEN" ] && { echo "TOKEN ERROR"; exit 1; }

GRAPH="https://graph.microsoft.com/v1.0/users/${MAILBOX}"
AUTH=(-H "Authorization: Bearer ${TOKEN}")

# $search spans ALL folders in the mailbox (Deleted Items included).
curl -s -G "${GRAPH}/messages" "${AUTH[@]}" \
  --data-urlencode "\$search=\"subject:${SUBJECT}\"" \
  --data-urlencode '$select=subject,from,receivedDateTime,categories,parentFolderId' \
  --data-urlencode '$top=25' \
  | python3 - "$TOKEN" "$GRAPH" <<'PY'
import sys, json, urllib.request

token, graph = sys.argv[1], sys.argv[2]
d = json.load(sys.stdin)
if 'error' in d:
    print(json.dumps(d['error'], indent=2)); raise SystemExit

folder_cache = {}
def folder_path(fid):
    """Resolve a folder id to its full path by walking parentFolderId upwards."""
    if fid in folder_cache:
        return folder_cache[fid]
    req = urllib.request.Request(
        f"{graph}/mailFolders/{fid}?$select=displayName,parentFolderId",
        headers={"Authorization": f"Bearer {token}"})
    try:
        with urllib.request.urlopen(req) as r:
            f = json.load(r)
    except Exception as e:
        folder_cache[fid] = f"(unresolvable: {e})"
        return folder_cache[fid]
    name, parent = f.get("displayName", "?"), f.get("parentFolderId")
    # Walk up until the parent stops resolving (msgfolderroot etc.).
    path = name
    if parent:
        parent_path = folder_path(parent)
        if not parent_path.startswith("(unresolvable"):
            path = f"{parent_path} / {name}"
    folder_cache[fid] = path
    return path

hits = d.get('value', [])
if not hits:
    print("NOT FOUND anywhere in this mailbox — you are viewing a different mailbox in Outlook.")
for m in hits:
    frm = ((m.get('from') or {}).get('emailAddress') or {}).get('address', '?')
    print(f"  {(m.get('receivedDateTime') or '?')[:16]} | {(m.get('subject') or '')[:60]}")
    print(f"      from:   {frm}")
    print(f"      cats:   {m.get('categories') or []}")
    print(f"      folder: {folder_path(m.get('parentFolderId'))}")
    print()
PY
