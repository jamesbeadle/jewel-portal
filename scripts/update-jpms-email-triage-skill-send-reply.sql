-- ============================================================================
-- update-jpms-email-triage-skill-send-reply.sql  (2026-09-04, v2)
-- ============================================================================
-- Teaches the LIVE jpms-email-triage skill that the connector can now SEND from
-- the projects mailbox (send_mailbox_email — the Control Centre's Reply box,
-- confirm-first), replacing the "the connector never sends email / all paths
-- are draft-then-human-sends" doctrine that made every AI tool refuse to reply.
-- Also corrects the to-do action's name (create_todo_items_from_message).
--
-- v2: the first version matched the two stale passages word for word and found
-- the live body had drifted from the seeded wording (re-wrapped or re-saved),
-- so it changed nothing. This version replaces by STRUCTURE instead: the whole
-- of step 7 (from "7. **Replies LAST" up to the "## Decisions, not defaults"
-- heading) and the whole "## What you cannot do (by design)" section (to the
-- end of the body), whatever they currently say — and prints the text it
-- replaced so the log shows exactly what went. Everything else in the body is
-- untouched, and the outgoing body is copied to SkillRevisions first, the way
-- the portal's own save does it, so the previous wording is one click away in
-- the skill's revision trail. Idempotent: a body without the stale sentences is
-- left alone; a body whose headings have been renamed is left alone with a
-- notice to finish by hand. One-off data fix — not an EF migration; run via
-- sqlcmd.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON; -- a failed revision insert must roll the whole change back, never half-apply it

DECLARE @by nvarchar(256) = N'automation@jewelbb.co.uk';
DECLARE @key nvarchar(128) = N'jpms-email-triage';
DECLARE @body nvarchar(max) = (SELECT [Body] FROM [dbo].[Skills] WHERE [SkillKey] = @key);
-- The stored body's own line ending, whichever the seed or the editor wrote.
DECLARE @nl nvarchar(2) = CASE WHEN CHARINDEX(NCHAR(13), ISNULL(@body, N'')) > 0 THEN NCHAR(13) + NCHAR(10) ELSE NCHAR(10) END;

DECLARE @stepStart nvarchar(100) = N'7. **Replies LAST';
DECLARE @stepEnd nvarchar(100) = N'## Decisions, not defaults';
DECLARE @cannotHeading nvarchar(100) = N'## What you cannot do (by design)';

DECLARE @newReplies nvarchar(max) = N'7. **Replies LAST.** Everything above must be filed before any reply goes, so a failed send' + @nl +
    N'   loses nothing already filed. send_mailbox_email IS the Control Centre''s Reply box: read the' + @nl +
    N'   email with get_mailbox_message, take its replyAll envelope (to, cc, subject — the same prefill' + @nl +
    N'   the page shows), write the body as plain text, then show the user the recipients, subject and' + @nl +
    N'   body and get their explicit yes — the action is confirm-first, and the email leaves the' + @nl +
    N'   projects mailbox the moment the confirmed call succeeds. A sent reply tags the thread' + @nl +
    N'   JPMS/Replied and it leaves the queue. saveAsDraftOnly true stages the reviewed draft in the' + @nl +
    N'   mailbox''s Drafts folder instead, for a person to send from Outlook; the prepare_*_draft actions' + @nl +
    N'   do the same for record-anchored emails (a purchase order, a request''s official document).';
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
    DECLARE @stepFrom int = CHARINDEX(@stepStart, @body);
    DECLARE @stepTo int = CASE WHEN @stepFrom > 0 THEN CHARINDEX(@stepEnd, @body, @stepFrom) ELSE 0 END;
    DECLARE @cannotFrom int = CHARINDEX(@cannotHeading, @body);

    IF @stepFrom = 0 OR @stepTo = 0 OR @cannotFrom = 0 OR @cannotFrom < @stepTo
    BEGIN
        PRINT N'jpms-email-triage still says the connector cannot send, but its headings have moved (step 7 / "Decisions, not defaults" / "What you cannot do") — nothing changed. Finish it by hand on /admin/skills from docs/ai/skills/jpms/jpms-email-triage.md.';
    END
    ELSE
    BEGIN
        DECLARE @oldReplies nvarchar(max) = SUBSTRING(@body, @stepFrom, @stepTo - @stepFrom);
        DECLARE @oldCannot nvarchar(max) = SUBSTRING(@body, @cannotFrom, LEN(@body) - @cannotFrom + 1);

        DECLARE @updated nvarchar(max) =
            REPLACE(LEFT(@body, @stepFrom - 1), N'create_todos_from_message', N'create_todo_items_from_message')
            + @newReplies + @nl + @nl
            + SUBSTRING(@body, @stepTo, @cannotFrom - @stepTo)
            + @cannotHeading + @nl + @nl + @newCannot + @nl;

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
        PRINT N'--- step 7 as it WAS (kept in SkillRevisions): ---';
        PRINT @oldReplies;
        PRINT N'--- "cannot do" section as it WAS (kept in SkillRevisions): ---';
        PRINT @oldCannot;
    END
END;
