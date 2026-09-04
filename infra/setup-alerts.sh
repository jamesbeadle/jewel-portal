#!/usr/bin/env bash
# Standing checks for the JPMS portal (proposal item 2). Idempotent: safe to re-run.
#
# Creates one action group and three log alerts against the Log Analytics workspace behind
# appi-jpms-prod, plus an outside-in availability test on the public site.
#
# NOTE ON WHAT CATCHES WHAT — worth understanding before trusting these:
# the 503 of 4 September was answered by the Static Web Apps GATEWAY and never reached the
# Functions host, so it appears in NO server-side table. A 5xx alert would not have caught it.
# The availability test is the one that would have: it calls the site from outside, exactly as a
# user does. Keep it even if you drop the others.
set -euo pipefail

RG=rg-jpms-prod
WS=log-jpms-prod
AI=appi-jpms-prod
SITE_URL="https://portal.jewelbb.co.uk/"
ALERT_EMAIL="${ALERT_EMAIL:-nigel.reilly@jewelgroup.co.uk}"

az extension add -n log-analytics 2>/dev/null || true
az extension add -n application-insights 2>/dev/null || true

WS_ID="$(az monitor log-analytics workspace show -g "$RG" -n "$WS" --query id -o tsv)"
AI_ID="$(az monitor app-insights component show -g "$RG" -a "$AI" --query id -o tsv)"

echo "[1/5] Action group (who gets told)"
az monitor action-group create -g "$RG" -n ag-jpms-oncall --short-name jpmsops \
  --action email primary "$ALERT_EMAIL" -o none
AG_ID="$(az monitor action-group show -g "$RG" -n ag-jpms-oncall --query id -o tsv)"

mk_alert () {  # name, description, window, KQL, threshold, severity
  az monitor scheduled-query create -g "$RG" -n "$1" \
    --scopes "$WS_ID" --action-groups "$AG_ID" \
    --description "$2" --evaluation-frequency "$3" --window-size "$3" --severity "$6" \
    --condition "count 'q' > $5" --condition-query q="$4" -o none 2>&1 | tail -2 || \
    echo "    (exists or failed — check in the portal)"
}

echo "[2/5] Server errors"
mk_alert jpms-server-errors "More than three 5xx responses in five minutes." 5m \
  'AppRequests | where ResultCode startswith "5"' 3 1

echo "[3/5] Slow responses"
mk_alert jpms-slow-responses "More than ten requests over five seconds in fifteen minutes." 15m \
  'AppRequests | where DurationMs > 5000' 10 2

echo "[4/5] Exception spike"
mk_alert jpms-exception-spike "More than twenty exceptions in fifteen minutes." 15m \
  'AppExceptions' 20 2

echo "[5/5] Availability test — the one that catches a gateway 503"
az monitor app-insights web-test create -g "$RG" -n jpms-portal-availability \
  --app-insights-id "$AI_ID" --location westeurope --web-test-kind standard \
  --frequency 300 --timeout 60 --enabled true --retry-enabled true \
  --locations Id=emea-nl-ams-azr Id=emea-gb-db3-azr Id=emea-ru-msa-edge \
  --request-url "$SITE_URL" --expected-status-code 200 \
  --defined-web-test-name jpms-portal-availability --web-test-name jpms-portal-availability \
  -o none 2>&1 | tail -3 || {
    echo "    CLI refused the web test — create it in the portal instead:"
    echo "    Application Insights > $AI > Availability > Add Standard test"
    echo "    URL $SITE_URL, every 5 minutes, 3 locations, alert to ag-jpms-oncall."
  }

echo
echo "Done. Review under: Monitor > Alerts > Alert rules (resource group $RG)."
echo "Alert email: $ALERT_EMAIL  (override with ALERT_EMAIL=... before running)"
