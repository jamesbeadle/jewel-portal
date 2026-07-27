# Role-Based Access Review — current state, target state, and the work between them

Status: **proposal for approval**. Nothing has been changed. Every claim below was read out of the
code on 2026-07-27; file paths and line numbers are given so you can check any of it.

Decisions already taken (from the brief):

- **Health & Safety Officer** — park it. No side nav, no functionality.
- **QS / Estimator** — leave exactly as-is this pass.
- **Finance Director** — give it the administrator's permissions, so there is a fully-privileged
  role that can be granted from the user directory rather than the hard-coded email list.
- **Deliverable** — this document first; implementation after sign-off.

---

## 1. How access is decided today

Three planes, and they do not agree with each other.

| Plane | Where it lives | What it does |
|---|---|---|
| **Side nav** | `jpms/Services/Navigation/DesktopNavigation.cs` + `SidebarFolders.cs` | Which folders and rows a role sees. Driven by `Session.ActiveRole`. |
| **Page & control gates** | 33 `.razor` files, ~44 inline checks | Which buttons and panels render. Driven by `Session.AvailableRoles` — **not** ActiveRole. |
| **API gates** | `api/Gates/` + ~90 per-feature `RoleSet`s across 351 endpoints | What data actually comes back. Driven by the caller's **full** role list. |

### 1.1 The single most important finding

> **"Viewing as" is cosmetic. It changes the sidebar and the dashboard and nothing else.**

`Session.ActiveRole` is stored in browser `localStorage` (`jpms/Services/ActiveRoleStorage.cs`) and
is **never sent to the API**. Verified: no `X-Role` header, no `DelegatingHandler`, no
`DefaultRequestHeaders` anywhere in `jpms/Program.cs:51-54`; zero occurrences of `activeRole` in
`api/`. The server rebuilds the caller from the session cookie and authorises against
`SignedInUser.Roles` — the complete list (`api/Gates/RoleSet.cs`, `api/Gates/SignedInUserResolver.cs`).

Client-side it is just as porous. `ActiveRole` drives only: the sidebar (`Layout/SideNav.razor:248-256`),
the dashboard branch (`Pages/Dashboard.razor:20`), the page heading, the chat launcher, the workspace
tab row, and four valuation controls. **Every other capability gate — 44 of them — reads
`Session.AvailableRoles` or `Auth.CurrentRoles`, i.e. the roles the user _holds_.**

The practical consequence, and it bears directly on how you planned to test this:

**Signing in as an administrator and switching role will not show you what that role sees.** You
will get that role's sidebar wrapped around an administrator's buttons and an administrator's data.
Concretely, an admin viewing as *Client* still gets: Delete request
(`Pages/ProjectRequestDetail.razor:907`), Return to triage (`:911`), Approve/reject variations
(`Pages/ProjectVariationDetail.razor:765`), Edit contract terms
(`Components/ProjectContractPanel.razor:182`), Retention schedule
(`Components/ProjectRetentionPanel.razor:186`), Raise/cancel valuation invoices
(`Components/ValuationInvoicesSection.razor:378`), the full triage queue, the full audit trail and
the full directory — all reachable by URL, because most pages have no page-level gate either.

**To test a role today you need a real second account holding only that role.** Fixing this
properly is item C1 in §5.

### 1.2 The second most important finding

> **Most pages have no role gate at all — only an "are you approved" check.**

12 of 57 pages in `jpms/Pages/` have a role gate. 35 are approval-only, 6 are unguarded redirect
stubs, 4 are public auth pages. There is no `AuthorizeRouteView` and no `[Authorize]` attribute
anywhere; `App.razor` uses a bare `<Router>`.

Reachable by typing the URL, by **any** signed-in approved user of **any** role: `/finance`,
`/finance/cash-summary`, `/finance/allocation`, `/finance/xero`, `/rate-library`,
`/rate-library/stale`, `/labour/workers`, `/projects`, `/sales-analytics`, `/nurture`,
`/estimating-queue`, `/rfis`, and every `/projects/{id}/*` tab including `/financials`,
`/valuation` and `/cashflow`.

In most cases the API then refuses the data, so the page renders empty or errors rather than
leaking figures. That is a backstop, not a design — it leaks page structure, labels, project IDs
and route shape, and it is one over-broad `RoleSet` away from leaking the numbers too. Which brings
us to:

### 1.3 The third finding: `AllInternal` is doing far too much work

`JpmsRoleSets.AllInternal` (`api/Gates/JpmsRoleSets.cs`) = MD, FD, PM, QS, Site Manager, H&S
Officer, Office & Compliance, **Foreman**, **Accounts**. It gates roughly 70 read endpoints,
including the entire commercial surface: `GetProjectFinancialSummary`, `ListValuationsForProject`,
`ListValuationLinesForProject`, `ListClaimLines`, `ListCostCodeBudgetsForProject`,
`ListCostCentreActualCosts`, `ListProjectCostOfSalesLines`, `ListWorkOrderInvoiceSummaries`,
reconciliation packages, all CVR queries, closeout settlement/VAT/retention, LADs, valuation
invoices, the leads pipeline, and project contracts.

So **Accounts, Foreman and the H&S Officer can all read every project's full financial position**,
by API call. The comment in `contracts/Models/Role.cs` claiming Accounts "carries none of the FD's
money-facing reach" is not true — Accounts is only excluded from `CommercialTeam` (cashflow, Xero,
the labour £ column), which is a small fraction of the money in the system.

---

## 2. Current state, role by role

Administrator bypasses every nav gate (`DesktopNavigation.CanSee`), so it is omitted from the nav
column below — it sees everything.

### 2.1 Side nav today

| Role | Folders / rows visible today |
|---|---|
| **Administrator** | Everything. |
| **Managing Director** | Client (Requests, Architect's Instructions, Valuation Report Snapshots) · Subcontractor (Bid Package Invites, Work Orders, WO Allocation) · Internal (Todo, Labour, Workers, Agent Activity, Directory) · Project (all 6) · Financials (all 7). **No Triage, no Audit Trail.** |
| **Finance Director** | Everything the MD has, **plus** Audit Trail and Triage. |
| **Project Manager** | As MD, minus Agent Activity and Cash Summary, **plus** Audit Trail and Triage. |
| **QS / Estimator** | Client (all 3) · Subcontractor (all 3) · Internal (Todo, Labour) · Project (all 6) · Financials (6 of 7, no Cash Summary). |
| **Site Manager** | Client (Requests, Architect's Instructions) · Subcontractor (all 3) · Internal (Todo, Labour) · Project (all 6). No Financials. |
| **H&S Officer** | Client (Requests) · Subcontractor (all 3) · Internal (Todo, Labour) · Project (all 6). |
| **Office & Compliance** | Identical to H&S Officer. |
| **Architect / Designer** | One row, flattened: **Architect's Instructions**. Nothing else. |
| **Client / Homeowner** | Nothing. |
| **Subcontractor** | Nothing — redirected to `/portal`. |
| **Foreman / Site Team** | Nothing. |
| **Site Operative** | Nothing. |
| **Accounts** | One row, flattened: **Todo**. |

### 2.2 Data permissions today — the headline gaps

| Role | What it can actually read | Verdict against your brief |
|---|---|---|
| **Administrator (master email)** | Everything. The 3 emails in `contracts/Models/JpmsAdministrators.cs` get `Enum.GetValues<Role>()`. | ✅ correct |
| **Administrator (assigned in the UI)** | **Broken.** A directory-assigned `Role.Admin` is *not* in `AllInternal`, `InternalAndArchitect`, `DrawingReaders` or `CommercialTeam`, so it is 403'd on ~70 read endpoints. It gets the admin sidebar and the admin dashboard, then most pages fail to load. | ❌ contradicts "an administrator gets all the roles by default" |
| **Managing Director** | Everything except: triage/mailbox, the unnarrowed audit register, pending access requests, directory user management, `DeleteRequest` (Admin-only). | ⚠️ triage + audit missing vs "full access except user management" |
| **Finance Director** | Everything the MD reads, **plus** triage, the full audit register, pending access requests, any directory user, and — via `AdminGate` — the ability to create users and assign any role including Admin. | ⚠️ FD currently outranks MD |
| **Project Manager** | Full internal reach except Cash Summary, Xero transactions, AI assistant, directory management. | ✅ close to target |
| **QS / Estimator** | Full internal + commercial + Xero transactions. | — leaving as-is |
| **Site Manager** | Full `AllInternal` read (incl. all financials) + requests + variations + drawings. Writes limited to site/H&S/progress/programme-update/defects/dayworks/to-dos. | ⚠️ reads too wide, writes about right |
| **H&S Officer** | Full `AllInternal` read — every project's financials. Writes: H&S records, mobilisation, request messages. | ❌ to be parked |
| **Office & Compliance** | Full `AllInternal` read — every project's financials. Writes: the whole procurement/bid-package lifecycle, subcontractor directory + compliance docs. | ⚠️ reads far wider than "internal todos + compliance" |
| **Architect / Designer** | **All requests, all RFIs, all variations, all drawings, all Architect's Instructions — on every project, with no scoping whatsoever.** Writes: raise requests, edit request details and forms, prepare and resend email drafts, attach files, create/update/delete Architect's Instructions, raise defects. | ❌ the largest gap vs "data security is important for this role" |
| **Client / Homeowner** | **Zero read permissions.** But it *can* `POST variation-orders/{id}/approve` and `/reject` on **any** variation on **any** project (`api/Features/Variations/VariationRoles.cs:16`), raise defects, and post request messages. | ❌ approve-without-read; unscoped |
| **Subcontractor** | Scoped correctly on the 8 `/portal/*` endpoints via `SubcontractorScope`. **Unscoped** on: all drawings for any project (`DrawingReaders`), `SubmitQuoteForBidPackage`, `ReviseQuote`, `RaiseRequest`, `PostRequestMessage`, `SubmitTimesheet`. | ⚠️ portal is right, the rest is not |
| **Foreman / Site Team** | Full `AllInternal` read — every project's financials. Writes: post request message, request attachments, own timesheet. | ❌ "not currently used" |
| **Site Operative** | `GetMyLabourDay` only. Writes: own sign-in/out/resubmit. | ✅ correctly minimal |
| **Accounts** | Full `AllInternal` read — every project's financials. Writes: to-dos. | ❌ vs "just internally assigned todos" |

### 2.3 Dashboard today

`Pages/Dashboard.razor:20` branches on `ActiveRole == Role.Admin` → `AdminHome`, else `RoleHome`.

| Role | Panels |
|---|---|
| **Administrator** | AdminStatsRow, My To-dos, Open Requests, Next Valuations, Pending Access Requests, Approved Users |
| MD | My To-dos, Open Requests, Next Valuations + RFI and valuation tiles |
| FD | My To-dos, Next Valuations + valuation tiles |
| PM | My To-dos, Open Requests + RFI tiles |
| QS | My To-dos, Open Requests, Next Valuations, Stale Rates + all tiles |
| Site Manager | My Day, My To-dos, Open Requests + RFI tiles |
| H&S Officer | My To-dos |
| Office & Compliance | My To-dos |
| **Architect** | Open Requests + RFI tiles — **and the panel is dead.** It reads `ListRfisAcrossProjects`, gated by `RfiDashboardRoles.AllowedToViewDashboard`, which excludes Architect. The 403 is swallowed by a `catch` and the panel renders its empty state. The tiles link to `/rfis`, which also 403s. |
| Client | Nothing but the greeting. |
| Subcontractor | n/a — `/portal` |
| Foreman, Site Operative | My Day |
| Accounts | My To-dos |

Two structural notes: `AdminHome.razor` performs **no role check of its own** — it is protected
solely by the `ActiveRole == Role.Admin` branch on the dashboard, and the same is true of
`PendingRequestsPanel`, `ApprovedUsersPanel`, `InviteUserForm` and `ApprovedUserRow`. And
`Role.Admin` **is selectable in the UI** in three places: `Components/InviteUserForm.razor:85`,
`Components/RoleAssignmentForm.razor:29` (used when approving an access request), and
`Components/ApprovedUserRow.razor:171-175`.

---

## 3. Proposed target

### 3.1 Side nav

Proposed role sets (replacing those in `DesktopNavigation.cs`):

```
AdminRoles       = { Admin, FinanceDirector }                    // user management rows
ProjectRoles     = { MD, FD, PM, QS, SiteManager, OfficeCompliance }   // H&S Officer removed
FinanceRoles     = { MD, FD, PM, QS }
TriageRoles      = { MD, FD, PM }                                // MD added
DirectorRoles    = { MD, FD }
DirectoryRoles   = { MD, FD, PM }
WorkerRegistry   = { MD, FD, PM }
TodoListRoles    = ProjectRoles + Accounts
ArchInstrRoles   = { MD, FD, PM, QS, SiteManager }                // Architect removed — see Q2
ClientReadRoles  = { Architect }                                  // new: read-only client section
Parked           = { HealthSafetyOfficer, Client, Foreman, SiteOperative }  // no rows at all
```

| Role | Target side nav |
|---|---|
| **Administrator** | Everything, plus a **Users** row (new — user management currently has no nav entry at all and lives only on the dashboard). |
| **Managing Director** | Everything except the Users row. Gains **Triage** and **Audit Trail**. |
| **Finance Director** | Identical to Administrator, including the Users row. |
| **Project Manager** | Unchanged from today, minus nothing. (Already correct: no Agent Activity, no Cash Summary, no Users.) |
| **QS / Estimator** | Unchanged. |
| **Site Manager** | Unchanged. |
| **Office & Compliance** | Internal (Todo) · Subcontractor (Bid Package Invites, Work Orders) · Project (Drawings, Communications). Loses Financials-adjacent and Labour rows it never used. |
| **Architect / Designer** | Client folder, flattened: **Requests** (read-only) and **Valuation Report Snapshots** (read-only). Loses Architect's Instructions unless you say otherwise (Q2). |
| **H&S Officer, Client, Foreman, Site Operative** | Nothing. |
| **Subcontractor** | Nothing — `/portal`. Unchanged for now; expands when the role goes live. |
| **Accounts** | **Todo** only. Unchanged. |

### 3.2 Data permissions

The structural change is to **split `AllInternal` into three tiers**, because one set gating both
"read the project list" and "read every valuation line" is what produced the Accounts / Foreman /
H&S over-reach:

| New set | Members | Gates |
|---|---|---|
| `InternalFloor` | MD, FD, PM, QS, SiteManager, OfficeCompliance, Accounts | Project list & detail, project contacts, directory reads, trades, to-dos |
| `ProjectDelivery` | MD, FD, PM, QS, SiteManager, OfficeCompliance | Requests, variations, drawings, programme, progress, site reports, H&S records, procurement, mobilisation |
| `CommercialRead` | MD, FD, PM, QS | **All money**: valuations, claims, claim lines, budgets, cost-of-sales, cost-centre actuals, financial summary, CVR, closeout, retention, LADs, valuation invoices, reconciliation packages, project contracts, leads |

Then per role:

| Role | Target data permissions |
|---|---|
| **Administrator** | Everything. **Fix required**: add `Role.Admin` to every shared set, or (better) grant every role to any user holding `Role.Admin`, not just the three hard-coded emails — see C2. |
| **Managing Director** | Everything except `AdminGate` writes (create/remove directory users, resolve access requests, invite, password reset). Add MD to `TriageRoles` and the audit register. |
| **Finance Director** | Everything, including `AdminGate`. Already true — the work is auditing the ~15 sets that name `Role.Admin` but not FD and adding FD to them. |
| **Project Manager** | Unchanged. |
| **QS / Estimator** | Unchanged. |
| **Site Manager** | Drop from `CommercialRead` (loses all valuation/claim/CVR/closeout/LAD reads). Keep `ProjectDelivery` + `InternalFloor`. Writes unchanged — they are already correctly narrower than PM's (no variations, no drawings management, no procurement, programme *update* but not add/remove/baseline). |
| **Office & Compliance** | Drop from `CommercialRead`. Keep `InternalFloor` + procurement/subcontractor-compliance writes. |
| **Architect / Designer** | **Read-only, and scoped.** Reads: requests + RFIs + variations + valuation-report snapshots **for projects where the architect's party is the project's `PartyId`**. Remove every architect write: `RaiseRequest`, `UpdateRequestDetails`, `UpdateRequestForm`, all three `PrepareRequestEmailDraft*`, `ResendRequestDocument`, request attachments, `ArchitectInstructionRoles.AllowedToManage`, `RaiseDefect`, `PostRequestMessage`. Remove from `DrawingReaders` unless Q3 says otherwise. |
| **Client / Homeowner** | No reads, and **remove `Role.Client` from `AllowedToApproveVariations`** — approving a record you cannot read is not a workflow, it is an unscoped write on every project in the system. Also remove from `RaiseDefect` and `PostRequestMessage` until the role is designed. |
| **Subcontractor** | Unchanged portal scoping. **Remove from `DrawingReaders`** (currently reads every drawing on every project) and add a bid-package-recipient check to `SubmitQuoteForBidPackage` / `ReviseQuote`, and a company check to `SubmitTimesheet`. |
| **Foreman / Site Team** | Remove from `AllInternal` entirely. Leave `LogOwnTime` only, like Site Operative. |
| **Site Operative** | Unchanged — already correct. |
| **Accounts** | `InternalFloor` only, so it can read its to-dos and resolve project names. Remove from every commercial read. |
| **H&S Officer** | Remove from every set. Leave the role in the enum (it persists as an integer — see the note in `Role.cs`) but grant it nothing. |

### 3.3 Dashboards

| Role | Target dashboard |
|---|---|
| **Administrator** | **AdminStatsRow · Pending Access Requests · Approved Users.** Remove `MyTodosPanel`, `OpenRequestsPanel` and `NextValuationsPanel` from `AdminHome.razor:20-22` — exactly as you asked. |
| **Managing Director** | My To-dos · Open Requests · Next Valuations + RFI/valuation tiles. Unchanged. |
| **Finance Director** | As MD, plus a **pending access requests** tile so user administration is visible from the role that now owns it. |
| **Project Manager** | Unchanged. |
| **QS / Estimator** | Unchanged. |
| **Site Manager** | Unchanged (My Day · My To-dos · Open Requests). |
| **Office & Compliance** | My To-dos. Unchanged. |
| **Architect / Designer** | A **scoped** read-only requests panel — the current one is dead (§2.3) and cross-project. Either add Architect to `RfiDashboardRoles` *with* project scoping, or replace the panel with a per-project register. Remove the RFI count tiles, which are portfolio-wide. |
| **Client / Homeowner** | Greeting only, no panels, no "Where to next" cards. Already the behaviour — confirm the empty state reads deliberately rather than looking broken. |
| **Subcontractor** | `/portal`. Unchanged. |
| **Foreman, Site Operative** | My Day. Unchanged. |
| **Accounts** | My To-dos. Unchanged. |
| **H&S Officer** | Greeting only, same as Client. |

---

## 4. Open questions

**Q1 — MD vs FD.** Your brief said MD and FD both get "full access except user management", then
you asked for the admin permissions to be copied into FD. As written above, **FD outranks MD** (FD
gets user management, MD does not). That is implementable and I have specced it that way, but it
is worth a moment's thought: an FD can then grant themselves or anyone else any role, including
Administrator. If the intent is "a fully-privileged role I can grant from the directory", a cleaner
answer is to fix directory-assigned `Role.Admin` (item C2) and grant *that* — leaving FD as a
finance role. Tell me which you want.

**Q2 — Architect's Instructions.** The Architect currently sees the AI register and can create,
edit and delete instructions. "Client section only, read-only" implies removing this. But an
architect filing their own AI rather than emailing it is genuinely useful, and the code comments
say that was deliberate. Keep it read-only, remove it, or leave the write?

**Q3 — Architect and drawings.** Architects are in `DrawingReaders` and can download any revision
of any drawing on any project. Drawings are usually *their* output. Do they keep drawing access
(scoped to their projects), or does "client section only" mean no drawings?

**Q4 — "read only snapshot of variation reports".** The only snapshot page in the app is
**Valuation Report Snapshots** (`/projects/{id}/valuation-snapshots`), point-in-time captures of
issued valuation reports. Variations live inside the Requests register. I have assumed you mean
the valuation-report snapshots. Confirm, or point me at the page you meant.

**Q5 — Site Manager write scope.** You said "write access will be different from the PM, they add
information downstream". Today the SM can already write: progress updates/reports/photos, H&S
records, mobilisation, site report assembly, programme task *update* (not add/remove/baseline),
defects, dayworks, timesheet submission, to-dos, and request messages/drafts. That reads like a
correct downstream-contributor set to me. Anything you want added or removed?

---

## 5. Work required, in dependency order

### Phase A — the security floor (do first; nothing else is meaningful without it)

- **A1.** Remove Foreman, Accounts and H&S Officer from the commercial read surface. Split
  `AllInternal` into `InternalFloor` / `ProjectDelivery` / `CommercialRead` and re-point ~70
  endpoints. *Largest single change; mechanical; well covered by the existing set structure.*
- **A2.** Remove `Role.Client` from `AllowedToApproveVariations`, `RaiseDefect` and
  `PostRequestMessage`.
- **A3.** Remove `Role.Subcontractor` from `DrawingReaders`; add recipient/company checks to
  `SubmitQuoteForBidPackage`, `ReviseQuote`, `SubmitTimesheet`.
- **A4.** Add a page-level role guard to the 35 unguarded pages. Cheapest correct form: a
  `<RoleGuard Roles="...">` wrapper component plus one line per page, or a route→roles table
  checked once in `MainLayout`.

### Phase B — the role model you asked for

- **B1.** Park the H&S Officer: remove from `ProjectRoles`, `AllInternal`, and every feature set.
- **B2.** Add MD to `TriageRoles` (nav + API) and to the audit register.
- **B3.** Add FD to every set that currently names `Role.Admin` without it (~15 sets), and add a
  **Users** nav row gated to `AdminRoles = { Admin, FD }`.
- **B4.** Architect: strip all writes; scope all reads. Requires a new `PartyId` column on
  `DirectoryUserEntity` (mirroring the existing `SubcontractorId`) and a `PartyScope` gate
  mirroring `api/Gates/SubcontractorScope.cs`, then a project-ownership check on each architect
  read. *This is the only item needing a migration.*
- **B5.** Narrow Office & Compliance to `InternalFloor` + its procurement/compliance writes.
- **B6.** `AdminHome.razor`: drop the three project panels. Add a role check inside `AdminHome`,
  `PendingRequestsPanel` and `ApprovedUsersPanel` so they do not depend solely on the dashboard
  branch.
- **B7.** Architect dashboard: replace or scope the dead requests panel; drop the portfolio tiles.

### Phase C — making it testable and true

- **C1.** Make "viewing as" authoritative. Send the active role to the API (a header set by a
  `DelegatingHandler`, validated server-side against the user's held roles), and gate on that one
  role rather than the full list. Then change the ~44 client-side gates from
  `Session.AvailableRoles` / `Auth.CurrentRoles` to `Session.ActiveRole`. *This is what makes your
  original plan — log in, switch role, check what you see — actually work. Without it, every role
  must be tested from a separate account.*
- **C2.** Fix directory-assigned `Role.Admin`: grant every role to any user holding `Role.Admin`,
  not just the three emails in `JpmsAdministrators.cs`. One line in `UserRoles.ForAsync` and
  `SignedInUserResolver.ResolveRolesAsync`, plus the client mirror in `EffectiveRoles.cs`. This is
  what delivers "an administrator gets all the roles in the role selection by default".
- **C3.** Update `docs/05-data-model/permissions-matrix.md` to match — it currently documents
  intent that the code does not implement (notably the Accounts commentary).

### What you can test today, before any of this lands

Roles that are already close enough to their target to be worth exercising **from a dedicated test
account, not by role-switching as admin**: Project Manager, QS/Estimator, Site Manager, Accounts.

Roles that will mislead you if you test them now: Architect (dead dashboard panel, unscoped data),
Client (no reads at all but live write endpoints), Foreman / H&S / Office & Compliance (full
financial read), and anything tested by switching roles as an administrator.
