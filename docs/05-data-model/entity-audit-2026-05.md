# JPMS Entity Model Audit — Stage 4 Reconciliation

CLAUDE.md stage 4 audit performed after the Phase-1 UI shipped. Walks the as-built UI against the original scoping `entities.md` and the model files. UI demand is the tie-breaker.

## Summary

- **Confirmed: 28** entities — both modelled and demanded by at least one route.
- **Speculative (defer to Phase 2): 18** — listed in `entities.md` but no Phase-1 screen demands them.
- **Missing (must add): 6** — Phase-1 UI demands them but neither file captures them.
- **Three data-loss bugs in the as-built code** flagged for immediate fix.
- **Three consolidation verdicts** confirmed: keep `HsRecord` merged, keep `ChangeRecord` merged, keep Lead inline-contact for Phase 1.

## Three data-loss bugs found

These all stem from missing entities — the UI captures data the model can't hold, so the code concatenates / drops it.

1. **Toolbox-talk attendance** (`SiteToolboxTalk.razor:51-60`) — the form collects an attendee count but jams `"{topic} ({attendees} attendees)"` into `HsRecord.Summary`. The roster of who attended (required for H&S compliance evidence) is not captured at all. **Fix:** add `HsRecordAttendance` child table.

2. **Architect / client RFI reply** (`ArchitectRfiCard.razor:54-59`) — the reply text is appended to `ChangeRecord.Description` as `"…\n\nArchitect reply:\n{reply}"`. The author, timestamp, and "implies variation" flag (`ArchitectRfiCard.razor:21`) are all discarded. **Fix:** add `ResponseText`, `RespondedByEmail`, `ImpliesVariation` columns to `ChangeRecord`.

3. **BoQ Director sign-off** (`ProjectBoqSignOff.razor:60-72`) — currently held in component-local state. Refresh the page and it's gone. **Fix:** add `BoqSignOff` entity persisted via `IBoqStore`.

## Six missing entities

| Entity | UI evidence | Minimum properties |
|---|---|---|
| `BoqSignOff` | `/projects/{id}/boq/sign-off` | ProjectId, SignedOffByEmail, SignedOffAt, TenderTotalAtSignOff |
| `PracticalCompletion` | `/projects/{id}/closeout/pack` row 6, `/portal/client/.../practical-completion` (route declared) | ProjectId, AchievedAt, CertificateFileRef?, IssuedByEmail, IsClientSigned |
| `HandoverPackItem` | `/projects/{id}/closeout/pack` (currently hardcoded checklist) | ProjectId, Label, Detail, IsReady, EvidenceFileRef? |
| `Photo` | five UI surfaces use `PhotoGridTile` as a shared component | ProjectId, TakenAt, TakenByEmail, BlobUri, AttachedKind, AttachedId, Caption?, GpsLat?, GpsLng? |
| `HsRecordAttendance` | `/site/projects/{id}/toolbox-talk` | HsRecordId, AttendeeName, SignatureBlobRef?, SignedAt |
| `ChangeRecordResponse` (or fields on `ChangeRecord`) | architect/client approval flows | ResponseText, RespondedByEmail, ImpliesVariation. Recommend inline columns on `ChangeRecord` — single response per change is sufficient for Phase 1. |

## Speculative entities (defer to Phase 2 appendix)

Listed in `entities.md` but no Phase-1 screen demands them, so adding them now would just churn the SQL schema:

`Opportunity`, `Contact`, `Company / Architect Practice`, `Tender`, `Submittal` (as separate entity — it's a `ChangeKind`), `Correspondence`, `Instruction Log`, `RAMS`, `Renewal Event`, `Induction Record`, `Temporary Works`, `Audit`, `Inspection Template`, `Inspection (instance)`, `Daywork`, `Contra Charge`, `Subcontractor Retention`, `Aftercare Record`, `Portfolio Snapshot`, `Leading Indicator`, `Threshold`, `Exception Alert`, `Margin Trace` (kept as derived `CvrPackageRow`), `Win/Loss Reason` (collapsed into `LeadOutcome.Reason`).

Each gets a "promoted to Phase 1 when [trigger]" note in the appendix.

## Consolidation verdicts

### Keep `HsRecord` merged + add `HsRecordAttendance`

`/hs`, `/portfolio/hs`, `/projects/{id}/hs/golden-thread` all render the six kinds (Observation / NearMiss / Incident / CorrectiveAction / ToolboxTalk / Permit) in **one unified table**. The merge is demanded by the UI. The only field the unified shape can't carry is per-attendee toolbox-talk roster — fix with a child entity, not a split.

### Keep `ChangeRecord` merged + add response fields

`/projects/{id}/changes` renders all four kinds (RFI / Submittal / Variation / NoticeOfDelay) in one table. The portal approval pages share the same `ArchitectApprovalCard` component. The merge is demanded. The missing fields (`ResponseText`, `RespondedByEmail`, `ImpliesVariation`) are common to all four kinds, so add as columns rather than a child table.

### Keep `Lead` inline-contact for Phase 1

The scoping doc has `Contact` / `Company` / `ArchitectPractice` as first-class entities. **No Phase-1 screen demands a contact-centric pivot.** Every view shows the contact through the Lead. The `/portal/architect` portal uses an inline email (`PortalContext.ActingArchitectEmail`), not a referenced practice. Promotion trigger: when `/portal/architect/leads/new` ships, or a "leads-by-architect-practice" report is requested, or contact deduplication becomes a real requirement.

## Other consolidations confirmed

- `Opportunity` collapsed into `Lead.Stage`.
- `Tender` collapsed into `BoqLineItem` + `Project.Stage`.
- `BoQ` parent collapsed — implicit from line items.
- `Rate Library` parent collapsed — implicit from rates.
- `Margin Trace` kept derived (`CvrPackageRow`), not stored.
- `Programme Valuation Report` (issued artefact) collapsed into `Valuation.IsIssued`.
- `Timesheet Approval` (weekly batch) collapsed into boolean per-row.
- `Cost Code Allocation` collapsed into `Timesheet.CostCode` foreign-key.

## Updated ERD

See the updated `entities.md` for the consolidated diagram.
