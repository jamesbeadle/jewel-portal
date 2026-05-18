# Requirements

Cross-cutting requirements that are not specific to a single journey.

> 📁 **`_templates/`** holds reference-only scaffolding (persona card template, example permission matrix). Real requirements live in this folder root.

---

## Files

- [`personas.md`](personas.md) — Persona cards for every confirmed user role. Five drafted from the 2026-05-18 domain discovery (P01 Architect, P02 QS, P03 Subcontractor, P04 Accountant, P05 MD). Each carries a `Sourced from:` link to the meeting that captured it.
- [`glossary.md`](glossary.md) — Construction and Jewel-Enterprises-specific terms (Tender, RFI, VO, Cash Call, Cost Code, etc.).
- _(to be created)_ `permission-matrix.md` — Role × Feature matrix. Shape reference in [`_templates/permission-matrix-example.md`](_templates/permission-matrix-example.md).
- _(to be created)_ `non-functional.md` — performance, security, reporting, offline behaviour, audit, retention.
- _(to be created)_ `integrations.md` — Microsoft 365, Teams, Graph, Power BI, accounting system.

A requirement is "Confirmed" once it has been referenced from at least one journey and signed off by the relevant actor in the role-play session for that journey.

---

## Process for adding a persona

1. Hold the persona conversation with someone in that role (or shadow them).
2. Capture the conversation in a meeting note under `/docs/meetings/`.
3. Add or update the relevant card in `personas.md` using the structure from `_templates/personas-template.md`. Status starts as **Draft** and becomes **Confirmed** only when the person reviews their own card.
4. Update the personas table in the root [`README.md`](../../README.md#5-personas).
