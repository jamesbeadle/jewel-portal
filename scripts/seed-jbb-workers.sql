-- Seed the JPMS worker registry from the accountant's JBB staff-and-rates export
-- (jbbstaffandrates, pulled from the JPS system 18 Aug 2026; JBB days 1 Jul – 14 Aug).
--
-- One-off data seed per CLAUDE.md: reviewed script under scripts/, run via sqlcmd. Never
-- touches schema. Idempotent and conservative:
--   · a worker missing from the registry is INSERTED with rate (day rate ÷ 8), rate history
--     and a 20% CIS status row;
--   · a worker already present is LEFT ALONE (rates are never changed by a seed — that is a
--     deliberate act with rate history through the Workers page) except that a missing CIS
--     status row is added, since the JPS system never stored CIS and 20% was assumed;
--   · SubcontractorId is deliberately left NULL everywhere — whether each worker is invoiced
--     direct or cross-charged via Jewel Property Serve is Jeremy's per-worker decision, made
--     on the Workers page, and the settlement schedule needs that link to reconcile.
--
-- Notes carried over from the export:
--   · "Pranas Jancauskas" is the timesheet spelling (staff register says just "Pranas") — the
--     registry uses the full timesheet name so capture matches.
--   · Zack Hamer has no JPS staff register row at all; £190 comes from the rates table only.
--     He is seeded with no contact details — fill them in on the Workers page.
--   · Frank Stroffolino is a co-director with 0 JBB days this period; seeded so his account
--     can link to a worker record if he ever logs BB days.
--   · CIS is 20% ASSUMED for everyone (JPS stores no per-person CIS). First gross-status or
--     30% person: change it on the Labour overview, which appends an effective-dated row.
--   · Contracted days per month are NOT in the export, so none are seeded — the forecast
--     shows £0 projected until they are set per worker on the Labour overview.

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
-- CIS effective from before the July period so historic schedules resolve a rate.
DECLARE @cisFrom datetimeoffset = '2026-07-01T00:00:00+00:00';

DECLARE @seed TABLE (
    Name nvarchar(256) NOT NULL,
    DayRate decimal(18,4) NOT NULL,
    Email nvarchar(256) NOT NULL,
    Phone nvarchar(64) NOT NULL
);

INSERT INTO @seed (Name, DayRate, Email, Phone) VALUES
    (N'James Everitt',     250, N'',                             N'07904462263'),
    (N'Lawrence Downey',   230, N'',                             N'07415117404'),
    (N'Pranas Jancauskas', 250, N'',                             N'07484741671'),
    (N'Frank Stroffolino', 250, N'fstroffo@yahoo.co.uk',         N'07944623636'),
    (N'John Ahern',        230, N'jlahern@hotmail.co.uk',        N'07878794455'),
    (N'Adam Midgley',      200, N'',                             N'07487529742'),
    (N'Dan Prowse',        200, N'prowsedaniel5@gmail.com',      N'07808005629'),
    (N'Zack Hamer',        190, N'',                             N''),
    (N'Jack Easty',        180, N'jackeasty18@icloud.com',       N'07514768962'),
    (N'Finley Taylor',     150, N'finley.taylor33@outlook.com',  N'07926722106');

DECLARE @name nvarchar(256), @dayRate decimal(18,4), @email nvarchar(256), @phone nvarchar(64);
DECLARE @workerId nvarchar(64), @hourly decimal(18,4);

DECLARE seed_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT Name, DayRate, Email, Phone FROM @seed;
OPEN seed_cursor;
FETCH NEXT FROM seed_cursor INTO @name, @dayRate, @email, @phone;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Hourly rate is the agreed day rate ÷ 8 (docs/Labour-Time-Tracking-Scope.md).
    SET @hourly = @dayRate / 8.0;

    SELECT @workerId = WorkerId FROM Workers WHERE LOWER(Name) = LOWER(@name);

    IF @workerId IS NULL
    BEGIN
        SET @workerId = LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', ''));

        INSERT INTO Workers (WorkerId, Name, SubcontractorId, HourlyRate, IsActive, ContactEmail, ContactPhone)
        VALUES (@workerId, @name, NULL, @hourly, 1, @email, @phone);

        INSERT INTO WorkerRateHistories (WorkerRateHistoryId, WorkerId, HourlyRate, EffectiveFrom)
        VALUES (LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')), @workerId, @hourly, @cisFrom);

        PRINT 'INSERTED  ' + @name + '  (day rate £' + CONVERT(varchar(20), @dayRate)
            + ' = £' + CONVERT(varchar(20), @hourly) + '/hr)';
    END
    ELSE
    BEGIN
        -- Existing worker: never touch the rate from a seed. Say so if it disagrees.
        IF EXISTS (SELECT 1 FROM Workers WHERE WorkerId = @workerId AND HourlyRate <> @hourly)
            PRINT 'RATE DIFFERS  ' + @name
                + '  registry rate does not equal export day rate ÷ 8 — review on the Workers page.';
        ELSE
            PRINT 'EXISTS    ' + @name + '  (left unchanged)';

        -- Backfill contact details only where the registry holds nothing.
        UPDATE Workers SET ContactEmail = @email
            WHERE WorkerId = @workerId AND ContactEmail = '' AND @email <> '';
        UPDATE Workers SET ContactPhone = @phone
            WHERE WorkerId = @workerId AND ContactPhone = '' AND @phone <> '';
    END

    -- CIS: 20% standard for everyone in the export; only added where no status exists yet.
    IF NOT EXISTS (SELECT 1 FROM WorkerCisStatuses WHERE WorkerId = @workerId)
    BEGIN
        INSERT INTO WorkerCisStatuses (WorkerCisStatusId, WorkerId, CisRatePercent, VerifiedRef, EffectiveFrom)
        VALUES (LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')), @workerId, 20, N'', @cisFrom);
        PRINT '  + CIS 20% (assumed per export — no per-person CIS held in JPS)';
    END

    SET @workerId = NULL;
    FETCH NEXT FROM seed_cursor INTO @name, @dayRate, @email, @phone;
END

CLOSE seed_cursor;
DEALLOCATE seed_cursor;

COMMIT TRANSACTION;

PRINT '';
PRINT 'Done. Still to do by hand on the portal:';
PRINT '  1. Link each worker to a subcontractor (direct invoice, or Jewel Property Serve for cross-charge).';
PRINT '  2. Set contracted days per month per worker (Labour overview) — none were in the export.';
PRINT '  3. Zack Hamer: no contact details anywhere — fill in on the Workers page.';
PRINT '  4. Invite each as a portal user (Site Operative) with the same email to link My Day.';
