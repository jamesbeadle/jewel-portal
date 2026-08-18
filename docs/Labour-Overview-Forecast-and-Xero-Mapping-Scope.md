# Labour Overview, Forecast, Xero Automation & Monday Replacement — Scope

**Status:** Draft for review · August 2026
**Inputs:** Jeremy's JPS timesheets app (timesheets.jewelps.co.uk) and WhatsApp brief and follow-up conversation of 2026-08-18, `docs/Labour-Time-Tracking-Scope.md` (Phase 1, implemented July 2026), existing JPMS Labour/Commercial/Xero features.
**Owner decisions taken (2026-08-18):** extend the JPMS Labour module (one portal, not a second app) · daily My Day capture stays; the weekly views are read + sign-off layers over the same per-day rows · **JBB only** — JPMS remains the Jewel Bespoke Build portal; Property Serve stays on Jeremy's JPS app for now ("the same but for JBB"). The JPS app is the functional reference, not a data source. The modules are built so they can later be lifted into per-company sites, but no cross-entity data model ships in this scope.

---

## 1. Purpose

Give JPMS the timesheet workflow Jeremy has prototyped for Property Serve, **built for Jewel Bespoke Build**: portfolio-wide **by site / by worker / by trade (cost code)** views over JBB labour, a **weekly PM sign-off** that fixes each day's project and cost code, a **projected labour spend forecast** (contracted days × day rate, net of absence, CIS-adjusted for cashflow), and a **settlement schedule** published per worker per period so the covering invoice in Dext is coded to matching totals — with an effective-dated **site/cost-code ↔ Xero tracking mapping** so the JBB profit report stays right across historic and future data. PS itself is out: Jeremy's app keeps running Property Serve.

---

## 2. The two systems today (crossover map)

| Capability | JPS timesheets app (Jeremy) | JPMS Labour (Phase 1, live) | Verdict |
|---|---|---|---|
| Capture | Weekly form: who/week-ending, default site + per-day override, TBC-site escape hatch, notes; sign-in/out times stream live, days land on submit | My Day: authenticated daily sign-in/out, end-of-day allocation to cost codes, half-hour steps, rejected-day resubmission | **JPMS wins** (decision above) — but the My Day and Labour-tab UI gets redesigned (§4a); the Phase 1 screens are functionally right and visually rough. |
| Sign-off | PM signs off at end of week, allocating to project + cost code | Weekly batch approval grid per project: adjust, re-code, reject, budget hard-block, rate snapshot | **Already built** — but per-project only. Needs a cross-project weekly sign-off surface. |
| By labourer / by site views | Dashboard modals: ranked bars + tables, day-by-day drill-down, month picker | None company-wide — Labour tab is one project, one week | **New build** (§4). |
| By trade / cost code view | Wanted, not built ("Show by Trade/cost code?") | Cost codes on every timesheet row already | **New build**, cheap — the data is already coded. |
| Forecast | Projected labour spend: contracted days × day rate − holiday/half-days, submission-confidence bar, per-worker Amount due (= net of 20% CIS) | None — cashflow/CVR deliberately consume approved cost only | **New build** (§5). |
| Absence / holiday | Days recorded as holiday / not worked reduce the projection | No absence concept at all | **New build** (§5). |
| Reconciliation vs invoices | Monthly: timesheets vs each labourer's Xero bill, verdict + itemised bill, invoices-to-chase | Settlement view per project: covered-by-timesheets marking, variance posting, four closure paths | **JPMS is more rigorous**; needs the per-worker-per-month cut and the chase list. |
| Xero site/cost-code mapping | Concern raised, not solved | Tracking categories synced (site + cost-code categories stamped), site P&L synced nightly | **Extend** (§6). |
| CIS labour / materials / travel | Flagged in brief; 20% CIS baked into "Amount due" | Nothing | **New build** (§6). |
| Cross-entity (BB/PS) | "Total across both £46,100 — worked by the same team"; BB badge per worker | Commercial model mandates a cross-entity flag at source; not yet in Labour code | **Deferred** — JBB-only decision. The shared-team total stays a conversation between the two systems for now; an entity dimension lands only if PS ever moves into the portal. |
| Xero data entry | Manual coding by Jeremy; wants "no manual input on Xero by me but rather by Claude" once the month is approved | Portal holds Xero API permissions + agent surface; posting not built | **New build** (§6a) — approval-gated automated coding. |
| Registers, assets, people ("take out of Monday") | To-dos, Orders, Forms in, Renewals, Vans, Subscriptions, Trade accounts, Staff, Contacts, Documents | Directory/contacts, documents, drawings, subcontractor compliance exist; no insurance/subscription/renewal registers | **New build** (§8) — Monday replacement phase. |
| Staff sign-off forms (NDAs, policies) | Requested in follow-up ("build a sign form for NDA's staff policies etc") | Nothing | **New build** (§8). |
| Phone-call log, board summary | Built in the JPS app | Out of scope here | **Not crossing over** in this scope. |

What this means: roughly half of Jeremy's brief is already implemented in JPMS at a deeper level than the JPS app (approval, rate snapshots, budget control, settlement variance discipline). The genuinely new work is the **portfolio-wide read layer**, the **forecast/absence model**, the **Dext/Xero/CIS mapping** — and a proper **redesign of the worker-facing and Labour-tab UI**, which shipped functional but visually rough in Phase 1 (§4a).

---

## 3. Data model

New entities:

**`WorkerContractEntity`** — WorkerId, ContractedDaysPerMonth (or per-week), EffectiveFrom. Drives the forecast baseline. Effective-dated like rate history; the forecast for a month uses the contract effective in that month.

**`WorkerAbsenceEntity`** — WorkerId, Date, Kind (Holiday / HalfDay / NotWorked / Sick), Note, RecordedByEmail. One per worker per date. Absence reduces the forecast at the full day rate; a recorded absence is also surfaced on the sign-off grid so a missing timesheet day with a recorded absence is *explained*, not chased.

**`SiteXeroMappingEntity`** — ProjectId, XeroTrackingOptionId (site category), EffectiveFrom, EffectiveTo (nullable). The effective-dated bridge that answers the historic-vs-current concern: Xero options are never renamed to chase portal names; reports translate through the map, so a site whose tracking option changes mid-year reads correctly on both sides of the change. A parallel `CostCodeXeroMappingEntity` (CostCode → tracking option or account code) covers the cost-code category the same way.

**`WorkerCisStatusEntity`** — WorkerId, CisRate (0 / 20 / 30 %), VerifiedRef, EffectiveFrom. Standard deduction is 20%; gross-status and unverified workers differ. Used only for the net-cash line on the forecast and settlement schedule — JPMS does not run CIS returns (that stays in Xero/HMRC tooling, per `commercial-model.md`'s "what's NOT in JPMS").

Changes to existing entities:

- **None to the timesheet row itself** — day, project, cost code, hours, rate snapshot, status all exist, and JBB projects are already first-class. No entity flag, no PS projects, no budget-block changes: the 07-D hard-block stays absolute, because every site in scope is a budgeted JBB project.
- The JPS "TBC site" escape hatch is **not** carried over: My Day capture is per assigned project, so an unallocatable day cannot arise. The equivalent risk on JBB is a *missing* day, which the chase list (§4) owns.

One migration, additive throughout; apply commands ship in the same reply as the code, scoped-script procedure per CLAUDE.md.

---

## 4. The Labour overview (new company-wide pages)

A new **Labour overview** page set under the sidebar's Time folder (`/labour/overview`), month-picked, alongside the existing per-project Labour tab and Workers registry. This is Jeremy's dashboard re-homed under JPMS conventions.

**Header strip** — `MetricStat`s: projected JBB labour spend for the month, time off logged (− £), and the **submission-confidence bar**: % of elapsed working days confirmed by an approved or submitted timesheet, with the £ still unconfirmed. His footnote is kept nearly verbatim as the method statement: *contracted days × day rate, less every day recorded as holiday, a half day or not worked; days not yet submitted stay at the full rate, so the figure is only as accurate as the submission rate.*

**Three views, one selector** (local panes, not routes):

- **By worker** — ranked bar list (projected cost per worker), table: rate (inline-editable for ManageWorkers roles, as his pencil icon), days worked, days off, projected cost, **amount due** (net of the worker's CIS rate — his 80% column, now labelled for what it is: *net payable after CIS*). Row expands to the day-by-day grid of where they worked.
- **By site** — same shape per JBB project, each row opening "who worked here, day by day". (No TBC banner: JBB capture is per assigned project, so unallocated days cannot exist — the failure mode is a missing day, owned by the chase list.)
- **By trade / cost code** — the same aggregation keyed on cost code (and the cost-code → trade grouping the architect codes imply). No new data needed; this is the view Jeremy asked for but hadn't built.

**Chase list** — workers with elapsed days that have neither a timesheet nor a recorded absence, and open attendance rows (missed sign-outs). This is his "1 timesheet outstanding this week" / chase-list nav item, generalised.

**Weekly sign-off** — a cross-project week view (workers × days across all their projects) so the PM signs the week off in one sitting: approve/adjust/re-code per day, then a per-week **Signed off** marker per worker recording who signed and when. Under the hood it drives the existing `ApproveTimesheets`/`AdjustTimesheet`/`RejectTimesheet` contracts — sign-off is a view over approval, not a second state machine. A week is signable only when every elapsed day is approved, rejected-and-explained, or covered by an absence.

Conventions that bind all of it: stores/read models keyed per month with `LoadedFor(month)`, `Refresh` once from `OnInitializedAsync`; panels reveal in one piece via `LoadState.UntilAll`; the month picker renders disabled while loading, never gated; `Stat`/`MetricStat` take `IsLoading`; one `Toolbar` of icon buttons (refresh | `ExportToExcelButton` with current-view/include-all); failures open the gate with `dataFailed`; nullable backing fields. Terminology: **worker** and **timesheet** in identifiers and UI copy (per glossary — "labourer" survives only in conversation), **Programme** never "Schedule", and no valuation-invoice interactions anywhere in this feature.

### Design intent — this must not look like the reference app

The JPS dashboard is the *functional* reference only. Its visual layer is stock AI output — white canvas, default blue bars, undifferentiated tables — and none of it carries over. The overview is built in the JPMS visual system (dark canvas, `surface`/`surface-raised` panels, single `accent` green, `content` type scale, jewel-pulse loading) and should read as the most considered screen in the portal, since it is the one the MD and FD will live in:

- **The hero is the placement grid, not a bar chart.** The month renders as a workers × days matrix — each cell a site-coloured chip (a muted categorical hue per project), absence hatched, unsubmitted days hollow, rejected days flagged in the warning tone. Ranked bars become a thin, single-accent affordance beside the table, not the centrepiece; the grid is what no off-the-shelf build gives them.
- **One figure leads.** Projected spend for the month is the page's single large numeral (`MetricStat`, tabular figures); time off and confidence sit as quiet secondary stats. No competing big numbers.
- **The confidence bar is an instrument, not a progress bar** — segmented by week, tooltip per segment ("w/c 4 Aug — 3 days unconfirmed, £560"), animating only on data change.
- **Verdict language reuses `StatusPill`** — *Matches* / *Variance open* / *No bill yet* / *Signed off* as pills consistent with the rest of the portal, never coloured text.
- **Drill-downs are `Modal`/`Panel` compositions** matching the cost-centre modals (site → who worked here day-by-day; worker → where they worked), with the toolbar pattern, not bespoke popovers.
- **Numbers behave:** right-aligned tabular numerals, £ to the pound in overviews and to the penny only in schedules/reconciliation, `negative` red reserved for true negatives, sign-off and approval states never conveyed by colour alone.
- **Motion and load discipline:** one jewel per screen; month navigation refreshes via `Overlay` gates over held layout, so switching months never collapses the page.

### 4a. My Day & Labour tab redesign

The Phase 1 screens are functionally right and visually below the portal's bar — the owner's own verdict. `ProjectLabour.razor` renders its blocks as bare `<section>`/`<h3>` runs rather than `Panel` compositions, the sub-views (manual entry, site register, settlement) stack without hierarchy, and My Day is serviceable rather than considered. This scope includes a redesign pass with teeth:

- **Every block becomes a `Panel`.** Timesheet grid, missed sign-outs / manual entry, how-workers-log-time, site register and settlement each sit in a proper `Panel` with its own header, toolbar affordances and one-piece load reveal — no floating `<h3>` sections anywhere on the tab. The busier sub-views (settlement, manual entry) collapse behind the panel header rather than stacking down the page.
- **The week grid gets the overview's visual language** — the same chip/cell treatment as the placement grid (§ design intent), pills for Submitted/Approved/Rejected, right-aligned tabular £, so the per-project tab and the portfolio overview read as one family.
- **My Day is redesigned as the portal's flagship mobile screen**, since it is the only screen most workers ever see: one card per assigned project with a single dominant action (Sign in → Sign out), oversized touch targets on the half-hour steppers, the running total pinned while allocating, rejected days surfaced as a distinct card with the reason up front, and the same dark surface/accent system as the rest of the portal — not a shrunk desktop page.
- **Acceptance bar:** the redesign is done when the Labour tab and My Day are indistinguishable in polish from the best screens in the portal — screenshot review against the Financials tab, not a checklist.

### 4b. Chat-created timesheets

The portal's existing chat window becomes a first-class way to get time recorded — often the easiest way. The pieces already exist: the `AgentCatalogue` already carries a **TIMESHEETS agent** ("hours are recorded against a project, a date…"), and the chat's staged-action pattern (`SystemActionKind` + `StagedRecordActionEditor`) already lets the agent *propose* a record for a human to confirm before anything commits. What's missing is the action set — no timesheet kinds exist today. Scope:

- **New `SystemActionKind` entries** mapping straight onto the existing Labour contracts, so chat gains no new write paths, only a new mouth for the existing ones: `AddWorkerTimesheet` ("put Danny down for 8 hours on Chiltern Court yesterday, second-fix"), `AdjustTimesheet`, `ApproveTimesheets` / `RejectTimesheet`, and `RecordAbsence` ("Frank's on holiday Thursday and Friday").
- **Staged, never silent.** Every chat-created entry renders as a staged action card — worker, date, project, cost code, hours resolved and shown — and commits only on the user's confirm, exactly like RFIs and VOs today. Fuzzy references ("Danny", "the Chiltern job", "yesterday") are resolved by the agent and displayed resolved, so what you confirm is what posts.
- **Same rules, same gates.** Chat entries are ordinary `Submitted` timesheets: half-hour steps validated, role gates unchanged (PM/commercial roles stage on a worker's behalf via the manual-entry path; a SiteOperative in chat can only act as themselves), rates never surfaced to non-commercial roles, approval and the budget hard-block untouched. Chat is a capture surface, not a bypass.
- **Bulk conversational entry** is the real win for missed sign-outs: "the whole crew did 8 hours on Sugar House Monday except Zack who left at 1" stages one card per worker for a single confirm-all — feeding the chase list down instead of a form-per-day slog.

---

## 5. Forecast & cashflow

Projected month cost per worker = contracted days effective that month × day rate effective per day − absence days at full rate, with elapsed unsubmitted days held at full rate (the confidence bar carries the caveat). Aggregations by site and cost code fall out of the day-by-day expected placement: days already worked use the recorded project; future days use the worker's current default placement (last-worked site, PM-overridable) so the by-site projection stays honest about being a projection.

**Net cash line:** amount due = projected cost × (1 − CIS rate) for CIS-labour workers; materials/travel elements (§6) are not CIS-deducted. This is the number Jeremy cashflows on.

The FD-level cashflow forecast and CVR **continue to consume approved cost only** — the Phase 1 decision stands. The labour forecast is published as its own read (`GetLabourForecast(month)`) that the Cashflow tab may *display* as a commitment line, clearly separated from actuals. Feeding it into the forecast calculation proper is a follow-on decision for the accountant, not a default.

---

## 6. Dext, Xero and CIS: map the totals, don't split the invoices

Jeremy's target flow: PM sign-off fixes each worker-month's allocation → the worker's invoice arrives in Dext → the bookkeeper codes it to **the same totals** — no invoice splitting — and the Xero profit report still carries site and cost code.

**Settlement schedule (new).** At month close (or any time after sign-off), JPMS publishes per worker per period a schedule: gross totals split by site (project) × cost code × **line nature** — `CisLabour` / `CisMaterials` / `Travel` — plus the CIS deduction and net payable. Rendered on the reconciliation view and exportable (Excel via the toolbar; PDF later if the bookkeeper wants an attachable doc). Line nature defaults to CisLabour; materials/travel lines are added at sign-off level where a worker's arrangement includes them, since they change both the Xero account and the CIS treatment.

**Coding contract with Xero.** Each schedule line maps to: account code by nature (CIS labour / CIS materials / travel accounts) + site tracking option + cost-code tracking option, resolved through the effective-dated mapping entities (§3). The bookkeeper's job in Dext collapses to: match the bill's lines to the schedule's site/nature totals. Because the mapping is effective-dated, historic bills coded under old option names still report correctly, and current/future data maps consistently — the exact "historic vs current" worry in the brief.

**Reconciliation (extended, not rebuilt).** The existing settlement machinery — `SetXeroLineTimesheetCover`, `AddLabourSettlementVariance`, the four closure paths — gains a **per-worker per-month** cut on the Labour overview: each worker-month row gets a verdict (*matches* / *variance £x open* / *no bill yet*), the itemised bill lines behind it, and an **invoices-to-chase** count (approved timesheet cost with no covering bill after the period). His "Labourers to reconcile (11)" and "+ Invoices to chase (5)" chips map one-to-one. Where a bill mixes labour with materials from another arrangement, the existing `XeroCostSplit` path handles it — unchanged.

Boundary restated: JPMS publishes schedules and verifies bills against them. AP, the CIS return, payment runs and HMRC stay in Xero/Dext/Brightpay.

### 6a. Automated Xero coding (approval-gated)

Jeremy's follow-up sharpens the ambition: once the month is signed off, **the portal writes the coding into Xero itself** — "no manual input on Xero by me but rather by Claude." The portal already holds the Xero connection and an agent surface, so this is an extension of the settlement schedule, not a new integration:

- **Trigger:** month sign-off (§4) is the *only* trigger. Nothing reaches Xero from unsigned data — the same discipline as approval-gated posting in Phase 1, now applied to the ledger boundary.
- **Action:** for each worker-month schedule, the portal either (a) **codes the matching Dext-arrived bill** — sets account code by line nature, site and cost-code tracking options per the mapping entities, splitting bill lines to the schedule totals — or (b) where no bill has arrived yet, **stages a draft bill** matching the schedule for the subcontractor to be reconciled against when the real one lands. Everything is written as **DRAFT / awaiting-approval in Xero**: the automation does the keying, the accountant's approval in Xero remains the human gate. Nothing is auto-approved or auto-paid.
- **Audit:** every write is logged against the worker-month (what was coded, by which run, from which signed-off schedule), and the reconciliation verdicts (§6) then read back what Xero actually holds — so the automation is checked by the same machinery that checks a human.
- **Failure honesty:** where the mapping is incomplete (site with no tracking option, cost code unmapped, nature ambiguous) the run *skips and reports*, adding the gap to the chase list — it never guesses a code.

This lands after Phase C proves the mapping: the automation is only as safe as the mapping table it reads.

---

## 7. Phasing

**Phase A — the read layer, sign-off & UI redesign:** Labour overview (by worker / by site / by cost code) with month picker, chase list, export · cross-project weekly sign-off marker · absence recording · My Day + Labour tab redesign (§4a) · chat-created timesheets via the staged-action pattern (§4b).

**Phase B — forecast:** worker contracts + CIS status · projected spend with confidence bar · amount-due (net-of-CIS) column · forecast read exposed to the Cashflow tab as display-only.

**Phase C — settlement mapping:** line natures · settlement schedule per worker-month · Xero mapping entities + admin surface for the mapping · per-worker reconciliation verdicts + invoices-to-chase.

**Phase D — automated Xero coding (§6a):** approval-gated draft coding/bill staging via the Xero API, audit log, skip-and-report gaps. Gated on Phase C's mapping running clean for at least one full month cycle.

**Phase E — Monday replacement & sign-off forms (§8):** registers + staff acknowledgement forms. Can run in parallel with B–D; it shares no data model with Labour.

**Explicitly out of scope:** phone-call logging and the board summary (separate product decisions) · payroll/Brightpay export (Phase 2 of the original Labour scope, unchanged) · CIS verification or returns · feeding unapproved labour into CVR/cashflow calculations · replacing My Day with weekly capture · auto-approving or paying anything in Xero.

---

## 8. Monday replacement: registers & sign-off forms

Jeremy's follow-up widens the brief beyond timesheets: "add other items which we have tried to take out of Monday … forms, insurances, Subscriptions, etc. So no need for Monday — but also build a sign form for NDA's, staff policies etc." Scoped here as its own phase so it never blocks the labour work, and grounded in what the portal already has (directory contacts, document storage with revisioning, subcontractor compliance uploads, renewal-style chasing patterns):

**Registers.** A common register pattern — item, owner, counterparty, key dates, linked documents, status — instantiated per type rather than one generic grid, because the types differ in what expires: **Insurances** (policy, insurer, cover, renewal date, certificate on file), **Subscriptions** (service, cost, billing cycle, next renewal, cancellation notice period), **Trade accounts**, **Vans / assets** (registration, MOT, tax, insurance, assigned driver). Every dated field feeds the existing chase/to-do machinery so renewals surface before they lapse — that is the whole reason these live in Monday today. Existing to-dos and the Documents page absorb his "To-dos" and "Documents" nav items; nothing new needed there.

**Sign-off forms.** Staff acknowledgement flow for NDAs, staff policies, H&S documents: an admin publishes a document + form to a set of portal users (staff are already users; workers already sign in for My Day — same accounts), each recipient reads and signs on their own login (typed name + timestamp + document revision recorded — the same evidential pattern as drawing approval), and an admin view shows signed / outstanding per document with chasing. New policy revisions re-trigger the cycle. This deliberately reuses the portal's RBAC and document control rather than inventing an e-signature integration; if counter-signed third-party NDAs are ever needed, that is a DocuSign-class decision taken separately.

**Migration.** A one-off read of the Monday boards (CSV export or API) to seed the registers, so day one is populated, then Monday is retired. Agent-assisted mapping of Monday columns → register fields, human-reviewed before import.

---

## 9. Open questions

1. **Trade grouping:** the by-trade view needs a cost-code → trade mapping (architect codes are per project). Is a simple portal-maintained grouping table enough, or should trades come from a Xero account structure?
2. **Default placement for future days** in the by-site projection: last-worked site is proposed — does the PM want an explicit weekly plan instead (that drifts toward resourcing/rota territory)?
3. **Who records absence** — PM only, or workers via My Day ("mark today as holiday")? Latter improves the confidence bar but adds a worker-facing surface.
4. **Accountant confirmation** (carried over from the Phase 1 scope §9, still open): the settlement model — timesheet as timely actual, bill as final truth — now with the added covered-by-schedule coding step in Dext. This scope doubles down on it; it needs their yes.
5. **Later convergence:** if PS ever moves into the portal (or the modules are lifted into per-company sites, as discussed), the deferred entity dimension revives — park it, but keep new tables free of JBB-only assumptions where free to do so.
6. **Xero write scope (§6a):** confirm the connected Xero app's OAuth scopes cover bill create/update on the JBB organisation.
7. **Monday inventory (§8):** which boards are actually live today? Need the list (and an export) from Jeremy before the register set is final — "etc." is doing a lot of work in the brief.
