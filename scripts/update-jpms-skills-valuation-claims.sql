-- ============================================================================
-- update-jpms-skills-valuation-claims.sql  (2026-09-04)
-- ============================================================================
-- Valuation claims became a linkable record in email triage (RecordType.ValuationClaim,
-- mail tag JPMS/VAL-{project}-{claim number}) and the connector's tools learnt it in the
-- same deploy. The two LIVE workflow skills must say so as well — the seeded rows are the
-- connector's doctrine, and a re-run of seed-jpms-workflow-skills.sql never touches an
-- existing row, so the addition goes in here. Each body gains the same wording the
-- markdown sources gained (docs/ai/skills/jpms/jpms-email-triage.md and
-- jpms-valuation-cycle.md), the way a save on /admin/skills would do it: the outgoing body
-- is copied to SkillRevisions first, then Version steps up. The triage wording lands after
-- its "File to records" step when that sentence is still there, otherwise at the end.
-- Idempotent: a body that already mentions ValuationClaim is left alone, and a missing
-- skill is reported, not created. One-off data fix — not an EF migration; run via sqlcmd.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON; -- a failed revision insert must roll the whole change back, never half-apply it

DECLARE @by nvarchar(256) = N'automation@jewelbb.co.uk';
DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();

-- ---------------------------------------------------------------- jpms-email-triage
DECLARE @triageKey nvarchar(128) = N'jpms-email-triage';
DECLARE @triageAnchor nvarchar(max) = N'   email can feed a request AND a cost centre AND the programme at once; multiple filings are
   normal, not a smell.
';
DECLARE @triageAddition nvarchar(max) = N'   A valuation period is a record too: mail about the month''s claim — the site-meeting notes
   that settle what is claimed, the QS''s working, the architect''s early queries — files to the
   LIVE claim (type ValuationClaim; the recordId is the claim''s ValuationClaimId from
   get_valuation_context). The frozen statement (type ValuationReportSnapshot, ids from
   list_valuation_snapshots) is for the client''s response to what was actually sent; a snapshot
   reads its claim''s mail as well as its own, so filing to the claim is never the wrong choice.
';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = @triageKey)
    PRINT 'jpms-email-triage: not seeded on this database — run seed-jpms-workflow-skills.sql first (its body already carries the wording).';
ELSE IF EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = @triageKey AND [Body] LIKE N'%ValuationClaim%')
    PRINT 'jpms-email-triage: already carries the valuation-claim wording — nothing to do.';
ELSE
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO [dbo].[SkillRevisions] ([SkillRevisionId], [SkillKey], [Version], [Body], [Description], [SavedByEmail], [SavedAt])
    SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'')), [SkillKey], [Version], [Body], [Description], [UpdatedByEmail], @now
    FROM [dbo].[Skills] WHERE [SkillKey] = @triageKey;

    UPDATE [dbo].[Skills]
    SET [Body] = CASE WHEN CHARINDEX(@triageAnchor, [Body]) > 0
                      THEN REPLACE([Body], @triageAnchor, @triageAnchor + @triageAddition)
                      ELSE [Body] + NCHAR(10) + N'## Valuation correspondence' + NCHAR(10) + NCHAR(10) + @triageAddition END,
        [Version] = [Version] + 1,
        [UpdatedByEmail] = @by,
        [UpdatedAt] = @now
    WHERE [SkillKey] = @triageKey;

    COMMIT TRANSACTION;
    PRINT 'jpms-email-triage: valuation-claim wording added (previous body kept in SkillRevisions).';
END;

-- ---------------------------------------------------------------- jpms-valuation-cycle
DECLARE @valuationKey nvarchar(128) = N'jpms-valuation-cycle';
DECLARE @valuationAddition nvarchar(max) = N'
## Correspondence

- **The live claim is a record in its own right.** Mail about the period — what to claim, the
  QS''s working, the architect''s early queries — files to the claim (file_email_to_record, type
  ValuationClaim, recordId = the claim''s ValuationClaimId from get_valuation_context) and reads
  back with read_record_emails (recordType valuation_claim). Its mail tag is
  JPMS/VAL-{project reference}-{claim number}.
- **A snapshot inherits its claim''s mail.** Every snapshot frozen from a claim shows the claim''s
  correspondence beside anything tagged to the snapshot itself (type ValuationReportSnapshot),
  so the statement carries the period''s whole story; the client''s reply to a sent statement
  can go on either.
- **Roll-over moves the tag on its own.** Confirm & roll over starts the next claim with the
  next number — new mail files to the new period; nothing is re-tagged.
';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = @valuationKey)
    PRINT 'jpms-valuation-cycle: not seeded on this database — run seed-jpms-workflow-skills.sql first (its body already carries the wording).';
ELSE IF EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = @valuationKey AND [Body] LIKE N'%ValuationClaim%')
    PRINT 'jpms-valuation-cycle: already carries the Correspondence section — nothing to do.';
ELSE
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO [dbo].[SkillRevisions] ([SkillRevisionId], [SkillKey], [Version], [Body], [Description], [SavedByEmail], [SavedAt])
    SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'')), [SkillKey], [Version], [Body], [Description], [UpdatedByEmail], @now
    FROM [dbo].[Skills] WHERE [SkillKey] = @valuationKey;

    UPDATE [dbo].[Skills]
    SET [Body] = [Body] + @valuationAddition,
        [Version] = [Version] + 1,
        [UpdatedByEmail] = @by,
        [UpdatedAt] = @now
    WHERE [SkillKey] = @valuationKey;

    COMMIT TRANSACTION;
    PRINT 'jpms-valuation-cycle: Correspondence section added (previous body kept in SkillRevisions).';
END;

SELECT [SkillKey], [Version], [UpdatedByEmail], [UpdatedAt], LEN([Body]) AS BodyLength
FROM [dbo].[Skills]
WHERE [SkillKey] IN (N'jpms-email-triage', N'jpms-valuation-cycle');
