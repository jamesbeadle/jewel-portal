#!/usr/bin/env bash
#
# READ-ONLY inventory + performance evidence for the jewel portal Azure estate.
# Changes nothing. Run from the repo root with a logged-in az CLI:
#   bash infra/azure-inventory.sh
# Output lands in infra/.azure-inventory.txt — hand that file to the performance review.
#
set -uo pipefail
RG="${RG:-rg-jpms-prod}"
OUT="infra/.azure-inventory.txt"
exec > "$OUT" 2>&1

section() { echo; echo "=================================================================="; echo "== $1"; echo "=================================================================="; }

section "All resources in $RG"
az resource list -g "$RG" -o table || true

section "App Service plans (SKU / workers)"
az appservice plan list -g "$RG" \
  --query "[].{name:name, sku:sku.name, tier:sku.tier, capacity:sku.capacity, linux:reserved, status:status}" -o table || true

section "Function apps"
az functionapp list -g "$RG" --query "[].{name:name, state:state, plan:appServicePlanId}" -o table || true
for app in $(az functionapp list -g "$RG" --query "[].name" -o tsv); do
  echo "--- $app config"
  az functionapp config show -g "$RG" -n "$app" \
    --query "{alwaysOn:alwaysOn, linuxFxVersion:linuxFxVersion, use32Bit:use32BitWorkerProcess, http20:http20Enabled, preWarmed:preWarmedInstanceCount}" -o table || true
done

section "Static Web Apps (SKU)"
az staticwebapp list -g "$RG" --query "[].{name:name, sku:sku.name, tier:sku.tier, host:defaultHostname}" -o table || true

section "SQL servers + databases (tier / size)"
for srv in $(az sql server list -g "$RG" --query "[].name" -o tsv); do
  echo "--- server: $srv"
  az sql db list -g "$RG" -s "$srv" \
    --query "[].{name:name, sku:currentSku.name, tier:currentSku.tier, capacity:currentSku.capacity, maxSizeBytes:maxSizeBytes, status:status}" -o table || true
done

section "Storage accounts"
az storage account list -g "$RG" --query "[].{name:name, sku:sku.name, kind:kind, accessTier:accessTier}" -o table || true

section "SQL metrics - last 7 days"
for srv in $(az sql server list -g "$RG" --query "[].name" -o tsv); do
  for db in $(az sql db list -g "$RG" -s "$srv" --query "[?name!='master'].name" -o tsv); do
    id=$(az sql db show -g "$RG" -s "$srv" -n "$db" --query id -o tsv)
    echo "--- $srv/$db  (DTU model)"
    az monitor metrics list --resource "$id" --metric dtu_consumption_percent \
      --interval PT6H --offset 7d --aggregation Average Maximum -o table 2>/dev/null || true
    echo "--- $srv/$db  (vCore model)"
    az monitor metrics list --resource "$id" --metric cpu_percent \
      --interval PT6H --offset 7d --aggregation Average Maximum -o table 2>/dev/null || true
    echo "--- $srv/$db  storage"
    az monitor metrics list --resource "$id" --metric storage_percent \
      --interval PT12H --offset 7d --aggregation Maximum -o table 2>/dev/null || true
  done
done

section "Function app metrics - last 3 days"
for app in $(az functionapp list -g "$RG" --query "[].name" -o tsv); do
  id=$(az functionapp show -g "$RG" -n "$app" --query id -o tsv)
  echo "--- $app response time"
  az monitor metrics list --resource "$id" --metric HttpResponseTime \
    --interval PT6H --offset 3d --aggregation Average Maximum -o table 2>/dev/null || true
  echo "--- $app requests / errors"
  az monitor metrics list --resource "$id" --metric Requests Http5xx \
    --interval PT6H --offset 3d --aggregation Total -o table 2>/dev/null || true
  echo "--- $app memory"
  az monitor metrics list --resource "$id" --metric MemoryWorkingSet \
    --interval PT6H --offset 3d --aggregation Average Maximum -o table 2>/dev/null || true
done

echo
echo "DONE - hand infra/.azure-inventory.txt back for the review."
