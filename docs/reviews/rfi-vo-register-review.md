# RFI & VO Registers — Structure Review & Build Plan

**Prepared for:** Nigel Reilly · **Date:** 1 June 2026 · **Scope:** review + plan only, no code, no data import.

The two spreadsheets (`RFI Register - By France`, `Variation Register - By France`) and the WhatsApp notes are treated here as a **guide to what each register view must show and the statuses each record moves through** — not as data to load.

---

## 1. Verdict

**Yes — the structure already handles an RFI and VO register, at skeleton level.** A unified change layer runs end-to-end today:

- a domain model (`ChangeRecord` with `ChangeKind` = `Rfi` / `Submittal` / `Variation` / `NoticeOfDelay`),
- CQRS contracts (`RaiseChange`, `UpdateChangeDetails`, `ListChangesForProject`, `GetChangeById`),
- API handlers, validation, authorisation and route registration for all four,
- a persisted EF table (`ChangeRecords`) + initial migration,
- register UI (`ProjectChanges.razor` with All / RFIs / Submittals / Variations / Delays filters, via `ChangeRecordTable`), plus portal views `ArchitectRfis.razor` and `ClientVariations.razor`.

The site map already routes the register pages the spreadsheets imply (`/projects/{id}/changes/rfis`, `/changes/variations`, `/changes/variations/report`).

So the question "can a project show a register of RFIs and VOs?" is **architecturally already yes.** What's missing is *register-grade detail*: the per-kind **status lifecycles** (the "moving through statuses" logic you called out), the **fields** the two register views actually capture, the **trigger/source** of each change, and the **RFI→VO link**. None of this needs re-architecting — it's completing a frame that's already the right shape.

**One fork to settle first (Section 4).** Today all four change kinds share **one `ChangeRecord` row and one `ChangeStatus` enum**. The docs (`docs/05-data-model/entities.md`, `status-models.md`) model RFI, Submittal, Variation and NoD as **separate entities with separate lifecycles**. That tension is the root of most gaps below, so it deserves a decision before any build.

---

## 2. What exists today

| Layer | Artefact | Notes |
|---|---|---|
| Model | `contracts/Models/ChangeRecord.cs` | One record, shared `ChangeStatus`, `ImpliesVariation` flag |
| Contracts (CQRS) | `contracts/Changes/*.cs` | `RaiseChange`, `UpdateChangeDetails`, `ListChangesForProject`, `GetChangeById` |
| API | `api/Features/Changes/**` | Handlers, validation, auth gates, endpoints, entity mapping |
| Persistence | `api/Data/Entities/ProcurementEntities.cs` → `ChangeRecordEntity`; migration `20260528221059_InitialCreate` | `ChangeRecords` table |
| Read side | `jpms/Services/IChangeRegister.cs` + `HttpChangeRegister.cs`; `jpms/Features/Changes/ChangesReadModel.cs` | Per-project caching, kind filter |
| UI — register | `jpms/Pages/ProjectChanges.razor` + `jpms/Components/ChangeRecordTable.razor` | Generic 6-column table, kind filter tabs |
| UI — portals | `jpms/Pages/ArchitectRfis.razor`, `ClientVariations.razor`; cards `ArchitectRfiCard`, `ArchitectApprovalCard` | Architect RFI reply, client VO approval |
| Routes | `docs/site-map.md` §1.7 | `/changes`, `/changes/rfis[/{id}]`, `/changes/variations[/{id}]`, `/changes/variations/report`, `/changes/delays` |
| Domain docs | workflow 05, `status-models.md`, `entities.md` | Richer lifecycles already written, not yet in code |

---

## 3. What the register views demand vs. what the model holds

### 3.1 RFI register

| Register column | Held today? | Field |
|---|---|---|
| RFI No. | ✅ | `Reference` |
| Date Issued | ✅ | `RaisedAt` |
| Subject | ✅ | `Title` |
| Drawing / Detail Ref | ❌ | — |
| Raised To (e.g. PLG Architects) | ❌ | — |
| Response Due | ❌ | — (needed for the overdue rule) |
| Response Date | ✅ | `RespondedAt` |
| Days Outstanding | ❌ (derive) | — |
| Status — Open / Responded / Closed | ⚠️ mismatch | `ChangeStatus` doesn't match |
| Notes | ✅ | `Description` / `ResponseText` |
| Related Drawing / Spec | ❌ | — (drawing link) |

### 3.2 VO register

| Register column | Held today? | Field |
|---|---|---|
| VO No. | ✅ | `Reference` |
| Date Issued | ✅ | `RaisedAt` |
| Subject | ✅ | `Title` |
| Drawing / Detail Ref | ❌ | — |
| Trade / Package Affected | ❌ | — (BoQ / cost-code link) |
| Value (£ Excl. VAT) | ✅ | `Value` (net) |
| VAT (£) | ❌ | — |
| Total (£ Incl. VAT) | ❌ (derive) | — |
| Date Submitted to EA | ❌ | — (needed for the >14d overdue rule) |
| Status — For Review / Submitted / Approved / Rejected / Superseded | ⚠️ mismatch | `ChangeStatus` doesn't match |
| Approved Date | ⚠️ partial | `RespondedAt` |
| Notes | ✅ | `Description` |
| Linked Clause | ❌ | — |
| **Linked RFI / NoD** | ❌ | `ImpliesVariation` is the only half-built hook |
| Days Outstanding | ❌ (derive) | — |

### 3.3 Trigger / source (from the WhatsApp notes)

The notes describe **who/what sets a change in motion** and the **RFI→VO chain**:

- **RFI triggers:** Site Manager → PM query · revised Drawings → PM · Verbal → PM/SM.
- **VO triggers:** change to scope (client) · an answered RFI · unknown (e.g. ground conditions).
- **Flow:** client change → VO · SM→PM query → RFI raised to Client/Architect · **RFI → VO once answered** · revised drawings → RFI.

There is **no field today for the trigger/source of a change**, and **no real link** from a Variation back to the RFI that spawned it. Capturing these is what "handle what is mentioned" means in practice.

---

## 4. The core decision — one record, or split per kind?

Everything in Section 3 flows from this. The codebase made an early call (one `ChangeRecord`, one `ChangeStatus`) that the domain docs don't share.

**Option A — keep the unified `ChangeRecord`, add nullable kind-specific fields.**
Cheapest now. But it piles VO-only columns (VAT, submitted-to-EA, linked RFI, linked clause) and RFI-only columns (raised-to, response-due) onto a single row, most of them null for any given kind. Against `CLAUDE.md`: a record whose fields are meaningless half the time isn't telling the truth about the domain, and `ChangeStatus` can't read true for two different lifecycles at once.

**Option B — keep the shared identity, split the kind-specific detail.**
`ChangeRecord` stays the common spine (ref, title, project, raised-by/at, trigger, status-as-string-per-kind), and RFI-specific and Variation-specific attributes live in their own records/tables (`RfiDetail`, `VariationDetail`) with their own status enums. Matches the docs' separate-entity model and the site map's already-separate `/rfis` and `/variations` views. More work up front; each view then reads exactly the fields it shows, and the status logic is enforceable per kind.

**Recommendation: Option B.** It's the structure the docs already describe, it keeps each register view honest to one entity, and it makes the "moving through statuses" rules implementable as real guards rather than a free-for-all enum. Flagging it rather than quietly picking, per `CLAUDE.md`.

---

## 5. Status lifecycles to implement

Today: one shared enum `ChangeStatus { Open, AwaitingResponse, Approved, Rejected, Closed }`, and `UpdateChangeDetails` lets any status be set with no transition rules. The "domain logic of moving through statuses" you want needs distinct, **guarded** lifecycles:

- **RFI** (docs + register): `Open → Awaiting reply → Replied → Closed`, with a branch `Replied → Implies variation` (promotes to a VO). Register surfaces three states (Open / Responded / Closed); *overdue* is derived (open > 7 days past Response Due).
- **Variation** (docs + register): `Drafted → Pricing → (Awarded) → Awaiting client approval → Approved | Rejected | Withdrawn`. Register surfaces For Review / Submitted / Approved / Rejected / Superseded; *overdue* is derived (submitted to EA > 14 days, unapproved).

These are different state machines and shouldn't be forced through one enum (Section 4).

---

## 6. Build plan (phased, no code in this pass)

Walking the `CLAUDE.md` chain — story → UI → site map → data → backend — each phase ties to confirmed user stories in workflow 05.

**Phase 0 — decide.** Settle Section 4 (unified vs split) and confirm the two status vocabularies in Section 5. Everything downstream depends on it.

**Phase 1 — model the registers properly.** Add the trigger/source (option set per kind), the RFI fields (raised-to, drawing ref, response-due), and the VO fields (net + VAT, trade/package, submitted-to-EA, linked clause, **linked RFI**). Derive Days Outstanding, Total Incl. VAT and Overdue rather than storing them. *Serves:* US-05-01/02/07.

**Phase 2 — status state machines.** Per-kind status with transition commands and gates (the legal moves, not "set any value"). Replaces the open-ended `UpdateChangeDetails` for status changes. *Serves:* the "moving through statuses" requirement; US-05-08/10/12.

**Phase 3 — register views.** Give RFI and VO their own tables/columns (the generic `ChangeRecordTable` can't serve both well): RFI = raised-to, response due, days outstanding, status; VO = net/VAT/gross, trade, submitted-to-EA, status, linked RFI. *Serves:* US-05-11 (the canonical VO list); site-map `/changes/rfis`, `/changes/variations`.

**Phase 4 — detail views + the RFI→VO promotion.** Build `/changes/rfis/{id}` and `/changes/variations/{id}` (routes exist, pages don't), and the "raise VO from this RFI" action that carries the link across — turning the existing `ImpliesVariation` flag into a real chain. *Serves:* US-05-06/07.

**Phase 5 — VO list report.** `/changes/variations/report` — the canonical VO view (status, value, client-approval state, linked WO), filterable. *Serves:* US-05-11.

Entry of data stays manual through these views, as intended — no import, no seeding.

---

## 7. Not touched / out of scope

- No code written and no migrations added in this pass.
- No spreadsheet data imported or seeded; the registers fill up through the app's own views.
- Submittals and NoD lifecycles are noted but not expanded here — they ride the same decision in Section 4.
- Programme/valuation feed on VO approval (US-05-12) is downstream of Phase 2 and out of this register's scope.
