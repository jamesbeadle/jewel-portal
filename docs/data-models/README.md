# Data Models

JSON Schemas (draft 2020-12) for every major business entity, plus an entity-relationship diagram that ties them together.

Once journeys are signed off, these schemas are exported as the basis of:

- OpenAPI request/response shapes
- Database table definitions (Azure SQL)
- Contract tests for the eventual API

> 📁 **`_templates/`** holds reference-only example schemas. Real schemas live in this folder root.

---

## Process for adding a real entity

1. **Identify the entity during a journey.** The journey's "Data structures" section names it.
2. **Capture the source meeting note** in the schema's `description`.
3. **Copy the shape** of [`_templates/entity-schema-example.json`](_templates/entity-schema-example.json) — never copy fields verbatim.
4. **Create** `{entity}.schema.json` in this folder (e.g. `project.schema.json`).
5. **Update** the entity-relationship diagram (see below) and the root [`README.md`](../../README.md#6-business-entities) entities table.

---

## Conventions

- One file per entity: `{entity}.schema.json` (e.g. `lead.schema.json`, `project.schema.json`).
- Use `$id`, `title`, `description` on every schema.
- Use `$ref` between schemas rather than copy-pasting nested shapes.
- Enums live alongside the schema that owns them, unless reused — then promote to `enums/`.
- A short `.md` companion file is allowed for any schema that needs narrative explanation beyond `description` fields.

---

## Entity Relationships

First-cut ERD is in [`entity-relationship.md`](entity-relationship.md), sourced from the [JBB workflow audit](../meetings/2026-05-20-jbb-workflow-audit.md) and the [2026-05-18 domain discovery](../meetings/2026-05-18-domain-discovery.md). The diagram is split into four sub-diagrams (project lifecycle, subcontractor & compliance, finance, people & ops) for legibility. Schemas are written workflow-by-workflow as each workflow moves Draft → In Review.

---

## Entity index

Full surfaced-entity list is in [`entity-relationship.md`](entity-relationship.md). Schemas are added here as each one is written.

| Entity | Schema | Description |
|---|---|---|
| _Schemas to be written workflow-by-workflow. See [`entity-relationship.md`](entity-relationship.md) for the full list of surfaced entities._ | | |
