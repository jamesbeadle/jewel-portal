# Variations (VOQ → VO) — Linking Investigation & Build Plan

**Prepared for:** Nigel Reilly · **Date:** 22 July 2026 · **Scope:** investigation + plan; no code changed in this pass.

Trigger: on `/projects/{id}/voq/bf-voq-v72` (VOQ-0072, "Coffer Details and Insulation ZZ bedroom"), clicking the **VO V72** chip in the lineage bar does nothing, and the Variations register is full of red **NOT LINKED** badges. This document explains exactly why, maps the current machinery end to end, and sets out a phased plan — incorporating the three decisions taken on 22 July (chip = scroll/highlight; manual approve = full write-through; linking moves off the register onto the detail page).

---

## 1. Summary of findings

1. **The dead VO chip is a navigation gap, not a data gap.** V72 *is* correctly linked to VOQ-0072 in the database (`VariationOrderEntity.VariationOrderQuoteId`). There is simply no VO destination to navigate to: the lineage bar deliberately points the VO chip at the VOQ page ("managed on the VOQ page"), so when you are *already on* that page the chip is a link to the URL you're on. Clicking it re-navigates to the same route — which is why "when I click it it's loading voq72".
2. **The NOT LINKED problem is the seeded history.** All By France variations (V01–V76), plus Abbot Road and Albany Mews, were seeded from the Valuation 18 workbook by `api/Migrations/seed-byfrance-variations.sql` (and siblings) with `RequestId = ''` — the script comments say so explicitly: *"RequestId is empty (no originating RFQ email exists for seeded history)."* The red badge is the UI honestly reporting that the lifecycle trace stops at the VOQ.
3. **A repair path already exists, in the wrong place.** `LinkVoqToRequest` (API command + `Link…` button on the register) attaches a VOQ to a request after the fact, validates project match and one-VOQ-per-request, and sets `HasRfq` on the request. You've said the inline register UI for this is cluttered — the plan moves it onto the VOQ detail page.
4. **There is no manual approval path.** `ApproveVariationOrderQuoteHandler` hard-requires status `Selected` (a winning tender chosen), which itself requires a bid package and subcontractor. Seeded VOQs stuck in `Tendering` (V19, V70, V71) and any future "just approve it" case have no route to becoming a VO without performing the whole tender ceremony.
5. **Bug found along the way — cross-project numbering.** Both `CreateVoqFromRfqHandler` and `ApproveVariationOrderQuoteHandler` compute the next number as `MAX(Number) + 1` across **all projects** (no `ProjectId` filter). With By France seeded to V76, the next VO approved on *any other project* would be numbered V77+. VOQ numbering has the same defect. This should be fixed before manual approval goes live, or new projects will inherit By France's sequence.

---

## 2. How the pieces fit together today

### 2.1 Data model (`api/Data/Entities/VariationEntities.cs`)

| Entity | Key links | Notes |
|---|---|---|
| `VariationOrderQuoteEntity` | `ProjectId`, `RequestId` (string, empty allowed), `SelectedBidPackageId`, `SelectedSubcontractorId` | Status: Draft(0) → Inviting(1) → Tendering(2) → Selected(3) → Approved(4) / Rejected(5) |
| `VariationOrderEntity` | `VariationOrderQuoteId`, `RequestId` (copied from VOQ at approval), `SubcontractorId`, `CostCode` | Status: Approved(0) → Issued(1) / Cancelled(2). Ref `V{n}` |
| `SubcontractorVariationRequestEntity` | `WorkOrderId`, → `VariationOrderQuoteId` on acceptance | Portal-raised; acceptance creates a *Selected* VOQ |

Links are loose string ids, not FK-enforced navigation properties, so "unlinked" states are representable and everything tolerates them — good for history, but it means the UI has to do the tracing.

### 2.2 The intended lifecycle

```
Request (REQ-####, mailbox triage)
  → promote to RFI (official instrument)
    → enable RFQ on the RFI
      → CreateVoqFromRfq  (one VOQ per request, status Draft)
        → bid packages → tenders → SelectVoqTender (status Selected)
          → ApproveVariationOrderQuote  ← the only door to a VO today
              writes, in ONE transaction:
              1. VariationOrder (status Approved)
              2. Valuation Report line (ElementType=Variation, ref V{n})
              3. CVR QS accrual (add)
              4. Cost-centre budget CommittedAmount
              5. VOQ → Approved
          → IssueVariationOrder / ReviseVariationOrderValue / CancelVariationOrder
          → IssueWorkOrderForVariationOrder (instructs the work)
```

### 2.3 UI surfaces

- **Register** — `jpms/Pages/ProjectRequests.razor` ("Variations" filter): one row per VOQ (`VariationRow`), the VO carrying ref/value/status once approved. The **Request** column renders the red `Not linked` badge + inline `Link…` select when `RequestFor(voq)` is null.
- **VOQ detail** — `jpms/Pages/ProjectVoqDetail.razor` (`/projects/{id}/voq/{voqId}`): bid packages, tender selection, the **Approve & raise VO** panel (only when `Selected`), and the **Variation Order** side panel (only when a VO exists) with Issue / Revise value.
- **Lineage bar** — `jpms/Components/RecordLineage.razor` + `LineageChip.razor`: Request → RFI → VOQ → VO → bid packages. Rendered on request, VOQ and bid-package pages.
- **No VO page exists.** Routes registered in `VariationsRouteRegistration.cs` include `GetVariationOrderById`, but nothing in the front end routes to a VO.

### 2.4 Root cause of the dead click, precisely

`RecordLineage.razor` line ~76:

```razor
@if (Vo is not null)
{
    <LineageChip Href="@VoqHref"                      @* ← points at the VOQ page *@
                 IsActive="@(ActiveId == "vo")"        @* ← never "vo" on the VOQ page *@
                 ... />
}
```

On `ProjectVoqDetail`, `ActiveId="voq"`, so the VOQ chip is the highlighted inert one and the VO chip renders as a *live link* whose href is the current URL. Blazor navigates to the same route; `KeyedPageRouteView` sees identical route values, so nothing visibly changes. The chip isn't broken data — it's a link to where you already are.

---

## 3. The old-data picture

- Seed scripts in `api/Migrations/`: `seed-byfrance-variations.sql` (V01–V76: 64 Approved+Issued pairs, 6 Rejected-only, 3 Tendering-only), `seed-abbotroad-variations.sql`, `seed-albanymews-variations.sql`, plus `seed-byfrance-v77-voq.sql`. All idempotent MERGEs on stable ids (`bf-voq-vNN` / `bf-vord-vNN`).
- Every seeded VOQ has `RequestId = ''` and `CreatedByEmail = 'seed@jewelgroup.co.uk'` — the latter is a usable heuristic for "historic record" if we ever want to badge them differently.
- RFIs *were* separately seeded (`scripts/seed-byfrance-rfis.sql` etc.), so **some** variations have a real RFI they could be linked to via the existing `LinkVoqToRequest` — but the workbook register doesn't record an RFI-per-VO mapping, so matching is a judgement call, done record by record by someone who knows the job. That fits your decision: linking belongs on the **detail page**, done deliberately, not squeezed into a register cell.
- The valuation report lines for seeded VOs were written directly by the seed scripts. The one door that normally writes those lines (`ApproveVoq`) was bypassed — which is fine for history, but it is exactly why a *manual approve* must go through the real handler, so future approvals never fork from the valuation/CVR/budget again.

---

## 4. Build plan

Phases are ordered so each lands value on its own; 1–3 are small, 4 is the substantive feature.

### Phase 1 — Make the lineage bar honest (the V72 fix)

*Decision taken: scroll/highlight the VO panel.*

1. Give the Variation Order side panel on `ProjectVoqDetail.razor` a stable anchor, e.g. `id="variation-order"`.
2. In `RecordLineage.razor`, when the VO chip is rendered *on the VOQ page* (`ActiveId == "voq"`), point its href at `{VoqHref}#variation-order` and add a brief highlight (e.g. a `:target`-triggered or JS-interop flash of the panel border) so the click visibly lands somewhere.
3. From *other* pages in the chain (request page, bid-package page) the VO chip keeps navigating to the VOQ page — now with the `#variation-order` fragment so it arrives focused on the VO.
4. While in the file: the VOQ chip placeholder logic is untouched; no behaviour change elsewhere. If a first-class VO register page is wanted later, it slots in here by swapping the href — nothing else in the bar changes.

*Touches:* `RecordLineage.razor`, `ProjectVoqDetail.razor`, small CSS/JS for the highlight. No API change, no migration.

### Phase 2 — Fix cross-project numbering (prerequisite for Phase 4)

1. Scope both `MAX(Number)` queries by `ProjectId`:
   - `CreateVoqFromRfqHandler` (VOQ numbers)
   - `ApproveVariationOrderQuoteHandler` (VO numbers)
2. Audit existing data for casualties: any VOQ/VO whose number continues another project's sequence. (Query: per project, compare `MIN/MAX(Number)` vs `COUNT`; seeded projects are internally consistent, so damage is limited to records created through the app after seeding.)
3. Consider a unique index `(ProjectId, Number)` on both tables to make the invariant structural.

*Touches:* two handlers, one migration for the indexes, a one-off audit SQL in `scripts/`.

### Phase 3 — Move linking to the detail page; calm the register

*Decision taken: linking is done on the click-through, the dashboard gets neater.*

1. **VOQ detail page gains an "Originating request" panel** (left column, above or below Bid packages) shown when `voq.RequestId` is empty:
   - a `SearchSelect` over the project's requests (RFIs first, already-taken requests excluded — reuse the `LinkCandidates` ordering logic, moved out of `ProjectRequests.razor` into a shared service or the store),
   - a Link button calling the existing `Variations.LinkToRequestAsync` (no API change needed — `LinkVoqToRequest` already validates project match and one-VOQ-per-request and sets `HasRfq`),
   - on success the lineage bar re-renders with the Request/RFI chips populated — the payoff is visible on the spot.
   - When `RequestId` is set but the request no longer resolves, show the same panel with a warning rather than pretending it's linked.
2. **Register slims down**: the Request column keeps the link-through chip when linked; when not linked it shows a *quiet* badge (muted "No request" rather than red NOT LINKED) that is itself a link to the VOQ page where fixing it lives. Remove the inline select/Link…/Cancel machinery and its state (`linkingVoqId`, `linkTargetRequestId`, `linkBusy`, `linkError`) from `ProjectRequests.razor`.
3. Optional polish: rows whose VOQ `CreatedByEmail = 'seed@jewelgroup.co.uk'` could badge as "Historic" — worth doing only if the muted badge alone doesn't read clearly enough.

*Touches:* `ProjectVoqDetail.razor`, `ProjectRequests.razor`, possibly a small shared component (`LinkRequestPanel.razor`). No API change, no migration.

### Phase 4 — Manual approval: VOQ → Approved ⇒ VO, without the tender ceremony

*Decision taken: full write-through, same pipeline as the normal path.*

1. **API**: relax `ApproveVariationOrderQuoteHandler`'s gate from `Status == Selected` to `Status is Draft or Inviting or Tendering or Selected` (still refusing Approved and Rejected). Everything else stays identical — same transaction writing VO + valuation line + QS accrual + committed budget. One door, now with a wider frame.
   - Keep requiring an explicit **cost code**, and a **value** (command value, else `EstimatedValue`, else refuse) — unchanged.
   - `SubcontractorId` on the VO simply stays null when no tender was selected — the entity already allows it.
   - Record provenance: the approval is already stamped (`ApprovedAt`, `ApprovedByEmail`). If you want manual approvals distinguishable later, add a nullable `ApprovalRoute` ("tendered" / "manual") to the VOQ — decision point, not required for function.
   - Authorisation: reuse `VariationRoles.AllowedToManageVariations` (Admin / MD / PM / QS) — same as today, no new role logic.
2. **UI** (`ProjectVoqDetail.razor`): show the existing **Approve & raise Variation Order** panel whenever there is no VO and the VOQ isn't Rejected — not only when `Selected`. For non-Selected statuses, title it "Approve manually & raise VO" and add one sentence of copy ("No tender selected — the VO is raised without a subcontractor attached"). Same cost-code + value fields, same `ApproveVoqAsync` call.
3. **Guard the seeded-history edge**: the three By France TBC VOQs (V19, V70, V71) net to £0 in the workbook and have **no** seeded valuation line that counts, so manually approving them writes through cleanly. But as a rule, before manually approving any *seeded* VOQ, check the valuation report doesn't already carry that `VariationRef` — the handler currently creates its line unconditionally. Cheap safety: in the handler, if a `ValuationLineItems` row with the same `ProjectId + VariationRef` already exists, skip creating a duplicate line (and log/comment). This also makes approve idempotent-ish against reseeds.
4. **Validation file** (`ApproveVariationOrderQuoteValidation`) is untouched — the status rule lives in the handler.

*Touches:* one handler (+ optional entity field & migration), one validation note, `ProjectVoqDetail.razor`. Contract shape (`ApproveVariationOrderQuote`) unchanged, so no front-end store changes beyond the UI condition.

### Phase 5 — Verification

- Unit/integration: approve from `Tendering` with cost code + value → assert all four write-throughs and VOQ → Approved; approve with existing same-ref valuation line → assert no duplicate line; approve on a second project → assert numbering starts at that project's own max (Phase 2).
- UI walk: V72 chip click scrolls/highlights; unlinked VOQ links from its detail page and the lineage bar fills in; register shows the quiet badge and no inline select.
- Data audit after Phase 2: `SELECT ProjectId, COUNT(*), MIN(Number), MAX(Number) FROM VariationOrders GROUP BY ProjectId` (and VOQs) — confirm sequences are per-project sane before and after.

---

## 5. Decision log & open points

| Point | Status |
|---|---|
| VO chip behaviour | **Decided:** anchor-scroll + highlight of the VO panel on the VOQ page; no dedicated VO page for now |
| Manual approve semantics | **Decided:** full write-through via the existing handler, gate widened; no record-only variant |
| Linking UX | **Decided:** moves to the VOQ detail page; register badge goes quiet and links through |
| Bulk-link seeded VOQs to seeded RFIs by script | Open — deferred; per-record linking on the detail page is the chosen mechanism, a bulk pass can come later if it proves tedious |
| `ApprovalRoute` provenance field on VOQ | Open — nice-to-have, needs a migration |
| Unique index `(ProjectId, Number)` | Recommended in Phase 2 — confirm before adding |
| "Historic" badge off `seed@jewelgroup.co.uk` | Optional polish in Phase 3 |

---

## 6. File map (for whoever picks this up)

| Concern | File |
|---|---|
| Lineage bar / chips | `jpms/Components/RecordLineage.razor`, `LineageChip.razor` |
| VOQ detail (approve, VO panel, future link panel) | `jpms/Pages/ProjectVoqDetail.razor` |
| Register (Variations tab, NOT LINKED, inline link UI to remove) | `jpms/Pages/ProjectRequests.razor` |
| Front-end store | `jpms/Services/IVariationStore.cs`, `HttpVariationStore.cs` |
| Front-end routes | `jpms/Features/Variations/VariationsRouteRegistration.cs` |
| Approve handler (gate + write-through + numbering) | `api/Features/Variations/Commands/ApproveVariationOrderQuoteHandler.cs` |
| VOQ creation (numbering) | `api/Features/Variations/Commands/CreateVoqFromRfqHandler.cs` |
| Link repair command | `api/Features/Variations/Commands/LinkVoqToRequestHandler.cs` |
| Entities | `api/Data/Entities/VariationEntities.cs` |
| Seeded history | `api/Migrations/seed-*-variations.sql`, `seed-byfrance-v77-voq.sql` |
| Prior art / earlier review | `docs/reviews/rfi-vo-register-review.md`, `docs/Entity-Refactor-Request-VO-Valuation-Plan.md` |
