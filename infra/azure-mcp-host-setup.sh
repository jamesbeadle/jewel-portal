#!/usr/bin/env bash
#
# Provisions the standalone Function App that serves the MCP connector endpoint (/api/mcp).
#
# WHY A SEPARATE HOST: Azure Static Web Apps strips/overwrites the client's Authorization
# header before requests reach its managed functions (github.com/Azure/static-web-apps
# issues 158 & 275), so bearer-authenticated MCP calls can never work through the SWA. The
# browser-facing OAuth flow (register/authorize/approve/token, the consent page) stays on the
# portal domain, where it already works; only the JSON-RPC endpoint moves to this host, where
# the Authorization header passes through untouched. docs/ai/10-mcp-connector.md §2.
#
# Idempotent: safe to re-run. Run with an az CLI logged into the subscription (Cloud Shell is fine):
#   bash infra/azure-mcp-host-setup.sh
#
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-rg-jpms-prod}"
LOCATION="${LOCATION:-westeurope}"
SWA_NAME="${SWA_NAME:-swa-jpms-prod}"
FUNC_APP="${FUNC_APP:-func-jpms-mcp-prod}"
# The B1 plan the app runs on (custom domain + free managed cert need Basic or above, and it
# removes cold starts). Created by hand 2026-08-28; linux consumption apps cannot be migrated
# onto a plan in place — delete and recreate instead (Azure's own guidance).
FUNC_PLAN="${FUNC_PLAN:-plan-jpms-mcp}"

command -v jq >/dev/null || { echo "jq is required (Cloud Shell has it)."; exit 1; }

# ---- Storage for the Functions runtime: reuse the existing jpms account when there is one. ----
STORAGE_ACCOUNT="${STORAGE_ACCOUNT:-$(az storage account list --resource-group "${RESOURCE_GROUP}" \
  --query "[?starts_with(name, 'stjpmsprod')].name | [0]" --output tsv)}"
if [[ -z "${STORAGE_ACCOUNT}" ]]; then
  STORAGE_ACCOUNT="stjpmsmcp$(openssl rand -hex 3)"
  echo "Creating storage account ${STORAGE_ACCOUNT}…"
  az storage account create --name "${STORAGE_ACCOUNT}" --resource-group "${RESOURCE_GROUP}" \
    --location "${LOCATION}" --sku Standard_LRS --output none
fi
echo "Runtime storage: ${STORAGE_ACCOUNT}"

# ---- The Function App (Linux consumption, dotnet-isolated 8). ----
if ! az functionapp show --name "${FUNC_APP}" --resource-group "${RESOURCE_GROUP}" --output none 2>/dev/null; then
  echo "Creating Function App ${FUNC_APP} on plan ${FUNC_PLAN}…"
  if ! az appservice plan show --name "${FUNC_PLAN}" --resource-group "${RESOURCE_GROUP}" --output none 2>/dev/null; then
    az appservice plan create --name "${FUNC_PLAN}" --resource-group "${RESOURCE_GROUP}" \
      --location "${LOCATION}" --sku B1 --is-linux --output none
  fi
  az functionapp create \
    --name "${FUNC_APP}" \
    --resource-group "${RESOURCE_GROUP}" \
    --storage-account "${STORAGE_ACCOUNT}" \
    --plan "${FUNC_PLAN}" \
    --functions-version 4 \
    --runtime dotnet-isolated \
    --runtime-version 8 \
    --output none
fi
MCP_HOSTNAME="$(az functionapp show --name "${FUNC_APP}" --resource-group "${RESOURCE_GROUP}" \
  --query defaultHostName --output tsv)"
MCP_PUBLIC_URL="https://${MCP_HOSTNAME}/api/mcp"
echo "Function App: ${MCP_HOSTNAME}"

# ---- Copy the portal API's app settings across, so the same code finds the same database,
#      mailbox and stores. Keys are renamed ':' -> '__' (Linux env names cannot carry ':';
#      .NET configuration reads both spellings identically). Runtime-owned keys are skipped. ----
echo "Copying app settings from ${SWA_NAME}…"
az staticwebapp appsettings list --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" --output json \
  | jq '[.properties | to_entries[]
         | select(.key | test("^(AzureWebJobsStorage|FUNCTIONS_|WEBSITE_|APPINSIGHTS_|APPLICATIONINSIGHTS_)") | not)
         | { name: (.key | gsub(":"; "__")), value: .value, slotSetting: false }]' \
  > /tmp/jpms-mcp-settings.json
az functionapp config appsettings set --name "${FUNC_APP}" --resource-group "${RESOURCE_GROUP}" \
  --settings @/tmp/jpms-mcp-settings.json --output none
rm -f /tmp/jpms-mcp-settings.json

# ---- Tell both hosts where the MCP endpoint publicly lives: the discovery documents (served by
#      the SWA) advertise this exact URL as the OAuth resource, and it must match the URL the AI
#      tool connects to. ----
az functionapp config appsettings set --name "${FUNC_APP}" --resource-group "${RESOURCE_GROUP}" \
  --settings "Mcp__PublicUrl=${MCP_PUBLIC_URL}" --output none
az staticwebapp appsettings set --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" \
  --setting-names "Mcp__PublicUrl=${MCP_PUBLIC_URL}" --output none

# ---- CORS: not needed (AI tools call server-to-server), left closed on purpose. ----

cat > infra/.azure-mcp-output.env <<EOF
FUNC_APP=${FUNC_APP}
MCP_HOSTNAME=${MCP_HOSTNAME}
MCP_PUBLIC_URL=${MCP_PUBLIC_URL}
STORAGE_ACCOUNT=${STORAGE_ACCOUNT}
EOF

echo
echo "Done. Finish the wiring:"
echo "  1. GitHub repo -> Settings -> Secrets and variables -> Actions:"
echo "       Variable JPMS_MCP_APP_NAME = ${FUNC_APP}"
echo "       Secret   AZURE_FUNCTIONAPP_PUBLISH_PROFILE_MCP = output of:"
echo "         az functionapp deployment list-publishing-profiles --name ${FUNC_APP} \\"
echo "           --resource-group ${RESOURCE_GROUP} --xml"
echo "  2. Push (or re-run the 'Deploy JPMS MCP host' workflow) to deploy the api to it."
echo "  3. The connector URL for Claude / Perplexity is: ${MCP_PUBLIC_URL}"
