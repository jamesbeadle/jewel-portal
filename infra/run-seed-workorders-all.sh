#!/usr/bin/env bash
# ============================================================================
# Work orders, end to end: applies the schema migration then seeds BOTH
# projects (Abbot Road + By France) into the PROD database.
#
# Runs, in order:
#   1. api/Migrations/apply-workorders-migration.sql
#        Applies 20260708130000_ExtendWorkOrdersForCostCenters by hand and
#        records it in __EFMigrationsHistory, so the next API deploy's
#        startup auto-migrate skips it. No-op if already applied. This fixes
#        the "Invalid column name 'Number'" error from running the seeds
#        before the updated API was deployed.
#   2. api/Migrations/seed-abbotroad-workorders.sql
#        9 orders / 17 lines. Expected: 92,324.63 committed / 69,987.42 paid.
#   3. api/Migrations/seed-byfrance-workorders.sql
#        36 orders / 98 lines. Expected: 1,128,986.98 committed / 876,375.53 paid.
#   4. api/Migrations/seed-woodhouse-workorders.sql
#        8 orders / 19 lines. Expected: 323,825.91 committed / 196,772.95 paid.
#   5. api/Migrations/seed-coombelane-workorders.sql
#        50 orders / 65 lines. Expected: 544,284.38 committed / 542,702.38 paid.
#
# Prerequisite: seed-cost-centers.sql / migrate-cost-centers-v2.sql already
# run (the mapped codes must exist in the CostCenters master).
#
# Does everything end-to-end from your Mac (same pattern as seed-master-admin.sh):
# reads infra/.azure-prod-output.env, installs sqlcmd if needed, opens the
# Azure SQL firewall for your current IP, runs the three scripts.
#
# Usage:  bash infra/run-seed-workorders-all.sh
# Safe to re-run: every script is idempotent (NOTE: a re-run resets the seeded
# orders and lines to the printed PO values, overwriting in-app edits since).
# ============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${REPO_ROOT}/infra/.azure-prod-output.env"
MIGRATION_FILE="${REPO_ROOT}/api/Migrations/apply-workorders-migration.sql"
SEED_ABBOTROAD="${REPO_ROOT}/api/Migrations/seed-abbotroad-workorders.sql"
SEED_BYFRANCE="${REPO_ROOT}/api/Migrations/seed-byfrance-workorders.sql"
SEED_WOODHOUSE="${REPO_ROOT}/api/Migrations/seed-woodhouse-workorders.sql"
SEED_COOMBELANE="${REPO_ROOT}/api/Migrations/seed-coombelane-workorders.sql"

[[ -f "$ENV_FILE" ]] || { echo "Missing $ENV_FILE"; exit 1; }
for f in "$MIGRATION_FILE" "$SEED_ABBOTROAD" "$SEED_BYFRANCE" "$SEED_WOODHOUSE" "$SEED_COOMBELANE"; do
  [[ -f "$f" ]] || { echo "Missing $f"; exit 1; }
done

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
    --name "seed-workorders-$(date +%Y%m%d-%H%M%S)" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none && echo "Firewall rule added." || echo "Could not add firewall rule (you may already have access) — continuing."
else
  echo "Couldn't determine public IP — skipping firewall step."
fi

run_sql() {
  echo ""
  echo "==> Running $(basename "$1")..."
  sqlcmd -S "${SQL_SERVER}.database.windows.net" -d "${SQL_DATABASE}" \
    -U "${SQL_ADMIN_USER}" -P "${SQL_ADMIN_PASSWORD}" \
    -i "$1" -b
}

# 4. Apply schema, then seed both projects ----------------------------------
run_sql "$MIGRATION_FILE"
run_sql "$SEED_ABBOTROAD"
run_sql "$SEED_BYFRANCE"
run_sql "$SEED_WOODHOUSE"
run_sql "$SEED_COOMBELANE"

echo ""
echo "Done. Each seed's reconciliation SELECT should show no MISMATCH rows:"
echo "  Abbot Road:   9 orders — OrderValue sum    92,324.63 · Paid    69,987.42"
echo "  By France:   36 orders — OrderValue sum 1,128,986.98 · Paid   876,375.53"
echo "  Woodhouse:    8 orders — OrderValue sum   323,825.91 · Paid   196,772.95"
echo "  Coombe Lane: 50 orders — OrderValue sum   544,284.38 · Paid   542,702.38"
