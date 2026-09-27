# JPMS hosting upgrade — the plan, and how it got here

The portal's API moves off Static Web Apps managed functions onto its own always-ready
Function App in North Europe, next to the database. This file is the plan of record in the
repository; the same plan, step by step with its gates, is on the task *Move the portal API
onto always-on hosting next to the database* in Your Business Today (goal *Hosting and
Infrastructure*), where each step is a subtask and the runbook PDF is attached.

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
| 22 Sep | Final answer after 18 days and four engineers: **P1v4 in North Europe refused on capacity**. App Service plans are scarce across North Europe, West Europe and UK South this year. |

The only thing that plan created was the runtime storage account `stjpmsapi69e23c` in North
Europe, which the new plan reuses. Nothing in production was touched.

## The plan now: Azure Functions Flex Consumption

**Flex Consumption (FC1) in North Europe.** Same Functions project, same deploy action, same
region as the database. It has its own quota (250 cores per region by default), no 100 MB cap,
always-ready instances to remove the cold start, and a rolling site update to remove the restart
on release. About £16 a month per always-ready 2 GB instance at list, against £94 for P1v4, so
the revised ask is +£35–45 a month rather than +£90 (Nigel's yes is its own subtask).

Fallback order if North Europe refuses the Flex create: **UK South** (Step 1 says how), then
**Container Apps in North Europe**, then **P1v4 in UK South with the database moved after**.
Never a different cloud.

The runbook PDF on the task is the P1v4 edition. Its Phases 2, 3, 4 and 5 apply as written;
Phases 1 and 6 are replaced by Steps 1 and 6 below, and Phase 7 (consolidating the MCP host
and worker onto the plan) no longer applies because Flex has no shared plan.

## What is in this folder

| File | Step | What it does |
|---|---|---|
| `phase0-preflight.sh` | before Step 1 | Read-only checks and rollback snapshots. Writes `~/jpms-upgrade/vars.sh`. Flex edition. |
| `phase1-provision.sh` | Step 1 | Creates the Flex app (the capacity test) and copies the 24 portal settings onto it. Idempotent. |
| `../../.github/workflows/jpms-api.yml` | Step 2 | Deploys the API to the Flex app on every push to `main` touching `api/**`, signed in with a federated credential. |
| `../../.github/workflows/jpms-swa-rehearsal.yml` | Step 3 | Temporary. Publishes the frontend to the *upgrade* preview environment with no API. Deleted after Gate 3. |

`quota-p1v4.sh` is gone with the P1v4 route; it is in git history if the story is ever needed.

## The steps

Every command runs on the Mac with the Azure CLI, signed in as
admin.james@jewelenterprises.co.uk on subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec.
Each block prints `BLOCK COMPLETE` or `STOPPED AT THE FIRST ERROR ABOVE`; a stopped block ran
nothing past the failure. Each step ends in a gate: if the output does not match, stop and
paste it to Claude. Steps 1 to 4 in order. Step 4 in a quiet window. Steps 5 and 6 after.

### Before Saturday

1. Merge this branch's pull request so `main` carries the Flex scripts and workflows. The API
   workflow will fail its first run for want of the variables Step 2 adds; that is harmless.
2. Reply to Microsoft on 2609040050000920: yes, archive it (one line).
3. Nigel's yes on +£35–45 a month, noted on its task. Needed before Step 4, not before Step 1.

### Step 1 — create the Flex Consumption app (Sat, 30 min, £0)

```bash
az login
bash infra/hosting-upgrade/phase0-preflight.sh     # want: North Europe listed, stjpmsapi69e23c exists, func name free
bash infra/hosting-upgrade/phase1-provision.sh     # creates the app, copies the settings, prints Gate 1
```

Nothing bills until always-ready is switched on in Step 4.

If the create is refused on capacity or quota in North Europe:

```bash
FLEX_LOC=uksouth STG=stjpmsapiuks bash infra/hosting-upgrade/phase1-provision.sh
```

Refused there too: stop, paste the output to Claude. Container Apps is next; production is
untouched.

**Gate 1:** `state Running`, a hostname, `https true`, `sku FlexConsumption`; the 24 portal
setting names listed with `MailboxIntake__*` using double underscores; `APIHOST` and `FUNC_ID`
recorded in `~/jpms-upgrade/vars.sh`.

**Undo if abandoning:** `az functionapp delete -n func-jpms-api-prod -g rg-jpms-prod`, then
delete the plan it created.

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

**Known trap:** a green deploy whose Functions list shows only *WarmUp* is the Flex/.NET
packaging fault. Confirm `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED=0` is set (phase1 sets it) and
re-run the workflow. Still wrong: paste the run log to Claude.

### Step 3 — rehearse: link the new API to a preview copy of the portal (Sat or Sun, 45 min)

Runbook Phase 3 (pages 9–11), unchanged. This is the go/no-go for Flex: the link switches on
built-in authentication on the new app, and nothing documents that for a Flex app, so it is
proven here on a preview environment, never in production.

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

**Before:** switch on the always-ready instance. Billing starts here, about 55p a day:

```bash
az functionapp scale config always-ready set -g rg-jpms-prod -n func-jpms-api-prod --settings http=1
curl -s -o /dev/null -w "%{http_code} in %{time_total}s\n" https://$APIHOST/api/version   # twice; the second well under a second
```

1. The 4.1 block (read-only pre-check). The validate may complain only that managed functions
   are present; that is expected and confirms the order.
2. Edit `.github/workflows/jpms-swa.yml`: `api_location: ''` and remove the two `'api/**'`
   path entries (runbook Appendix C). Commit *Portal API now served by func-jpms-api-prod
   (linked backend)*, push, watch *JPMS portal* go green (5–8 min). The API gap starts when it
   goes green.
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

Runbook Phase 5 (pages 15–16) without its budget lines; the £300 budget and alerts are their
own task. Independent of the hosting: do it Saturday while a deploy runs.

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

### Step 6 — releases without interruption (the weekend after Step 4, 30 min)

Flex has no deployment slots. Its rolling site update replaces instances in batches during a
deploy; with one instance it still restarts, so this needs two.

1. Two always-ready instances (about £32 a month for both at 2 GB; two is also resilience):

   ```bash
   az functionapp scale config always-ready set -g rg-jpms-prod -n func-jpms-api-prod --settings http=2
   ```

2. Rolling update. ARM only (public preview, not in the CLI or portal; GA in four US/Asia
   regions and rolling out elsewhere):

   ```bash
   cd ~/jpms-upgrade && source vars.sh
   az rest --method patch --url "https://management.azure.com$FUNC_ID?api-version=2024-11-01" \
     --body '{"properties":{"functionAppConfig":{"siteUpdateStrategy":{"type":"RollingUpdate"}}}}' \
     --query "properties.functionAppConfig.siteUpdateStrategy" -o json
   ```

   If the response is `null` or the PATCH is refused naming the property or the region, North
   Europe does not have it yet: keep the two instances and release in quiet windows with the
   ~10 s restart until it lands; check monthly.

3. Push a trivial API change and watch:

   ```bash
   while true; do curl -s -o /dev/null -w "%{http_code} %{time_total}s $(date +%T)\n" https://portal.jewelbb.co.uk/api/version; sleep 2; done
   ```

**Gate 6:** no failed calls during the deploy, and the `x-jpms-version` header changes over;
or RollingUpdate refused in North Europe and that recorded on the task with the quiet-window
rule instead.

Also here: read the app's memory over its first week (Metrics blade, memory working set). Over
~1.5 GB → `az functionapp scale config set -g rg-jpms-prod -n func-jpms-api-prod
--instance-memory 4096` (doubles the instance cost, restarts the app).

### Afterwards

- Old P1v4 leftovers gone: nothing named `plan-jpms-prod` exists; `stjpmsapi69e23c` is the
  Flex app's storage and nothing else.
- The related tasks on the same goal: the £300 budget with alerts, the five monitoring alert
  rules, and the backup restore rehearsal after Step 5.
- For the next portal, do not repeat this migration: build it on this shape from day one.
  See `docs/09-operations/hosting-a-new-portal.md`.
