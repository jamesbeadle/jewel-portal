# Hosting a new portal on Azure without the lag

The instruction set an AI session follows to do this for any customer, with the parameters,
the commands, the costs and the acceptance checks, is `hosting-brief-for-any-new-portal.md`.
This page is the reasoning behind it.

The Jewel portal spent September 2026 moving its API off Static Web Apps managed functions
because of four things nobody chose on purpose: a cold start after 20 minutes idle, a restart
for everyone on every release, the API in a different region from its database, and a 100 MB
package cap. The full story is in `infra/hosting-upgrade/README.md`. A new customer with the
same shape (Blazor portal, Functions API, MCP connector, Azure SQL, document storage) is set up
so none of it happens, in this order.

## 1. Pick the region by capacity, before naming anything

Everything sits in **one region**, chosen by what Azure will actually sell you there today.
App Service plans (P1v4 and friends) were refused on capacity in North Europe in September 2026
and are scarce across North Europe, West Europe and UK South. Flex Consumption has its own
quota (250 cores per region by default) and was not affected. So the region is decided by three
read-only checks, run on the day:

```bash
az functionapp list-flexconsumption-locations -o table                 # want your region listed
az sql db list-editions -l <region> --edition GeneralPurpose -o table   # serverless GP_S_Gen5 offered there
```

Static Web Apps content is served globally; its `--location` only places the metadata, so it
does not constrain the choice.

North Europe or UK South, whichever lists Flex and SQL; UK West if neither, since Microsoft
says it is not under the same pressure, though Premium v4 is not sold there; Sweden Central only
with the customer's agreement that their data leaves the UK and Ireland. Do not put the
database in one region and the compute in another to get a cheaper tier: the round trip
(North Europe to West Europe 17 ms, to UK South 12 ms, UK South to UK West 7 ms) costs more
than the tier.

The rule behind all of this, learned in September 2026: never pin the offer to a SKU or a
region. Provision each customer into whichever region sells the capacity that day, keep the
scripts parameterised, and sell the behaviour (always-on, next to its data, releases without
interruption), never the hardware. The full reasoning is in
`2026-09-22-azure-p1v4-refused-and-the-way-forward.md`.

Never raise a quota ticket for an App Service plan to get started. The Jewel ticket took 18
days and ended in a refusal. If Flex is not offered in the region you need, choose another
region rather than another plan type.

## 2. The shape

| Piece | Service | Why this and not the cheaper thing |
|---|---|---|
| Portal frontend | Static Web App, **Standard** | Standard is required to link your own backend. The Free tier can only use managed functions, which is the cold-start trap. |
| Portal API | Azure Functions **Flex Consumption**, .NET 8 isolated, 2 GB instances, **one always-ready instance** from day one | Always-ready removes the cold start. Same region as SQL removes the per-query latency. No 100 MB cap. About £16 a month per instance. |
| MCP connector host | A second Flex Consumption app, same region, same code, route prefix blanked at deploy | SWA strips the Authorization header before managed functions see it, so bearer-authenticated MCP calls need their own host. Give it an always-ready instance too, or the first tool call each morning waits. |
| Background worker (mailbox, timers) | A third Flex Consumption app, or Consumption if nothing user-facing waits on it | Timers and queues tolerate a cold start; users do not. |
| Database | Azure SQL, General Purpose **serverless, auto-pause disabled**, min 0.5 vCore, zone or geo backup redundancy | Auto-pause is the database's own cold start (30 seconds or more). Disabled, it idles at the minimum vCore and never sleeps. |
| Documents | Storage account, StorageV2, **Standard_GRS**, blob and container soft delete 14 days, no public blob access | Geo-redundant from the start; converting later is a background copy but still a task. |
| Telemetry | Application Insights in the same resource group as the Function Apps | `az functionapp create --app-insights` requires it in the same group. |
| Sign-in | One Entra app registration per environment | Test and production never share redirect URIs. |

Build the test environment on the same shape at the smallest sizes (Flex with no always-ready
instance, SQL serverless with auto-pause on) so the deploy pipeline is identical and only the
sizing differs. Test is allowed to be slow; production is not.

## 3. Provision in this order

Each block is idempotent. Names carry the customer prefix and the environment:
`rg-<customer>-prod`, `sql-<customer>-prod-<random>`, `func-<customer>-api-prod`,
`func-<customer>-mcp-prod`, `func-<customer>-worker-prod`, `swa-<customer>-prod`,
`st<customer>prod<random>` (documents), `st<customer>api<random>` (Functions runtime),
`appi-<customer>-prod`, `entra-<customer>-prod`.

1. **Resource group** in the chosen region.
2. **SQL server and database.** `infra/azure-prod-setup-v2.sh` is the model: serverless,
   `--auto-pause-delay -1`, `--backup-storage-redundancy Geo`, firewall rule for Azure services,
   35-day point-in-time restore and a 12-week / 12-month long-term retention policy set at
   creation, not after go-live.
3. **Documents storage** with GRS and soft delete from the first command.
4. **Application Insights.**
5. **The three Function Apps**, each with its own runtime storage account in the same region:

   ```bash
   az functionapp create -g $RG -n func-<customer>-api-prod --storage-account $API_STG \
     --flexconsumption-location $LOC --runtime dotnet-isolated --runtime-version 8.0 \
     --instance-memory 2048 --app-insights $AI --https-only true
   az functionapp scale config always-ready set -g $RG -n func-<customer>-api-prod --settings http=1
   az functionapp scale config set -g $RG -n func-<customer>-api-prod --trigger-type http --trigger-settings perInstanceConcurrency=100
   ```

   The concurrency line matters: a 2 GB instance takes 16 requests at once by default and
   starts a cold instance for the seventeenth, and a portal route load fetches several things
   at once. Repeat both for the MCP host; for the worker leave always-ready off. After every
   deploy, sync the triggers and wait for the function list, because the deploy action does not
   and a green .NET isolated deploy on Flex can list only *WarmUp* until something does; the
   step in `.github/workflows/jpms-api.yml` is the model.
6. **Static Web App, Standard**, with the API app linked as its backend *before* the first
   deploy, so managed functions are never published:

   ```bash
   az staticwebapp backends link -n swa-<customer>-prod -g $RG \
     --backend-resource-id $(az functionapp show -n func-<customer>-api-prod -g $RG --query id -o tsv) \
     --backend-region $LOC
   ```

   The SWA workflow then carries `api_location: ''` from day one and never triggers on
   `api/**`. Linking switches on built-in authentication on the Function App so only the SWA
   can call it; the direct hostname answering 401 afterwards is correct. Two things learned on
   27 September 2026: on CLI 2.86 `az functionapp show` returns a Flex app with no state or
   hostname, so read it with `az resource show`; and `backends unlink --remove-backend-auth`
   leaves that authentication on with no provider, so after unlinking a preview, switch it off
   with a PUT of `{"properties":{"platform":{"enabled":false}}}` to `config/authsettingsV2`.
7. **Entra app registration** with the SWA's redirect URIs.
8. **Settings** on the API app first, then copied to the MCP host and worker with the filter in
   `infra/azure-mcp-host-setup.sh` (`:` becomes `__`, runtime-owned keys skipped, values never
   printed). Secrets go in Key Vault references where the setting supports them.
9. **Budget and alerts** on the resource group at creation: a monthly budget with e-mail at 80%
   actual and 100% forecast, and the alert rules from `infra/setup-alerts.sh` (server errors, slow
   responses, an exception spike, and the availability test that catches a gateway 503),
   sent to an action group.

## 4. Deploy without stored secrets

Every workflow signs in with a federated credential and deploys with
`Azure/functions-action@v1` given `sku: flexconsumption` and `remote-build: false`. Publish
for the Linux host with every runtime kept (`dotnet publish ... -p:KeepAllRuntimes=true`) or a
Linux keep-list: the API project trims every `runtimes/<platform>` folder except Windows for
the Static Web App's cap, and that removes the Linux build of the SQL driver, so every
database read then fails with `FileNotFoundException` while the deploy stays green.
`.github/workflows/jpms-api.yml` is the model; the MCP variant adds the one `jq` step that
blanks the route prefix. One Entra application per repository, Website Contributor on each
Function App it deploys, trusting `repo:<owner>/<repo>:ref:refs/heads/main` only. Repository
*variables* hold the client, tenant and subscription ids and the app names. No publish profiles.

## 5. Releases without a restart

Flex has no deployment slots. Its rolling site update replaces instances in batches, which
needs at least two always-ready instances to mean anything. Decide this per customer by how
many people notice a ten-second restart: for a portal with a handful of daily users one
always-ready instance and releases outside working hours is enough; for one used all day, two
instances and the RollingUpdate site strategy (`az functionapp update-strategy config set
--type RollingUpdate`, CLI 2.87 or later; see Step 6 of `infra/hosting-upgrade/README.md` for
the region caveat).

## 6. What to verify before handing over

- First call after 20 minutes idle: `curl -w "%{time_total}"` on `/api/version` well under a
  second through the portal domain.
- App Insights: SQL dependency p50 in single-digit milliseconds. Double digits means the
  compute and the database are not in the same region.
- A release watched with `/api/version` polled every 2 seconds: no failed calls, or a known
  restart window agreed with the customer.
- The direct Function App hostname answers 401; the portal domain answers 200.
- `az staticwebapp environment functions` lists nothing: no managed functions were ever
  published.
- Point-in-time restore days 35, long-term retention set, documents storage GRS with soft
  delete, budget and alert rules present, and one restore rehearsed and written down.

## 7. Costs, list prices, September 2026

| Line | About |
|---|---|
| Flex Consumption API, one always-ready 2 GB instance | £16 a month idle, about £20 with a working day's traffic |
| Flex Consumption MCP host, one always-ready instance | £16 a month |
| Worker, Flex with no always-ready or Consumption | a few pounds |
| SQL serverless GP 0.5 to 4 vCore, auto-pause off | the largest line; read the current Jewel invoice for the real figure |
| Static Web App Standard | about £7 a month |
| Storage, GRS, plus backups beyond the free allowance | a few pounds |

The P1v4 plan the Jewel portal
could not buy would have added £94 a month for one instance; Flex delivers the same always-on
behaviour for a sixth of that.
