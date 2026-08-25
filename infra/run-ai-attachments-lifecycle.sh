#!/usr/bin/env bash
# ============================================================================
# Sets the retention rule for the assistant's chat attachments (PROD).
#
# Files attached to the Jewel Assistant chat are kept as bytes in the
# "ai-attachments" blob container so any part of them can be re-read on demand
# (docs/ai/06-context-retrieval.md). Nothing in the app deletes them —
# conversations are never deleted either — so this lifecycle rule is the
# retention policy: blobs under ai-attachments/ are deleted 90 days after they
# were last modified. A read after that gets a plain "the file is no longer
# held — attach it again" from the assistant.
#
# Reads STORAGE_ACCOUNT and RESOURCE_GROUP from infra/.azure-prod-output.env
# (written by azure-prod-setup-v2.sh). Safe to re-run: the policy is replaced,
# not appended — and it only carries this one rule, so if the account ever
# gains other lifecycle rules, fold this one into them rather than running it.
#
# Usage:  bash infra/run-ai-attachments-lifecycle.sh [days]   (default 90)
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
DAYS="${1:-90}"

[[ -f "$ENV_FILE" ]] || { echo "Missing $ENV_FILE"; exit 1; }
# shellcheck disable=SC1090
source "$ENV_FILE"
: "${STORAGE_ACCOUNT:?STORAGE_ACCOUNT missing from $ENV_FILE}"
: "${RESOURCE_GROUP:?RESOURCE_GROUP missing from $ENV_FILE}"

echo "Setting a ${DAYS}-day delete rule on ai-attachments/ in ${STORAGE_ACCOUNT} (${RESOURCE_GROUP})..."

POLICY="$(mktemp)"
cat > "$POLICY" <<JSON
{
  "rules": [
    {
      "enabled": true,
      "name": "ai-attachments-${DAYS}d",
      "type": "Lifecycle",
      "definition": {
        "filters": { "blobTypes": [ "blockBlob" ], "prefixMatch": [ "ai-attachments/" ] },
        "actions": { "baseBlob": { "delete": { "daysAfterModificationGreaterThan": ${DAYS} } } }
      }
    }
  ]
}
JSON

az storage account management-policy create \
  --account-name "${STORAGE_ACCOUNT}" \
  --resource-group "${RESOURCE_GROUP}" \
  --policy @"$POLICY" \
  --output table

rm -f "$POLICY"
echo "Done."
