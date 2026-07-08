#!/usr/bin/env bash
# ============================================================================
# Seeds By France Claim 18 (Draft) into the PROD database.
#
# Runs api/Migrations/seed-byfrance-claim18.sql: one Draft valuation claim
# (number 18, dated 26 Jun 2026) plus a ClaimLine per counting valuation line
# carrying the Valuation 18 workbook's cumulative % complete. Populates the
# Financials tab's Completion % / Expected Actual Cost and makes the
# percentages editable on the Valuation Report tab (select Claim 18).
#
# Prerequisites: seed-byfrance-valuation.sql and seed-byfrance-variations.sql
# must already have been run (every ClaimLine references their line ids).
#
# Does everything end-to-end from your Mac (same pattern as seed-master-admin.sh):
#   1. reads the prod connection details from infra/.azure-prod-output.env
#   2. installs sqlcmd (Homebrew) if it isn't already on PATH
#   3. opens the Azure SQL firewall for your current public IP
#   4. runs api/Migrations/seed-byfrance-claim18.sql
#
# Usage:  bash infra/run-seed-byfrance-claim18.sh
# Safe to re-run: the firewall rule upserts and the SQL script is idempotent
# (NOTE: a re-run resets Claim 18's percentages to the workbook values,
# overwriting any edits made in the app since).
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
SQL_FILE="${REPO_ROOT}/api/Migrations/seed-byfrance-claim18.sql"

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
    --name "seed-bf-claim18-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

# 4. Run the seed -----------------------------------------------------------
echo "Running seed-byfrance-claim18.sql..."
sqlcmd -S "${SQL_SERVER}.database.windows.net" -d "${SQL_DATABASE}" \
  -U "${SQL_ADMIN_USER}" -P "${SQL_ADMIN_PASSWORD}" \
  -i "$SQL_FILE" -b

echo "Done. The sanity-check SELECT above should show:"
echo "  WorksClaimed      = 1675411.45"
echo "  VariationsClaimed =  -27420.80"
echo "  TotalClaimed      = 1647990.65"
