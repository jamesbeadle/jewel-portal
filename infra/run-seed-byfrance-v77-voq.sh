#!/usr/bin/env bash
# ============================================================================
# Seeds By France V77 Front Entrance Canopy (unapproved VOQ) into PROD.
#
# Runs api/Migrations/seed-byfrance-v77-voq.sql: one VariationOrderQuotes row
# (VOQ-0077, Status Tendering, GBP 5,354.80). No VO, no valuation line — see
# the SQL file header. Once seeded, V77 appears in the email-triage
# "Variation Order Quote" picker (tag stem JPMS/VOQ-JBB-2026-001-0077).
#
# Same end-to-end pattern as run-seed-byfrance-claim18.sh:
#   1. reads prod connection details from infra/.azure-prod-output.env
#   2. installs sqlcmd (Homebrew) if missing
#   3. opens the Azure SQL firewall for your current public IP
#   4. runs the seed
#
# Usage:  bash infra/run-seed-byfrance-v77-voq.sh
# Safe to re-run: idempotent MERGE keyed on bf-voq-v77 (a re-run overwrites
# any edits made to VOQ-0077 in the app since).
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
SQL_FILE="${REPO_ROOT}/api/Migrations/seed-byfrance-v77-voq.sql"

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
    --name "seed-bf-v77-voq-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

# 4. Run the seed -----------------------------------------------------------
echo "Running seed-byfrance-v77-voq.sql..."
sqlcmd -S "${SQL_SERVER}.database.windows.net" -d "${SQL_DATABASE}" \
  -U "${SQL_ADMIN_USER}" -P "${SQL_ADMIN_PASSWORD}" \
  -i "$SQL_FILE" -b

echo "Done. The sanity-check SELECT above should show:"
echo "  V77QuotePending = 1"
echo "  V77Value        = 5354.80"
echo "  V77VoRows       = 0"
echo "  V77ReportLines  = 0"
echo "  NetVariations   = 215737.58"
