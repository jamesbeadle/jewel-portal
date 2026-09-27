# Hosting brief for any new client portal

The instruction set an AI session follows to host a new portal on Azure with no lag, at a
reasonable cost, without repeating September 2026. It is abstract: every name carries the
customer's prefix, every region is chosen on the day. The reasoning behind each choice is in
`hosting-a-new-portal.md`; the Jewel portal's own migration and everything it taught is in
`../../infra/hosting-upgrade/README.md`.

## The prompt to start with

Paste this, filled in, to the session that will do the work:

> Host the **<Customer>** portal on Azure following `docs/09-operations/hosting-brief-for-any-new-portal.md` in the jewel-portal repository, with customer prefix `<prefix>`, subscription `<id>`, environment `prod`, and the budget `<£ per month>`. Run the region probe first and tell me the region before creating anything. Every command runs on my Mac after `az login`; give me each block with what its output should say, and stop at any output that does not match. Do not raise any Azure quota ticket. When it is done, prove each of the acceptance checks at the end of the brief and record the numbers on the task.

## Parameters

| Name | Meaning | Example |
|---|---|---|
| `CUSTOMER` | prefix in every resource name, lower case, no spaces | `pfp` |
| `ENV` | `prod` or `test` | `prod` |
| `SUB` | the subscription id | |
| `LOC` | the region the probe chose | `northeurope` |
| `BUDGET` | monthly budget before VAT, set above the expected bill | `150` |
| `ALERT_EMAIL` | who the budget and alert rules write to | |

Resource names: `rg-$CUSTOMER-$ENV`, `sql-$CUSTOMER-$ENV-<random>`, `func-$CUSTOMER-api-$ENV`,
`func-$CUSTOMER-mcp-$ENV`, `func-$CUSTOMER-worker-$ENV`, `swa-$CUSTOMER-$ENV`,
`st${CUSTOMER}${ENV}<random>` (documents), `st${CUSTOMER}api<random>` (Functions runtime),
`appi-$CUSTOMER-$ENV`, `entra-$CUSTOMER-$ENV`.

## The rules that are not negotiable

1. **Never an App Service plan, never a quota ticket.** App Service plans (P0v4, P1v4, B1 and
   friends) are the scarce hardware in Europe; the Jewel ticket took eighteen days and ended in
   a refusal. Compute is Azure Functions Flex Consumption, which has its own capacity pool and
   quota. If Flex is not offered in the region you need, change region, never plan type.
2. **One region for everything**, chosen by capacity on the day. Compute in one region and the
   database in another costs 12 to 17 ms on every query.
3. **Nothing that sleeps.** Always-ready instances on every app a user waits for; the database
   on a tier that never pauses.
4. **Publish for a Linux host.** The API project trims platform folders for the Static Web
   App's Windows host; a Linux Function App must be published with every runtime kept, or its
   SQL driver is missing and every database read fails while the deploy stays green.
5. **The Static Web App is Standard and linked to the API from day one**, `api_location: ''`
   from the first workflow, so managed functions are never published.
6. **The budget is set before VAT, with its 80 per cent line above the expected bill**, so it
   speaks only when something grows.
7. **No stored secrets in GitHub.** Federated credentials, one Entra application per
   repository, Website Contributor on each Function App it deploys.

## 1. Region probe (read-only, run first)

```bash
az login && az account set --subscription $SUB
az version --query '"azure-cli"' -o tsv                                  # want 2.87 or newer
az functionapp list-flexconsumption-locations -o table                  # want your region listed
az sql db list-editions -l $LOC --edition Standard --query "[].{name:name}" -o table | head   # the DTU tier below, offered there
```

North Europe or UK South, whichever lists Flex; UK West if neither; Sweden Central only with the
customer's written agreement that their data leaves the UK and Ireland. Tell the person the
region before creating anything.

## 2. The shape, and what each piece costs (list prices, before VAT, September 2026)

| Piece | Service | Setting that matters | About |
|---|---|---|---|
| Portal frontend | Static Web App, **Standard** | linked backend from day one | £7 |
| Portal API | Functions **Flex Consumption**, .NET 8 isolated, 2 GB | one always-ready instance, HTTP concurrency 100 | £16 idle, about £20 busy; a second instance for releases without interruption doubles it |
| MCP connector host | a second Flex app, same code, route prefix blanked at deploy | one always-ready instance | £16 to £20 |
| Background worker | a third Flex app, no always-ready | timers and queues tolerate a cold start | a few pounds |
| Database | Azure SQL, **Standard S2 (50 DTU)**, fixed compute, never pauses, 35-day restore | S1 (£23) for a very small client, S3 (£115) if DTU sits over 60 per cent | £58 |
| Documents | Storage V2, Standard_GRS, 14-day soft delete | no public blob access | a few pounds |
| Telemetry | Application Insights, same resource group | | a few pounds |
| Sign-in | one Entra app registration per environment | | free |

**About £110 a month before VAT with one warm API instance, about £130 with two.** The Jewel
portal pays £210 a month for its serverless database at a 0.5 vCore floor; a new client on
Standard S2 gets a database that never sleeps for a quarter of that. Choose serverless only
when the customer's load is genuinely spiky, and then with auto-pause disabled.

## 3. Provision, in this order (each block idempotent)

1. **Resource group** in `$LOC`.
2. **SQL server and database**: `az sql db create ... --service-objective S2`, then
   `az sql db str-policy set --retention-days 35` and
   `az sql db ltr-policy set --weekly-retention P12W --monthly-retention P12M`, a firewall
   rule `AllowAzureServices` from `0.0.0.0` to `0.0.0.0`, backup storage redundancy Geo.
3. **Documents storage**: `--sku Standard_GRS --kind StorageV2 --allow-blob-public-access false`,
   then blob and container soft delete at 14 days.
4. **Application Insights** in the same resource group (the Function App create requires it).
5. **Three Function Apps**, each with its own runtime storage account in `$LOC`. The create is
   the capacity test; a refusal means another region, never another plan type:

   ```bash
   az functionapp create -g $RG -n func-$CUSTOMER-api-$ENV --storage-account $API_STG \
     --flexconsumption-location $LOC --runtime dotnet-isolated --runtime-version 8.0 \
     --instance-memory 2048 --app-insights $AI --https-only true
   az functionapp scale config always-ready set -g $RG -n func-$CUSTOMER-api-$ENV --settings http=1
   az functionapp scale config set -g $RG -n func-$CUSTOMER-api-$ENV --trigger-type http --trigger-settings perInstanceConcurrency=100
   ```

   Repeat for the MCP host; for the worker leave always-ready off. Read the app afterwards
   with `az resource show --ids <id> --query "{state:properties.state,host:properties.defaultHostName}"`,
   not `az functionapp show`, which on CLI 2.86 returns a Flex app with no state or hostname.
6. **Static Web App, Standard**, then link the API before the first deploy:

   ```bash
   az staticwebapp backends link -n swa-$CUSTOMER-$ENV -g $RG --backend-resource-id <api app id> --backend-region $LOC
   ```

   Linking switches on built-in authentication on the API app so only the Static Web App can
   call it; the direct hostname answering 401 afterwards is correct. If you ever unlink with
   `--remove-backend-auth`, that authentication stays on with no provider: switch it off with
   a PUT of `{"properties":{"platform":{"enabled":false}}}` to `config/authsettingsV2`.
7. **Entra app registration** with the Static Web App's redirect URIs.
8. **Settings** on the API app first, then copied to the MCP host and worker (`:` becomes
   `__`; the runtime's own keys `AzureWebJobsStorage`, `FUNCTIONS_*`, `WEBSITE_*`,
   `APPLICATIONINSIGHTS_*` are never copied). Do not set `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED`;
   Flex ignores it.
9. **Budget and alerts**: a monthly budget of `$BUDGET` on the subscription with e-mail at 80
   per cent actual and 100 per cent forecast, and the alert rules in `infra/setup-alerts.sh`
   (server errors, slow responses, an exception spike, sign-in failures, and the outside-in
   availability test that catches a gateway 503).

## 4. Deploy

- `azure/login` with a federated credential trusting `repo:<owner>/<repo>:ref:refs/heads/main`;
  repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` and the
  app names; no publish profiles.
- `dotnet publish api/JpmsApi.csproj -c Release -o ./publish -p:KeepAllRuntimes=true`, then
  `Azure/functions-action@v1` with `sku: flexconsumption` and `remote-build: false`.
- After the deploy, sync the triggers and wait for a known function to be listed; the action
  does not, and a green deploy can otherwise serve nothing. `.github/workflows/jpms-api.yml`
  is the model, including that step.
- The Static Web App workflow carries `api_location: ''` and never triggers on `api/**`; keep
  `api/**` under `pull_request` so a pull request still gets its compile check.
- Releases without interruption: two always-ready instances and the rolling site update,
  `az functionapp update-strategy config set --type RollingUpdate` (CLI 2.87) or the full
  `functionAppConfig` PATCH in the Jewel README's Step 6. Judge it on the second deploy after
  the change.

## 5. Code rules the host enforces

- No HTTP route may start with `admin/`; the Functions host reserves it and silently refuses
  the function. The Jewel portal shipped two such endpoints that never loaded.
- The app must finish starting in 30 seconds on Flex; do not run migrations or heavy work at
  start-up.
- Every endpoint a user waits for stays under the Static Web App's 45-second request limit;
  long work goes to the worker.

## 6. Acceptance checks before handing over

Record the numbers on the task.

1. First call after 20 minutes with nobody on the portal: `curl -w "%{time_total}"` on
   `/api/version` through the portal domain, well under two seconds; every call after it
   under half a second.
2. A release watched by `while true; do curl ... /api/version; sleep 2; done`: no failed
   call. With two instances and the rolling update, no gap either.
3. The direct Function App hostname answers 401; the portal domain answers 200; the version
   header on both matches the announced version, never `dev`.
4. A bogus login answers 401 in under a second (it proves the database is reached; a 500 or a
   240-second hang means the SQL driver is missing from the package).
5. `az staticwebapp environment functions` lists nothing: no managed functions were ever
   published.
6. Point-in-time restore 35 days, long-term retention set, documents storage GRS with soft
   delete, budget and alert rules present, and one restore rehearsed and written down.
7. App Insights, after a day of use: requests by name with p95; anything over a second is a
   task for the code, not the hosting.
