# JPMS — Jewel Project Management System (Blazor WebAssembly PWA)

The production web app Jewel Enterprises and its clients will use day-to-day. This is **not** the scoping prototype — that lives in `/prototypes/journey-index/`. This folder is the start of the real product.

- **Tech:** Blazor WebAssembly · .NET 8 LTS · PWA · Tailwind (via CDN for now)
- **Hosts on:** Azure Static Web Apps (will move to App Service once an ASP.NET Core API is added)
- **Auth (current):** mocked Microsoft + Google sign-in with an in-memory directory + RBAC
- **Auth (target):** Microsoft Entra ID + Google Identity Services, with an admin-managed user directory in the JPMS backend

---

## What's built in this first cut

| Route | Page | Behaviour |
|---|---|---|
| `/` | `Pages/Login.razor` | Landing page. Two buttons: **Continue with Microsoft** and **Continue with Google**. The buttons currently open a mock prompt asking which email to sign in as. |
| `/dashboard` | `Pages/Dashboard.razor` | Role router. Looks the user up in the internal directory, then renders the right home view for their role. |

The `/dashboard` route resolves to one of three states:

| State | When | Renders |
|---|---|---|
| **Admin home** | Signed-in email is in the directory with `Role.Admin` | `Components/AdminHome.razor` — users panel, pending requests, stats |
| **Role-specific home** | Signed-in email is in the directory with any other role | `Components/PlaceholderHome.razor` (one placeholder per non-admin role until each journey is signed off) |
| **Request access** | Signed-in email isn't in the directory | `Components/RequestAccessView.razor` — submits a request to the admin queue |

### Sign-in + RBAC flow

```
Login screen ──► Pick provider ──► Enter email (mock) ──► /dashboard
                                                            │
                                                            ├─ in directory, Role.Admin   ──► AdminHome
                                                            ├─ in directory, other role   ──► PlaceholderHome
                                                            └─ not in directory           ──► RequestAccessView ──► admin queue
```

When real OAuth is wired up, the *"Enter email (mock)"* step disappears — the provider returns the email itself; everything downstream stays the same.

---

## RBAC

One role per user for now (`Models/Role.cs`). The current roles map directly to the scoping personas:

| Role | Persona | Notes |
|---|---|---|
| `Admin` | — | Manages users + platform configuration. The only role that sees `AdminHome`. |
| `ManagingDirector` | P05 | Top-level executive view (to be built). |
| `Accountant` | P04 | Cashflow forecast, cash calls (to be built first per scoping priorities). |
| `QuantitySurveyor` | P02 | Pricing + measurement workflows. |
| `Architect` | P01 | External client view. |
| `Subcontractor` | P03 | Mobile-first site workflows. |

The `Role` enum lives in `Models/Role.cs` together with display-name, code, accent-colour and `IsAdministrative()` helpers, so UI components never `switch` on the enum directly.

When SQL lands the model becomes many-to-many with no UI changes — `directoryUser.Role` becomes `directoryUser.Roles`, the access checks already go through helper methods.

### Seeded users (edit `Services/AllowListUserDirectory.cs`)

| Email | Role |
|---|---|
| `jamesbeadle1989@gmail.com` | Admin |
| `admin@jewelgroup.co.uk` | Admin |
| `nigel.reilly@jewelgroup.co.uk` | Managing Director |
| `accountant@jewelgroup.co.uk` | Accountant |
| `qs@jewelgroup.co.uk` | Quantity Surveyor |

The list is mutable in-memory — when an Admin approves a pending access request, the new user is added live and shows up in the **Users** panel without a reload.

---

## Run locally on Mac

### 1. Install the .NET 8 SDK

```bash
brew install --cask dotnet-sdk
```

Or download the macOS .NET 8 SDK from <https://dotnet.microsoft.com/en-us/download/dotnet/8.0> (Arm64 for Apple Silicon, x64 for Intel).

Verify:

```bash
dotnet --version
# expect 8.0.x
```

### 2. Restore + run

From the repo root:

```bash
cd jpms
dotnet restore
dotnet run
```

Then open the URL the console prints (typically `https://localhost:5001`). The PWA install button shows up in Chrome/Edge after a moment.

> **Hot reload:** use `dotnet watch` instead of `dotnet run` to auto-rebuild on file save.

### 3. Try the flows

- Sign in with **`jamesbeadle1989@gmail.com`** → lands on the admin homepage.
- Sign in with **`accountant@jewelgroup.co.uk`** → lands on the role placeholder home.
- Sign in with any other email (e.g. `someone@example.com`) → lands on **Request access**. Click **Request access**, sign out, sign back in as the admin and you'll see the request in the queue. Approve it with a role and the new user appears in the Users panel.

### 4. Build a production bundle

```bash
dotnet publish -c Release -o publish
```

The deployable static site is at `publish/wwwroot/`.

---

## Project layout

```
jpms/
├── Jewel.JPMS.csproj            Project file — targets net8.0
├── Program.cs                   App entry point. Registers Auth + directory + access-request services.
├── App.razor                    Top-level router
├── _Imports.razor               Razor using directives (every .razor sees these)
├── Layout/
│   └── MainLayout.razor         Header (with sign-in/out) + footer
├── Pages/
│   ├── Login.razor              Landing page — provider buttons
│   └── Dashboard.razor          Slim role router → AdminHome / PlaceholderHome / RequestAccessView
├── Components/
│   ├── ProviderButton.razor     Microsoft / Google sign-in button with its logo
│   ├── RequestAccessView.razor  Shown when a signed-in user isn't in the directory
│   ├── AdminHome.razor          Admin homepage — users panel, pending requests, stats
│   ├── PlaceholderHome.razor    Generic post-sign-in home for non-admin roles (until each is built)
│   ├── RoleBadge.razor          Small coloured role chip
│   └── Stat.razor               Small labelled stat card
├── Models/
│   ├── Role.cs                  Role enum + display / RBAC helpers
│   ├── User.cs                  AuthProvider, AuthenticatedUser, DirectoryUser records
│   └── AccessRequest.cs         Pending-request record
├── Services/
│   ├── AuthService.cs           Holds the current user + notifies subscribers on sign-in/out
│   ├── IUserDirectory.cs        Backend-shaped contract for the approved-user directory
│   ├── AllowListUserDirectory.cs   In-memory directory; seeded + mutable for the session
│   ├── IAccessRequestStore.cs   Queue contract for pending access requests
│   └── InMemoryAccessRequestStore.cs   Session-scoped queue
└── wwwroot/                     Everything served to the browser (HTML, CSS, icons, SW)
```

The style language matches the scoping prototype on purpose — same Tailwind palette (slate + accent dots), same rounded cards, same typography stack — so design learnings from the prototype carry over.

---

## Replacing the mocks

There are three seams to wire up when we go live:

### 1. Real OAuth in `AuthService.SignInAsync(...)`

- Add Microsoft Entra ID via `Microsoft.Authentication.WebAssembly.Msal` and register a SPA app in Entra.
- Add Google sign-in via Google Identity Services. Register the client ID in Google Cloud Console.
- Replace the body of `SignInAsync` so it consumes the OAuth callback rather than accepting any email.
- Configure both client IDs in `wwwroot/appsettings.json` (added at that time — not committed today).

### 2. Real directory in `IUserDirectory`

- Stand up an `/api/users` set of endpoints on the ASP.NET Core backend (get me, list all, upsert, remove).
- Add a `HttpUserDirectory : IUserDirectory` implementation that calls those endpoints.
- Swap the DI registration in `Program.cs` — every page already talks to the interface, nothing else changes.

### 3. Real access-request store in `IAccessRequestStore`

- Add `/api/access-requests` endpoints (submit, list pending, remove).
- Add an `HttpAccessRequestStore : IAccessRequestStore` implementation.
- Swap the DI registration — UI is already wired to the interface.

All three swaps are isolated so they can each land as a small focused PR.

---

## Deploy to Azure Static Web Apps

Identical to the prototype's flow — same Blazor preset, just point at `/jpms` instead of `/prototypes/journey-index`. Once the backend lands, this app moves behind a proper ASP.NET Core API and that deploy plan will get its own section here.

---

## Known limitations (today)

- **Mock sign-in.** Any email you type is accepted. Wire OAuth before any real users see this.
- **In-memory directory.** Lives in `Services/AllowListUserDirectory.cs`. Approvals during a session are real but vanish on refresh.
- **In-memory access requests.** Same story — pending queue resets on refresh.
- **One role per user.** Will become many-to-many once SQL lands.
- **No backend.** AdminHome's "what's next" tiles are placeholders.
- **Tailwind via CDN.** Convenient for dev, swap for the Tailwind CLI build before going live.
- **No persisted session.** Refreshing the browser signs the user out. A real OAuth token cache fixes this.
