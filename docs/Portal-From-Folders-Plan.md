# Portal from Folders — the plan

*Drafted 2026-09-26. A company's shared folders are the record of how it works; this plan turns that record into the company's portal, the way the Jewel portal was built by hand, and makes the folder system it read redundant. The Jewel portal (this repository) is the worked example and the template.*

## 1. The idea in one paragraph

Every business keeps a folder system — SharePoint, a file server, a synced drive — and the shape of that system is the shape of the business. A folder that recurs with the same children under every sibling is an entity. A folder name is a grouping, and a grouping is a status, a type or a relationship. A document saved on a rhythm is a step in a process. A spreadsheet's columns are an entity's properties, a Word template is a generated output, a folder of photographs is an input a person captures on their phone, and the file names carry the identifiers everything keys on. Read the whole system with those eyes and the portal's data structure, its site map, its roles and its user stories fall out — exactly the chain in `CLAUDE.md` (*stories → UI → site map → data structure → backend*), walked in reverse from the evidence. A local model on the Mac mini does the reading, so no document leaves the building; Claude does the judgement on a structured summary of what was read; the Jewel portal supplies the shell every portal shares (sign-in, directory, roles, audit, documents, mail tagging, to-dos, the skill store, the OAuth server and the MCP host); and the domain — entities, commands, queries, views, connector tools — is generated per company and refined by a person. The company then operates its portal from its own AI tool through the connector, the way Jewel does today.

## 2. What we have already, and what it becomes

| Already in this repository | Its role in the pipeline |
|---|---|
| `docs/00-business-context` … `docs/05-data-model` (overview, glossary, personas, workflows, journeys, entities, status models, permissions matrix) | **The target shape of the analysis.** The pipeline's second stage writes this exact tree for the new company. It is the specification the scaffold is generated from. |
| `docs/site-map.md`, `docs/ui/navigation-map.md`, `docs/ui/component-anatomy.md` | The target shape of stage three, the site map. |
| `docs/cqrs/*` and `api/Features/*` (one handler per command and query, gates at the entry point) | The backend pattern the generator emits. Every user story becomes a command or a query, so the story list *is* the file list. |
| `api/Features/Connect`, `api/Features/Mcp`, `api/Features/Ai/Tools`, `docs/ai/10-mcp-connector.md` | The connector, unchanged: OAuth 2.1 with PKCE, a stateless MCP host, a per-role tool catalogue, writes composing the same handlers as the HTTP endpoints. The generator adds a `list_*` / `get_*` / `perform_action` tool per query and command. |
| `jpms/Components` and the `siteDefinition.catalogue` in `tools/refactor/rules.json` (Page, PageHeader, Panel, RecordsTable, FormField, TabRow, StatTile, ConfirmDialog …) | The widget catalogue every generated view composes. A generated view never hand-rolls a table. |
| `infra/*.sh` (SQL serverless, storage, Static Web App, the separate MCP-host Function App, alerts, App Insights) | The Azure environment, parameterised by company. You said you will run the infrastructure; the scripts take a company slug and a subscription and nothing else. |
| `tools/refactor`, `tools/mcp`, `.claude/skills` (the project-process kit) | The process discipline the generated repository is born with: the audit, the gate, the score, the skills. The kit's bootstrap installs it into the new repository on day one, so a generated portal scores as this one does. |
| `docs/ai/skills/jbb-second-brain.md` and the skill store | The pattern for the company's *house knowledge* skill: who they are, how they say things, what the portal calls them. Stage two writes the first draft of it from the glossary the survey collects. |

The one thing that does not exist yet is the front of the pipeline: the survey and the inference. That is what this plan adds.

## 3. The pipeline

Six stages. Each has a named skill, a named input, a named output, and a person's checkpoint before the next. Nothing downstream is generated from a stage the person has not signed.

```
SharePoint ──▶ 1 Survey ──▶ 2 Infer ──▶ 3 Design ──▶ 4 Scaffold ──▶ 5 Connect ──▶ 6 Migrate
              (Mac, local)  (Claude)    (Claude)     (Claude)       (Claude)      (Mac + portal)
              evidence pack docs/00-05  site map,    repository,    connector     records and
                                        entities,    schema, CQRS,  tools, house  files in the
                                        stories      views, Azure   skill         portal
```

### Stage 1 — Survey (the long one, on the Mac)

**Where it runs.** The Mac mini (M5 Pro, 18-core CPU, 20-core GPU, 64 GB unified memory, 1 TB). Everything in this stage stays on that machine.

**How the files get there.** Two feeds, used together:

- **Content:** sync the document libraries to the Mac with the OneDrive client and crawl the local filesystem. A local model reads files from disk far faster than over Graph, and the crawl is resumable.
- **Metadata:** one pass over Microsoft Graph (`Sites.Read.All`, `Files.Read.All`, an app registration in the company's tenant) for what the synced copy loses: created by, modified by, the **version count and version dates** of each file, and sharing links. Version history is the best frequency signal there is — a weekly report saved as one file with 52 versions looks like one file on disk. A small local MCP server over Graph (or the Microsoft 365 connector once it is authorised) serves this pass; the crawl calls it per file.

**The model.** Choose at setup, not now — the sizing rule is what matters, and Qwen's current generation is the default:

| Job | Model class | Why |
|---|---|---|
| Reading every document (volume) | Qwen3 mixture-of-experts, ~30B total / ~3B active, 8-bit | Fast per token, so tens of thousands of files are feasible; ~32 GB leaves room for a long context. |
| Judgement inside the survey (classifying an unusual document, naming a pattern) | Qwen3 dense ~32B, 4- to 5-bit | Better reasoning; slower; called only when the fast model is unsure. |
| Image folders | Qwen vision model, 8B to 32B | "What is this a photograph of, and is it a form?" — the input-signal question. |
| Clustering near-duplicates and templates | An embedding model (bge-m3 or nomic-embed) | Finds the template families without a language model call per pair. |

Rule of thumb on 64 GB: about one byte per parameter at 8-bit and 0.6 at 4-bit, keep ~15 GB free for the context window and the system, and never load two large models at once. Run them with MLX (`mlx-lm`, `mlx-vlm`) for Apple Silicon throughput, or Ollama for convenience; both expose an OpenAI-compatible endpoint the crawler calls.

**How long.** A documents-heavy company might hold 50,000 files. At one to two thousand tokens in and a few hundred out per file, the fast model on this machine reads a file in roughly ten to twenty seconds, so reading everything is days, not hours. The crawler makes that acceptable three ways: it classifies by extension, size and name pattern before any model call; it clusters near-duplicates by embedding and reads a **sample** of each template family rather than every instance (a family of 900 valuation spreadsheets is one document kind read ten times, plus 900 rows of metadata); and it checkpoints every file into SQLite so it runs overnight, unattended, and resumes after a restart. Expect the first company to take a working week of machine time and a day of your attention.

**What it writes: the evidence pack.** A folder of JSON and Markdown, `discovery/<company>/survey/`, that is the *only* thing that leaves the Mac. It carries structure, names, field lists, patterns and counts — never a document's body. Its records:

- **Folder record:** path, depth, name and its tokens, child folder and file counts, file-type mix, date span, files-per-month histogram, the **naming pattern** of its files as a template (`{ProjectNumber} - {Client} - {DocumentKind} - {Date}.xlsx`), and its **sibling similarity**: does this subtree recur, with the same children, under each of its parent's other children? (Yes → the parent is a collection of one entity and the recurring children are that entity's tabs.)
- **File record:** path, kind (invoice, quote, valuation, RAMS, letter, drawing register, meeting minutes, photo, form …), size, created and modified, author and last editor, version count and cadence, whether it is a **template or an instance** (which family it belongs to), the **fields** it carries (a spreadsheet's headers and the type of each column; a Word document's headings and the labelled blanks a person fills; a PDF form's fields), the **references** in it (project numbers, client names, invoice and order numbers, dates, people), and a one-line summary.
- **Family record:** one per template family — the fields the family shares, how many instances, who makes them, how often, and which folder pattern they land in.
- **Image folder record:** what the photographs are of, how many, who added them, their cadence, and whether they sit beside a document that references them (a site photo folder beside a weekly report is the report's input).
- **Glossary:** every capitalised term, abbreviation and code the company uses, with the count and three example contexts — the seed of the house-knowledge skill.
- **People:** every author and editor with what they create and where, which is the seed of the roles.

### Stage 2 — Infer (Claude, from the evidence pack alone)

The inference rules, which are the pattern you found doing Jewel by hand, written down so a model applies them the same way every time:

1. **A recurring subtree is an entity.** `Projects/<one per job>/{Drawings, Valuations, H&S, Photos}` → `Project` is the collection; `Drawing`, `Valuation`, `HsRecord`, `Photo` are related entities and the project page's tabs.
2. **A grouping folder is a status, a type or a relationship.** `Live / Completed / Archive` is a status ladder; `Clients / Suppliers / Consultants` is a party type; `By Trade / By Phase` is a relationship to another entity.
3. **A file-name pattern is a set of keys.** The tokens that recur across names are identifiers and foreign keys (`P0231`, `INV-1042`, a client name), and a number that increments is a sequence the portal will mint (`REQ-0123`).
4. **A rhythm is a process.** Files, or versions of one file, saved weekly, monthly or per stage are a scheduled output or a period entity (weekly report, monthly valuation, quarterly review) with a due date the portal can chase.
5. **A template family is a form or a generated document.** Its shared fields are an entity's properties; its instances are that entity's rows. A spreadsheet family becomes a table and a page; a Word family becomes a generated document from the same data.
6. **A spreadsheet that reads other spreadsheets is a report.** Its columns are derived, not stored; the portal computes them.
7. **An image folder is a user input.** Photographs captured on a rhythm beside a record are the record's mobile capture (the Jewel progress-photo intake is the pattern).
8. **Saved e-mail is correspondence tagged to a record.** `.msg` and `.eml` files beside a record are the record's thread; the portal's mail tagging takes them over.
9. **Who saves what is a role.** The people record clusters into personas by what they author and where; the permissions matrix starts from that.
10. **A folder nobody has written to in two years is not a story.** It is archived on migration and gets no view.

Stage 2 writes `docs/00-business-context` to `docs/05-data-model` for the new company in the shape this repository has: overview, glossary, personas, workflows (one per top-level process the rhythms found), user journeys, entities with their properties and where each property was evidenced (the family and field it came from), status models, integrations, permissions matrix, and the first `<company>-second-brain` skill. Every inference cites its evidence record, so a person checking it can say *no, that folder is not an entity, it is one client's odd filing* and the downstream regenerates.

**Checkpoint:** the person reads the entities, the status models and the stories and signs them. This is the stage where the domain is understood or it is not; nothing further is generated until it is.

### Stage 3 — Design (Claude)

From the signed stories: the wireframe per story, the consolidated site map (`docs/site-map.md`, `docs/ui/navigation-map.md`), the shared views, the navigation hierarchy, the speculative views deleted and the missing entry points surfaced — stage 3 of *How to Approach a Codebase*. The site map names every route, the stories it serves and the roles that own and read it; the entity diagram is finalised from what the *unified* views demand. Design tokens come from the company's own material: the survey's letterheads, templates and logo give `docs/design/brand.md` through the existing `widget-design` skill ("Extract the brand from <references>").

**Checkpoint:** the site map, walked through with the person.

### Stage 4 — Scaffold (Claude, into a new repository)

Generate the repository from the template: the shell copied, the domain generated.

- **Template:** the Jewel portal with its domain removed — sign-in, directory and roles, audit, documents and storage, mail tagging, to-dos, calendar, the skill store, the connector's OAuth and MCP host, the widget catalogue, the process kit. Carving this out of `jewel-portal` is the one piece of engineering the first run needs (section 6).
- **Per entity, what the pattern predicts:** the EF entity and migration, the contract types, the commands and queries one per story with their gates, the handlers, the list view, the detail view with its tabs, the form with its limits mirrored from the columns (*Inputs Are Checked Where They Enter* — the limits stated once, beside the schema, read by handler and form alike), the status pill and ladder, and the connector tools. Files land named as their Jewel siblings are named, so the audit's "files the design pattern predicts" figure is zero from birth.
- **Azure:** `infra/azure-setup.sh` and `azure-prod-setup.sh` with the company slug, plus the MCP-host script; the GitHub workflows with the deployment tokens. You run these.
- **The kit:** bootstrap installs `tools/refactor`, `tools/mcp`, `.claude/skills` and `CLAUDE.md`; `PROJECT.md` is written from stage 2's glossary and conventions; the first quality check sets the baseline.

### Stage 5 — Connect (Claude)

The connector is what makes the folder system redundant, because it is how the company will *work* rather than merely look things up. Per command and query: a tool with a description in the company's words, role-filtered as Jewel's are; the `describe_action` / `perform_action` gateway for writes; the house-knowledge skill pinned; one process skill per workflow stage 2 found (the Jewel analogues are `jpms-valuation-cycle.md`, `jpms-tender-award.md` …), drafted from the workflow document and refined by the people who do the work. The connector parity audit (`docs/ai/11-connector-parity-audit.md`) is run against the generated catalogue before hand-over.

### Stage 6 — Migrate (Mac and the portal)

The survey already knows which family every file belongs to and which record it references. Migration maps them: each template-family instance becomes a row (read on the Mac, written through the connector's own write tools, so every import is audited under a named person); each other document is filed against its record in the portal's document store with its folder path kept as provenance; images go through the photo intake; e-mails through mail tagging. What the mapping cannot place is listed for a person. The folders are then read-only, and the portal is the system of record.

## 4. The skill files to write

Each is a `SKILL.md` in the kit (so every repository on the process can run it), named for its sentence, reading and writing only what its stage says. They follow the kit's rule that a process is asked for by name and writes its own output and nothing else.

| Skill | Sentences | Runs where | Reads | Writes | Branch |
|---|---|---|---|---|---|
| `folder-survey` | "Survey the company's folders", "run the survey on <library>" | The Mac, local model | The synced libraries, the Graph metadata server, `discovery/<company>/survey.json` (the crawl config: roots, exclusions, sampling per family) | `discovery/<company>/survey/` — the evidence pack, and the SQLite checkpoint beside it (untracked) | none; the pack is committed by the person |
| `domain-inference` | "Infer the domain from the survey", "what does the survey say the entities are" | Claude | The evidence pack | `docs/00-business-context` … `docs/05-data-model`, the second-brain skill draft, `discovery/<company>/inference-log.md` (every rule applied, with its evidence) | `feature/domain-inference` |
| `site-map-design` | "Draw the site map", "design the views from the stories" | Claude | The signed stage-2 documents | `docs/site-map.md`, `docs/ui/*`, `docs/design/brand.md` via `widget-design` | `feature/site-map` |
| `portal-scaffold` | "Scaffold the portal", "generate the portal from the site map" | Claude, new repository | The template, the signed stage-2 and stage-3 documents | The repository: schema, contracts, commands and queries, views, `infra/`, `PROJECT.md`, the kit | `feature/scaffold` in the new repository |
| `connector-generation` | "Generate the connector", "add the connector tools for <entity>" | Claude | The command and query catalogue | `api/Features/Ai/Tools/*`, `docs/ai/skills/*`, the parity audit | `feature/connector` |
| `folder-migration` | "Import the folders into the portal", "migrate <library>" | The Mac, through the connector | The evidence pack's family and reference records, the synced files | Records and documents in the portal; `discovery/<company>/migration-report.md` (placed, unplaced, duplicates) | none; it writes records, not files |

Two things sit beside the skills:

- **`tools/discovery/`** — the crawler (Python, standard library plus the model client and the Graph client), the evidence-pack schema (`schema/*.json`, so the pack is validated before Claude reads it), the family clustering and the migration mapper. It is the survey's `tools/refactor`: the skills run it, and it is measured by the same audit.
- **`docs/discovery/inference-rules.md`** — the ten rules above as the standard, versioned, with the Jewel evidence that produced each. Each company run adds the cases the rules got wrong and how they were corrected. This is the asset that compounds: by the fourth company the rules, not the prompting, carry the analysis.

## 5. Privacy and evidence

The pitch to a client is that their documents never leave their premises: the Mac is on their network or yours, the model is local, and the pack that goes to Claude holds names, structures and counts. The pack schema enforces that — no field for a document body, summaries capped at one line, and a scrub pass over summaries and glossary contexts for personal data before the pack is committed. `discovery/<company>/` lives in the company's own repository, not in a shared one.

## 6. Order of work

1. **Carve the template out of `jewel-portal`.** A `portal-template` repository: the shell without Jewel's domain, buildable and deployable empty, with the kit installed and a score. This is the largest piece and it is worth doing first because stages 4 and 5 cannot be tested without it. It also answers a question the Jewel repository has been circling: which of its parts are the product and which are the company.
2. **Write the crawler and the evidence-pack schema** (`tools/discovery`), and run it against Jewel's own SharePoint. Jewel is the control: the pack it produces should let stage 2 rediscover the entities this repository already has. Where it does not, either the crawler is missing a signal or the rule is wrong, and that is found before the first paying company.
3. **Write `inference-rules.md` and the `domain-inference` skill**, and run it on the Jewel pack. Compare its `docs/05-data-model/entities.md` with the real one. Iterate until the difference is Jewel's history rather than the rule's blindness.
4. **Set up the Mac** (22 September pickup): MLX, the models, the OneDrive sync, the Graph app registration, the crawler as a launchd job with its checkpoint. Run the next company's survey overnight for a week.
5. **Write `site-map-design`, `portal-scaffold` and `connector-generation`** while the survey runs, tested on the Jewel pack.
6. **Run the next company end to end**, you holding the Azure side. Every correction a person makes at a checkpoint goes into the rules or the template, never only into that company's output.
7. **Write `folder-migration`** last, once a portal exists to migrate into.

## 7. What is deliberately not in this plan

- **A fully local pipeline.** The local model reads; it does not design. Claude does the judgement stages because the quality of the entity model decides everything after it, and the pack is small enough that cost is not the constraint.
- **Automatic sign-off.** Every stage ends with a person, because ambiguity means the domain is not understood, and the evidence pack cannot resolve that on its own.
- **A product name or a licence model.** Decide that when the second company is live.
