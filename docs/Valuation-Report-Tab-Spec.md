# Valuation Report tab — design spec

**Status:** Draft for sign-off
**Author:** Cowork (for Nigel Reilly)
**Date:** 26 June 2026
**Source artefact:** `By France - Valuation 18 - June 26 - New Build.pdf`

---

## 1. What we're building

A new **Valuation Report** tab in the project view, sitting third — **Overview → Requests → Valuation Report → Financials**. The report is an interim valuation: a priced statement of contract works plus variations, with the cumulative value claimed to date, that can be **snapshotted** and used to request funds for a client. As the job progresses, you claim more cash each period and offset omitted/declined work; the system keeps a dated record of every claim so each snapshot reconciles.

The user-facing ask is the ability to add and maintain **three elements** and show them on a UI close to the existing report:

1. **Contract values** — the priced bill (the original contract sum).
2. **Variation orders** — listed by line item, including omits and additions.
3. **Cash called up** — the dated record of what has been claimed/certified each period.

---

## 2. How the By France report is structured

The PDF is one workbook with three priced blocks, a variations register, and a summary.

### 2.1 Contract Sum block
Line items grouped by **work section** (NRM/SMM codes: `A10 Preliminaries`, `C20 Demolition`, `D20 Excavation & filling`, `E10 In situ concrete`, … `Q50 Site furniture`). Each line carries:

| Field | Example |
|---|---|
| Cost code | `0001`, `0005`, `0017` (these match your existing cost codes) |
| Description | "Site Supervision" |
| Quantity | `52` |
| Unit | `week`, `item`, `m²`, `nr`, `m`, `m³`, `Tn`, `kg` |
| Rate | `£1,250.00` |
| Contract Sum (line total) | `£65,000.00` (qty × rate) |
| Comments | "Omit item V03", "Provisional Sum" |
| Claim % | cumulative % complete (e.g. `100%`, `70%`, `20%`, `0%`) |
| Claim 10 … Claim 22 | amount claimed in each valuation period |
| Check | reconciliation column (`£0.00` when balanced) |

Block subtotal: **£1,432,573.00** (90.48% complete).

### 2.2 PC Sums block (`PC01`–`PC23`)
Prime-cost / provisional sums (tiling, sanitary ware, staircase supply, alarms, landscaping, etc.). Same column structure; most are later omitted into variations. Subtotal: **£297,882.00**.

### 2.3 Contingency block
Single line, **£50,000.00** (omitted under V45).

> Contract Sum total = 1,432,573 + 297,882 + 50,000 = **£1,780,455.00**

### 2.4 Variations register (`V01`–`V73`)
Each variation has a number and **one or more line items**. A line is one of:

- **Omit item** — negative amount, removes scoped work (often a PS being replaced).
- **New item** — added work, positive amount.
- **Declined** / **TBC** — recorded but not priced into the total.

Lines carry the same qty/unit/rate/amount/claim%/per-claim columns as the contract block. Variations net out to **£235,185.33** (omits −£… offset by adds +£…). Example: `V18` omits the £35k/£45k window & door PS lines and adds the £88,640 Generation supply.

### 2.5 Summary / retention block
```
Contract Sum                              £1,780,455.00
Net Variations                              £235,185.33
Current Revised Contract Sum              £2,015,640.33
Total Contract & Variation Works Complete £1,589,530.47
Less Retention @ 5%                         £(79,476.52)
Add back Retention Release @ 2.5%                  £ -
Less Certified to Date                    £1,513,295.82
Total Payment Due Excluding VAT               £(3,241.87)
```

The calculation chain we must reproduce:

```
Revised Contract Sum   = Contract Sum + Net Variations
Total Works Complete   = Σ (line claim% × line amount)   across contract + variations
Retention Held         = Total Works Complete × Retention%
Retention Released     = eligible works × ReleasePercent
Payment Due (ex VAT)   = Total Works Complete − Retention Held + Retention Released − Certified to Date
```

"Certified to Date" is the sum of everything called up in prior valuations — i.e. the **cash called up** record. The negative payment due here shows an over-certification that the next claim offsets.

---

## 3. Mapping to the current system

Your codebase already contains most of the building blocks. Architecture recap: Blazor frontend (`jpms`) with project tabs in `Components/ProjectTabNav.razor`; CQRS contracts in `contracts/`; EF Core handlers in `api/Features/`, entities in `api/Data/Entities/`, migrations in `api/Migrations/`; frontend talks to the API through `Http*Store` services behind `I*Store` interfaces, surfaced to pages via read models.

| Report concept | Existing in system | Decision |
|---|---|---|
| Contract bill line | `BoqLineItem` (code, desc, unit, qty, rate, cost code) | Closely matches. New dedicated `ValuationLineItem` (per decision in §6), optionally **seeded** from BoQ. |
| Cost code (`0001`…) | `CostCode` model + `CostCodeEntity` | Reuse — line items reference it. |
| Valuation period / "Claim n" + date | `ClaimPeriod` (PeriodNumber, Start/End dates) | Reuse — gives the **date** for cash called up. |
| Summary/retention per period | `Valuation` (gross/retention/net, IsIssued, IssuedAt) | Overlaps with the new snapshot. See §6 decision 3. |
| Snapshot pattern | `CvrSnapshot` (frozen figures at a timestamp) | Mirror this pattern for the issued valuation report. |
| Tab shell + nav | `ProjectPageShell`, `ProjectTabNav` | Add one enabled tab entry + one page. |

So this is largely **assembling existing patterns**, not new infrastructure.

---

## 4. Proposed data model (new dedicated)

Four records in `contracts/Models/Commercial.cs` (+ matching entities, DbSets, migration). All data is **entered manually through the UI** — no BoQ seeding, no importer (see §6).

**`ValuationLineItem`** — one priced row of the bill (contract or variation). Added manually.
```
ValuationLineItemId, ProjectId,
ElementType: ContractWorks | PcSum | Contingency | Variation,
SectionCode ("A10"), SectionName ("Preliminaries"),   // works/PC
VariationRef ("V18"), VariationTitle,                  // variations
LineType: Priced | ProvisionalSum | Omit | Declined | Tbc,
CostCode ("0001"), Description, Unit, Quantity, Rate,
LineAmount,            // qty × rate; negative for omits
Comments, DisplayOrder
```

**`ValuationClaim`** — one valuation period (a "Claim n" / the funds request event), with a status lifecycle.
```
ValuationClaimId, ProjectId,
ClaimNumber (18),
ClaimDate,
Status: Draft | Preapproved | Confirmed,
RetentionPercent (5.0), RetentionReleasePercent (2.5),
PreapprovedAt, ConfirmedAt,
// totals frozen when Confirmed:
ContractSum, NetVariations, RevisedContractSum,
TotalWorksComplete, RetentionHeld, RetentionReleased,
CertifiedToDate, PaymentDueExVat
```

**`ClaimLine`** — per claim, per line item: the % complete entered and the resulting claimed amount.
```
ClaimLineId, ValuationClaimId, ValuationLineItemId,
PercentComplete,          // cumulative % entered this claim
CumulativeClaimed,        // PercentComplete × LineAmount
PeriodIncrement           // CumulativeClaimed − previous confirmed cumulative for this line
```

### Claim lifecycle (the workflow)

```
1. Build the bill   — manually add ContractWorks / PcSum / Contingency lines and Variation lines.
2. Start a claim    — new ValuationClaim (Draft). For each line, enter / update % complete.
                      System computes CumulativeClaimed and PeriodIncrement live.
3. Hit OK           — "we are claiming this" → Status = Preapproved. Amounts locked for the claim,
                      awaiting the client.
4. Client pays      — Status = Confirmed. Per-row claimed amounts become final; CertifiedToDate
                      advances; the next claim measures its increment from here.
```

Derived figures (recomputed from source, so every claim reconciles):
```
CumulativeClaimed(line, claim) = PercentComplete × LineAmount
CertifiedToDate(claim)         = Σ CumulativeClaimed across all Confirmed claims before this one
TotalWorksComplete(claim)      = Σ CumulativeClaimed for this claim across all lines
RetentionHeld                  = TotalWorksComplete × RetentionPercent
PaymentDue (ex VAT)            = TotalWorksComplete − RetentionHeld + RetentionReleased − CertifiedToDate
```

Each **Confirmed** claim is one `Claim n` column in the Excel report.

---

## 5. Build plan (after sign-off)

1. **Contracts** — add the four models to `contracts/Models/Commercial.cs`; add command/query records under `contracts/Commercial/` (`AddValuationLineItem`, `RecordClaimEntry`, `IssueValuationReport`, `ListValuationLinesForProject`, `GetValuationReport`).
2. **Persistence** — add entities to `api/Data/Entities/CommercialEntities.cs`, `DbSet`s to `JpmsContext`, and one EF migration (`AddValuationReport`).
3. **API** — handlers/endpoints/validation/authorisation under `api/Features/Commercial/` (mirror the existing `DraftValuation*` set).
4. **Frontend store** — `IValuationReportStore` + `HttpValuationReportStore`, plus a read model for the page.
5. **UI** — new page `Pages/ProjectValuation.razor` at `/projects/{ProjectId}/valuation`, wrapped in `ProjectPageShell ActiveTab="valuation"`; table component `Components/ValuationReportTable.razor` (grouped: Contract Works by section → PC Sums → Contingency → Variations by VO, with per-claim columns and the summary/retention footer); add/edit dialogs for contract lines, variations, and "record a claim" (cash called up, with date). A **Snapshot / Issue** action freezes a `ValuationReport`.
6. **Tab nav** — insert `new("valuation", "Valuation Report", true)` between `requests` and `financials` in `ProjectTabNav.razor` (the slug also drives the `HrefFor`/`ClassFor` logic already in place).
7. **Verification** — unit tests on the summary maths reproducing the By France figures exactly (Revised Contract Sum £2,015,640.33, Works Complete £1,589,530.47, Retention £79,476.52, Payment Due −£3,241.87), plus a build + render check of the new tab.

---

## 6. Decisions — resolved

1. **Claim granularity** → full **per-line × per-period** model (`ClaimLine` per `ValuationClaim`), with a **Draft → Preapproved → Confirmed** lifecycle driven by per-line % completion. Mirrors the spreadsheet and auto-reconciles.
2. **Seed from BoQ** → no. Contract lines and VOs are **added manually** through the UI; data is built up naturally.
3. **Existing `Valuation`** → leave it for CVR/cashflow; build this as a separate model. No collision.
4. **Importer** → none. If seed data is needed later it'll be a throwaway script, not a product feature.

The dashboard should visually resemble the Excel report it replaces (grouped bill + claim columns + retention/payment-due footer).
