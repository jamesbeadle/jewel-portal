---
name: input-validation-check
description: Check the inputs against their columns — read the schema and every entry point that writes to it (API routes, server actions, command handlers, MCP tool and action handlers, imports) and write tools/refactor/input-validation.md, every column a door writes with whether the service checks it against the column's type and constraints, whether the form mirrors it, and whether anything truncates or coerces instead of rejecting. Read-only, no branch, no pull request, no score box. Use when the person says "Run the input validation check", "check the inputs against the schema", "can anything write data that does not fit its column", "is the MCP server validating its inputs", or asks whether a particular write is checked. Not for fixing what it finds (that is a fix/ branch the person asks for) and not "Refactor the repo", which never changes behaviour.
---

# The input validation check

One reading of every door into the data, written down: for each entry point that writes, every column it writes, set against what that column accepts, and whether the value is checked before it gets there. The rule it reads against is *Inputs Are Checked Where They Enter* in *How I Write Code*: the service is the guard, the form mirrors it for the user, a value that does not fit is rejected and never repaired, and the limits are stated once. The result is `tools/refactor/input-validation.md`; *"can an agent write a 300-character name through the MCP server"* is a search of that file.

It **reads** and changes nothing else. It is an ad hoc check, run when the person asks for it, like the widget identification: not part of the score, not gated, not a pass of the refactor round. It does not fix what it finds — adding a check changes behaviour (a value accepted yesterday is refused today), so a refactor round, which never changes behaviour, cannot carry it; the fix is a `fix/input-validation-<area>` branch the person asks for, like any other fix. Because it reads, it needs no branch: run it on whatever is checked out, the default branch included, and commit nothing.

## 1. Read the contract

Find where the schema is stated and take each table's columns as they stand now — the latest migration wins over the first one, and the database itself (a Supabase or SQL connector, if the session has one) wins over the files. For every column note what it accepts: type; maximum length for `char`/`varchar`/`nvarchar`/`text` with a limit; precision and scale for `decimal`/`numeric`; the bounds of `smallint`/`int`/`bigint`; date and time validity; `NOT NULL` without a default; `CHECK` constraints and enum values; `uuid` format; boolean; foreign keys. A column the application never writes (an identity, a `created_at` the database fills) is left out.

## 2. Find every door

List every place a value from outside reaches a write: API routes and endpoints, server actions, form posts, command handlers, message and webhook consumers, scheduled imports, and **every MCP tool or action handler** — an agent calls those without ever seeing a form, so they are read with the same care as the API. For each door, the columns it can write and the field each comes from. A door that writes through another (an MCP action that calls the command handler) inherits that handler's checks; say which one it goes through.

## 3. Set each door against the contract

For every door and column, one verdict:

- **Guarded** — the service checks the value against the column's limit, at its gate, before any domain logic, and rejects with an error naming the field.
- **Mismatched** — a check exists but its limit is not the column's (a validator allowing 500 characters into a `varchar(200)`, a decimal accepted at four places into `numeric(10,2)`).
- **Unguarded** — nothing on the service side checks it; the database is the first thing to complain, or the value is written as sent.
- **Form only** — the form checks it and the service does not; an API or MCP caller goes straight past.
- **Repaired** — the value is truncated, rounded, clamped or coerced to fit instead of being refused.

Beside each, whether the form that feeds that door mirrors the limit (`maxLength`, `min`/`max`, `step`, `required`, `pattern`, a list of choices), and whether the limit is stated once or typed out again in each place. The service verdict is the finding; the form column is usability, reported but never counted as the guard.

## 4. Write it down

Write `tools/refactor/input-validation.md`: a line with the date and the commit read; the totals by verdict; then one section per door, grouped by area, as a table — column, what it accepts, service verdict with the file and line of the check (or of the write, when there is none), form mirror, where the limit is stated. Put the MCP doors first when there are any: they are the reason the check exists.

## 5. Answer and leave it there

Tell the person, in four sentences: how many doors and columns were read and how many are guarded; the unguarded, form-only and repaired counts, with the worst door named; where the document is; and that fixing them is a `fix/` branch they ask for — the check never fixes. When the question was narrower (*is `create_todo` checked*), answer it from the same reading with the file and line. `input-validation.md` is left in the working tree, to ride along with whatever is committed next or be deleted. No branch, no commit, no pull request, no README box, no plan, no baseline. If the session is logged against a task in Your Business Today, post the four sentences there.
