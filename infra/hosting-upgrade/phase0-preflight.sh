#!/usr/bin/env bash
# JPMS hosting upgrade — Phase 0: pre-flight (read-only checks, exact prices, rollback snapshots).
# Transcribed verbatim from JPMS-hosting-upgrade-runbook.pdf (4 Sep 2026), pages 2–3.
# Cost: none. Downtime: none. Creates only files under ~/jpms-upgrade.
# Run from your Mac after `az login`:   bash infra/hosting-upgrade/phase0-preflight.sh

( set -eo pipefail
mkdir -p ~/jpms-upgrade && chmod 700 ~/jpms-upgrade && cd ~/jpms-upgrade
cat > vars.sh <<'EOF'
RG=rg-jpms-prod
SUB=08c5510c-bb27-4da8-b826-a8e76fb270ec
LOC=northeurope
SWA=swa-jpms-prod
PLAN=plan-jpms-prod
FUNC=func-jpms-api-prod
AI=appi-jpms-prod
SQLSRV=sql-jpms-prod-54cf9e
DOCS_STG=stjpmsprod54cf9e
BUDGET_EMAIL=nigel.reilly@jewelgroup.co.uk
EOF
source vars.sh
command -v jq >/dev/null || brew install jq
echo "## CLI version (want 2.60 or newer)"; az version --query '"azure-cli"' -o tsv
az account set --subscription $SUB && az account show --query "{subscription:name,signedInAs:user.name}" -o table
az extension add --name application-insights --upgrade --only-show-errors
echo "## Static Web App tier - linked backends need Standard"
az staticwebapp show -n $SWA -g $RG --query "{sku:sku.name,stagingEnvironments:stagingEnvironmentPolicy,linkedBackends:linkedBackends}" -o json
echo "## exact list prices, GBP, North Europe - nothing is created yet"
curl -sG "https://prices.azure.com/api/retail/prices" --data-urlencode "currencyCode='GBP'" \
  --data-urlencode "\$filter=armRegionName eq 'northeurope' and serviceName eq 'Azure App Service' and priceType eq 'Consumption' and (contains(skuName,'v3') or contains(skuName,'v4') or skuName eq 'B1')" \
  | jq -r '.Items[] | select(.skuName | test("^(P0 ?v3|P1 ?v3|P0 ?v4|P1 ?v4|B1)$")) | "\(.productName) | \(.skuName) | £\(.unitPrice)/hour | ≈ £\((.unitPrice*730*100|round)/100)/month"' | sort
echo "## is Premium v4 Linux offered to this subscription in North Europe? (want: North Europe)"
az appservice list-locations --sku P1V4 --linux-workers-enabled --query "[?contains(name,'North Europe')].name" -o tsv
echo "## names free?"
az appservice plan show -n $PLAN -g $RG --query name -o tsv 2>/dev/null && echo "PLAN EXISTS - stop" || echo "$PLAN is free"
az functionapp show -n $FUNC -g $RG --query name -o tsv 2>/dev/null && echo "FUNC EXISTS - stop" || echo "$FUNC is free"
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
