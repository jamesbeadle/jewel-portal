#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# JPMS production provisioning.
#
# Creates a production-grade environment that is separate from the test setup
# created by azure-setup.sh:
#
#   - Azure SQL Server + Database (Serverless GP_S_Gen5, AUTO-PAUSE DISABLED so
#     there is no cold-start delay, min capacity kept warm, GEO-redundant
#     backups, 35-day point-in-time restore, weekly long-term retention).
#   - Static Web App on the STANDARD SKU (SLA, custom auth, custom domains).
#   - A dedicated production Entra ID app registration.
#   - Application Insights + a monthly cost budget alert.
#   - SqlConnectionString, AAD client id/secret all wired into the SWA settings,
#     so the Functions API recreates the full schema on first run via its
#     built-in EF Core migration step (structure only, no data).
#
# Run it on a machine where the Azure CLI is logged in:
#
#   az login
#   az account set --subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec
#   ./infra/azure-prod-setup.sh
#
# Re-running is safe: every resource is created only if missing.
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_ENV="${SCRIPT_DIR}/.azure-prod-output.env"

if [ -f "${OUTPUT_ENV}" ]; then
  echo "Reusing names from previous run: ${OUTPUT_ENV}"
  # shellcheck disable=SC1090
  source "${OUTPUT_ENV}"
fi

# --- Configurable settings (override via environment variables) -------------
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-jpms-prod}"
SQL_LOCATION="${SQL_LOCATION:-northeurope}"
SWA_LOCATION="${SWA_LOCATION:-westeurope}"
SQL_SERVER="${SQL_SERVER:-sql-jpms-prod-$(openssl rand -hex 3)}"
SQL_DATABASE="${SQL_DATABASE:-jpms}"
SWA_NAME="${SWA_NAME:-swa-jpms-prod}"
ENTRA_APP_NAME="${ENTRA_APP_NAME:-entra-jpms-prod}"
SQL_ADMIN_USER="${SQL_ADMIN_USER:-jpmsadmin}"
SQL_ADMIN_PASSWORD="${SQL_ADMIN_PASSWORD:-$(openssl rand -base64 24 | tr -d '/+=' | cut -c1-20)Aa1!}"
APP_INSIGHTS_NAME="${APP_INSIGHTS_NAME:-appi-jpms-prod}"

# Production SQL sizing. Auto-pause disabled (-1) eliminates cold start.
SQL_MIN_CAPACITY="${SQL_MIN_CAPACITY:-0.5}"   # vCores kept continuously warm
SQL_MAX_CAPACITY="${SQL_MAX_CAPACITY:-4}"     # burst ceiling
SQL_BACKUP_REDUNDANCY="${SQL_BACKUP_REDUNDANCY:-Geo}"
SQL_PITR_DAYS="${SQL_PITR_DAYS:-35}"          # short-term (point-in-time) retention

# Monthly budget alert (GBP). Alerts only — never blocks resources.
BUDGET_AMOUNT="${BUDGET_AMOUNT:-150}"
BUDGET_ALERT_EMAIL="${BUDGET_ALERT_EMAIL:-nigel.reilly@jewelgroup.co.uk}"

# Test SQL server to retire after prod is verified (left in place by default).
TEST_SQL_SERVER="${TEST_SQL_SERVER:-}"
TEST_RESOURCE_GROUP="${TEST_RESOURCE_GROUP:-rg-jpms-test}"

CLIENT_IP="$(curl -s https://api.ipify.org)"

echo "=============================================================="
echo "  JPMS PRODUCTION provisioning"
echo "=============================================================="
echo "Resource group:    ${RESOURCE_GROUP}"
echo "SQL server:        ${SQL_SERVER}.database.windows.net"
echo "SQL database:      ${SQL_DATABASE} (Serverless GP_S_Gen5, auto-pause OFF)"
echo "  min/max vCores:  ${SQL_MIN_CAPACITY} / ${SQL_MAX_CAPACITY}"
echo "  backups:         ${SQL_BACKUP_REDUNDANCY}-redundant, ${SQL_PITR_DAYS}-day PITR"
echo "Static Web App:    ${SWA_NAME} (Standard)"
echo "Entra app:         ${ENTRA_APP_NAME}"
echo "App Insights:      ${APP_INSIGHTS_NAME}"
echo "Budget alert:      £${BUDGET_AMOUNT}/month -> ${BUDGET_ALERT_EMAIL}"
echo "SQL admin user:    ${SQL_ADMIN_USER}"
echo "SQL admin password (save this): ${SQL_ADMIN_PASSWORD}"
echo "Client IP:         ${CLIENT_IP}"
echo
echo "Active subscription:"
az account show --query "{name:name, id:id}" --output table || true
echo

read -r -p "Continue with these values? (y/N) " confirmation
[[ "${confirmation}" == "y" || "${confirmation}" == "Y" ]] || { echo "Aborted."; exit 1; }

# --- Resource providers -----------------------------------------------------
echo
echo "Registering required resource providers..."
REQUIRED_PROVIDERS=(Microsoft.Sql Microsoft.Web Microsoft.Insights)
for namespace in "${REQUIRED_PROVIDERS[@]}"; do
  state="$(az provider show --namespace "${namespace}" --query registrationState --output tsv 2>/dev/null || echo NotRegistered)"
  if [ "${state}" != "Registered" ]; then
    echo "  ${namespace}: ${state} — registering"
    az provider register --namespace "${namespace}" --output none
  else
    echo "  ${namespace}: already registered"
  fi
done
echo "Waiting for provider registrations to complete..."
for namespace in "${REQUIRED_PROVIDERS[@]}"; do
  while true; do
    state="$(az provider show --namespace "${namespace}" --query registrationState --output tsv)"
    [ "${state}" == "Registered" ] && break
    echo "  ${namespace}: ${state}"; sleep 10
  done
  echo "  ${namespace}: Registered"
done

# --- Resource group ---------------------------------------------------------
echo
echo "Ensuring resource group exists..."
existing_rg_location="$(az group show --name "${RESOURCE_GROUP}" --query location --output tsv 2>/dev/null || echo "")"
if [ -n "${existing_rg_location}" ]; then
  echo "  resource group already exists in ${existing_rg_location}"
else
  echo "  creating in ${SQL_LOCATION}"
  az group create --name "${RESOURCE_GROUP}" --location "${SQL_LOCATION}" --output none
fi

# --- SQL server (with region fallback + eventual-consistency handling) ------
echo "Creating SQL server (skip if exists)..."
existing_location="$(az sql server show --name "${SQL_SERVER}" --resource-group "${RESOURCE_GROUP}" --query location --output tsv 2>/dev/null || echo "")"
if [ -n "${existing_location}" ]; then
  SQL_LOCATION="${existing_location}"
  echo "  SQL server already exists in ${SQL_LOCATION} — adopting"
else
  SQL_REGIONS_TO_TRY=("${SQL_LOCATION}" "westeurope" "ukwest")
  SQL_SERVER_CREATED=false
  TRIED_REGIONS=()
  for try_location in "${SQL_REGIONS_TO_TRY[@]}"; do
    if [[ " ${TRIED_REGIONS[*]:-} " == *" ${try_location} "* ]]; then continue; fi
    TRIED_REGIONS+=("${try_location}")
    echo "  attempting ${try_location}..."
    set +e
    create_output="$(az sql server create \
      --name "${SQL_SERVER}" --resource-group "${RESOURCE_GROUP}" --location "${try_location}" \
      --admin-user "${SQL_ADMIN_USER}" --admin-password "${SQL_ADMIN_PASSWORD}" --output none 2>&1)"
    create_exit=$?
    set -e
    if [ "${create_exit}" -eq 0 ]; then
      SQL_LOCATION="${try_location}"; SQL_SERVER_CREATED=true; echo "  created in ${try_location}"; break
    fi
    if [[ "${create_output}" == *"RegionDoesNotAllowProvisioning"* ]] \
        || [[ "${create_output}" == *"LocationCapacityLimitReached"* ]] \
        || [[ "${create_output}" == *"NotAvailableForSubscription"* ]]; then
      echo "  ${try_location}: at capacity, trying next region..."; continue
    fi
    echo "${create_output}"; exit 1
  done
  if [ "${SQL_SERVER_CREATED}" != "true" ]; then
    echo "Could not create SQL server in any of: ${SQL_REGIONS_TO_TRY[*]}"; exit 1
  fi
fi

# --- SQL firewall -----------------------------------------------------------
echo "Configuring SQL firewall (Azure services + client IP)..."
az sql server firewall-rule create --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none 2>/dev/null || true
if ! az sql server firewall-rule create --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" \
    --name ClientIP --start-ip-address "${CLIENT_IP}" --end-ip-address "${CLIENT_IP}" --output none 2>/dev/null; then
  az sql server firewall-rule update --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" \
    --name ClientIP --start-ip-address "${CLIENT_IP}" --end-ip-address "${CLIENT_IP}" --output none
fi

# --- SQL database (production serverless, NO auto-pause, geo backups) --------
echo "Creating SQL database (skip if exists)..."
if ! az sql db show --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" --name "${SQL_DATABASE}" --output none 2>/dev/null; then
  az sql db create \
    --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" --name "${SQL_DATABASE}" \
    --edition GeneralPurpose --family Gen5 --capacity "${SQL_MAX_CAPACITY}" \
    --compute-model Serverless \
    --auto-pause-delay -1 \
    --min-capacity "${SQL_MIN_CAPACITY}" \
    --backup-storage-redundancy "${SQL_BACKUP_REDUNDANCY}" \
    --output none
  echo "  created (auto-pause disabled — stays warm, no cold start)."
else
  echo "  database exists — ensuring auto-pause stays disabled."
  az sql db update --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" --name "${SQL_DATABASE}" \
    --auto-pause-delay -1 --min-capacity "${SQL_MIN_CAPACITY}" --capacity "${SQL_MAX_CAPACITY}" --output none
fi

echo "Setting point-in-time (short-term) backup retention to ${SQL_PITR_DAYS} days..."
az sql db str-policy set --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" --name "${SQL_DATABASE}" \
  --retention-days "${SQL_PITR_DAYS}" --diffbackup-hours 12 --output none 2>/dev/null || \
  echo "  (could not set STR policy automatically — set it in the portal if needed)"

echo "Setting weekly long-term backup retention (4 weeks)..."
az sql db ltr-policy set --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" --name "${SQL_DATABASE}" \
  --weekly-retention P4W --output none 2>/dev/null || \
  echo "  (could not set LTR policy automatically — set it in the portal if needed)"

# --- Static Web App (Standard) ----------------------------------------------
echo "Creating Static Web App on Standard SKU (skip if exists)..."
if ! az staticwebapp show --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" --output none 2>/dev/null; then
  az staticwebapp create --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" \
    --location "${SWA_LOCATION}" --sku Standard --output none
else
  echo "  exists — ensuring Standard SKU."
  az staticwebapp update --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" --sku Standard --output none
fi

SWA_HOSTNAME="$(az staticwebapp show --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" \
  --query defaultHostname --output tsv)"

# --- Application Insights ----------------------------------------------------
echo "Creating Application Insights (skip if exists)..."
if ! az monitor app-insights component show --app "${APP_INSIGHTS_NAME}" --resource-group "${RESOURCE_GROUP}" --output none 2>/dev/null; then
  az monitor app-insights component create --app "${APP_INSIGHTS_NAME}" --resource-group "${RESOURCE_GROUP}" \
    --location "${SWA_LOCATION}" --application-type web --output none 2>/dev/null || \
    echo "  (App Insights create failed — install the extension with: az extension add -n application-insights)"
fi
APPINSIGHTS_CONNECTION_STRING="$(az monitor app-insights component show --app "${APP_INSIGHTS_NAME}" \
  --resource-group "${RESOURCE_GROUP}" --query connectionString --output tsv 2>/dev/null || echo "")"

# --- Entra ID app registration ----------------------------------------------
echo "Creating Entra ID app registration (skip if exists)..."
ENTRA_APP_ID="$(az ad app list --display-name "${ENTRA_APP_NAME}" --query '[0].appId' --output tsv 2>/dev/null || true)"
if [ -z "${ENTRA_APP_ID}" ] || [ "${ENTRA_APP_ID}" == "null" ]; then
  ENTRA_APP_ID="$(az ad app create --display-name "${ENTRA_APP_NAME}" --sign-in-audience AzureADMyOrg \
    --web-redirect-uris "https://${SWA_HOSTNAME}/.auth/login/aad/callback" \
    --enable-id-token-issuance true --query appId --output tsv)"
else
  az ad app update --id "${ENTRA_APP_ID}" \
    --web-redirect-uris "https://${SWA_HOSTNAME}/.auth/login/aad/callback" \
    --enable-id-token-issuance true --output none
fi
if [ -z "${ENTRA_SECRET:-}" ]; then
  echo "Generating Entra ID client secret..."
  ENTRA_SECRET="$(az ad app credential reset --id "${ENTRA_APP_ID}" --append \
    --display-name "swa-prod-secret" --years 2 --query password --output tsv)"
fi

# --- Wire all settings into the Static Web App ------------------------------
SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=${SQL_DATABASE};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"

echo "Wiring SqlConnectionString + AAD client id/secret into the Static Web App..."
az staticwebapp appsettings set --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" \
  --setting-names \
    "SqlConnectionString=${SQL_CONNECTION_STRING}" \
    "AAD_CLIENT_ID=${ENTRA_APP_ID}" \
    "AAD_CLIENT_SECRET=${ENTRA_SECRET}" \
    "APPLICATIONINSIGHTS_CONNECTION_STRING=${APPINSIGHTS_CONNECTION_STRING}" \
  --output none

SWA_DEPLOYMENT_TOKEN="$(az staticwebapp secrets list --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" \
  --query properties.apiKey --output tsv)"

# --- Monthly budget alert ----------------------------------------------------
echo "Creating monthly cost budget alert (£${BUDGET_AMOUNT})..."
SUBSCRIPTION_ID="$(az account show --query id --output tsv)"
BUDGET_START="$(date -u +%Y-%m-01)"
az consumption budget create \
  --budget-name "budget-jpms-prod" \
  --amount "${BUDGET_AMOUNT}" \
  --category Cost \
  --time-grain Monthly \
  --start-date "${BUDGET_START}" \
  --end-date "2030-01-01" \
  --resource-group "${RESOURCE_GROUP}" \
  --output none 2>/dev/null || \
  echo "  (budget create skipped — set a budget in Cost Management > Budgets, alert ${BUDGET_ALERT_EMAIL})"

# --- Write outputs -----------------------------------------------------------
cat > "${OUTPUT_ENV}" <<ENVFILE
RESOURCE_GROUP=${RESOURCE_GROUP}
SQL_LOCATION=${SQL_LOCATION}
SWA_LOCATION=${SWA_LOCATION}
SQL_SERVER=${SQL_SERVER}
SQL_DATABASE=${SQL_DATABASE}
SQL_ADMIN_USER=${SQL_ADMIN_USER}
SQL_ADMIN_PASSWORD=${SQL_ADMIN_PASSWORD}
SQL_CONNECTION_STRING="${SQL_CONNECTION_STRING}"
SWA_NAME=${SWA_NAME}
SWA_HOSTNAME=${SWA_HOSTNAME}
SWA_DEPLOYMENT_TOKEN=${SWA_DEPLOYMENT_TOKEN}
ENTRA_APP_NAME=${ENTRA_APP_NAME}
ENTRA_APP_ID=${ENTRA_APP_ID}
ENTRA_SECRET=${ENTRA_SECRET}
APP_INSIGHTS_NAME=${APP_INSIGHTS_NAME}
APPINSIGHTS_CONNECTION_STRING=${APPINSIGHTS_CONNECTION_STRING}
ENVFILE
chmod 600 "${OUTPUT_ENV}"

echo
echo "=============================================================="
echo "  Done."
echo "=============================================================="
echo "Public URL:            https://${SWA_HOSTNAME}"
echo "SQL server:            ${SQL_SERVER}.database.windows.net"
echo "Entra App (client) ID: ${ENTRA_APP_ID}"
echo
echo "Schema: the Functions API runs EF Core migrations on startup, so the full"
echo "table structure is created automatically (no data) on the first request"
echo "after deployment. To apply it manually instead, run from ./api:"
echo "  SqlConnectionString='${SQL_CONNECTION_STRING}' dotnet ef database update"
echo
echo "Next:"
echo "  1. Add SWA_DEPLOYMENT_TOKEN (in ${OUTPUT_ENV}) as GitHub repo secret"
echo "     AZURE_STATIC_WEB_APPS_API_TOKEN_PROD."
echo "  2. Point the deploy workflow at the prod token and push to main."
echo
if [ -n "${TEST_SQL_SERVER}" ]; then
  echo "Retiring the old test SQL server '${TEST_SQL_SERVER}' in ${TEST_RESOURCE_GROUP}..."
  read -r -p "  Delete it now? (y/N) " del_confirm
  if [[ "${del_confirm}" == "y" || "${del_confirm}" == "Y" ]]; then
    az sql server delete --name "${TEST_SQL_SERVER}" --resource-group "${TEST_RESOURCE_GROUP}" --yes --output none
    echo "  deleted."
  else
    echo "  left in place."
  fi
else
  echo "To retire the test SQL server later, re-run with:"
  echo "  TEST_SQL_SERVER=sql-jpms-test-673bc2 ./infra/azure-prod-setup.sh"
  echo "or delete the whole test group: az group delete --name rg-jpms-test --yes --no-wait"
fi
