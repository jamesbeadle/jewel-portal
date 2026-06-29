# Read-only Microsoft Graph probe.
#
# Purpose: prove whether we can paginate the projects@ inbox filtered by category (the make-or-break
# for the category-based triage design) using the SAME app-only identity the SWA uses. It only does
# GET requests against the mailbox — it never tags, moves, or deletes anything.
#
# Run (PowerShell 5.1 or 7):
#   $env:MAILBOX_TENANT_ID     = "<tenant id>"
#   $env:MAILBOX_CLIENT_ID     = "<app client id>"
#   $env:MAILBOX_CLIENT_SECRET = "<app client secret>"
#   ./scripts/probe-graph-categories.ps1
#
# The three values are the same MailboxIntake:TenantId / ClientId / ClientSecret the worker/SWA use
# (Azure Portal -> the app -> Configuration, or your Key Vault). Don't commit the secret anywhere.

param(
    [string]$TenantId     = $env:MAILBOX_TENANT_ID,
    [string]$ClientId     = $env:MAILBOX_CLIENT_ID,
    [string]$ClientSecret = $env:MAILBOX_CLIENT_SECRET,
    [string]$Mailbox      = "projects@jewelbb.co.uk",
    [string]$Category     = "JPMS/Triaged"
)

if (-not $TenantId -or -not $ClientId -or -not $ClientSecret) {
    Write-Error "Set MAILBOX_TENANT_ID, MAILBOX_CLIENT_ID and MAILBOX_CLIENT_SECRET first (or pass -TenantId/-ClientId/-ClientSecret)."
    exit 1
}

# 1) App-only token (client-credentials) — the exact identity the SWA/worker use.
$token = (Invoke-RestMethod -Method Post `
    -Uri "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token" `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        client_id     = $ClientId
        client_secret = $ClientSecret
        scope         = "https://graph.microsoft.com/.default"
        grant_type    = "client_credentials"
    }).access_token

$baseHeaders = @{ Authorization = "Bearer $token" }
$base = "https://graph.microsoft.com/v1.0/users/$Mailbox/mailFolders/inbox/messages"

function Probe([string]$label, [string]$url, [hashtable]$extra = @{}) {
    Write-Host "`n=== $label ===" -ForegroundColor Cyan
    Write-Host $url
    try {
        $resp = Invoke-WebRequest -Method Get -Uri $url -Headers ($baseHeaders + $extra) -ErrorAction Stop
        $json = $resp.Content | ConvertFrom-Json
        Write-Host ("Status:        {0} OK" -f [int]$resp.StatusCode) -ForegroundColor Green
        Write-Host ("Items:         {0}" -f @($json.value).Count)
        Write-Host ("Has nextLink:  {0}" -f [bool]$json.'@odata.nextLink')
    }
    catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { "?" }
        $body = $_.ErrorDetails.Message
        if (-not $body -and $_.Exception.Response) {
            try { $body = (New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() } catch {}
        }
        Write-Host ("Status:        {0} (FAILED)" -f $code) -ForegroundColor Red
        Write-Host ("Error body:    {0}" -f $body) -ForegroundColor Yellow
    }
}

$catFilter    = [uri]::EscapeDataString("categories/any(c:c eq '$Category')")
$notCatFilter = [uri]::EscapeDataString("not categories/any(c:c eq '$Category')")
$orderBy      = [uri]::EscapeDataString("receivedDateTime desc")
$select       = "subject,categories"

# Query 1 — is filtering on categories valid at all (expect 200; value may be [] since nothing is tagged yet).
Probe "Query 1: positive category filter" `
    ($base + '?$filter=' + $catFilter + '&$select=' + $select + '&$top=10')

# Query 2 — the decisive one: untriaged, newest-first, paged.
Probe "Query 2: NOT category + orderby + top" `
    ($base + '?$filter=' + $notCatFilter + '&$orderby=' + $orderBy + '&$select=' + $select + '&$top=10')

# Query 2b — same, but with advanced-query headers in case the negated filter needs them.
Probe "Query 2b: NOT category + orderby + count (ConsistencyLevel: eventual)" `
    ($base + '?$filter=' + $notCatFilter + '&$orderby=' + $orderBy + '&$select=' + $select + '&$top=10&$count=true') `
    @{ ConsistencyLevel = "eventual" }

Write-Host "`nDone. Paste the three blocks above back to interpret them." -ForegroundColor Cyan
