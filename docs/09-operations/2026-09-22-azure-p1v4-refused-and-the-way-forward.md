# Microsoft refused the P1v4 quota — what the upgrade was for, and the way forward (22 Sep 2026)

**Status:** decided and applied in Your Business Today on 22 Sep 2026 (goal *Azure Hosting and
Infrastructure* rewritten; parent task *Move the portal API onto always-on hosting next to the
database*, id `1638660d-c0cf-4de5-a4dc-7508ceeaff4b`, with subtasks Nigel's yes + Steps 1–6).
Nothing applied in Azure yet — James runs the steps over the weekend of 26–27 Sep. The repo work
owed before Saturday (the Flex `phase1-provision.sh`, `.github/workflows/jpms-api.yml` and the
rehearsal workflow) landed on 27 Sep on branch `claude/great-lovelace-80pjht`, pull request
jamesbeadle/jewel-portal#149; the runnable plan is `infra/hosting-upgrade/README.md`.

## What Microsoft said (ticket 2609040050000920, 21–22 Sep)

- 21 Sep, Ganesh Chintha: "due to regional constraints and high demand for App Service in this
  region, PG team unable to approve your quota request" — suggests an alternate region.
- 22 Sep, Syed Mohsin (Technical Advisor): confirmed. North Europe "is currently experiencing
  significant demand for App Service capacity"; the self-service quota experience may be used
  for another region and SKU; SQL/Blob relocation is out of the App Service team's scope; asks
  whether the case may be archived. James replies one line: yes.
- The ask that was refused: App Service Premium v4, P1v4, non-zone-redundant, North Europe,
  2 instances, subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec. Eighteen days, four
  engineers, refused on capacity — no longer a form problem.

## What the upgrade was for (proposal 4 Sep, runbook, James's own emails to Microsoft)

1. The API on the Static Web Apps managed-functions tier sleeps after ~20 min idle: the first
   click after a quiet spell waits ~7 s. Fix: Always On / always-ready.
2. Every release restarts the API for everyone. Fix: a warm standby swapped in (slots) or a
   rolling update.
3. The app runs in West Europe and the database in North Europe: 17 ms round trip per query
   (Microsoft's latency table, July 2026). Fix: co-locate.
4. Data protection: point-in-time restore 7 → 35 days, weekly backups 12 weeks, monthly 12
   months, documents storage GRS with 14-day soft delete. SQL and storage only — never
   depended on the plan.
5. Visibility: Application Insights alert rules. Never depended on the plan (the 21 Sep
   sign-in outage went unnoticed for three hours because this was queued behind the upgrade).
6. Cost guard: a Cost Management budget with forecast alerts. Never depended on the plan.
7. Found since: the SWA managed-functions 100 MB package cap that Magick.NET hit on 16 Sep. A
   dedicated Function App has no such cap.

Only 1, 2, 3 and 7 need a different compute home. 4, 5 and 6 were only sequenced behind it,
and are now un-gated.

## The wider picture (checked 22 Sep)

- Azure MVP write-up, 21 Jul 2026: North Europe is the worst-hit region in Europe (Irish
  grid), new subscriptions get zero, quota increases "almost certainly going to be refused",
  "for a number of years"; West Europe also constrained; Microsoft points people at Sweden
  Central. https://azurealan.ie/2026/07/21/azure-capacity-constraints-in-europe-why-north-europe-is-feeling-it-most/
- Microsoft Q&A, 21–27 Jul 2026: UK South refused even a Y1 (Consumption) App Service plan —
  `SubscriptionIsOverQuotaForSku, Current Limit (Total VMs): 0` — on "regional capacity
  constraints". Microsoft's own suggestions: UK West ("not under the same capacity pressure")
  and Flex Consumption FC1 ("a separate quota model based on memory, often easier to approve
  in capacity-constrained regions"). https://learn.microsoft.com/en-au/answers/questions/5953110/cannot-create-app-service-plan-in-uk-south-total-v
- Premium v4 is not offered in UK West at all; it is offered in UK South, North Europe, West
  Europe, Sweden Central. https://learn.microsoft.com/en-us/azure/app-service/app-service-configure-premium-tier
- UK South list price, Linux: P1v4 £0.1347/h ≈ £98.34/month; P0v4 £0.0672/h ≈ £49.06/month
  (retail prices API, 22 Sep).
- Inter-region round trips (Microsoft, July 2026): North Europe–West Europe 17 ms, North
  Europe–UK South 12 ms, West Europe–UK South 11 ms, UK South–UK West 7 ms, North Europe–UK
  West 16 ms.

So App Service plans (the stamp hardware) are what is scarce across North Europe, West Europe
and UK South. Azure is not.

## Stay or leave Azure

Stay. The scarce thing is one product family's hardware in one corner of Europe; two other
Azure compute services (Flex Consumption, Container Apps) have their own capacity pools and
quotas. The reason for Azure — clients who run on Microsoft: Entra sign-in, the Graph projects
mailbox, Communication Services email — is untouched and invisible to the SKU. The SQL server,
storage, mailbox, App Insights, four workflows and the runbook are all built; another cloud has
the same shape of problem (new-account quotas) and would cost weeks to solve what a different
Azure service solves in an afternoon.

**Lesson for the multi-client platform:** never pin the offer to a SKU or a region. Provision
per client into whichever region sells the capacity that day (probe first), keep the scripts
parameterised, and sell the behaviour (always-on, next to its data, releases without
interruption), never the hardware. That shape is written up in `hosting-a-new-portal.md`.

## The route: Azure Functions Flex Consumption (FC1), North Europe

- Same Functions project, Linux, .NET 8 isolated, same `Azure/functions-action` deploy; same
  region as the database, so co-location comes free — no SQL move.
- Always-ready instances remove the cold start. Instance sizes 512 MB / 2,048 MB (1 core) /
  4,096 MB (2 cores). Decision: 2,048 MB — it is what the API runs in today on SWA managed
  functions, and the MCP host (same code) peaks at 1.35 GB on a 1.75 GB B1; Step 6 reads the
  memory over the first week and moves to 4,096 MB if the working set passes ~1.5 GB.
- Rolling site update (`properties.functionAppConfig.siteUpdateStrategy.type = RollingUpdate`,
  ARM/Bicep only, public preview; GA so far only in East Asia, West Central US, North Central
  US, West US 2, rolling out elsewhere) replaces instances in batches during a deploy — with
  one instance it still restarts, so Step 6 runs two always-ready instances (about £32/month
  for both). Flex has no deployment slots.
- Its own quota: 250 cores per region per subscription by default, separate from App Service
  plans. One app per Flex plan.
- No 100 MB package cap.
- Cost at list (USD list, GBP approx.): always-ready baseline $0.000004/GB-s → 2 GB ≈
  £16/month per instance, plus execution time ($0.000016/GB-s while handling requests; a few
  pounds at 12 users). Bill after the move ≈ £225–235/month: the ask to the MD falls from +£90
  to about +£35–45/month including Step 5's backups and GRS. **Corrected 27 Sep:** the September bill is £280 before the move, not the ~£190 this
  implied; the delta holds, the total does not. The corrected table and the £350 budget are in
  `infra/hosting-upgrade/README.md`, *What it will cost*.
- Rough edges, all known: `functions-action` with Flex + .NET has needed `sku: flexconsumption`,
  `remote-build: false`, `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED=0` and RBAC sign-in
  (azure/login with a federated credential) rather than a publish profile — runbook 6.2's
  password-less sign-in comes forward to Step 2. Built-in authentication (which the SWA link
  switches on) is supported on Flex per Microsoft's migration guide; it is not documented
  against Static Web Apps linking specifically, so Step 3's rehearsal on a preview environment
  is the gate that proves it before production.
- `WEBSITE_WARMUP_PATH`, `--always-on`, `healthCheckPath` from the P1v4 phase1-provision.sh do
  not exist on Flex; the always-ready setting replaces them.

## Fallbacks, in order — decided by the Step 1 create itself (£0 until always-ready is on)

1. Flex Consumption in UK South (12 ms from the database, UK compute) if the North Europe
   create is refused.
2. Container Apps in North Europe: min replicas 1, revisions give zero-downtime releases,
   supported as a Static Web Apps linked backend, ~£20–50/month; costs a Dockerfile, a
   registry and a new workflow (about a day).
3. App Service P1v4 in UK South (£98.34/month) or P1v3 in UK West, with the database moved
   after cut-over (failover group, planned failover, 94 MB, minutes, no data loss) — the
   existing runbook with LOC and SKU changed. Only if 1 and 2 fail.
4. Sweden Central P1v4 — Microsoft's recommended region; works, but the data leaves the
   UK/Ireland.

## What was changed in Your Business Today (22 Sep)

**Goal Azure Hosting and Infrastructure** — measure reworded to the behaviour, not the SKU:
(1) API always-on next to its database, no idle freeze, no interruption on release; (2) data
protection proven by a restore; (3) alerts within 5 minutes, including sign-in failure;
(4) bill about £225–235/month ex VAT, budget alerts, image capacity cap, no stale resources.

**Task Upgrade Azure → Move the portal API onto always-on hosting next to the database:**
details, checklist ("Weekend run") and acceptance criteria replaced; P1v4 phase1-provision.sh
attachment removed; runbook PDF kept as the P1v4 edition (Phases 2, 3, 4, 5 still apply; 1 and
6 superseded). Subtasks, in order: Get Nigel's yes on the revised figure (before Step 4) ·
Step 1 create the Flex app (the create is the capacity test) · Step 2 deploy and test on its
own hostname · Step 3 rehearse via a preview environment · Step 4 production cut-over · Step 5
data protection · Step 6 rolling update on two instances. Each step's details carry the exact
commands or the runbook pages; its acceptance criterion is its gate.

- Chase Microsoft → Done (refused; ticket archived).
- Azure Phase 7 (MCP host and worker onto the shared plan) → deleted: one app per Flex plan,
  and the £11/month saving was its only reason.
- Budget (£300, six notifications, both James and Nigel), Monitoring (five rules incl.
  sign-in; the schema-drift reading lives on the migration task) and Backups (restore drill
  after Step 5) → rewritten tight, un-gated, dated for the weekend / the weekend after.
- 126–134 security tasks → unchanged; the SQL server stays where it is.

**Weekend order:** Friday — reply to Microsoft, message Nigel. Saturday — Step 1, Step 2,
budget, Step 5, monitoring rules. Saturday or Sunday — Step 3. Sunday evening — Step 4, then
the End-to-End Regression Test. The weekend after — Step 6, restore drill.

## Repo work (landed 27 Sep, pull request jamesbeadle/jewel-portal#149)

`infra/hosting-upgrade/phase1-provision.sh` (Flex create, settings copy,
`WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED=0`), `phase0-preflight.sh` (Flex locations and
meters), `quota-p1v4.sh` retired, `.github/workflows/jpms-api.yml` (Appendix A with
`azure/login@v2`, `sku: flexconsumption`, `remote-build: false`),
`.github/workflows/jpms-swa-rehearsal.yml` (Appendix B, temporary), and Step 6's `az rest`
PATCH for `siteUpdateStrategy` (in `infra/hosting-upgrade/README.md`).
