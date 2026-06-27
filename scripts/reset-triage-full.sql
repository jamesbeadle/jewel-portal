/* =============================================================================
   reset-triage-full.sql  —  HARD reset / full re-import of mailbox triage
   -----------------------------------------------------------------------------
   Use this (instead of the soft reset-triage.sql) when the physical mailbox has
   changed underneath the database — e.g. you dragged the moved emails back into
   the Inbox and deleted the REQ-xxxx folders. Those drags give the messages NEW
   Graph ids, so the stored IntakeEmails rows now hold STALE ids. A soft reset
   would leave that drift in place; this script wipes the intake rows and rewinds
   the delta cursor so the next sweep re-reads the whole Inbox FRESH with correct
   ids — realigning the triage count to whatever is actually in the Inbox now.

   What it does:
     * Deletes ALL request conversation messages   (RequestMessages)
     * Deletes ALL requests                         (Requests)
     * Deletes ALL intake emails                    (IntakeEmails)   <-- full wipe
     * Rewinds the mailbox sync cursor              (MailboxSyncStates)
         - DeltaLink      = NULL  -> next sweep does a full backlog enumeration
         - BacklogImported = 0
         - LastSyncedAt    = NULL

   What it does NOT touch:
     * Projects                  (left exactly as-is)
     * The mailbox itself        (Outlook contents are untouched)

   AFTER running, trigger a re-import one of two ways:
     1. Restart func-jpms-worker-prod, OR
     2. Just wait — the delta-sweep timer fires every 5 minutes.
   The worker will re-enumerate the Inbox and repopulate IntakeEmails. The triage
   screen should then show one row per current Inbox email (~1252).

   How to run (SQL auth; omit -P to be prompted securely for the password):
     sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms ^
            -U jpmsadmin -i scripts\reset-triage-full.sql

   Safe to run repeatedly. Everything is wrapped in one transaction, so it
   either all applies or none of it does.
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;   -- roll the whole thing back automatically on any error

BEGIN TRANSACTION;

    /* ---- Before snapshot ------------------------------------------------- */
    PRINT '--- BEFORE ---';
    SELECT
        (SELECT COUNT(*) FROM dbo.Requests)             AS Requests,
        (SELECT COUNT(*) FROM dbo.RequestMessages)      AS RequestMessages,
        (SELECT COUNT(*) FROM dbo.IntakeEmails)         AS IntakeEmails,
        (SELECT COUNT(*) FROM dbo.MailboxSyncStates)    AS SyncStates,
        (SELECT COUNT(*) FROM dbo.Projects)             AS Projects_Untouched;

    /* ---- 1. Remove request conversation messages ------------------------- */
    DELETE FROM dbo.RequestMessages;

    /* ---- 2. Remove the requests themselves (projects are NOT touched) ----- */
    DELETE FROM dbo.Requests;

    /* ---- 3. Remove ALL intake emails (full wipe) ------------------------- */
    DELETE FROM dbo.IntakeEmails;

    /* ---- 4. Rewind the delta cursor so the next sweep re-imports fresh ---- */
    UPDATE dbo.MailboxSyncStates
    SET DeltaLink       = NULL,   -- null cursor -> full Inbox enumeration
        BacklogImported = 0,
        LastSyncedAt    = NULL;

    /* ---- After snapshot -------------------------------------------------- */
    PRINT '--- AFTER ---';
    SELECT
        (SELECT COUNT(*) FROM dbo.Requests)             AS Requests,
        (SELECT COUNT(*) FROM dbo.RequestMessages)      AS RequestMessages,
        (SELECT COUNT(*) FROM dbo.IntakeEmails)         AS IntakeEmails,
        (SELECT COUNT(*) FROM dbo.MailboxSyncStates)    AS SyncStates,
        (SELECT COUNT(*) FROM dbo.Projects)             AS Projects_Untouched;

COMMIT TRANSACTION;
PRINT 'Hard reset complete. Intake wiped and delta cursor rewound.';
PRINT 'Restart func-jpms-worker-prod (or wait up to 5 min) to re-import the Inbox.';
