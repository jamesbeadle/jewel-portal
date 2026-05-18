# Journey Index (Blazor WebAssembly PWA)

A walkthrough menu for on-site role-play sessions with stakeholders. As real user journeys are articulated in `/docs/user-journeys/`, each one will get a tappable card on this app's home page that opens a mobile-friendly demo.

- **Tech:** Blazor WebAssembly · .NET 8 LTS · PWA · Tailwind (via CDN, prototype-only)
- **Hosts on:** Azure Static Web Apps (free tier is enough)
- **Runs locally on:** Mac, Windows, Linux — anywhere the .NET SDK installs.

---

## Run locally on Mac

### 1. Install the .NET 8 SDK

The simplest way is Homebrew:

```bash
brew install --cask dotnet-sdk
```

Or download the installer from <https://dotnet.microsoft.com/en-us/download/dotnet/8.0> (pick the macOS .NET 8 SDK — Arm64 for Apple Silicon, x64 for Intel).

Verify:

```bash
dotnet --version
# expect 8.0.x
```

### 2. Restore + run

From this folder:

```bash
cd prototypes/journey-index
dotnet restore
dotnet run
```

The console will print a URL like `https://localhost:5001`. Open it in your browser. The PWA install button shows up in Chrome/Edge after a moment.

> **Hot reload:** use `dotnet watch` instead of `dotnet run` to auto-rebuild on file save.

### 3. Build a production bundle

```bash
dotnet publish -c Release -o publish
```

The deployable static site is at `publish/wwwroot/`. That's what you upload to Azure (or any static host).

---

## Deploy to Azure Static Web Apps

Azure Static Web Apps is the right product for this: free tier, global CDN, automatic GitHub Actions, custom domains, and HTTPS out of the box.

### Option A — Azure Portal + GitHub Actions (recommended)

1. **Provision the resource:**
   - Open <https://portal.azure.com> → **Create a resource** → search **Static Web App** → **Create**.
   - **Subscription / Resource group:** pick or create (e.g. `rg-jewel-scoping`).
   - **Name:** `jewel-journey-index` (becomes part of the URL).
   - **Plan type:** Free.
   - **Region:** West Europe (or nearest).
   - **Deployment source:** GitHub. Sign in and pick `jamesbeadle/jewel-enterprises`, branch `main`.
   - **Build details:**
     - **Build presets:** `Blazor`
     - **App location:** `/prototypes/journey-index`
     - **Api location:** _(leave empty)_
     - **Output location:** `wwwroot`
   - **Review + create.**
2. Azure commits a `.github/workflows/azure-static-web-apps-<id>.yml` file to your repo and triggers the first deploy. ~3 minutes later the URL appears on the resource's Overview page (e.g. `https://jewel-journey-index.azurestaticapps.net`).
3. **Custom domain (optional):** Static Web App → **Custom domains** → Add. Point a CNAME from your DNS to the Azure-generated hostname.

### Option B — Azure CLI

```bash
# install once: brew install azure-cli
az login

az group create --name rg-jewel-scoping --location westeurope

az staticwebapp create \
  --name jewel-journey-index \
  --resource-group rg-jewel-scoping \
  --source https://github.com/jamesbeadle/jewel-enterprises \
  --location westeurope \
  --branch main \
  --app-location "/prototypes/journey-index" \
  --output-location "wwwroot" \
  --login-with-github
```

This does the same thing as the portal flow but scripted.

### Option C — Manual upload (no GitHub Actions)

Useful for a one-off deploy or if you want to detach from GitHub later:

```bash
# install once: npm install -g @azure/static-web-apps-cli
dotnet publish -c Release -o publish
swa deploy ./publish/wwwroot --deployment-token <YOUR_TOKEN>
```

Get the deployment token from the Static Web App resource → **Manage deployment token**.

---

## What's in this project

```
prototypes/journey-index/
├── Jewel.JourneyIndex.csproj    Project file — targets net8.0
├── Program.cs                   App entry point (configures Blazor + HttpClient)
├── App.razor                    Top-level router
├── _Imports.razor               Razor using directives (every .razor sees these)
├── Layout/
│   └── MainLayout.razor         Header / footer wrapper for every page
├── Pages/
│   └── Home.razor               The welcome page (route "/")
└── wwwroot/                     Everything served to the browser
    ├── index.html               Shell HTML (loads Tailwind from CDN, then Blazor)
    ├── manifest.webmanifest     PWA manifest (name, icons, theme colour)
    ├── service-worker.js        Dev SW (no caching)
    ├── service-worker.published.js   Production SW (offline cache)
    ├── favicon.svg              Vector favicon (desktop browsers)
    ├── favicon-32.png           PNG favicon fallback
    ├── icon-192.png / .svg      PWA install icon
    ├── icon-512.png / .svg      PWA install icon (high-res)
    └── css/
        └── app.css              Minimal supplemental CSS
```

---

## Why Tailwind via CDN?

For a scoping prototype, the CDN script (`<script src="https://cdn.tailwindcss.com">`) is the path of least friction: no Node dependency, no build step, full Tailwind utilities available. **Before production**, switch to the Tailwind CLI (`npx tailwindcss -i ./input.css -o ./wwwroot/css/tailwind.css --watch`) so the CSS is purged to only what's used. The CDN warns about this in the console — that's fine for now.

---

## Adding a journey demo (later)

Once a journey is articulated in `/docs/user-journeys/`:

1. Create a new page component: `Pages/Journeys/{NN}-{slug}.razor` with a `@page "/journeys/{slug}"` route.
2. Render a step-by-step walkthrough using Tailwind layout + dummy data.
3. Link to the page from `Pages/Home.razor` (the journey card grid).
4. Keep state in memory (`@code { ... }`) — no API calls; this is a demo.

A reusable `JourneyStep.razor` component (in `Components/` once created) will make every journey page consistent.

---

## Known limitations

- **No auth yet.** Entra ID wiring comes in a later iteration.
- **No API.** All data is hard-coded; this is a demo, not a live app.
- **Maskable icons** are the placeholder "JE" mark — replace with proper Jewel branding before public release.
- **Tailwind CDN** prints a console warning in dev tools. Fine for prototype, swap for CLI build in production.
