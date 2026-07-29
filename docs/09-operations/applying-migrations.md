# Applying migrations by hand

The API never touches the schema. `api/Program.cs` deliberately has no migration call — see the
comment above `host.RunAsync()` — and nothing in GitHub Actions or the worker applies one either.
Every schema change reaches the production database the same way: a script, generated from the
migrations, read by a person, run with `sqlcmd`. If a `MigrateAsync()`/`EnsureCreated()` call
reappears anywhere, remove it; do not configure it.

Two reasons, worth restating because they are the reasons this file exists:

1. **Safety.** A startup migration means whichever managed-function instance happens to cold-start
   first after a deploy alters the schema, unreviewed and unwatched — and EF Core 8 has no
   migration lock, so two instances scaling up together could both attempt it. The old code's
   `catch` block swallowed any failure, so a half-applied migration would have surfaced only as
   mysterious endpoint errors.
2. **Speed.** The migration call sat in front of `host.RunAsync()`, so every cold start built the
   full EF model and made a round trip to SQL before a single endpoint would answer. Removing it is
   part of the fix for the twice-daily portal hangs.

The admin login (`jpmsadmin`) is for a person at a keyboard, running these steps. The API's own
login — whatever `SqlConnectionString` in the Static Web App's settings uses — should hold only
`db_datareader`, `db_datawriter` and execute. While it can `ALTER`, the guarantee above is only as
deep as the code; narrowing the login makes it structural.

## The procedure

**1. Generate the script — idempotent, always.**

```bash
cd api
dotnet ef migrations script --idempotent -o migrate.sql
```

`--idempotent` is not optional: it wraps every migration in a check against
`__EFMigrationsHistory`, so the same script is safe whether the database is one migration behind or
five, and safe to run twice.

**2. Read the script.** The whole point of doing this by hand. Flag anything non-additive before it
runs:

- `DROP COLUMN` / `DROP TABLE`
- a column type narrowing (e.g. `nvarchar(max)` → `nvarchar(200)`)
- `ALTER COLUMN ... NOT NULL` without a default on a populated table
- any `UPDATE` that moves data between columns or tables

Purely additive scripts (new tables, new nullable columns, new indexes) proceed. Anything on the
list above gets the expand/migrate/contract treatment — see "Ordering", below.

**3. Note the restore point.** Write down the UTC time before running anything — Azure SQL
point-in-time restore is how a bad migration is undone. For anything on the destructive list, take
a copy first instead of relying on it:

```bash
az sql db copy --resource-group rg-jpms-prod --server sql-jpms-prod-54cf9e \
  --name jpms --dest-name jpms-premigration-$(date -u +%Y%m%d)
```

(And delete the copy once the change has settled — it bills as a second database. This is what the
leftover `jpms-restore-20260722` was.)

**4. Run it.**

```bash
sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i migrate.sql -b -o migrate.log
```

`-b` is not decoration: without it sqlcmd carries on past a failed statement and exits 0, so a
half-applied script reads as a success. With it the first error stops the run and exits non-zero.
`-o` keeps the log — read `migrate.log` even on success. (`-G` Entra ID auth works too if the
login is set up for it.)

**5. Confirm it landed.**

```bash
sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
  -Q "SELECT TOP 5 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"
```

The top row must match the newest folder in `api/Migrations`. If it does not, the code must not be
deployed — endpoints touching the missing columns will fail (loudly, which is the intended
behaviour, but still).

## Ordering: schema first, then code

For **additive** changes: apply the script, confirm `__EFMigrationsHistory`, then push the code.
Old code runs happily against the wider schema; new code never meets the narrower one.

For **destructive** changes: expand/migrate/contract. First ship an additive migration and code
that writes both shapes; then move the data; only when nothing reads the old shape, ship the
contraction as its own later migration. A destructive change never rides in the same script as the
code that requires it.

## Rolling back

`az sql db restore` restores to a **new** database — never over the top of the live one:

```bash
az sql db restore --resource-group rg-jpms-prod --server sql-jpms-prod-54cf9e \
  --name jpms --dest-name jpms-rollback --time "<UTC time noted in step 3>"
```

Then either repoint `SqlConnectionString` at the restored copy, or use it to recover the affected
rows into the live database by hand. Delete whichever database is not kept.
