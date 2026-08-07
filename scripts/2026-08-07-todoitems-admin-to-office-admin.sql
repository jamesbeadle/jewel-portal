-- One-off data fix (2026-08-07): move to-do items assigned to the ADMINISTRATOR super-role
-- onto the new internal OfficeAdmin role.
--
-- Why: Administrator (Role.Admin = 0) is no longer an assignable to-do role — the pickers now
-- offer the lower-level "Office Admin" (Role.OfficeAdmin = 14) instead, and the command
-- validations reject Administrator as an assignee. Any item still carrying AssigneeRole = 0
-- would render with no picker match and fail validation on its next edit, so it is remapped
-- here. Pins are untouched: a pinned person kept their item only if they hold the new role,
-- and no directory user holds OfficeAdmin yet, so pins on remapped items are cleared to let
-- the items fall back to the role (mirroring TodoAssigneeGuard's person-holds-role rule).
--
-- Data-only — no schema changes (scripts/ convention: never touch schema here). Idempotent:
-- a second run finds no AssigneeRole = 0 rows and does nothing.
--
-- Run with:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--     -i scripts/2026-08-07-todoitems-admin-to-office-admin.sql -b

SELECT COUNT(*) AS AdministratorAssignedBefore FROM [TodoItems] WHERE [AssigneeRole] = 0;
GO

UPDATE [TodoItems]
SET [AssigneeRole] = 14,           -- Role.OfficeAdmin
    [AssigneePersonEmail] = NULL   -- nobody holds OfficeAdmin yet; items fall back to the role
WHERE [AssigneeRole] = 0;          -- Role.Admin (Administrator)
GO

SELECT COUNT(*) AS OfficeAdminAssignedAfter FROM [TodoItems] WHERE [AssigneeRole] = 14;
GO
