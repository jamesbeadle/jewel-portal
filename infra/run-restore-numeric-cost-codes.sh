#!/usr/bin/env bash
# ============================================================================
# Runs the cost-code migration against the PROD database.
#
# RESTORE: reverts the erroneous v2 (trade-prefixed) cost-code migration.
# Re-activates the numbered Jewel master (00001..00137 per CostCodes_20260707
# .xlsx), retires the trade-prefixed codes, and reverts valuation lines AND
# Xero ledger allocations via the exact inverse mapping (1:1 -- allocations
# made while the prefixed codes were live come back cleanly).
# Transactional and self-verifying: rolls back if valuation totals move.
#
# Does everything end-to-end from your Mac (same pattern as seed-master-admin.sh):
#   1. reads the prod connection details from infra/.azure-prod-output.env
#   2. installs sqlcmd (Homebrew) if it isn't already on PATH
#   3. opens the Azure SQL firewall for your current public IP
#   4. runs api/Migrations/restore-numeric-cost-codes.sql
#
# Usage:  bash infra/restore-numeric-cost-codes.sh
# Safe to re-run: the firewall rule upserts and the SQL script is idempotent.
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
SQL_FILE="${REPO_ROOT}/api/Migrations/restore-numeric-cost-codes.sql"

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
    --name "costcoderestore-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

# 4. Run the migration -------------------------------------------------------
echo "Running cost-code migration (transactional; auto-rolls-back on checksum failure)..."
sqlcmd \
  -S "${SQL_SERVER}.database.windows.net" \
  -d "${SQL_DATABASE}" \
  -U "${SQL_ADMIN_USER}" \
  -P "${SQL_ADMIN_PASSWORD}" \
  -i "${SQL_FILE}"

echo ""
echo "Done. Check the post-migration report above:"
echo "  - LinesOnMasterCodes should be the bulk of every project"
echo "  - LinesOnLegacyCodes lists app-added lines to recode by hand (also listed row-by-row)"
echo "  - Totals must match the valuation reports (checksum enforced in-script)"
echo "Then open a project's Valuation Report in JPMS — the CODE column now shows 00001-style codes."
