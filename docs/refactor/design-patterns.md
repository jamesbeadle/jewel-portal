# The System's Design Patterns

What surfaced when the ten largest files were broken into their component parts. Per the coding
rules, none of these was imposed from a catalogue — each is a shape the code was already reaching
for, now named because it has formed. Dated 2026-09-01, from the `refactor/prose-standard` branch.

## 1. Page = markup + concern partials

Every large page divided the same way, which makes it the codebase's page anatomy:

```
ProjectRequestDetail.razor          the markup — what the page looks like
ProjectRequestDetail.razor.cs       the core partial — state, loading, panes
ProjectRequestDetail.Menus.cs       one nameable concern each …
ProjectRequestDetail.DraftVariation.cs
ProjectRequestDetail.HeaderFactsEdit.cs
ProjectRequestDetail.DetailEdit.cs
ProjectRequestDetail.OfficialDocument.cs
ProjectRequestDetail.EmailDraft.cs
```

The rule that emerged: **a partial file is a concern, and its filename is the concern's name.**
A reader looking for "how does the invite composer work" opens
`ProjectBidPackageInviteDetail.InviteComposer.cs` and reads nothing else. The one exception is a
member that renders markup (a `RenderFragment`): it stays in the `.razor` file's `@code` block,
because Razor syntax only compiles there — TriageQueue's `PathwayPaneFragment`, CashForecast's
`CategoryRow` and `BalanceChart`, LabourOverview's six widget fragments.

The same anatomy applied to the backend's one giant: `XeroClient` became one file per type
(`IXeroClient`, `NullXeroClient`, `XeroCallFailedException`) and one partial per API area
(`Reads`, `Writes`, `SitePnl`, `Attachments`, `LineItems`, `Http`).

## 2. The gated page (a decorator around every page)

Every page opened with the same three-branch scaffold: session loading → not signed in → not
approved → the page. That is a decorator, and it is now the `ApprovedSessionGate` component:

```razor
<ApprovedSessionGate>
    … the page, written as if the user is signed in and approved …
</ApprovedSessionGate>
```

24 pages adopted it outright; pages whose gates vary (extra role checks, custom loading) kept
their own and can adopt it when their variance is understood. This is the UI twin of the backend
rule that every endpoint passes authentication → authorisation → validation before domain logic.

## 3. The widget: parameters in, events out

The triage breakdown surfaced the component lifecycle the playbook's Stage 3 describes, and the
extracted widgets follow it:

- `TriageEmailRow` — one email in any triage list; the queue, discarded and tagged lists render
  the same row with flags for the bits that differ (draft marker, thread tags, filing chips).
- `EmailListPager` — cursor paging: Previous / "Page x of y · n emails" / Next.
- `TriageMessageDetail` — the open email (header, attachments, body, sent replies, thread tabs),
  rendered identically by the email pane and its read-only mirror; an `AfterHeader` slot lets the
  queue keep its tagged-chips row without the component knowing about it.
- `NoticePanel` — the dismissible outcome notice (positive / warning) that four inline copies
  used to hand-roll.

Every widget takes typed parameters named for the domain, raises named `EventCallback`s, and
never reaches back into its page. When a widget needs a caller-specific patch of markup, it
exposes a named `RenderFragment` slot rather than growing flags.

## 4. Display modules: how a thing reads is one decision

Formatting was the most-duplicated code in the repository — the same helper pasted into every
page that needed it. Each is now a module whose name says what kind of reading it governs:

| Module | Concept | Copies removed |
| --- | --- | --- |
| `MoneyFormats` | money reads as GB pounds (`Money`, `WholeMoney`) | 37 |
| `FileSizeFormat` | file sizes read for humans ("3.2 MB") | 14 |
| `TriageEmailDisplay` | senders, list dates, previews, tag labels | ~8 per-page sets |
| `TriagePathways` | the pathway enum, its buckets, labels, chip colours | page-private set |
| `JewelDocumentStyle` | the house PDF style: palette, fonts, headings, footers | 6 renderer sets |

`_Imports.razor` carries one `@using static` for each, so a page writes `@Money(total)` — the
call site reads as a sentence and the format decision lives in exactly one place. Members whose
local shape genuinely differed (a renderer's tighter cell padding, a page's whole-pound money)
stayed local: same name, different concept, per the DRY-with-care rule.

## 5. Patterns already in the codebase, confirmed and strengthened

The breakdown made these existing shapes more visible; they are the house patterns to keep:

- **CQRS with gates** — commands/queries as named intentions (`SetXeroAllocation`,
  `SaveReconciliationPackage`), endpoints gating auth → authorisation → validation first.
- **Snapshot store + stale-while-revalidate** — read models (`Payables`, `Projects`,
  `CostCenters`) cache a snapshot, raise `OnChange`, and pages `RefreshAsync()` in the
  background on entry. This is the app's one loading idiom; `LoadGate` is its render half.
- **Null object** — `NullXeroClient` serves "not configured" states instead of throwing, so the
  UI explains rather than 500s.
- **Config-fed pane** — TriageQueue's four pathway panes are one `PathwayPane` component fed
  four `PathwayPaneConfig`s: a strategy expressed as data, and the model for any "N similar
  panes" surface.
- **Shared editor over shared state** — XeroAllocation's `SplitEditorCore` renders the same
  split editor inside two hosts (standalone modal, invoice viewer) over one state object.

## 6. What the next extractions should be (the shapes are already visible)

- **Twin pages**: `AgedPayables` ↔ `AgedReceivables`, `AdminUsers` ↔ `AdminRevokedUsers`,
  `ProjectDefects` ↔ `ProjectInventory` still clone ~100 lines each — each pair wants one
  parameterised component the way the aged pair already shares `AgedPayablesMaths`.
- **The sortable column header**: ProfitSummary's `SortHeader` fragment is the widget every
  sortable table re-invents.
- **The upload endpoint**: `UploadMyComplianceDocumentEndpoint` ↔
  `UploadComplianceDocumentFileEndpoint` share their multipart/validation scaffold — an upload
  gate runner wants to exist.
- **The blob store**: `ArchitectInstructionBlobStore` ↔ `BidPackageAttachmentStore` (and
  siblings) repeat the Azure-blob CRUD shell — one generic store, per-feature naming on top.
- **Over-long function names flagged by the new audit check** (45): each is a missing type —
  `CommercialIdentifierFactory.NextCostCentreGroupMemberId` and family want to become
  `identifiers.CostCentreGroupMembers.Next()`-style objects.

## The engine, restated

Shrink first (division of two, repeated), abstract second (only the repeats that survived
shrinking), let patterns arrive third (this document). Nothing here was designed up front; all
of it was recognised in code that had become small enough to see.
