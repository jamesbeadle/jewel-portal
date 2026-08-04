#!/usr/bin/env bash
# Creates the App Insights resource the original setup script intended
# (appi-jpms-prod) and wires its connection string into swa-jpms-prod so the
# API finally has telemetry. Safe to re-run — every step skips or overwrites
# cleanly. Run with:  bash infra/setup-appinsights.sh

RG="rg-jpms-prod"
LOC="westeurope"
WS="log-jpms-prod"
AI="appi-jpms-prod"
SWA="swa-jpms-prod"

if ! command -v az >/dev/null 2>&1; then
  echo "ERROR: the 'az' command isn't available in this terminal."
  echo "Install the Azure CLI (brew install azure-cli) or use the terminal you ran the original infra scripts from."
  exit 1
fi

if ! az account show >/dev/null 2>&1; then
  echo "Not signed in to Azure — opening login..."
  az login || { echo "ERROR: az login failed."; exit 1; }
fi

echo "Signed in as: $(az account show --query user.name --output tsv)"

echo "[1/5] Ensuring the app-insights CLI extension is installed..."
az extension add --name application-insights --only-show-errors 2>/dev/null || true

echo "[2/5] Creating Log Analytics workspace ${WS} (no-op if it already exists)..."
az monitor log-analytics workspace create \
  --resource-group "${RG}" --workspace-name "${WS}" --location "${LOC}" \
  --output none \
  || { echo "ERROR: workspace creation failed — see message above."; exit 1; }

echo "[3/5] Creating Application Insights ${AI} (no-op if it already exists)..."
WS_ID="$(az monitor log-analytics workspace show \
  --resource-group "${RG}" --workspace-name "${WS}" --query id --output tsv)"
az monitor app-insights component create \
  --app "${AI}" --resource-group "${RG}" --location "${LOC}" \
  --application-type web --workspace "${WS_ID}" \
  --output none \
  || { echo "ERROR: App Insights creation failed — see message above."; exit 1; }

echo "[4/5] Wiring the connection string into ${SWA}..."
AI_CONN="$(az monitor app-insights component show \
  --app "${AI}" --resource-group "${RG}" --query connectionString --output tsv)"
if [ -z "${AI_CONN}" ]; then
  echo "ERROR: could not read the App Insights connection string."
  exit 1
fi
az staticwebapp appsettings set \
  --name "${SWA}" --resource-group "${RG}" \
  --setting-names "APPLICATIONINSIGHTS_CONNECTION_STRING=${AI_CONN}" \
  --output none \
  || { echo "ERROR: setting the Static Web App app setting failed."; exit 1; }

echo "[5/5] Verifying the setting landed on ${SWA}..."
az staticwebapp appsettings list --name "${SWA}" --resource-group "${RG}" --output table

echo ""
echo "Done. Load any page of the portal, wait 2-3 minutes, then check:"
echo "  Azure portal -> ${AI} -> Investigate -> Failures / Live metrics"
