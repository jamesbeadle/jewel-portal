#!/usr/bin/env bash
# Read-only diagnostics for the JPMS-1C0C1C 503 (GET /api/projects/{id}/records?type=Todo).
# Creates nothing. Queries the Log Analytics workspace behind appi-jpms-prod directly, because
# workspace-based App Insights lands in AppRequests/AppTraces/AppExceptions.
set -uo pipefail

RG=rg-jpms-prod
WS=log-jpms-prod

az extension add -n log-analytics 2>/dev/null

WSID="$(az monitor log-analytics workspace show -g "$RG" -n "$WS" --query customerId -o tsv)" || exit 1
echo "workspace: $WS ($WSID)"

la() {
  echo
  echo "== $1 =="
  az monitor log-analytics query -w "$WSID" --analytics-query "$2" -o table || echo "(query failed)"
}

# Is ANY telemetry arriving at all? This is the question that matters.
la "telemetry by table (24h)" \
  'union withsource=Tbl AppRequests, AppTraces, AppExceptions, AppDependencies | where TimeGenerated > ago(24h) | summarize n=count() by Tbl'

la "non-2xx requests (24h)" \
  'AppRequests | where TimeGenerated > ago(24h) | where ResultCode !in ("200","201","202","204","304") | project TimeGenerated, Name, ResultCode, DurationMs, AppRoleInstance | order by TimeGenerated desc | take 50'

la "the /records endpoint (24h)" \
  'AppRequests | where TimeGenerated > ago(24h) | where Url has "/records" | project TimeGenerated, ResultCode, DurationMs, Url | order by TimeGenerated desc | take 30'

la "instances / cold starts (24h)" \
  'AppRequests | where TimeGenerated > ago(24h) | summarize n=count(), firstSeen=min(TimeGenerated), lastSeen=max(TimeGenerated) by AppRoleInstance | order by firstSeen asc'
