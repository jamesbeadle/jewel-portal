# Prototypes

Interactive prototypes used during on-site role-play sessions.

## Projects

| Project | Location | Tech | Purpose |
|---|---|---|---|
| **Journey Index** | [`journey-index/`](journey-index/) | Blazor WASM PWA · .NET 8 LTS · Tailwind | Walkthrough menu for stakeholder sessions. Each articulated user journey becomes a tappable card with a mobile-friendly demo. |

## Conventions

- One folder per prototype project.
- Each project has its own `README.md` with **run locally** and **deploy** instructions.
- All data inside prototypes is dummy / hard-coded — no live integrations from prototypes. The point is to demonstrate user journeys, not to be a working backend.
- Prototypes are static / client-only where possible, so they can deploy to Azure Static Web Apps (free tier).

## /demos/

Scratch HTML mockups for one-off step illustrations live in [`demos/`](demos/). Use these for quick throwaways; the Blazor Journey Index is the durable home for journey demos.
