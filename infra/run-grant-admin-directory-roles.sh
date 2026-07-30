#!/usr/bin/env bash
# ============================================================================
# Grants the Administrator DIRECTORY role to the former hard-coded master
# admins (james.beadle@jewelbb.co.uk, nigel.reilly@jewelenterprises.co.uk,
# admin.james@jewelenterprises.co.uk) in the PROD database.
#
# The in-code JpmsAdministrators list has been removed — run this BEFORE (or
# with) the deploy that removes it, or those accounts sign in with no roles.
#
# Does everything end-to-end from your Mac (same pattern as seed-master-admin.sh):
#   1. reads the prod connection details from infra/.azure-prod-output.env
#   2. installs sqlcmd (Homebrew) if it isn't already on PATH
#   3. opens the Azure SQL firewall for your current public IP
#   4. runs api/Migrations/grant-admin-directory-roles.sql
#
# Usage:  bash infra/run-grant-admin-directory-roles.sh
# Safe to re-run: the firewall rule upserts and the SQL script is idempotent.
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
SQL_FILE="${REPO_ROOT}/api/Migrations/grant-admin-directory-roles.sql"

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
    --name "adminroles-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

# 4. Run the grant script ----------------------------------------------------
echo "Granting Administrator directory roles..."
sqlcmd \
  -S "${SQL_SERVER}.database.windows.net" \
  -d "${SQL_DATABASE}" \
  -U "${SQL_ADMIN_USER}" \
  -P "${SQL_ADMIN_PASSWORD}" \
  -i "${SQL_FILE}"

echo ""
echo "Done. The read-back above should show Role 0 (Administrator) against each account."
echo "Each admin should sign out and back in to refresh their cached session roles."
