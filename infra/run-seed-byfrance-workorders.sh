#!/usr/bin/env bash
# ============================================================================
# Seeds the By France work orders into the PROD database.
#
# Runs api/Migrations/seed-byfrance-workorders.sql: the 36 Buildertrend
# purchase orders (PO-01..PO-37, PO-21 was never raised) as WorkOrders +
# WorkOrderLines for project 3490f944b29545c4b8d5a04130f42ab8, with
# Buildertrend cost codes mapped to the JBB Cost Code Master (mapping in the
# SQL header). Subcontractors are matched by company name and only inserted
# when missing; "The Steel Team Limited" reuses the Abbot Road seed's
# "The Steel Team" record via prefix match.
#
# Prerequisites:
#   - The 20260708130000_ExtendWorkOrdersForCostCenters migration applied
#     (deploy the updated API first — it migrates on startup).
#   - seed-cost-centers.sql / migrate-cost-centers-v2.sql already run.
#
# Does everything end-to-end from your Mac (same pattern as seed-master-admin.sh):
#   1. reads the prod connection details from infra/.azure-prod-output.env
#   2. installs sqlcmd (Homebrew) if it isn't already on PATH
#   3. opens the Azure SQL firewall for your current public IP
#   4. runs api/Migrations/seed-byfrance-workorders.sql
#
# Usage:  bash infra/run-seed-byfrance-workorders.sh
# Safe to re-run: the firewall rule upserts and the SQL script is idempotent
# (NOTE: a re-run resets the 36 orders and their lines to the printed PO
# values, overwriting any edits made in the app since).
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
SQL_FILE="${REPO_ROOT}/api/Migrations/seed-byfrance-workorders.sql"

[[ -f "$ENV_FILE" ]] || { echo "Missing $ENV_FILE"; exit 1; }
[[ -f "$SQL_FILE" ]] || { echo "Missing $SQL_FILE"; exit 1; }

# 1. Load prod connection details ------------------------------------------
# shellcheck disable=SC1090
source "$ENV_FILE"
echo "Target: ${SQL_SERVER}.database.windows.net / ${SQL_DATABASE}  (resource group ${RESOURCE_GROUP})"

# 2. Ensure sqlcmd is available --------------------------------------------
if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "sqlcmd not found — installing via Homebrew..."
  command -v brew >/dev/null 2>&1 || { echo "Homebrew not installed. See https://brew.sh"; exit 1; }
  brew install sqlcmd
fi

# 3. Open the SQL firewall for this machine's public IP --------------------
MY_IP="$(curl -fsS https://api.ipify.org || true)"
if [[ -n "$MY_IP" ]]; then
  echo "Adding firewall rule for ${MY_IP} on ${SQL_SERVER}..."
  az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER" \
    --name "seed-bf-workorders-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

# 4. Run the seed -----------------------------------------------------------
echo "Running seed-byfrance-workorders.sql..."
sqlcmd -S "${SQL_SERVER}.database.windows.net" -d "${SQL_DATABASE}" \
  -U "${SQL_ADMIN_USER}" -P "${SQL_ADMIN_PASSWORD}" \
  -i "$SQL_FILE" -b

echo "Done. The reconciliation SELECT above should show 36 orders,"
echo "every row's Check column blank (no MISMATCH), totalling:"
echo "  OrderValue sum = 1,128,986.98"
echo "  Paid sum       =   876,375.53"
