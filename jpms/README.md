# JPMS — Jewel Project Management System (Blazor WebAssembly PWA)

The production web app for Jewel Bespoke Build and its clients. Blazor WebAssembly + Tailwind, hosted on Azure Static Web Apps with database-backed email/password authentication and Azure SQL Serverless for persistence.

## Stack

| Layer | Tech | Where it runs |
|---|---|---|
| Front-end | Blazor WebAssembly · .NET 8 LTS · PWA | Azure Static Web Apps (Free tier, West Europe) |
| Styling | Tailwind v3 CLI build | compiled at `dotnet publish` time |
| Authentication | Database-backed email/password (sessions + invite tokens) via the `api` Functions app | Static Web Apps managed Functions |
| Database | Azure SQL Database GP_S_Gen5 Serverless, Free-Limit if available | UK South |

## What's built today

The whole site map's Phase-1 internal scope is shipped as a navigable shell with seeded in-memory data. SQL persistence replaces the in-memory stores in the next layer; nothing in the UI changes.

### Top-level navigation

| Route | Workflow | What it does |
|---|---|---|
| `/` | — | Email/password sign-in landing |
| `/dashboard` | — | Role-aware home (Admin / placeholder / request-access) |
| `/leads` + `/leads/{id}/{tab}` | 00 CRM | Lead pipeline with seven tabs (overview / qualification / site-visits / info-chase / bid-decision / proposal / outcome) |
| `/leads/new` | 00 CRM | Capture-lead form |
| `/estimating-queue` | 00 CRM | QS prioritised queue |
| `/nurture` | 00 CRM | Lost leads kept warm |
| `/sales-analytics` | 00 CRM | Source attribution + win rate |
| `/projects` + `/projects/{id}` | All | Portfolio + project hub with tabbed sub-areas |
| `/projects/{id}/drawings` (+`/{drawingId}`) | 01 Drawings | Drawing register, upload, revision history |
| `/projects/{id}/boq` | 02 BoQ | Line items, direct add, totals |
| `/projects/{id}/procurement` | 03 Procurement | Bid packages + work orders awarded |
| `/projects/{id}/mobilisation` | 04 Mobilisation | Site mobilisation checklist (hard gate) |
| `/projects/{id}/changes` (+`/{kind}`) | 05 Changes | Unified RFI/submittal/variation/NoD register |
| `/projects/{id}/site` | 06 Site | Programme + site reports |
| `/projects/{id}/commercial` | 07 Commercial | CVR snapshot + PVR valuations + cost-code budgets |
| `/projects/{id}/closeout` | 08 Close-out | Defects + settlement + VAT analysis + retention |
| `/subcontractors` + `/subcontractors/{id}` | 03 Procurement | Master directory + per-sub compliance docs |
| `/work-orders` | 03 Procurement | Portfolio-wide WO list (FD view) |
| `/hs` | 04 H&S | Cross-project H&S register (observations, incidents, corrective actions, toolbox talks, permits) |
| `/cashflow` | 07 Cashflow | FD cashflow snapshot (13-week net) |
| `/portfolio` | 09 Portfolio | Director / FD cross-project view (margin, exceptions) |
| `/rate-library` + `/rate-library/stale` | 02 Rates | Rate library and stale-rate queue |

### Auth + admin

| Route | Page | Behaviour |
|---|---|---|
| `/` · `/login` | `Login.razor` | Email + password form; posts to `/api/auth/login`, then navigates to `/dashboard` |
| `/set-password` | `SetPassword.razor` | Validates an invite token (`/api/auth/invite/{token}`) and sets the password via `/api/auth/set-password` |
| `/logout` | `Logout.razor` | Calls `/api/auth/logout`, clears the session, redirects to `/` |

Admins invite users from the directory panel; the invitee receives a single-use link to `/set-password?token=…`. The dashboard resolves to one of three states:

| State | When | Renders |
|---|---|---|
| Admin home | Signed-in email is in the directory with `Role.Admin` | `AdminHome` (stats row + pending requests + users + what's-next) |
| Role home | Signed-in email is in the directory with any other role | `RoleHome` (stats row + navigation tiles for the role's sections) |
| Request access | Signed-in email isn't in the directory | `RequestAccessView` — submits a request into the admin queue |

## Authentication flow

```
/ ──► email + password ──► POST /api/auth/login ──► verify hash + open session
                                                          │
/dashboard ◄── session cookie set ◄───────────────────────┘
       │
       ├─ GET /api/auth/me  (email + display name + roles, resolved server-side)
       ├─ AuthService.CurrentUser = email + roles
       │     ├─ Admin    ──► AdminHome
       │     ├─ Other    ──► RoleHome
       │     └─ Missing  ──► RequestAccessView

Invite:  admin invites email ──► POST /api/auth/invite ──► single-use link
         ──► /set-password?token=… ──► POST /api/auth/set-password
```

Sessions are HTTP-only secure cookies; passwords are stored as PBKDF2-SHA256 hashes and invite tokens only as their SHA-256 hashes. When the user clicks **Sign out**, the app navigates to `/logout`, which calls `/api/auth/logout`.

## Run locally on macOS

```bash
brew install --cask dotnet-sdk node
cd jpms
npm install
dotnet restore
dotnet run
```

Open the URL printed in the console. The auth endpoints live in the `api` Functions app; to exercise the full sign-in flow locally, run the API alongside the front-end (e.g. with the SWA CLI):

```bash
npm install -g @azure/static-web-apps-cli
swa start http://localhost:5000 --run "dotnet run --project jpms" --api-location api
```

Hot reload:

```bash
dotnet watch --project jpms
```

CSS-only watch:

```bash
cd jpms && npm run watch:css
```

## Deploy to Azure

Two steps. After step 1 you can re-deploy as often as you like with no further setup.

### 1. Provision the Azure resources (one-off)

```bash
az login
az account set --subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec
../infra/azure-setup.sh
```

The script creates the resource group, SQL Server + Serverless DB, Static Web App, and Entra ID app registration; it writes connection details to `infra/.azure-output.env` (gitignored). Copy `SWA_DEPLOYMENT_TOKEN` from that file into a GitHub repository secret named `AZURE_STATIC_WEB_APPS_API_TOKEN`.

### 2. Push to `main`

The workflow in `.github/workflows/jpms-swa.yml` installs Node + .NET, runs `npm install` and `dotnet publish` (which triggers the Tailwind build), then deploys `publish/wwwroot` to Static Web Apps.

## Project layout

```
jpms/
├── Jewel.JPMS.csproj              Blazor WASM project + Tailwind MSBuild target
├── Program.cs                     App entry point — registers HttpClient + RBAC services
├── App.razor                      Top-level router
├── _Imports.razor                 Razor using directives
├── staticwebapp.config.json       SWA platform + navigation fallback config
├── package.json                   Tailwind CSS toolchain
├── tailwind.config.js             Tailwind theme + content paths
├── Styles/
│   └── app.tailwind.css           Tailwind source — compiled to wwwroot/css/app.built.css
├── Layout/
│   ├── MainLayout.razor           Top bar + side nav shell + footer
│   ├── SideNav.razor              Vertical nav (persistent desktop, drawer on mobile)
│   ├── PortalLayout.razor         External portal shell (subcontractor/architect/client)
│   └── SiteLayout.razor           Mobile site-PWA shell
├── Pages/
│   ├── Login.razor                Landing page — email/password sign-in
│   ├── SetPassword.razor          Invite-token password setup (/set-password)
│   ├── Logout.razor               Ends the session and redirects to /
│   ├── Dashboard.razor            Role router
│   ├── Projects.razor             /projects — portfolio list
│   └── ProjectDetail.razor        /projects/{id} — project detail hub
├── Components/
│   ├── AdminHome.razor            Admin homepage composition
│   ├── AdminStatsRow.razor        Stats grid (approved / pending / roles)
│   ├── PendingRequestsPanel.razor List of pending access requests
│   ├── PendingRequestRow.razor    One request — approve/decline/role-pick
│   ├── RoleAssignmentForm.razor   Role dropdown + confirm/cancel
│   ├── ApprovedUsersPanel.razor   List of approved directory users
│   ├── ApprovedUserRow.razor      One approved user — revoke
│   ├── WhatsNextPanel.razor       Next-up backlog tiles
│   ├── RoleHome.razor             Non-admin role landing page with section tiles
│   ├── ProjectsTable.razor        Project list table with row navigation
│   ├── ProjectDetailView.razor    Project detail page body
│   ├── ProjectStageBadge.razor    Coloured project stage chip
│   ├── RoleSwitcher.razor         Header role dropdown for multi-role users
│   ├── InviteUserForm.razor       Admin invite form (email + display name + roles)
│   ├── RequestAccessView.razor    For signed-in users not yet on the directory
│   ├── RoleBadge.razor            Small coloured role chip
│   ├── Stat.razor                 Small labelled stat card
│   └── Icons/
│       ├── JewelIcon.razor
│       └── NavIcon.razor
├── Models/
│   ├── Role.cs                    Role enum + display / RBAC helpers
│   ├── User.cs                    AuthProvider, AuthenticatedUser, DirectoryUser records
│   ├── AccessRequest.cs           Pending-request record
│   ├── JpmsAdministrators.cs      Hardcoded admin email allowlist
│   ├── Project.cs                 Project record
│   ├── ProjectStage.cs            Project lifecycle stage enum + display
│   └── Organisation.cs            Jewel entity enum (JBB / JPS / JPF)
├── Services/
│   ├── AuthService.cs             Calls /api/auth/* (login, me, logout, set-password, invite); exposes CurrentUser + roles
│   ├── UserInviteService.cs       Admin-side invite call (POST /api/auth/invite)
│   ├── SessionService.cs          Holds active role for the signed-in user
│   ├── EffectiveRoles.cs          Merges hardcoded admin + directory roles
│   ├── IUserDirectory.cs          Directory contract
│   ├── AllowListUserDirectory.cs  In-memory directory (mutable for the session)
│   ├── IAccessRequestStore.cs     Access-request queue contract
│   ├── InMemoryAccessRequestStore.cs   Session-scoped queue
│   ├── IProjectStore.cs           Project store contract
│   └── InMemoryProjectStore.cs    Seeded in-memory project store
└── wwwroot/                       Everything served to the browser
    └── staticwebapp.config.json   SWA platform + navigation fallback config
```

## Code style

This codebase follows the style rules in the repository-root `CLAUDE.md`:

- Hard limit of 100 lines per file.
- No comments that explain *what* the code does — the names carry it.
- Booleans use `is`/`has`/`should`/`can` prefixes.
- No abbreviations in identifiers.
- Magic colours and strings live in Tailwind tokens or named constants.

When adding new code, run mentally through CLAUDE.md's pre-commit checklist before merging.

## What lands next

1. **Azure SQL persistence.** Replace `AllowListUserDirectory` and `InMemoryAccessRequestStore` with implementations that call a new API (running on Static Web Apps managed Functions).
2. **First domain screen.** The Programme Valuation Report is the highest-value module — that's the first screen wired to real entities.

Source of truth for what to build next is the user-story register in `/docs/03-workflows/`.

## Cost expectation (testing)

| Resource | Idle | Active |
|---|---|---|
| Static Web App (Free tier) | £0 | £0 up to 250GB/month bandwidth |
| Azure SQL Serverless (with Free-Limit) | £0 (paused) | £0 up to 100k vCore-sec + 32GB/month |

If the Free-Limit flag isn't accepted on the subscription, the SQL falls back to standard Serverless: ~£3-5/month idle (storage only), pennies per active hour.
