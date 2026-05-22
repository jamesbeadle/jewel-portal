# 05 — Data Model

The shared language between the business and the system. JSON Schemas are written workflow-by-workflow as each one moves Draft → In Review.

| File | What it holds |
|---|---|
| [`entities.md`](entities.md) | Catalogue of every domain concept JPMS talks about, grouped by lifecycle stage. The entity-relationship diagram is here too. |
| [`permissions-matrix.md`](permissions-matrix.md) | Role × Workflow responsibility matrix (11 personas × 10 workflows). |
| [`status-models.md`](status-models.md) | The lifecycle of every stateful entity (Lead, Project, Work Order, Variation, Inspection, Incident, Settlement Record, etc.). |
| [`approval-flows.md`](approval-flows.md) | Approval chains (who signs off what, at what threshold). |
| [`integrations.md`](integrations.md) | What feeds JPMS, what JPMS replaces, what consumes JPMS data downstream. |
