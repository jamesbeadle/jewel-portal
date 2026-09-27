#!/usr/bin/env bash
# JPMS hosting upgrade — Phase 0: pre-flight (read-only checks and rollback snapshots).
# Flex Consumption edition (27 Sep 2026); the P1v4 checks are gone with the P1v4 route.
# Cost: none. Downtime: none. Creates only files under ~/jpms-upgrade.
# Run from your Mac after `az login`:   bash infra/hosting-upgrade/phase0-preflight.sh

( set -eo pipefail
mkdir -p ~/jpms-upgrade && chmod 700 ~/jpms-upgrade && cd ~/jpms-upgrade
[ -f vars.sh ] && cp vars.sh vars.previous.sh
cat > vars.sh <<'VARS'
RG=rg-jpms-prod
SUB=08c5510c-bb27-4da8-b826-a8e76fb270ec
LOC=northeurope
FLEX_LOC=northeurope
SWA=swa-jpms-prod
FUNC=func-jpms-api-prod
AI=appi-jpms-prod
SQLSRV=sql-jpms-prod-54cf9e
DOCS_STG=stjpmsprod54cf9e
STG=stjpmsapi69e23c
BUDGET_EMAIL=nigel.reilly@jewelgroup.co.uk
VARS
source vars.sh
command -v jq >/dev/null || brew install jq
echo "## CLI version (want 2.71 or newer for Flex Consumption commands)"; az version --query '"azure-cli"' -o tsv
az account set --subscription $SUB && az account show --query "{subscription:name,signedInAs:user.name}" -o table
az extension add --name application-insights --upgrade --only-show-errors
echo "## Static Web App tier - linked backends need Standard"
az staticwebapp show -n $SWA -g $RG --query "{sku:sku.name,stagingEnvironments:stagingEnvironmentPolicy,linkedBackends:linkedBackends}" -o json
echo "## Flex Consumption regions offered to this subscription (want North Europe; UK South is the fallback)"
az functionapp list-flexconsumption-locations -o table | grep -iE "north europe|northeurope|uk south|uksouth" || echo "NEITHER REGION LISTED - stop"
echo "## Flex Consumption meters, GBP, North Europe (informational; always-ready is the standing cost)"
curl -sG "https://prices.azure.com/api/retail/prices" --data-urlencode "currencyCode='GBP'" \
  --data-urlencode "\$filter=armRegionName eq '$FLEX_LOC' and serviceName eq 'Functions' and contains(productName,'Flex')" \
  | jq -r '.Items[] | "\(.meterName) | £\(.unitPrice) per \(.unitOfMeasure)"' | sort -u || echo "(price lookup unavailable - not a blocker)"
echo "## the runtime storage account Phase 1.1 created on 15 Sep (want: exists, in $FLEX_LOC)"
az storage account show -n $STG -g $RG --query "{name:name,location:location,sku:sku.name}" -o json 2>/dev/null || echo "$STG does not exist - phase1 will create it"
echo "## names free?"
az functionapp show -n $FUNC -g $RG --query name -o tsv 2>/dev/null && echo "FUNC EXISTS - phase1 will reuse it" || echo "$FUNC is free"
az appservice plan show -n plan-jpms-prod -g $RG --query name -o tsv 2>/dev/null && echo "plan-jpms-prod EXISTS - a P1v4 leftover, delete it" || echo "no P1v4 leftover plan"
echo "## SQL: tier (vCore tiers allow 35-day restore) and a firewall rule that lets Azure services in (0.0.0.0)"
az sql db show -g $RG -s $SQLSRV -n jpms --query "{tier:sku.tier,sku:sku.name}" -o json
az sql server firewall-rule list -g $RG -s $SQLSRV --query "[].{name:name,start:startIpAddress,end:endIpAddress}" -o table
echo "## snapshot for rollback"
az staticwebapp show -n $SWA -g $RG -o json > swa-before.json
az staticwebapp appsettings list -n $SWA -g $RG -o json > swa-appsettings-before.json && chmod 600 swa-appsettings-before.json
az staticwebapp backends show -n $SWA -g $RG -o json > swa-backends-before.json 2>&1 || true
echo "## current budgets - resource-group scope, then subscription scope"
az consumption budget list -g $RG --query "[].{name:name,amount:amount,grain:timeGrain}" -o table 2>/dev/null || echo "(none at resource-group scope)"
az consumption budget list --query "[].{name:name,amount:amount,grain:timeGrain}" -o table 2>/dev/null || echo "(none at subscription scope)"
ls -la ~/jpms-upgrade
); rc=$?; if [ $rc -eq 0 ]; then echo "BLOCK COMPLETE"; else echo "STOPPED AT THE FIRST ERROR ABOVE (exit $rc) - do not continue; paste everything to Claude"; fi
