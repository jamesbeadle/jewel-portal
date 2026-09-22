-- One-off data fix (2026-09-22): every to-do names a role. Items with no assignee move onto
-- the new Miscellaneous desk (Role.Miscellaneous = 16) — "no role owns this", which is what
-- an unassigned item always meant, now said out loud.
--
-- Why: the command validations now refuse a to-do without a role (the picker starts empty and
-- the author picks a real role or Miscellaneous), so any row still carrying AssigneeRole NULL
-- would fail validation on its next edit. Pins are untouched: an unassigned item can carry no
-- pin (a person is only ever pinned WITH their role), so there is nothing to clear.
--
-- Data-only — no schema changes (scripts/ convention: never touch schema here). Idempotent:
-- a second run finds no NULL rows and does nothing. Apply with the deploy that carries the rule.
--
-- Run with:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--     -i scripts/2026-09-22-todoitems-unassigned-to-miscellaneous.sql -b

SELECT COUNT(*) AS UnassignedBefore FROM [TodoItems] WHERE [AssigneeRole] IS NULL;
GO

UPDATE [TodoItems] SET [AssigneeRole] = 16 WHERE [AssigneeRole] IS NULL;
GO

SELECT COUNT(*) AS UnassignedAfter FROM [TodoItems] WHERE [AssigneeRole] IS NULL;
SELECT COUNT(*) AS MiscellaneousNow FROM [TodoItems] WHERE [AssigneeRole] = 16;
GO
