-- ============================================================================
-- Retire Tender Enquiries — DATA TIDY-UP ONLY (2026-09-03). No schema change.
--
-- The feature's tables (TenderEnquiries, TenderEnquiryAnswers,
-- TenderEnquiryAttachments) are deliberately LEFT IN PLACE; nothing reads them.
-- This script only cleans the two live rows that still mention the removed
-- connector actions, so describe_action / the triage skill stop naming things
-- that no longer exist:
--   1. the "jpms-email-triage" skill body (seeded once; the seed does not
--      re-write an existing row) drops log_tender_enquiry_from_message;
--   2. any skill attachments wired to the five retired actions are removed
--      (they would show as orphans on the AI Actions admin page otherwise).
-- Idempotent — safe to run twice.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i 2026-09-03-retire-tender-enquiries.sql -b -o retire-tender-enquiries.log
-- ============================================================================

BEGIN TRANSACTION;
GO

-- 1. Triage skill text: drop the retired action from the one-create-per-pass list.
UPDATE [dbo].[Skills]
SET    [Body]           = REPLACE([Body], N', log_tender_enquiry_from_message', N''),
       [Version]        = [Version] + 1,
       [UpdatedByEmail] = N'system@jewelbespokebuild.co.uk',
       [UpdatedAt]      = SYSDATETIMEOFFSET()
WHERE  [SkillKey] = N'jpms-email-triage'
  AND  [Body] LIKE N'%log_tender_enquiry_from_message%';

PRINT CONCAT('jpms-email-triage skill rows updated: ', @@ROWCOUNT);

-- 2. Skill attachments pointing at the retired actions (or the retired area).
DELETE FROM [dbo].[AiActionSkills]
WHERE  [TargetKey] IN (
           N'log_tender_enquiry',
           N'log_tender_enquiry_from_message',
           N'update_tender_enquiry_details',
           N'set_tender_enquiry_status',
           N'set_tender_enquiry_answers',
           N'Tender enquiries');

PRINT CONCAT('orphaned action-skill attachments removed: ', @@ROWCOUNT);

COMMIT TRANSACTION;
GO
