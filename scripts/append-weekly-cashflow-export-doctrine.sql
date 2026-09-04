-- ============================================================================
-- append-weekly-cashflow-export-doctrine.sql  (2026-09-04)
-- ============================================================================
-- Teaches the LIVE jpms-cash-forecast skill about the redesigned Weekly
-- Cashflow export (one line per supplier, a column per week) and the
-- connector's get_weekly_cashflow_grid read. The seed script never overwrites
-- an existing skill, so this appends two bullets to the row that is already
-- there — the way the portal's own save does it: the outgoing body is copied to
-- SkillRevisions first, then the body and version move on. Idempotent: a body
-- that already carries the bullets is left alone. One-off data fix — not an EF
-- migration; run via sqlcmd.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON; -- a failed revision insert must roll the whole change back, never half-apply it

DECLARE @by nvarchar(256) = N'automation@jewelbb.co.uk';
DECLARE @key nvarchar(128) = N'jpms-cash-forecast';
-- LIKE treats _ as a wildcard; [_] makes the marker a literal match.
DECLARE @marker nvarchar(200) = N'get[_]weekly[_]cashflow[_]grid';
DECLARE @bullets nvarchar(max) = N'
- The Excel export is the grid on paper: "Weekly plan" — one line per supplier (a supplier group
  is one line) with a column per week, band totals, net movement and, for directors, the closing
  balance; "Detail" — every bill/invoice under its line with due and expected dates, parked
  entries listed uncounted; "Data" — the flat list for pivoting. A shaded amount is one the
  accountant moved.
- Over the connector, **get_weekly_cashflow_grid** is that same grid line by line (Xero-seeded,
  placements and exclusions applied, one line per supplier, amounts per week, moved flags) — quote
  it for "who do we pay which week"; get_weekly_cashflow_plan is only the raw overlay.
';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = @key)
BEGIN
    PRINT N'jpms-cash-forecast is not seeded on this database — run scripts/seed-jpms-workflow-skills.sql instead (it now carries these bullets).';
END
ELSE IF EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = @key AND [Body] LIKE N'%' + @marker + N'%')
BEGIN
    PRINT N'jpms-cash-forecast already carries the weekly cashflow export doctrine — nothing to do.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO [dbo].[SkillRevisions] ([SkillRevisionId], [SkillKey], [Version], [Body], [Description], [SavedByEmail], [SavedAt])
    SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'')), [SkillKey], [Version], [Body], [Description], [UpdatedByEmail], SYSDATETIMEOFFSET()
    FROM [dbo].[Skills]
    WHERE [SkillKey] = @key;

    UPDATE [dbo].[Skills]
    SET [Body] = RTRIM([Body]) + @bullets,
        [Version] = [Version] + 1,
        [UpdatedByEmail] = @by,
        [UpdatedAt] = SYSDATETIMEOFFSET()
    WHERE [SkillKey] = @key;

    COMMIT TRANSACTION;
    PRINT N'jpms-cash-forecast updated: weekly cashflow export + get_weekly_cashflow_grid doctrine appended, version bumped.';
END;
