# JPMS hosting upgrade — the plan, and how it got here

The portal's API moves off Static Web Apps managed functions onto its own always-ready
Function App in North Europe, next to the database. This file is the runnable plan in the
repository. The decision behind it, with Microsoft's words, the wider capacity picture and the
stay-or-leave reasoning, is `docs/09-operations/2026-09-22-azure-p1v4-refused-and-the-way-forward.md`.
The same steps with their gates are on the task *Move the portal API onto always-on hosting next
to the database* in Your Business Today (goal *Azure Hosting and Infrastructure*), where each
step is a subtask and the runbook PDF is attached.

## Why

On Static Web Apps managed functions the API:

- sleeps after about 20 minutes idle, so the first click of the morning takes ~7 seconds;
- restarts for everyone on every release;
- runs in West Europe while the database is in North Europe, costing ~17 ms per query;
- is capped at 100 MB, which Magick.NET hit on 16 September.

The MCP host and the mailbox worker already run on their own Function Apps for a different
reason (SWA strips the Authorization header), so the portal API was the last piece on managed
functions.

## What Microsoft refused, and when

The first plan (proposal and runbook of 4 September, both attached to the task) was an App
Service plan, **Premium v4 P1v4 Linux, in North Europe**, at £94.02 a month. It failed on quota,
then on capacity:

| Date | What happened |
|---|---|
| 4 Sep | `az appservice plan create` refused: *Current Limit (P1v4 VMs): 0, Current Usage: 0, Amount required: 1*. Support request **2609040050000920** raised for a limit of 2. |
| 10 Sep | Microsoft closed the ticket saying the quota had been increased and a deployment had completed. |
| 15 Sep | Phase 0 passed. Phase 1.1 refused with the identical *Current Limit (P1v4 VMs): 0* message. Reading the full thread: the engineer had expected James to complete the new self-service quota process and closed the case assuming it was done. The self-service view (Microsoft.Quota) showed a limit of 30, but App Service enforces its own allowance, which was 0, so self-service could never lift it. Only the App Service capacity team can. |
| 16 Sep | Microsoft explained the original request had been submitted as *current 60 → new 4*, was read as a reduction, and never reached the capacity team. A fresh self-service request (New Limit 32) was rejected by automatic review. Evidence sent back on the ticket. |
| 21 Sep | Ganesh Chintha: "due to regional constraints and high demand for App Service in this region, PG team unable to approve your quota request"; suggests an alternate region. |
| 22 Sep | Syed Mohsin (Technical Advisor) confirmed: North Europe "is currently experiencing significant demand for App Service capacity"; self-service may be used for another region and SKU; moving SQL and Blob is outside the App Service team's scope; may the case be archived. **P1v4, non-zone-redundant, North Europe, 2 instances: refused on capacity** after 18 days and four engineers. No longer a form problem. |

The refusal is not peculiar to this subscription. An Azure MVP write-up of 21 July 2026 calls
North Europe the worst-hit region in Europe (the Irish grid), with quota increases "almost
certainly going to be refused" "for a number of years" and West Europe also constrained; a
Microsoft Q&A thread of the same month shows UK South refusing even a Consumption plan, with
Microsoft itself suggesting UK West or Flex Consumption, whose quota is a separate model based
on memory. Premium v4 is not offered in UK West at all. App Service plans, the stamp hardware,
are what is scarce; Azure is not, which is why the answer is a different Azure service and not
a different cloud.

The only thing that plan created was the runtime storage account `stjpmsapi69e23c` in North
Europe, which the new plan reuses. Nothing in production was touched.

## The plan now: Azure Functions Flex Consumption

**Flex Consumption (FC1) in North Europe.** Same Functions project, same deploy action, same
region as the database. It has its own quota (250 cores per region by default), no 100 MB cap,
always-ready instances to remove the cold start, and a rolling site update to remove the restart
on release. About £16 a month per always-ready 2 GB instance at list while idle (the same seconds
bill at four times the rate while a request is being handled, so a busy instance costs a few
pounds more), against £94 for P1v4, so the revised ask is +£35–45 a month rather than +£90
(Nigel's yes is its own subtask). The pre-flight prints the live GBP meters.

Instance size is 2,048 MB: it is what the API runs in today on managed functions, and the MCP
host running the same code peaks at 1.35 GB on a 1.75 GB B1. Step 6 reads the memory over the
first week and moves to 4,096 MB if the working set passes ~1.5 GB.

Fallback order if North Europe refuses the Flex create, decided by the Step 1 create itself:

1. **Flex Consumption in UK South** (12 ms from the database, UK compute). Step 1 says how.
2. **Container Apps in North Europe**: min replicas 1, revisions for zero-downtime releases,
   supported as a Static Web Apps linked backend, ~£20–50/month; costs a Dockerfile, a registry
   and a new workflow, about a day.
3. **App Service P1v4 in UK South** (£98.34/month) or P1v3 in UK West, with the database moved
   after cut-over by a failover group (94 MB, minutes, no data loss). Only if 1 and 2 fail.
4. **Sweden Central P1v4**, Microsoft's recommended region; works, but the data leaves the
   UK and Ireland.

Never a different cloud.

### Will it work? (assessed 27 Sep)

The parts under our control are proven: a Flex app in North Europe with an always-ready
instance, next to the database, deployed by the new workflow; every command in this file checks
out against Microsoft's current documents and the CLI source. The one unknown is whether Static
Web Apps will accept that Flex app as its linked backend. For: the link is validated server-side
by resource type, and Functions is a supported type; the authentication it switches on is
supported on Flex; a Microsoft answer of 2025 recommends exactly this, Flex with always-ready
linked to a Static Web App. Against: the documents' list of plans a linked backend may be on
predates Flex, and one 2026 thread reports a Flex link that answered 404s with a reply calling
it unsupported (Step 3 has the detail). More likely to work than not, and unproven; so the plan
finds out first and cheaply: Step 1 and its probe take about 40 minutes, cost £0 and touch
nothing in production. A refusal makes the day the Container Apps fallback, about a day's
work and a similar monthly cost (below). Data protection, the alert rules and the budget do
not depend on the link at all. With the link proven, Steps 2 to 6 are all done the same day.

### What it will cost (27 Sep, from the August bill)

August's bill by service, before VAT, from Cost Management (the command is at the end of this
section):

| Service | August |
|---|---|
| SQL Database | £209.55 |
| Azure App Service (the Static Web App's Standard plan; the MCP host's B1 plan appears to have billed only part of the month) | £6.91 |
| Storage | £0.44 |
| Bandwidth, e-mail, Log Analytics, Functions | under £0.01 |
| **Total before VAT** | **£216.90** |

So the database is 97 per cent of the bill. The £280 quoted on 27 September is either the
figure with VAT (£217 plus 20 per cent is £260) or a month that ran higher; the month-to-date
command settles it. Expect September to carry about £10 more than August for a full month of
the MCP host's plan, so the working baseline is **about £227 a month before VAT**. The 22
September findings' "about £230 after the move" was near the mark for the wrong reason.

The move adds the lines below and removes none: the managed functions were free inside the
Static Web App's Standard plan, and Phase 7's £11 saving was dropped with the shared plan. List
prices before VAT, converted from Microsoft's dollar rates; the pre-flight prints the real GBP
meters and the first invoice is the truth.

| Line | After Step 4 (one instance) | After Step 6 (two instances) |
|---|---|---|
| Everything paid today (August, plus a full month of the MCP host's plan) | £227 | £227 |
| Flex always-ready 2 GB instance, idle rate ($0.000004 per GB-second) | +£16 | +£33 |
| The same instance while handling requests ($0.000016 per GB-second), at a working day's share | +£3 to £6 | +£6 to £10 |
| Flex runtime storage account | under £1 | under £1 |
| SQL 35-day restore and long-term backups, 94 MB database | +£1 to £2 | +£1 to £2 |
| Documents storage geo-redundant with soft delete | +£2 to £3 | +£2 to £3 |
| **Likely bill before VAT** | **about £250** | **about £270** |
| With VAT at 20 per cent | about £300 | about £325 |

The delta is +£23 to £28 a month after Step 4 and +£42 to £49 once the second instance runs,
which is the top of the +£35 to £45 Nigel agreed, or a few pounds over it. The Container Apps
fallback in place of the Flex instance would be about £31 a month for the smallest always-on
replica (0.5 vCPU, 1 GiB) and about £62 for 1 vCPU and 2 GiB.

Three things follow:

- **The budget is £350.** Azure budgets count cost before VAT. A £300 budget puts its 80 per
  cent line at £240, below the expected bill after Step 6, so it would fire every month and
  be ignored. £350 puts that line at £280, above the expected bill, so it speaks only when
  something grows, and the 100 per cent forecast line catches a runaway. That is the figure
  for the budget task.
- **The database is the lever, and it has a question in it.** £210 a month for a 94 MB
  database with near-zero CPU is well above the 0.5 vCore serverless floor, which is about
  £150 in North Europe. The pre-flight now prints the floor it is set to; the metric
  `app_cpu_billed` on the database shows the vCore-seconds actually billed. Whether it should
  move to a fixed tier, and which, is a separate task from this move.
- **This move does not reduce the bill.** It buys the behaviour: no idle freeze, no restart on
  release, the API next to its data.

The command, for the month so far (`az costmanagement query` is an extension the CLI does not
prompt for; `az rest` asks the same API directly). For a whole earlier month replace the
timeframe with `"timeframe":"Custom","timePeriod":{"from":"2026-08-01T00:00:00Z","to":"2026-08-31T23:59:59Z"}`:

```bash
az rest --method post \
  --url "https://management.azure.com/subscriptions/08c5510c-bb27-4da8-b826-a8e76fb270ec/providers/Microsoft.CostManagement/query?api-version=2023-11-01" \
  --body '{"type":"ActualCost","timeframe":"MonthToDate","dataset":{"granularity":"None","aggregation":{"totalCost":{"name":"PreTaxCost","function":"Sum"}},"grouping":[{"type":"Dimension","name":"ServiceName"}]}}' \
  --query "reverse(sort_by(properties.rows, &[0]))" -o table
```

The runbook PDF on the task is the P1v4 edition. Its Phases 2, 3, 4 and 5 apply as written;
Phases 1 and 6 are replaced by Steps 1 and 6 below, and Phase 7 (consolidating the MCP host
and worker onto the plan) no longer applies because Flex has no shared plan.

## What is in this folder

| File | Step | What it does |
|---|---|---|
| `phase0-preflight.sh` | before Step 1 | Read-only checks and rollback snapshots. Writes `~/jpms-upgrade/vars.sh`. Flex edition. |
| `phase1-provision.sh` | Step 1 | Creates the Flex app (the capacity test) and copies the 24 portal settings onto it. Idempotent. |
| `../../.github/workflows/jpms-api.yml` | Step 2 | Deploys the API to the Flex app on every push to `main` touching `api/**`, signed in with a federated credential, then syncs the triggers and waits for the functions to be listed. |
| `../../.github/workflows/jpms-swa-rehearsal.yml` | Step 3 | Temporary. Publishes the frontend to the *upgrade* preview environment with no API. Deleted after Gate 3. |

`quota-p1v4.sh` is gone with the P1v4 route; it is in git history if the story is ever needed.

## The steps

Every command runs on the Mac with the Azure CLI, signed in as
admin.james@jewelenterprises.co.uk on subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec.
Each block prints `BLOCK COMPLETE` or `STOPPED AT THE FIRST ERROR ABOVE`; a stopped block ran
nothing past the failure. Each step ends in a gate: if the output does not match, stop and
paste it to Claude. Steps 1 to 4 in order. Step 4 in a quiet window. Steps 5 and 6 after.

### The weekend order

As first written: Friday, reply to Microsoft, message Nigel. Saturday: Step 1, Step 2, the
budget task, Step 5, the monitoring rules. Saturday or Sunday: Step 3. Sunday evening: Step 4,
then the End-to-End Regression Test. The weekend after: Step 6, the restore drill. Superseded
by the one-day order below; only the restore drill stays for later.

### The same steps in one day (the run of 27 September)

Nothing in Steps 1 to 6 needs a night between them; the only hard orderings are Gate 3
before Step 4, Step 4 in a quiet window, and Step 6 after Step 4 has settled. So: Step 1;
then, before Step 2, start the rehearsal workflow and run the link probe at the end of Step
1, because that is the fastest way to learn whether Static Web Apps will accept a Flex app
at all; Step 2; Step 3 on the preview environment the probe already published; Step 5 while
a workflow runs; Step 4 in the evening, then the regression test; then Step 6 the same
evening. Step 6 was first written a week out for a week of memory readings and a second
instance not yet agreed; neither holds. The memory reading is not a gate (if 2 GB is tight
the fix is one command, below), and the second instance is inside the +£35 to £45 Nigel
agreed.

### Before Saturday

1. Merge this branch's pull request so `main` carries the Flex scripts and workflows. The API
   workflow will fail its first run for want of the variables Step 2 adds; that is harmless.
2. Reply to Microsoft on 2609040050000920: yes, archive it (one line).
3. Nigel's yes on +£35–45 a month, noted on its task. Needed before Step 4, not before Step 1.

### Step 1 — create the Flex Consumption app (Sat, 30 min, £0)

```bash
az login
bash infra/hosting-upgrade/phase0-preflight.sh     # want: North Europe listed, stjpmsapi69e23c exists, func name free, SQL autoPauseMinutes -1
bash infra/hosting-upgrade/phase1-provision.sh     # creates the app, copies the settings, prints Gate 1
```

Nothing bills until always-ready is switched on in Step 4.

If the create is refused on capacity or quota in North Europe:

```bash
FLEX_LOC=uksouth STG=stjpmsapiuks bash infra/hosting-upgrade/phase1-provision.sh
```

Refused there too: stop, paste the output to Claude. Container Apps is next; production is
untouched. Whichever region the app lands in, phase1 records it as `LOC` too, so the runbook's
link commands in Steps 3 and 4 (`--backend-region $LOC`) need no editing.

**Gate 1:** `state Running`, a hostname, `https true`, `sku FlexConsumption`, `memoryMB 2048`;
the 24 portal setting names listed with `MailboxIntake__*` using double underscores; `APIHOST`
and `FUNC_ID` recorded in `~/jpms-upgrade/vars.sh`.

Found on the day: on CLI 2.86 `az functionapp show` returns a Flex app with `state`,
`defaultHostName` and `hostNames` all null while the app is fine, so the script reads the app
through `az resource show` instead. Anything else that needs the app's state or hostname
should do the same.

**Undo if abandoning:** `az functionapp delete -n func-jpms-api-prod -g rg-jpms-prod`, then
delete the plan it created.

**The link probe (5 min, £0, nothing deployed).** Whether Static Web Apps accepts a Flex app
as a linked backend is the one thing this plan cannot prove from documents (see Step 3), and
it can be asked the moment the app exists, before any code is on it. Start Actions →
*Rehearsal: portal frontend to the "upgrade" preview environment* → Run workflow (~6 min),
then:

```bash
cd ~/jpms-upgrade && source vars.sh
az staticwebapp environment list -n $SWA -g $RG --query "[].{env:name,hostname:hostname,status:status}" -o table
az staticwebapp backends validate -n $SWA -g $RG --environment-name upgrade --backend-resource-id "$FUNC_ID" --backend-region $LOC
```

Validate passing (or complaining only about something the environment has, never about the
SKU, plan or resource type) means carry on to Step 2 and leave the environment up for
Step 3. A refusal naming the SKU, the plan or the resource type is the answer the whole
day turns on: stop, paste it to Claude, and the day becomes the Container Apps fallback with
production untouched and the Flex app deleted.

### Step 2 — deploy the API to it and test on its own hostname (Sat, 45 min)

Runbook Phase 2 (pages 7–8) with three changes for Flex.

1. **GitHub signs in without a secret** (runbook 6.2 brought forward). Run:

   ```bash
   cd ~/jpms-upgrade && source vars.sh
   ( set -eo pipefail
   setvar() { [ -n "$2" ] || { echo "refusing to record an empty $1"; return 1; }; grep -v "^$1=" vars.sh > vars.tmp; echo "$1=$2" >> vars.tmp; mv vars.tmp vars.sh; }
   [ -n "$APP_ID" ] || { APP_ID=$(az ad app create --display-name github-jpms-api-deploy --query appId -o tsv); setvar APP_ID "$APP_ID"; }; echo "APP_ID=$APP_ID"
   az ad sp create --id $APP_ID -o none 2>/dev/null || true
   SP_OBJ=$(az ad sp show --id $APP_ID --query id -o tsv)
   az role assignment create --assignee-object-id $SP_OBJ --assignee-principal-type ServicePrincipal --role "Website Contributor" --scope "$FUNC_ID" -o none
   az ad app federated-credential create --id $APP_ID --parameters '{"name":"github-main","issuer":"https://token.actions.githubusercontent.com","subject":"repo:jamesbeadle/jewel-portal:ref:refs/heads/main","audiences":["api://AzureADTokenExchange"]}' -o none
   TENANT=$(az account show --query tenantId -o tsv)
   echo "GitHub repository VARIABLES to add:"; echo " AZURE_CLIENT_ID=$APP_ID"; echo " AZURE_TENANT_ID=$TENANT"; echo " AZURE_SUBSCRIPTION_ID=$SUB"; echo " JPMS_API_APP_NAME=$FUNC"
   ); rc=$?; if [ $rc -eq 0 ]; then echo "BLOCK COMPLETE"; else echo "STOPPED AT THE FIRST ERROR ABOVE (exit $rc) - do not continue; paste everything to Claude"; fi
   ```

   In GitHub: Settings → Secrets and variables → Actions → **Variables**: add the four it
   printed. No publish-profile secret; skip runbook 2.1.

2. **Run the workflow.** Actions → *Deploy JPMS portal API to Azure Functions* → Run workflow.
   Green in about 4 minutes.

3. **Test** (runbook 2.3 against `$APIHOST`): `/api/version` → 200 with an `x-jpms-version`
   header; `/api/auth/me` → 401; `/api/well-known/oauth-authorization-server` → 200. The first
   call may take ~10 s (no always-ready yet). Five minutes later run the App Insights block on
   runbook page 8: requests under `func-jpms-api-prod`, SQL dependency failed 0 with p50 in
   single-digit ms, no exceptions.

**Gate 2:** all of 3.

**Known trap:** Flex abandons an app whose start-up passes 30 seconds, and the limit cannot be
raised; the first call's time in 3 says how close this API runs to it (it registers the whole
117-entity model at start-up, so it will not be instant). Well under 30 s: fine. Near it, or
a 5xx on the first call that clears on the second: paste the timing to Claude before Step 3.

**Known trap:** a green deploy whose Functions list shows only *WarmUp* is a known fault of the
deploy action on Flex (Azure/functions-action issue 373, open): it uploads the package and never
asks the host to sync its triggers, and the WarmUp placeholder is what the list shows until
specialisation completes. The workflow now does the sync itself after every deploy, waits for
`GetAppVersion` to be listed, and restarts the app once if it is slow; a red *Sync the triggers*
step means paste the run log to Claude. `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED`, which an
earlier edition of this plan set, is a Consumption-plan setting that Flex ignores; it is gone.

### Step 3 — rehearse: link the new API to a preview copy of the portal (Sat or Sun, 45 min)

Runbook Phase 3 (pages 9–11), unchanged. This is the go/no-go for Flex, and it is a real
question, not a formality. Checked 27 Sep: the Static Web Apps documents still list the
Functions plans a linked backend may be on as Consumption, Premium and Dedicated (a 2022
table) and say nothing either way about Flex; Microsoft's own Consumption-to-Flex migration
guide (Sep 2026) says built-in authentication, which the link switches on, is set up on a Flex
app like any other; and one Microsoft Q&A thread from 2026 describes a Flex app linked to a
Static Web App whose `/api/*` answered the Static Web App's own 404, with the reply calling
Flex unsupported and naming Consumption or Premium instead. That thread is one report with no
detail of how the app was set up, so it is not a verdict, and it is why the probe at the end of
Step 1 and this rehearsal exist: the proxy is proven on a preview environment, never in
production. (The `rolesSource` part of that thread does not apply here: the portal never uses
Static Web Apps' own sign-in; it has its own cookie session behind `/api`.)

1. Actions → *Rehearsal: portal frontend to the "upgrade" preview environment* → Run workflow
   (~6 min).
2. Run the 3.2 block (runbook page 9): validate, link; curl the preview hostname → 200 and
   version; discovery → an issuer; direct hostname → 401.
3. In the browser on the preview URL: log in, dashboard, a project, a to-do, the Control
   Centre, download a document, log out and in. Read only: it is the live database.
4. Re-run the API workflow while linked → green.
5. Run the 3.3 block (runbook page 10): unlink, delete the environment. Delete
   `.github/workflows/jpms-swa-rehearsal.yml`, commit *Remove rehearsal workflow*, push.

**Gate 3:** login and pages worked on the preview URL; the API workflow re-ran green while
linked; the environment list shows only *default*; the direct hostname answers 200 again.

If the link is refused naming the SKU, plan or resource type: stop, paste it to Claude. That is
the Container Apps fallback, and production is untouched.

### Step 4 — production cut-over (Sun evening or early morning, 30 min, 1–3 min API downtime)

Runbook Phase 4 (pages 12–14), unchanged. Needs Gate 3 and Nigel's yes.

**Before:** switch on the always-ready instance, and raise the instance's HTTP concurrency.
Billing starts with the first line, about 55p a day; the second is free. Flex hands a 2 GB
instance 16 requests at a time by default and starts a cold instance for the seventeenth, and
a Blazor route load fetches several things at once, so a Monday-morning burst from a dozen
people would spill onto cold instances and feel exactly like the lag this move is meant to
end. The API waits on SQL rather than on the CPU, so one warm instance can hold far more:

```bash
az functionapp scale config always-ready set -g rg-jpms-prod -n func-jpms-api-prod --settings http=1
az functionapp scale config set -g rg-jpms-prod -n func-jpms-api-prod --trigger-type http --trigger-settings perInstanceConcurrency=100
az functionapp scale config show -g rg-jpms-prod -n func-jpms-api-prod -o json     # want alwaysReady http 1, triggers http perInstanceConcurrency 100
curl -s -o /dev/null -w "%{http_code} in %{time_total}s\n" https://$APIHOST/api/version   # twice; the second well under a second
```

1. The 4.1 block (read-only pre-check). The validate may complain only that managed functions
   are present; that is expected and confirms the order.
2. Edit `.github/workflows/jpms-swa.yml`: `api_location: ''`, and remove `'api/**'` from the
   **push** paths only (one line, not the two of runbook Appendix C): under `pull_request` it
   stays, because that is what runs the *Build the API* compile check on a pull request, and
   `tests.yml` is manual. Commit *Portal API now served by func-jpms-api-prod (linked
   backend)*, push, watch *JPMS portal* go green (5–8 min). The API gap starts when it goes
   green.
3. The moment it is green: the link command 4.1 printed, then the 4.4 block: polls reach 200,
   discovery shows an issuer, managed functions list is empty, direct hostname 401.
4. Log in on portal.jewelbb.co.uk: dashboard, a project, a document download. Ask Nigel to do
   the same from his own machine.
5. The 15-minute App Insights block (runbook page 13): all traffic under `func-jpms-api-prod`,
   no 5xx. The only failures should be 401s from sessions that predate the cut-over.

**Gate 4:** all of 3–5, and after 20 minutes idle the first click is instant.

**Rollback:** `az staticwebapp backends unlink -n swa-jpms-prod -g rg-jpms-prod
--remove-backend-auth`, `git revert` the step 2 commit, push (5–8 min). A wrong setting is
fixed on the Function App instead with `az functionapp config appsettings set`; that restarts
the app in about ten seconds and users stay on the portal.

**After:** run the End-to-End Regression Test. Update the hosting line in `api/README.md`.
Leave a week before Step 6.

### Step 5 — data protection (any time, 15 min, a few pounds a month)

Runbook Phase 5 (pages 15–16) without its budget lines; the £350 budget and alerts are their
own task (£350, not the £300 the task was raised with: see *What it will cost*). Independent of the hosting: do it Saturday while a deploy runs.

```bash
cd ~/jpms-upgrade && source vars.sh
( set -eo pipefail
az sql db str-policy set -g $RG -s $SQLSRV -n jpms --retention-days 35 --query "{pitrDays:retentionDays}" -o json
az sql db ltr-policy set -g $RG -s $SQLSRV -n jpms --weekly-retention P12W --monthly-retention P12M --query "{weekly:weeklyRetention,monthly:monthlyRetention}" -o json
az storage account update -n $DOCS_STG -g $RG --sku Standard_GRS --query "{sku:sku.name}" -o json
az storage account blob-service-properties update --account-name $DOCS_STG -g $RG --enable-delete-retention true --delete-retention-days 14 --enable-container-delete-retention true --container-delete-retention-days 14 --query "{blobSoftDelete:deleteRetentionPolicy,containerSoftDelete:containerDeleteRetentionPolicy}" -o json
rm -f ~/jpms-upgrade/swa-appsettings-before.json; ls -la ~/jpms-upgrade
); rc=$?; if [ $rc -eq 0 ]; then echo "BLOCK COMPLETE"; else echo "STOPPED AT THE FIRST ERROR ABOVE (exit $rc) - do not continue; paste everything to Claude"; fi
```

**Gate 5:** pitrDays 35; weekly P12W, monthly P12M; sku Standard_GRS; both soft-delete
policies enabled, 14 days; the secrets snapshot gone. The GRS copy completes in the background
over a few hours.

### Step 6 — releases without interruption (the same evening, after Step 4 has settled, 30 min)

Flex has no deployment slots. Its rolling site update replaces instances in batches during a
deploy; with one instance it still restarts, so this needs two.

1. Two always-ready instances (about £32 a month for both at 2 GB; two is also resilience):

   ```bash
   az functionapp scale config always-ready set -g rg-jpms-prod -n func-jpms-api-prod --settings http=2
   ```

2. Rolling update. Checked 27 Sep: generally available in East Asia, West Central US, North
   Central US and West US 2 by Microsoft's note of May 2026 with the other regions "over the
   following weeks", a September 2026 write-up calls it available everywhere, and North Europe
   is named nowhere either way, so the command below is the test. CLI 2.87 or later has a
   command for it; the ARM PATCH beneath is the same change for an older CLI:

   ```bash
   cd ~/jpms-upgrade && source vars.sh
   az functionapp update-strategy config set -g $RG -n $FUNC --type RollingUpdate
   az functionapp update-strategy config show -g $RG -n $FUNC -o json
   # older CLI:
   # az rest --method patch --url "https://management.azure.com$FUNC_ID?api-version=2024-11-01" \
   #   --body '{"properties":{"functionAppConfig":{"siteUpdateStrategy":{"type":"RollingUpdate"}}}}' \
   #   --query "properties.functionAppConfig.siteUpdateStrategy" -o json
   ```

   Two things Microsoft's own page says to expect: an app on a single instance still sees a
   brief interruption on deploy whatever the strategy, which is why this step runs two; and in
   a region where the rollout is still in progress, the deploy that follows the change is
   carried out with the previous strategy, so judge it on the second deploy, not the first. If
   the command is refused naming the property or the region, North Europe does not have it
   yet: keep the two instances and release in quiet windows with the ~10 s restart until it
   lands; check monthly.

3. Run the API workflow twice (Actions → *Deploy JPMS portal API to Azure Functions* → Run
   workflow; no code change is needed, a re-deploy of the same build exercises the update
   exactly as a release would), with this running in a second terminal throughout:

   ```bash
   while true; do curl -s -o /dev/null -w "%{http_code} %{time_total}s $(date +%T)\n" https://portal.jewelbb.co.uk/api/version; sleep 2; done
   ```

   The first run may still restart (a region mid-rollout applies the change one deploy
   late); the second is the one that counts.

**Gate 6:** no failed calls and no gap in the 200s during the second run; or RollingUpdate
refused in North Europe and that recorded on the task with the quiet-window rule instead.
(The `x-jpms-version` header only changes on a real release; a re-deploy of the same build
keeps it, which is fine.)

Not a gate, but during the first week: look at the app's memory (Metrics blade, memory
working set) and at App Insights for restarts or out-of-memory exceptions. Over ~1.5 GB, or
any of those → `az functionapp scale config set -g rg-jpms-prod -n func-jpms-api-prod
--instance-memory 4096` (doubles the instance cost, restarts the app).

### Afterwards

- Old P1v4 leftovers gone: nothing named `plan-jpms-prod` exists; `stjpmsapi69e23c` is the
  Flex app's storage and nothing else.
- The related tasks on the same goal: the £350 budget with alerts, the five monitoring alert
  rules, and the backup restore rehearsal after Step 5.
- For the next portal, do not repeat this migration: build it on this shape from day one.
  See `docs/09-operations/hosting-a-new-portal.md`.
