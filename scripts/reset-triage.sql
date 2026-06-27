/* =============================================================================
   reset-triage.sql  —  Clean slate for testing mailbox triage
   -----------------------------------------------------------------------------
   What it does:
     * Deletes ALL request conversation messages   (RequestMessages)
     * Deletes ALL requests                         (Requests)
     * Un-triages EVERY intake email                (IntakeEmails -> NeedsTriage)
         - clears the link to any request, any claim, and triage notes
         - keeps the email rows + their Graph ids so the triage queue
           repopulates immediately (no re-import / re-sweep needed)

   What it does NOT touch:
     * Projects                  (left exactly as-is)
     * MailboxSyncStates         (the delta cursor — left as-is)
     * Anything else in the DB

   How to run (from a machine that can reach the SQL server):
     sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -G ^
            -i reset-triage.sql
       (-G = Entra auth; or use -U <user> -P <pwd> for SQL auth)

   Safe to run repeatedly. Everything is wrapped in one transaction, so it
   either all applies or none of it does.

   NOTE on the mailbox itself: this only resets the database. Any emails that
   were physically moved into "Requests/REQ-xxxx" folders in projects@ stay
   where they are in Outlook — drag them back to the Inbox if you want a clean
   physical inbox too (and you can delete the leftover REQ-xxxx folders). Most
   emails should still be in the Inbox since the moves were failing.
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;   -- roll the whole thing back automatically on any error

BEGIN TRANSACTION;

    /* ---- Before snapshot ------------------------------------------------- */
    PRINT '--- BEFORE ---';
    SELECT
        (SELECT COUNT(*) FROM dbo.Requests)        AS Requests,
        (SELECT COUNT(*) FROM dbo.RequestMessages) AS RequestMessages,
        (SELECT COUNT(*) FROM dbo.IntakeEmails)    AS IntakeEmails,
        (SELECT COUNT(*) FROM dbo.IntakeEmails WHERE Status <> 0) AS NonTriageEmails,
        (SELECT COUNT(*) FROM dbo.Projects)        AS Projects_Untouched;

    /* ---- 1. Remove request conversation messages ------------------------- */
    DELETE FROM dbo.RequestMessages;

    /* ---- 2. Remove the requests themselves (projects are NOT touched) ----- */
    DELETE FROM dbo.Requests;

    /* ---- 3. Send every intake email back to the triage queue -------------- */
    /*        Status 0 = NeedsTriage (see IntakeStatus enum).                  */
    UPDATE dbo.IntakeEmails
    SET Status          = 0,      -- NeedsTriage
        LinkedRequestId = NULL,
        ClaimedByEmail  = NULL,
        ClaimedAt       = NULL,
        Notes           = NULL;

    /* ---- After snapshot -------------------------------------------------- */
    PRINT '--- AFTER ---';
    SELECT
        (SELECT COUNT(*) FROM dbo.Requests)        AS Requests,
        (SELECT COUNT(*) FROM dbo.RequestMessages) AS RequestMessages,
        (SELECT COUNT(*) FROM dbo.IntakeEmails)    AS IntakeEmails,
        (SELECT COUNT(*) FROM dbo.IntakeEmails WHERE Status <> 0) AS NonTriageEmails,
        (SELECT COUNT(*) FROM dbo.Projects)        AS Projects_Untouched;

COMMIT TRANSACTION;
PRINT 'Reset complete. All intake emails are back at NeedsTriage; no requests remain.';


/* =============================================================================
   OPTIONAL — FULL RE-IMPORT instead of a soft reset
   -----------------------------------------------------------------------------
   Use this ONLY if you also want the worker to re-read the Inbox from scratch
   (e.g. after dragging moved emails back into the Inbox so their Graph ids are
   refreshed). It deletes the intake rows and rewinds the delta cursor so the
   next sweep re-imports every current Inbox message fresh.

   To use it: comment out section 3 above, then uncomment this block.

   -- DELETE FROM dbo.IntakeEmails;
   -- UPDATE dbo.MailboxSyncStates
   --   SET DeltaLink = NULL, BacklogImported = 0, LastSyncedAt = NULL;
   -- Then restart func-jpms-worker-prod (or wait for the delta-sweep timer)
   -- to trigger a full re-import.
   ============================================================================= */
