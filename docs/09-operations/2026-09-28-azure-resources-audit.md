# The production Azure resources, audited 28 September 2026

What `rg-jpms-prod` held the morning after the move to Flex Consumption, what each piece is
for, what it costs, and what to do about the ones that should not be there. Costs are list
prices before VAT; August's actual bill was £216.90, of which the database was £209.55.

## Every resource, and whether it should exist

| Resource | Region | What it is | Month | Verdict |
|---|---|---|---|---|
| `sql-jpms-prod-54cf9e` / `jpms` | North Europe | The database, serverless GP, 0.5 to 4 vCores, auto-pause off | £210 | Keep. 78 per cent of the bill for a 94 MB database; the lever if cost ever matters (a fixed Standard S2 is £58) |
| `func-jpms-api-prod` + `ASP-rgjpmsprod-29c9` (FC1) | North Europe | The portal API on Flex Consumption, two always-ready instances | £33 idle, up to £45 busy | Keep. Created 27 September |
| `stjpmsapi69e23c` | North Europe | The API app's runtime and deployment storage | under £1 | Keep |
| `swa-jpms-prod` | West Europe | The portal frontend, Standard | £7 | Keep. The region only places metadata; content is global |
| `func-jpms-mcp-prod` + `plan-jpms-mcp` (B1) | West Europe | The MCP connector host on a B1 App Service plan | £10 | Keep for now. Later: recreate as a Flex app in North Europe like the API (same cost, next to the database, no Windows-trim risk), once the connector's SQL driver fix has landed |
| `func-jpms-worker-prod` + `ASP-rgjpmsprod-ae13` (Y1) + `rgjpmsprod9179` | UK West | The mailbox worker on Windows Consumption and its runtime storage | pence | Keep. Windows is why its runtime trim never bit |
| `stjpmsprod54cf9e` | UK South | The documents storage, GRS with soft delete since 27 September; also the MCP host's runtime storage | £1 to £3 | Keep |
| `appi-jpms-prod` + `log-jpms-prod` | West Europe | The portal's telemetry and its workspace | £0 within the free 5 GB a month | Keep |
| `func-jpms-mcp-prod` (Application Insights) + `DefaultWorkspace-…-WEU` | West Europe | A second telemetry component and a default workspace Azure invented when the MCP host was created without one | £0 | Tidy: point the MCP host at `appi-jpms-prod`, delete both |
| `func-jpms-worker-prod` (Application Insights) + `DefaultWorkspace-…-UKW` | UK West | The same for the worker | £0 | Tidy: point the worker at `appi-jpms-prod`, delete both |
| `Application Insights Smart Detection` | Global | The action group Azure creates for every App Insights component | £0 | Keep |
| `jpms-comms-prod`, `jpms-email-prod`, `mail.jewelbb.co.uk` | Global | Invite and reset e-mail | pence | Keep |
| `oai-jpms-prod` | East US 2 | Azure OpenAI, the image model behind the Imagine feature | £0 until used, then per image | Keep, capped: the budget task sets the deployment's capacity cap. The prompts leave Europe; nothing personal is in them |
| `cv-jpms-prod` | UK South | Azure AI Vision, the OCR behind scanned uploads | £0 on the free tier, else per 1,000 reads | Keep; confirm the tier is F0 |
| `stjpmsapid9d6f9` | North Europe | A runtime storage account from the first P1v4 attempt on 4 September, before `vars.sh` recorded a name | under £1 | **Delete**, after the check below shows nothing references it |

Expected from October: about £270 a month before VAT, about £325 with VAT.

## The checks, from the Mac

```bash
RG=rg-jpms-prod
echo "## which storage account each Function App runs on (want: none says stjpmsapid9d6f9)"
for app in func-jpms-api-prod func-jpms-mcp-prod func-jpms-worker-prod; do
  printf "%s: " "$app"; az functionapp config appsettings list -n $app -g $RG --query "[?name=='AzureWebJobsStorage'].value | [0]" -o tsv | grep -o "AccountName=[^;]*"
done
echo "## anything at all inside the suspect account (want: no containers, no tables, no queues)"
az storage container list --account-name stjpmsapid9d6f9 --auth-mode login --query "[].name" -o tsv
echo "## the vision tier (want F0)"
az cognitiveservices account show -n cv-jpms-prod -g $RG --query "sku.name" -o tsv
echo "## the image model's deployments and their capacity"
az cognitiveservices account deployment list -n oai-jpms-prod -g $RG --query "[].{name:name,model:properties.model.name,capacity:sku.capacity}" -o table
```

Then, with the first check clean:

```bash
az storage account delete -n stjpmsapid9d6f9 -g rg-jpms-prod --yes
```

## The plan for the future

- **Per client, one resource group, one region, nothing that sleeps.** The PFP portal is the
  first built to it: `rg-jpfp-prod` in North Europe, the brief in
  `hosting-brief-for-any-new-portal.md`, the JPFP edition in its own `infra/README.md`.
- **Shared across portals: nothing that carries a client's data.** The subscription, the
  Azure OpenAI and Vision accounts (pay per use, no standing cost, no client data at rest) and
  Communication Services (one resource can hold several e-mail domains) can be shared when a
  portal needs them; a database, a storage account, a Static Web App and the Function Apps are
  always the client's own, so a client can be handed on or shut down by deleting one group.
- **The Jewel portal's own tidy-ups**, in order of worth: the leftover storage account (today),
  the MCP host onto Flex in North Europe (after its SQL driver fix), the two extra telemetry
  components and default workspaces (any quiet afternoon), and the database tier (only if cost
  ever outranks headroom).
