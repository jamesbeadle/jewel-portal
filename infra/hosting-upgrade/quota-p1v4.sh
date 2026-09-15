#!/usr/bin/env bash
# Requests App Service quota: Premium v4 P1v4 (non-zone-redundant), North Europe, limit 2.
# Self-service via Microsoft.Quota (what Azure support pointed us at on ticket 2609040050000920).
# Read-only apart from the quota request itself. Safe to re-run.
set -o pipefail
SUB=08c5510c-bb27-4da8-b826-a8e76fb270ec
SCOPE="/subscriptions/$SUB/providers/Microsoft.Web/locations/northeurope"

az extension add --name quota --upgrade --only-show-errors 2>/dev/null

echo "## 1. Microsoft.Quota provider registration"
if [ "$(az provider show --namespace Microsoft.Quota --query registrationState -o tsv)" != "Registered" ]; then
  az provider register --namespace Microsoft.Quota -o none
fi
until [ "$(az provider show --namespace Microsoft.Quota --query registrationState -o tsv)" = "Registered" ]; do echo "   registering..."; sleep 15; done
echo "   Registered"

echo "## 2. current P1v4 limit in North Europe (retrying while registration propagates)"
ok=""
for i in $(seq 1 12); do
  if out=$(az quota show --resource-name P1v4 --scope "$SCOPE" --query "properties.{limit:limit.value,usage:currentValue}" -o json 2>&1); then echo "$out"; ok=1; break; fi
  echo "   not visible yet (try $i of 12): $(echo "$out" | head -1)"; sleep 20
done
[ -n "$ok" ] || { echo "STOPPED - quota API still not answering after 4 min; paste this to Claude"; exit 1; }

echo "## 3. requesting P1v4 limit 2"
az quota create --resource-name P1v4 --scope "$SCOPE" --limit-object value=2 limit-object-type=LimitValue -o json \
  || { echo "STOPPED - quota request refused; paste this to Claude"; exit 1; }

echo "## 4. waiting for the new limit to show"
limit=0
for i in $(seq 1 15); do
  limit=$(az quota show --resource-name P1v4 --scope "$SCOPE" --query properties.limit.value -o tsv 2>/dev/null || echo 0)
  echo "   limit now: ${limit:-0}"
  if [ "${limit:-0}" -ge 1 ] 2>/dev/null; then break; fi
  az quota request list --scope "$SCOPE" --query "[].{state:properties.provisioningState,msg:properties.message}" -o table 2>/dev/null | tail -n +3 | tail -3
  sleep 20
done

if [ "${limit:-0}" -ge 1 ] 2>/dev/null; then
  echo "QUOTA OK (P1v4 limit = $limit). Now run:  bash infra/hosting-upgrade/phase1-provision.sh"
else
  echo "QUOTA STILL 0 after 5 min - paste this whole output to Claude"
fi
