#!/usr/bin/env bash
# ============================================================================
# Seeds the Abbot Road work orders into the PROD database.
#
# Runs api/Migrations/seed-abbotroad-workorders.sql: the nine Buildertrend
# purchase orders (PO-01..PO-09) as WorkOrders + WorkOrderLines for project
# 4ec1ad1ca3a440c69f32f46f73aea005, with Buildertrend cost codes mapped to the
# JBB Cost Code Master (mapping in the SQL header). Subcontractors are matched
# by company name and only inserted when missing.
#
# Prerequisites:
#   - The 20260708130000_ExtendWorkOrdersForCostCenters migration must have
#     been applied first (the API applies migrations on startup, so deploy the
#     updated API before seeding — WorkOrderLines won't exist until then).
#   - seed-cost-centers.sql / migrate-cost-centers-v2.sql already run (the
#     mapped codes must exist in the CostCenters master).
#
# Does everything end-to-end from your Mac (same pattern as seed-master-admin.sh):
#   1. reads the prod connection details from infra/.azure-prod-output.env
#   2. installs sqlcmd (Homebrew) if it isn't already on PATH
#   3. opens the Azure SQL firewall for your current public IP
#   4. runs api/Migrations/seed-abbotroad-workorders.sql
#
# Usage:  bash infra/run-seed-abbotroad-workorders.sh
# Safe to re-run: the firewall rule upserts and the SQL script is idempotent
# (NOTE: a re-run resets the nine orders and their lines to the printed PO
# values, overwriting any edits made in the app since).
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
SQL_FILE="${REPO_ROOT}/api/Migrations/seed-abbotroad-workorders.sql"

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
    --name "seed-ar-workorders-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

# 4. Run the seed -----------------------------------------------------------
echo "Running seed-abbotroad-workorders.sql..."
sqlcmd -S "${SQL_SERVER}.database.windows.net" -d "${SQL_DATABASE}" \
  -U "${SQL_ADMIN_USER}" -P "${SQL_ADMIN_PASSWORD}" \
  -i "$SQL_FILE" -b

echo "Done. The reconciliation SELECT above should show 9 orders,"
echo "every row's Check column blank (no MISMATCH), totalling:"
echo "  OrderValue sum = 92,324.63"
echo "  Paid sum       = 69,987.42"
