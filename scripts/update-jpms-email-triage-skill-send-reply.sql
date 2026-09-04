-- ============================================================================
-- update-jpms-email-triage-skill-send-reply.sql  (2026-09-04)
-- ============================================================================
-- Teaches the LIVE jpms-email-triage skill that the connector can now SEND from
-- the projects mailbox (send_mailbox_email — the Control Centre's Reply box,
-- confirm-first), replacing the "the connector never sends email / all paths
-- are draft-then-human-sends" doctrine that made every AI tool refuse to reply.
-- Also corrects the to-do action's name (create_todo_items_from_message). The
-- seed script never overwrites an existing skill, so this edits the row that is
-- already there the way the portal's own save does it: the outgoing body is
-- copied to SkillRevisions first, then the body and version move on. Only the
-- three stale passages are replaced, so any other hand edit the team made on
-- /admin/skills survives. Idempotent: a body without the stale passages is
-- left alone; a body where a passage no longer matches word for word is left
-- alone too, with a notice to finish it by hand. One-off data fix — not an EF
-- migration; run via sqlcmd.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON; -- a failed revision insert must roll the whole change back, never half-apply it

DECLARE @by nvarchar(256) = N'automation@jewelbb.co.uk';
DECLARE @key nvarchar(128) = N'jpms-email-triage';
DECLARE @body nvarchar(max) = (SELECT [Body] FROM [dbo].[Skills] WHERE [SkillKey] = @key);
-- The seeded body's own line ending, whichever sqlcmd stored.
DECLARE @nl nvarchar(2) = CASE WHEN CHARINDEX(NCHAR(13), ISNULL(@body, N'')) > 0 THEN NCHAR(13) + NCHAR(10) ELSE NCHAR(10) END;

DECLARE @oldTodos nvarchar(max) = N'3. **To-dos next** — anything the email demands of the team (create_todos_from_message).';
DECLARE @newTodos nvarchar(max) = N'3. **To-dos next** — anything the email demands of the team (create_todo_items_from_message).';
DECLARE @oldReplies nvarchar(max) = N'7. **Replies LAST, and only as drafts.** Everything above must be filed before any reply is' + @nl +
    N'   prepared, so a failed send loses nothing already filed. The connector never sends email —' + @nl +
    N'   prepare_*_draft actions stage a draft in the shared mailbox and the human sends from Outlook.';
DECLARE @newReplies nvarchar(max) = N'7. **Replies LAST.** Everything above must be filed before any reply goes, so a failed send' + @nl +
    N'   loses nothing already filed. send_mailbox_email IS the Control Centre''s Reply box: read the' + @nl +
    N'   email with get_mailbox_message, take its replyAll envelope (to, cc, subject — the same prefill' + @nl +
    N'   the page shows), write the body as plain text, then show the user the recipients, subject and' + @nl +
    N'   body and get their explicit yes — the action is confirm-first, and the email leaves the' + @nl +
    N'   projects mailbox the moment the confirmed call succeeds. A sent reply tags the thread' + @nl +
    N'   JPMS/Replied and it leaves the queue. saveAsDraftOnly true stages the reviewed draft in the' + @nl +
    N'   mailbox''s Drafts folder instead, for a person to send from Outlook; the prepare_*_draft actions' + @nl +
    N'   do the same for record-anchored emails (a purchase order, a request''s official document).';
DECLARE @oldCannot nvarchar(max) = N'Sending email (all paths are draft-then-human-sends) and bulk retagging are portal-only. Say so' + @nl +
    N'rather than improvising.';
DECLARE @newCannot nvarchar(max) = N'Attach a file from the user''s own computer to an email (attachments go by reference only — a' + @nl +
    N'drawing revision, a progress photo, the replied-to email''s own attachments, a record''s official' + @nl +
    N'PDF), stage decisions into the page''s pending Apply (each connector action lands at once), and' + @nl +
    N'bulk retagging are portal-only. Say so rather than improvising.';

IF @body IS NULL
BEGIN
    PRINT N'jpms-email-triage is not seeded on this database — run scripts/seed-jpms-workflow-skills.sql instead (it now carries the send_mailbox_email doctrine).';
END
ELSE IF CHARINDEX(N'The connector never sends email', @body) = 0
    AND CHARINDEX(N'draft-then-human-sends', @body) = 0
    AND CHARINDEX(N'create_todos_from_message', @body) = 0
BEGIN
    PRINT N'jpms-email-triage already carries the send_mailbox_email doctrine — nothing to do.';
END
ELSE
BEGIN
    DECLARE @updated nvarchar(max) = REPLACE(REPLACE(REPLACE(@body, @oldTodos, @newTodos), @oldReplies, @newReplies), @oldCannot, @newCannot);

    IF CHARINDEX(N'The connector never sends email', @updated) > 0
        OR CHARINDEX(N'draft-then-human-sends', @updated) > 0
        OR CHARINDEX(N'create_todos_from_message', @updated) > 0
    BEGIN
        PRINT N'jpms-email-triage still says the connector cannot send, but the passage no longer matches the seeded wording word for word — nothing changed. Finish it by hand on /admin/skills from docs/ai/skills/jpms/jpms-email-triage.md.';
    END
    ELSE
    BEGIN
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[SkillRevisions] ([SkillRevisionId], [SkillKey], [Version], [Body], [Description], [SavedByEmail], [SavedAt])
        SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'')), [SkillKey], [Version], [Body], [Description], [UpdatedByEmail], SYSDATETIMEOFFSET()
        FROM [dbo].[Skills]
        WHERE [SkillKey] = @key;

        UPDATE [dbo].[Skills]
        SET [Body] = @updated,
            [Version] = [Version] + 1,
            [UpdatedByEmail] = @by,
            [UpdatedAt] = SYSDATETIMEOFFSET()
        WHERE [SkillKey] = @key;

        COMMIT TRANSACTION;
        PRINT N'jpms-email-triage updated: replies now go through send_mailbox_email (confirm-first), to-do action name corrected, version bumped.';
    END
END;
