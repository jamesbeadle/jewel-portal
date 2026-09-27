#!/usr/bin/env bash
# JPMS hosting upgrade — Step 1: create the Flex Consumption app and copy the portal's settings onto it.
# Flex Consumption edition (27 Sep 2026), replacing the P1v4 plan Microsoft refused on capacity.
# Cost: nothing bills until always-ready is switched on in Step 4. Downtime: none. Production is untouched.
# Safe to re-run: reuses whatever already exists. Requires Phase 0 to have passed (creates ~/jpms-upgrade/vars.sh).
# Fallback: if the create is refused in North Europe, run  FLEX_LOC=uksouth STG=stjpmsapiuks bash infra/hosting-upgrade/phase1-provision.sh
# Run:   bash infra/hosting-upgrade/phase1-provision.sh

cd ~/jpms-upgrade && source vars.sh
FLEX_LOC="${FLEX_LOC:-northeurope}"
( set -eo pipefail
setvar() { [ -n "$2" ] || { echo "refusing to record an empty $1"; return 1; }; grep -v "^$1=" vars.sh > vars.tmp; echo "$1=$2" >> vars.tmp; mv vars.tmp vars.sh; }
setvar FLEX_LOC "$FLEX_LOC"; setvar STG "$STG"
setvar LOC "$FLEX_LOC"; echo "## LOC=$LOC - the runbook's link commands (--backend-region \$LOC) now follow the region the app is created in"
echo "## runtime storage account $STG in $FLEX_LOC (Flex needs it in the app's own region)"
az storage account show -n $STG -g $RG -o none 2>/dev/null || az storage account create -n $STG -g $RG -l $FLEX_LOC --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 --allow-blob-public-access false -o none
trap 'rm -f api-settings.json' EXIT
site() { az resource show -g $RG -n $FUNC --resource-type Microsoft.Web/sites --query "$1" -o "$2"; }
echo "## the Flex Consumption app - THIS is the capacity test; a refusal here means try the fallback region"
site id tsv >/dev/null 2>&1 || az functionapp create -g $RG -n $FUNC --storage-account $STG --flexconsumption-location $FLEX_LOC --runtime dotnet-isolated --runtime-version 8.0 --instance-memory 2048 --app-insights $AI --https-only true -o none
echo "## read through Resource Manager: on CLI 2.86 'az functionapp show' returns a Flex app with no state or hostname"
FUNC_ID=$(site id tsv); setvar FUNC_ID "$FUNC_ID"; echo "$FUNC_ID"
APIHOST=$(site properties.defaultHostName tsv); setvar APIHOST "$APIHOST"; echo "direct hostname: $APIHOST"
echo "## copy the portal API settings from the SWA (values never printed; ':' becomes '__')"
az staticwebapp appsettings list -n $SWA -g $RG -o json \
  | jq '[.properties | to_entries[] | select(.key | test("^(AzureWebJobsStorage|FUNCTIONS_|WEBSITE_|APPINSIGHTS_|APPLICATIONINSIGHTS_)") | not) | {name:(.key|gsub(":";"__")), value:.value, slotSetting:false}]' \
  > api-settings.json && chmod 600 api-settings.json
echo "## settings being copied (names only):"; jq -r '.[].name' api-settings.json | sort | tr '\n' ' '; echo
az functionapp config appsettings set -n $FUNC -g $RG --settings @api-settings.json -o none && rm -f api-settings.json
az functionapp config set -n $FUNC -g $RG --min-tls-version 1.2 --http20-enabled true -o none
echo "## GATE 1 - want Running, a hostname, https true, sku FlexConsumption, 2048 MB"
site "{state:properties.state,host:properties.defaultHostName,https:properties.httpsOnly,sku:properties.sku,location:location,memoryMB:properties.functionAppConfig.scaleAndConcurrency.instanceMemoryMB}" json
echo "## GATE 1 - want the 24 portal names, MailboxIntake__* with double underscores"
az functionapp config appsettings list -n $FUNC -g $RG --query "[].name" -o tsv | sort | tr '\n' ' '; echo
echo "## recorded in vars.sh for Steps 2-4:"; grep -E "^(FUNC_ID|APIHOST|FLEX_LOC|STG)=" vars.sh
); rc=$?; if [ $rc -eq 0 ]; then echo "BLOCK COMPLETE"; else echo "STOPPED AT THE FIRST ERROR ABOVE (exit $rc) - do not continue; paste everything to Claude"; fi
