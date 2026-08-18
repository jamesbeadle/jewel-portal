-- Backfill the JBB days for 1–14 August 2026 from Jeremy's per-worker calendars
-- (screenshots 18 Aug 2026 — day-level placements from the JPS system).
--
-- One-off data seed per CLAUDE.md: reviewed script under scripts/, run via sqlcmd. Rules:
--   · BB days only. PS-site days in the calendars (Chiltern Court, The Hub – Harrow PS,
--     TBC Site, 96/90 Chiltern) are deliberately NOT seeded — they are Property Serve's and
--     belong to Jeremy's system.
--   · Every row lands as a SUBMITTED 8-hour timesheet on cost code 'TBC' — Jeremy's ask was
--     "add the days now, cost codes later": re-code each row on the project's Labour tab
--     (Adjust) before approving. Nothing here posts as cost until approved.
--   · July is NOT seeded: the export gives July as totals only, with no dates. Inventing
--     dates for real cost records is worse than leaving July to reconcile through the old
--     route. If Jeremy can export July day-by-day, this script's pattern extends.
--   · Idempotent: a worker who already has ANY timesheet on a date is skipped for that date.
--
-- Site names in the calendars → JPMS projects (matched by name):
--   "By France – Chislehurst" → By France · "Abbott Road – Guildford" → Abbot Road
--   "Ravenswood – Surbiton BB" → Ravenswood Ave · "Woodhouse Lane – Dorking" → Woodhouse

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- The holding cost code, created if missing so the re-code dropdowns can show it.
IF NOT EXISTS (SELECT 1 FROM CostCenters WHERE Code = N'TBC')
BEGIN
    INSERT INTO CostCenters (CostCenterId, Code, Name, SortOrder, IsActive)
    VALUES (LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')), N'TBC',
            N'To be coded — labour backfill', 9999, 1);
    PRINT 'Created holding cost code TBC — re-code every backfilled day before approving.';
END

DECLARE @days TABLE (WorkerName nvarchar(256), SitePattern nvarchar(128), WorkDay int);

-- James Everitt — By France throughout.
INSERT INTO @days VALUES
 (N'James Everitt', N'By France%', 3),(N'James Everitt', N'By France%', 4),
 (N'James Everitt', N'By France%', 5),(N'James Everitt', N'By France%', 6),
 (N'James Everitt', N'By France%', 7),(N'James Everitt', N'By France%',10),
 (N'James Everitt', N'By France%',11),(N'James Everitt', N'By France%',12),
 (N'James Everitt', N'By France%',13),(N'James Everitt', N'By France%',14);

-- Pranas Jancauskas — By France, one day Ravenswood, then Abbot Road.
INSERT INTO @days VALUES
 (N'Pranas Jancauskas', N'By France%', 3),(N'Pranas Jancauskas', N'By France%', 4),
 (N'Pranas Jancauskas', N'By France%', 5),(N'Pranas Jancauskas', N'Ravenswood%', 6),
 (N'Pranas Jancauskas', N'Abbot%', 7),(N'Pranas Jancauskas', N'Abbot%',11),
 (N'Pranas Jancauskas', N'Abbot%',12),(N'Pranas Jancauskas', N'Abbot%',13),
 (N'Pranas Jancauskas', N'Abbot%',14);

-- Jack Easty — Abbot Road (his 3 Aug was 96 Chiltern Court: PS, not seeded).
INSERT INTO @days VALUES
 (N'Jack Easty', N'Abbot%', 1),(N'Jack Easty', N'Abbot%',11),
 (N'Jack Easty', N'Abbot%',12),(N'Jack Easty', N'Abbot%',13),
 (N'Jack Easty', N'Abbot%',14);

-- Lawrence Downey — Abbot Road, By France, Ravenswood, then Woodhouse.
INSERT INTO @days VALUES
 (N'Lawrence Downey', N'Abbot%', 1),(N'Lawrence Downey', N'By France%', 3),
 (N'Lawrence Downey', N'Ravenswood%', 4),(N'Lawrence Downey', N'Ravenswood%', 5),
 (N'Lawrence Downey', N'Ravenswood%', 6),(N'Lawrence Downey', N'Ravenswood%', 7),
 (N'Lawrence Downey', N'Ravenswood%',10),(N'Lawrence Downey', N'Ravenswood%',11),
 (N'Lawrence Downey', N'Woodhouse%',12),(N'Lawrence Downey', N'Woodhouse%',13),
 (N'Lawrence Downey', N'Woodhouse%',14);

-- Dan Prowse — By France, one day Abbot Road (13 Aug was TBC Site: not BB, not seeded).
INSERT INTO @days VALUES
 (N'Dan Prowse', N'By France%', 3),(N'Dan Prowse', N'By France%', 4),
 (N'Dan Prowse', N'By France%', 5),(N'Dan Prowse', N'By France%', 6),
 (N'Dan Prowse', N'By France%', 7),(N'Dan Prowse', N'By France%',10),
 (N'Dan Prowse', N'By France%',11),(N'Dan Prowse', N'By France%',12),
 (N'Dan Prowse', N'Abbot%',14);

-- Finley Taylor — BB days only (6,7,11–14 Aug were Chiltern/TBC/Harrow: PS, not seeded).
INSERT INTO @days VALUES
 (N'Finley Taylor', N'By France%', 3),(N'Finley Taylor', N'By France%', 4),
 (N'Finley Taylor', N'By France%', 5),(N'Finley Taylor', N'Abbot%',10);

-- John Ahern — one BB day (the rest of his fortnight was Chiltern/TBC: PS, not seeded).
INSERT INTO @days VALUES
 (N'John Ahern', N'Ravenswood%', 3);

-- No calendars were provided for Adam Midgley, Zack Hamer or Frank Stroffolino — nothing is
-- seeded for them. Send their day-level placements and this extends.

DECLARE @inserted int = 0, @skipped int = 0, @missing int = 0;
DECLARE @workerName nvarchar(256), @sitePattern nvarchar(128), @workDay int;
DECLARE @workerId nvarchar(64), @projectId nvarchar(64);
DECLARE @workedOn datetimeoffset;

DECLARE day_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT WorkerName, SitePattern, WorkDay FROM @days;
OPEN day_cursor;
FETCH NEXT FROM day_cursor INTO @workerName, @sitePattern, @workDay;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @workerId = WorkerId FROM Workers WHERE LOWER(Name) = LOWER(@workerName);
    SELECT TOP 1 @projectId = ProjectId FROM Projects WHERE Name LIKE @sitePattern ORDER BY Name;
    SET @workedOn = TODATETIMEOFFSET(DATEFROMPARTS(2026, 8, @workDay), 0);

    IF @workerId IS NULL OR @projectId IS NULL
    BEGIN
        SET @missing += 1;
        PRINT 'MISSING  ' + @workerName + ' / ' + @sitePattern
            + '  — worker or project not found, day ' + CONVERT(varchar(2), @workDay) + ' Aug NOT seeded.';
    END
    ELSE IF EXISTS (SELECT 1 FROM Timesheets WHERE WorkerId = @workerId AND WorkedOn = @workedOn)
        SET @skipped += 1;
    ELSE
    BEGIN
        INSERT INTO Timesheets
            (TimesheetId, ProjectId, PersonEmail, WorkedOn, Hours, CostCode, IsApproved,
             WorkerId, SiteAttendanceId, Status, RateApplied, CostAmount,
             ApprovedByEmail, ApprovedAt, RejectionReason)
        VALUES
            (LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')), @projectId, N'',
             @workedOn, 8.0, N'TBC', 0,
             @workerId, N'', 0 /* Submitted */, 0, 0, N'', NULL, N'');
        SET @inserted += 1;
    END

    SET @workerId = NULL; SET @projectId = NULL;
    FETCH NEXT FROM day_cursor INTO @workerName, @sitePattern, @workDay;
END

CLOSE day_cursor;
DEALLOCATE day_cursor;

COMMIT TRANSACTION;

PRINT '';
PRINT 'Inserted ' + CONVERT(varchar(10), @inserted) + ' submitted 8-hour days on cost code TBC; skipped '
    + CONVERT(varchar(10), @skipped) + ' already present; ' + CONVERT(varchar(10), @missing) + ' unresolved.';
PRINT 'Next: on each project''s Labour tab, Adjust each TBC row to its real cost code, then approve.';
