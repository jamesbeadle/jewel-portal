#!/usr/bin/env bash
# ============================================================================
# Applies the historic valuation-invoice backfill to the PROD database.
#
# Runs scripts/backfill-valuation-invoices.sql — GENERATE AND REVIEW IT FIRST:
#
#   dotnet run --project tools/XeroValuationBackfill
#
# The generator pulls every ACCREC sales invoice / ACCRECCREDIT credit note
# from Xero, groups them by the "Sites" tracking option, and writes one
# guarded batch per site that inserts paid MANUAL valuation invoices
# (IsManual = 1) so completed projects' "Certified to date" stops reading £0
# against their allocated cost on the Profit Summary.
#
# Safe to re-run: every batch SKIPs (with a PRINT, not an error) any project
# that already has valuation invoices — so live projects that invoice through
# JPMS are never touched, and a second run is a no-op. Projects holding a
# Preapproved claim are also skipped (their frozen totals need the app's
# manual-invoice flow, which re-freezes them).
#
# Does everything end-to-end from your Mac (same pattern as the seed scripts):
#   1. reads the prod connection details from infra/.azure-prod-output.env
#   2. installs sqlcmd (Homebrew) if it isn't already on PATH
#   3. opens the Azure SQL firewall for your current public IP
#   4. runs scripts/backfill-valuation-invoices.sql with -b, logging to
#      backfill-valuation-invoices.log — READ THE LOG: the SKIP/OK PRINTs are
#      the record of what actually happened, and the final SELECT shows the
#      certified totals per backfilled project.
#
# Usage:  bash infra/run-backfill-valuation-invoices.sh
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
SQL_FILE="${REPO_ROOT}/scripts/backfill-valuation-invoices.sql"
LOG_FILE="${REPO_ROOT}/backfill-valuation-invoices.log"

[[ -f "$ENV_FILE" ]] || { echo "Missing $ENV_FILE"; exit 1; }
[[ -f "$SQL_FILE" ]] || { echo "Missing $SQL_FILE — generate it first:  dotnet run --project tools/XeroValuationBackfill"; exit 1; }

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
    --name "backfill-val-invoices-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

# 4. Run the backfill -------------------------------------------------------
echo "Running backfill-valuation-invoices.sql..."
sqlcmd -S "${SQL_SERVER}.database.windows.net" -d "${SQL_DATABASE}" \
  -U "${SQL_ADMIN_USER}" -P "${SQL_ADMIN_PASSWORD}" \
  -i "$SQL_FILE" -b -o "$LOG_FILE"

cat "$LOG_FILE"
echo
echo "Done. Check the SKIP/OK lines above, then open the Profit Summary —"
echo "completed projects should now show their certified value instead of £0."
