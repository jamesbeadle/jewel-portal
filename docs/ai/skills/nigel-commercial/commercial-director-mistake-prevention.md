---
name: commercial-director-mistake-prevention
description: "Codified failure modes and prevention rules for construction commercial work. Load once at the start of any Commercial Director session and re-consult whenever authoring a variation workbook, a client-facing letter, a rate build-up, a subcontractor comparison, an EOT notice, a disclaimer, or any external deliverable. Captures specific, real mistakes made in prior sessions across variation workbooks, letters, disclaimers, brand handling, contract citation, sub-quote reconciliation, and deliverable QA — and gives a concrete prevention rule for each. Must be read fully once by any agent taking on the commercial-director role; skimming is not sufficient because several rules are counter-intuitive."
license: MIT
metadata:
  author: nigel-reilly
  version: '1.0'
---

# Commercial Director — Mistake Prevention

Read this fully once per session. Every rule here comes from a real error made on a live project. The rule stops it happening again.

Each rule has: **Symptom** (how the mistake shows up), **Root cause**, **Prevention rule**.

---

## M1 — Double-counting subtotals in a variation workbook

**Symptom:** Total row is wildly larger than the sum of the line items. Client challenges the maths.

**Root cause:** A single `=SUM(...)` was written across a column that already contained subtotal rows inside its range, so subtotals plus line items were both counted.

**Prevention rule:** Never SUM a column that contains subtotal rows in the same range. Either:
- Sum only the line-item rows (skip subtotals), or
- Sum only the subtotal rows (skip line items),
- Or use `=SUMIFS(...)` with a filter column marking line-item rows.
Always cross-check the total by inspection before saving. In `xlsx_repl`, verify by re-reading the totals cell after recalc.

---

## M2 — Subcontractor product-code vs drawing-spec confusion

**Symptom:** Rate analysis is comparing apples to oranges — the sub quoted product X, the spec calls for product Y, and the rate build-up treated them as equivalent.

**Root cause:** The subcontractor uses their internal product codes; the drawing/spec uses the manufacturer's spec codes. Without reconciliation, one gets treated as the other.

**Prevention rule:** Before any rate build-up:
1. Extract every product code the sub quoted.
2. Extract every product code the drawing/spec calls for.
3. Build a reconciliation table: sub code → sub product name → spec code → spec product name → match/mismatch.
4. Any mismatch is either (a) an RFI to the sub, (b) an RFI to the architect, or (c) a live risk logged in the variation. Do not paper over.

---

## M3 — SharePoint accessed via browser_task

**Symptom:** Browser task fails on SharePoint auth, or returns partial content, or times out.

**Root cause:** SharePoint sites (including private company tenants) sit behind M365 SSO and are not reliable via `browser_task`.

**Prevention rule:** For SharePoint content, use the `search_files_v2` connector (or equivalent files connector for the current environment). Never `browser_task` a SharePoint URL. If `search_files_v2` returns nothing useful, ask the user to download the file locally and re-attach.

---

## M4 — Using an umbrella/group name that the business has banned

**Symptom:** External deliverable references "X Group" when the business rule says always use the parent entity name (e.g. "X Enterprises").

**Root cause:** Agent defaulted to a natural-sounding phrase without checking the Business Profile.

**Prevention rule:** Read the Business Profile's "Never-use terms" and "Always-use terms" list before drafting. Search-and-replace the drafted deliverable for any banned term before sharing.

---

## M5 — Font download failures producing an unstyled PDF

**Symptom:** PDF renders in default Helvetica or Times because the intended font (e.g. Switzer, Inter, DM Sans) failed to download at runtime.

**Root cause:** Fetching TTFs from Google Fonts / Fontshare at runtime relies on network; when it fails silently the fallback is unstyled.

**Prevention rule:**
- Prefer system-installed fonts (Liberation Sans at `/usr/share/fonts/truetype/liberation/`) for any PDF that must ship reliably.
- If a branded font is required, verify the file exists on disk before registering it with the PDF library. Fail loudly if it doesn't.
- Always `registerFontFamily` in reportlab when using a custom family, otherwise `<b>` and `<i>` tags render as default weight.

---

## M6 — Bold not rendering in reportlab-generated PDF

**Symptom:** Text tagged `<b>` renders regular-weight in the output PDF.

**Root cause:** Reportlab needs `registerFontFamily` to map the bold TTF file to the family's bold slot. Registering only the regular face isn't enough.

**Prevention rule:** After `pdfmetrics.registerFont(TTFont('MyFont', ...))` and the bold variant, always call:
```python
from reportlab.pdfbase.pdfmetrics import registerFontFamily
registerFontFamily('MyFont', normal='MyFont', bold='MyFont-Bold', italic='MyFont-Italic', boldItalic='MyFont-BoldItalic')
```
Then verify by rendering a page with a `<b>` tag and inspecting the image.

---

## M7 — Quoting a JCT clause that doesn't exist in the executed form

**Symptom:** Correspondence cites, say, clause 4.20 for L&E — but the project is on MW 2016, where 4.20 doesn't exist.

**Root cause:** Agent used a clause number from memory or from a different JCT form without checking.

**Prevention rule:** Before citing any clause number:
1. Open the Project Profile's "Contract-specific clause references (verified)" table.
2. If the clause you need isn't there, either verify against the executed contract PDF, or use a descriptive reference ("under the variation provisions of the Contract") instead of a specific number.
3. Never guess. A wrong clause destroys the letter's authority.

---

## M8 — Sending a client-facing variation that leaks internal cost data

**Symptom:** The workbook shared with the CA shows subcontractor line-item rates or procurement quotes.

**Root cause:** Client-facing workbook was built as a copy of the internal cost sheet without stripping cost columns.

**Prevention rule:**
- Maintain two workbooks per variation: `V##-Internal-Cost.xlsx` (never shared) and `V##-Client-Facing.xlsx` (shared).
- The client-facing file shows only contract-basis rates and OH&P — never sub rates or procurement prices.
- Before sharing, run `deliverable-qa-preflight` which includes a "no sub-cost leak" check.

---

## M9 — Missing time allowance in a variation

**Symptom:** VO priced with materials + labour + OH&P only. No procurement or mobilisation time. Programme quietly slips, no EOT trigger recorded.

**Root cause:** Time was treated as free.

**Prevention rule:** Every VO carries an explicit time allowance = procurement lead time + mobilisation + works duration. Even fast VOs get at least a mobilisation entry. If the works are truly instant (e.g. a schedule tweak), state "no programme impact" explicitly. Silent is not acceptable.

---

## M10 — Missing mandatory VO inclusions

**Symptom:** VO priced without plant/hire, without protection to adjacent works, without muck-away/disposal — client accepts, then contractor absorbs those costs.

**Root cause:** Trade-specific pricing sheet was used without adding the standard inclusions the business always carries.

**Prevention rule:** Every VO has a mandatory inclusions checklist:
- Plant / hire (scaffold, MEWP, hoists as applicable)
- Protection to adjacent works
- Muck-away / disposal / skip
- Welfare (where variation genuinely adds welfare cost)
- Preliminaries impact (where variation extends the programme)
Tick each; if genuinely N/A, mark N/A explicitly. Never silent.

---

## M11 — In-sequence rates applied to out-of-sequence work

**Symptom:** Variation priced at in-sequence rates, but the work happens as a return-visit, standing time, or split visit — real cost significantly exceeds the priced rate.

**Root cause:** Rate basis wasn't gated at authoring.

**Prevention rule:** Every VO opens with a rate-basis gate:
- Is this work within the main works window and sequence? → in-sequence rates.
- Is this work displaced (return visit, split visit, standing time, out of build-up sequence)? → out-of-sequence rates, with explicit mobilisation, standing time, and split-visit uplift lines.
Document the gate decision in the workbook's assumptions tab.

---

## M12 — Auto-loading drafts into Outlook

**Symptom:** Draft reply lands in the send queue before the user has reviewed it.

**Root cause:** A pipeline was built that ended in `outlook.send` without a human gate.

**Prevention rule:** Never build a pipe that sends. Every draft is written to a file (or displayed) and handed to the user. If a workflow ends in "send", pause and hand off. This is doctrine D7 in the master skill — treat it as absolute.

---

## M13 — Reply that opens the door to the escalation we don't want

**Symptom:** Our reply, in defending one point, gives the CA a foothold to attack a different, weaker position.

**Root cause:** Wrote defensively across too many fronts in one reply.

**Prevention rule:** Focus every reply on the narrowest position that defeats the current challenge. Do not answer questions that were not asked. Do not concede or hint at positions on adjacent issues. Keep those positions in the Reserve Register for their own future letter if needed. This is doctrine D1.

---

## M14 — Trusting file previews from openpyxl

**Symptom:** Workbook looks correct in `xlsx_repl` but broken in Excel — merged-cell content lost, row heights clipped, print area wrong.

**Root cause:** openpyxl does not render — it just holds structure. What Excel/LibreOffice actually draws can differ.

**Prevention rule:** Before sharing any `.xlsx`, run the full `deliverable-qa-preflight` xlsx checklist: recalc, PDF-render each tab, visually inspect for merged-cell content loss and clipped narrative rows. See the `presentation-qa` user skill for the exhaustive list.

---

## M15 — Losing the reasoning behind a sent letter

**Symptom:** Three months later, we cannot remember why we conceded a specific point, or what evidence we had held back.

**Root cause:** Reserve Register entry was skipped in the rush.

**Prevention rule:** Every external commercial reply is paired with a Reserve Register entry stored in the project working folder. Non-negotiable. If the entry is missing, the letter is not "sent" — it goes back to draft. See `commercial-director/references/reserve-register-template.md`.

---

## M16 — Confusing RFI, AI, CI, and Variation Instruction

**Symptom:** Treating an RFI response as an instruction, or treating an architect's email as a Contract Administrator's Instruction.

**Root cause:** Under-informed classification of incoming documents.

**Prevention rule:**
- **RFI:** contractor's request for information. It is not an instruction until the response formally instructs a change.
- **CA Instruction (AI on some forms):** issued by the Contract Administrator, in writing, under the contract clause governing instructions. Only these are instructions.
- **CI (Change Instruction):** commonly used on D&B — check contract for the exact term.
- **Architect's email:** may or may not be an instruction. If it changes scope, cost, or time, request formal confirmation as a CA Instruction before proceeding.
Never proceed with abortive or additional works on an informal email alone. If pressed, issue a written notice of the risk and require formal instruction.

---

## M17 — Freelancing a variation without loading the variation-authoring skill

**Symptom:** VO workbook is missing time, missing inclusions, missing rate-basis gate, or is on internal cost basis.

**Root cause:** Agent tried to author from memory instead of loading the specialist.

**Prevention rule:** When the task is a variation, tender, bid pack, or quote, always load `variation-authoring` (and `jewel-variation-agent` if working on a Jewel entity). Never freelance.

---

## M18 — Treating the CA's valuation as final

**Symptom:** CA under-values a variation and the agent accepts, or the agent responds inside the CA's own valuation framework instead of asserting the contractor's.

**Root cause:** The CA's number was treated as the anchor.

**Prevention rule:** The contractor's priced VO is the anchor. Any CA challenge is treated as a challenge to that anchor. Respond by defending measurement, rate basis, and entitlement — not by negotiating down from their number. If the CA is also acting as Employer's QS, doctrine D5 applies: log the conflict, and expose it if they overreach.

---

## When you encounter a new mistake

Add a new M-entry to this skill. Include Symptom, Root cause, Prevention rule. Bump the skill version. This skill grows with the operator's experience.
