# Infrastructure

Azure provisioning for the JPMS test environment.

## What `azure-setup.sh` creates

| Resource | Tier | Cost |
|---|---|---|
| Resource group `rg-jpms-test` | n/a | free |
| Azure SQL Server `sql-jpms-test-<random>` | n/a | free |
| Azure SQL Database `jpms` | GP_S_Gen5 Serverless, auto-pause 60min, Free-Limit if available | ~£0 while idle, free up to 100k vCore-seconds + 32GB/month |
| Static Web App `swa-jpms-test` | Free | free (250GB/month bandwidth) |
| Entra ID app registration | n/a | free |

UK South for SQL, West Europe for the Static Web App (Free tier is not in UK South).

## Run it

```bash
az login
az account set --subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec
./infra/azure-setup.sh
```

The script writes `infra/.azure-output.env` with every value needed afterwards (SQL connection string, SWA deployment token, Entra client ID). That file is gitignored.

## After it runs

1. Copy `SWA_DEPLOYMENT_TOKEN` from `.azure-output.env`.
2. On GitHub, add it as repository secret `AZURE_STATIC_WEB_APPS_API_TOKEN`.
3. Push to `main` — the workflow in `.github/workflows/jpms-swa.yml` builds and deploys.

## Production environment

`azure-prod-setup.sh` provisions a separate, production-grade environment in its own
resource group (`rg-jpms-prod`). It differs from the test setup in the ways that matter:

| Resource | Test (`azure-setup.sh`) | Production (`azure-prod-setup.sh`) |
|---|---|---|
| SQL compute | Serverless, **auto-pause 60 min** (cold start) | Serverless, **auto-pause disabled** (always warm, no cold start), min 0.5 / max 4 vCores |
| SQL backups | default (local) | **Geo-redundant**, 35-day PITR, 4-week long-term retention |
| Static Web App | Free | **Standard** (SLA, custom auth, custom domains) |
| Entra app | `entra-jpms-test` | `entra-jpms-prod` (separate registration + redirect URIs) |
| Monitoring | none | Application Insights + monthly cost budget alert |
| `SqlConnectionString` | written to env file only (**never set on the SWA**) | **set on the SWA app settings** so the API can connect |

### Schema, no data

The Functions API runs EF Core migrations on startup (`api/Program.cs` →
`context.Database.MigrateAsync()`), so the full table structure is recreated
automatically — with no data — on the first request after deployment. Nothing to
export or import. (To apply it up front instead, from `./api`:
`SqlConnectionString='<conn>' dotnet ef database update`.)

### Scripts: v1 (record) vs v2 (current)

`azure-prod-setup.sh` is preserved unchanged as the **as-run record** of the
original production provisioning. **Use `azure-prod-setup-v2.sh` for any future
run** — it adopts the existing prod SQL server instead of creating a duplicate,
resets the admin password on adoption so the connection string stays valid, and
auto-installs the App Insights CLI extension.

### Run it

```bash
az login
az account set --subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec
./infra/azure-prod-setup-v2.sh
```

Writes `infra/.azure-prod-output.env` (gitignored) with the connection string,
SWA deployment token, Entra client id, and App Insights connection string.

Useful overrides (prefix the command): `SQL_MIN_CAPACITY=1` (warmer/faster),
`SQL_MAX_CAPACITY=8`, `BUDGET_AMOUNT=200`, `RESOURCE_GROUP=...`.

### After it runs

1. Copy `SWA_DEPLOYMENT_TOKEN` from `.azure-prod-output.env`.
2. On GitHub, add it as repository secret `AZURE_STATIC_WEB_APPS_API_TOKEN_PROD`
   (or, for a single environment, just replace the existing
   `AZURE_STATIC_WEB_APPS_API_TOKEN` value with the prod token).
3. Push to `main` — the workflow builds and deploys; the API self-migrates the schema.

### Retiring the test SQL server

You said the test data isn't needed. Once prod is verified, drop the old test SQL server:

```bash
TEST_SQL_SERVER=sql-jpms-test-673bc2 ./infra/azure-prod-setup.sh   # prompts to delete it
# or remove the whole test group:
az group delete --name rg-jpms-test --yes --no-wait
```

## Tearing it all down

```bash
az group delete --name rg-jpms-test --yes --no-wait
az group delete --name rg-jpms-prod --yes --no-wait
az ad app delete --id "$(grep ENTRA_APP_ID infra/.azure-output.env | cut -d= -f2)"
az ad app delete --id "$(grep ENTRA_APP_ID infra/.azure-prod-output.env | cut -d= -f2)"
```
