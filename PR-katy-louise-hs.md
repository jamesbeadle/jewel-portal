# Katy-Louise runs health and safety in the portal — the last eight tasks

Branch `feature/katy-louise-runs-hs-in-the-portal`, four commits on top of `main` (3797ab45).
YBT goal: Katy-Louise Runs Health and Safety in the Portal (15ae48d7).

To push and open the pull request from the Mac:

```
git switch feature/katy-louise-runs-hs-in-the-portal
git push -u origin feature/katy-louise-runs-hs-in-the-portal
gh pr create --base main --title "Katy-Louise runs health and safety in the portal: comments, one digest per sitting, officer-only close, and her six site check forms" --body-file PR-katy-louise-hs.md
```

## What changed

**Corrective actions (three tasks).** A row on the H&S tab's Actions pane opens the action's
thread: comments by the site manager and the officer, each with photographs of the work done, and
the status. A comment on an Open action moves it to In progress by itself. Closed is offered only
to the H&S officer or a director and is refused server-side to anyone else — on the endpoint and
over the connector alike (`HsActionRoles`, `HsRecordCloseScope`). Every change leaves an event, and
the worker sends ONE email per project per sitting (a sitting ends 30 minutes after its last
change): to the site manager with everything that was not his own doing, to each H&S officer with
everything that was not an officer's. The site manager's address is on the project's settings
(`SiteManagerName` / `SiteManagerEmail`, Edit details).

**Her site check forms (five tasks).** Eight definitions in the forms engine, exactly as her paper
sheets and no more: G-01 toolbox talk register, I-05 ladder record, I-01 equipment schedule, I-03
PUWER record, K-02 site incident and K-03 personnel incident reports, I-14 first aid kit checklist,
E-02 fire extinguisher record. Each opens at `/f/<slug>` (the Site check forms menu on her home and
on a project's H&S tab), files under its site, and is read back on the Forms register, which the
site manager and the H&S officer now open for the health and safety forms alone. The engine gained a
Table question (a paper register's rows), Site filing and a HealthAndSafety store to carry them.

## Database — run before or with the deploy

```
sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i api/Migrations/add-hs-action-thread.sql -b -o add-hs-action-thread.log
```

Adds `HsRecordComments`, `HsRecordPhotos`, `HsRecordEvents` and `Projects.SiteManagerName` /
`SiteManagerEmail`. Additive only. The `form-health-and-safety` blob container is created on first
use like the other form stores.

## After the deploy

- Project settings → Edit details on each live site: enter the site manager's name and email
  (James Everitt on By France), or nobody is told.
- Give James Everitt (and each site manager) a portal login with the Site Manager role so he can
  write on his actions; his name on the action is enough to be told, the login is for writing.
- Save the updated page guides through the connector if the stored copies are used.

## Verified

- `contracts` compiles (dotnet 8, 0 errors); `jpms` compiles with the real Razor compiler against
  a stubbed WebAssembly package (0 errors, no new warnings). `api`, `worker` and `tests` cannot be
  compiled from a cloud session (NuGet is blocked) — every changed file parses under Roslyn and
  `tools.build.resolve_type_names` finds no missing using; `tools.worker_link_check` passes.
- Quality score 76.5% → 77.4%; the gate reads the same as `main` (main's stale baseline already
  fails on `tangledConditionLines` 279; this branch adds none).
- New tests: `FormTableTests`, `HsActionThreadTests`, `HsNotificationDigestTests`, plus pins in
  `AiConnectorTests` and `FormsConnectorTests`. The first CI build is the compile and test check.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_01GRjsvELwMRjA5xz5CZRnmp
