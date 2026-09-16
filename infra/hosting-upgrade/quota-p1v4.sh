#!/usr/bin/env bash
# DIAGNOSTIC ONLY — shows the P1v4 (Premium v4) figures Azure reports for North Europe.
# It does NOT request anything. Safe to re-run at any time.
#
# Why no request step: on ticket 2609040050000920 Microsoft explained (16 Sep 2026) that
# App Service's own per-subscription Premium v4 allowance is what plan create enforces
# ("Current Limit (P1v4 VMs): 0"), and the only ways to raise it are
#   1. Portal: Quotas -> App Service (Public Preview) -> North Europe -> Premium v4 / P1v4 -> Edit
#      New Limit = the TOTAL wanted, above the current figure shown; or
#   2. "Create a Support Request" from that same page (routes to the Capacity team).
# `az quota` talks to the Microsoft.Quota service, which reports a different number (30) and
# whose request call is rejected/ignored for this SKU — so it is kept here only to read.
#
# Use:  bash infra/hosting-upgrade/quota-p1v4.sh
# Pass: the plan-create test at the end prints "QUOTA OK" — then run phase1-provision.sh.
set -o pipefail
SUB=08c5510c-bb27-4da8-b826-a8e76fb270ec
SCOPE="/subscriptions/$SUB/providers/Microsoft.Web/locations/northeurope"
RG=rg-jpms-prod

az account set --subscription "$SUB" -o none || { echo "STOPPED - az login first (admin.james@jewelenterprises.co.uk)"; exit 1; }
az extension add --name quota --upgrade --only-show-errors 2>/dev/null

echo "## 1. Microsoft.Quota view (informational only — NOT what plan create enforces)"
az quota show --resource-name P1v4 --scope "$SCOPE" --query "properties.{limit:limit.value,usage:currentValue,unit:unit}" -o json 2>&1 | head -20

echo "## 2. open quota requests on this scope (if any)"
az quota request list --scope "$SCOPE" --query "[].{state:properties.provisioningState,msg:properties.message,when:properties.requestSubmitTime}" -o table 2>/dev/null | tail -n +3 | tail -5

echo "## 3. what App Service actually enforces — a dry plan create (nothing is created)"
# --no-wait is not a dry run, so we probe with a validation-only ARM deployment.
tmp=$(mktemp); trap 'rm -f "$tmp"' EXIT
cat > "$tmp" <<'JSON'
{"$schema":"https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#","contentVersion":"1.0.0.0",
 "resources":[{"type":"Microsoft.Web/serverfarms","apiVersion":"2023-12-01","name":"quota-probe-delete-me","location":"northeurope",
 "kind":"linux","sku":{"name":"P1v4","tier":"PremiumV4","capacity":1},"properties":{"reserved":true}}]}
JSON
if out=$(az deployment group validate -g "$RG" --template-file "$tmp" -o none 2>&1); then
  echo "QUOTA OK - validation passed (this checks quota on most, not all, SKUs). Now run:  bash infra/hosting-upgrade/phase1-provision.sh"
else
  echo "$out" | grep -iE "quota|limit" || echo "$out" | tail -5
  echo "QUOTA STILL BLOCKED - raise/chase it in the portal (see header). Do not run phase 1 yet."
fi
