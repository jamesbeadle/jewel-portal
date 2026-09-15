#!/usr/bin/env bash
# JPMS hosting upgrade — Phase 1.1: provision the plan and the Function App.
# Transcribed verbatim from JPMS-hosting-upgrade-runbook.pdf (4 Sep 2026), pages 4–5.
# Cost: P1v4 plan starts billing here (£0.1288/hour = £94.02/month, cancel any time) + a storage account (<£1).
# Downtime: none. Nothing existing is modified. Safe to re-run: reuses what already exists.
# Requires Phase 0 to have passed (creates ~/jpms-upgrade/vars.sh).
# Run:   bash infra/hosting-upgrade/phase1-provision.sh

cd ~/jpms-upgrade && source vars.sh
( set -eo pipefail
setvar() { [ -n "$2" ] || { echo "refusing to record an empty $1"; return 1; }; grep -v "^$1=" vars.sh > vars.tmp; echo "$1=$2" >> vars.tmp; mv vars.tmp vars.sh; }
[ -n "$STG" ] || { STG=stjpmsapi$(openssl rand -hex 3); setvar STG "$STG"; }; echo "runtime storage account: $STG"
az storage account show -n $STG -g $RG -o none 2>/dev/null || az storage account create -n $STG -g $RG -l $LOC --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 --allow-blob-public-access false -o none
trap 'rm -f api-settings.json' EXIT
az appservice plan show -n $PLAN -g $RG -o none 2>/dev/null || az appservice plan create -n $PLAN -g $RG -l $LOC --sku P1V4 --is-linux -o none
az appservice plan show -n $PLAN -g $RG --query "{sku:sku.name,tier:sku.tier,instances:sku.capacity,linux:reserved,location:location,status:status}" -o json
az functionapp show -n $FUNC -g $RG -o none 2>/dev/null || az functionapp create -n $FUNC -g $RG --plan $PLAN --storage-account $STG --functions-version 4 --runtime dotnet-isolated --runtime-version 8 --app-insights $AI --https-only true -o none
FUNC_ID=$(az functionapp show -n $FUNC -g $RG --query id -o tsv); setvar FUNC_ID "$FUNC_ID"; echo "$FUNC_ID"
APIHOST=$(az functionapp show -n $FUNC -g $RG --query defaultHostName -o tsv); setvar APIHOST "$APIHOST"; echo "direct hostname: $APIHOST"
echo "## copy the portal API settings from the SWA (values never printed)"
az staticwebapp appsettings list -n $SWA -g $RG -o json \
  | jq '[.properties | to_entries[] | select(.key | test("^(AzureWebJobsStorage|FUNCTIONS_|WEBSITE_|APPINSIGHTS_|APPLICATIONINSIGHTS_)") | not) | {name:(.key|gsub(":";"__")), value:.value, slotSetting:false}]' \
  > api-settings.json && chmod 600 api-settings.json
echo "## settings being copied (names only):"; jq -r '.[].name' api-settings.json | sort | tr '\n' ' '; echo
az functionapp config appsettings set -n $FUNC -g $RG --settings @api-settings.json -o none && rm -f api-settings.json
az functionapp config appsettings set -n $FUNC -g $RG --settings WEBSITE_WARMUP_PATH=/api/version -o none
az functionapp config set -n $FUNC -g $RG --always-on true --http20-enabled true --min-tls-version 1.2 --ftps-state Disabled -o none
az functionapp config set -n $FUNC -g $RG --generic-configurations '{"healthCheckPath":"/api/version"}' -o none
echo "## verify"
az functionapp config show -n $FUNC -g $RG --query "{alwaysOn:alwaysOn,runtime:linuxFxVersion,http20:http20Enabled,minTls:minTlsVersion,healthCheck:healthCheckPath,ftps:ftpsState}" -o json
az functionapp show -n $FUNC -g $RG --query "{state:state,host:defaultHostName,httpsOnly:httpsOnly}" -o json
echo "## publish-profile deploys need SCM basic auth - want true"
az resource show -g $RG --name scm --namespace Microsoft.Web --resource-type basicPublishingCredentialsPolicies --parent sites/$FUNC --query properties.allow -o tsv
echo "## every setting now on the new app (names only):"; az functionapp config appsettings list -n $FUNC -g $RG --query "[].name" -o tsv | sort | tr '\n' ' '; echo
); rc=$?; if [ $rc -eq 0 ]; then echo "BLOCK COMPLETE"; else echo "STOPPED AT THE FIRST ERROR ABOVE (exit $rc) - do not continue; paste everything to Claude"; fi
