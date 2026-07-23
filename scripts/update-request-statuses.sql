/* ============================================================================
   Consolidate request statuses — data update for the four-status model
   ----------------------------------------------------------------------------
   The RequestStatus enum was consolidated to four statuses (2026-07-23):

     Needs action   = 0   (the value legacy Open rows already hold)
     Open           = 1   (the value legacy Awaiting-response rows already hold)
     Closed         = 4   (unchanged)
     Needs variation = 6  (new)

   Rows stored as 0 or 1 need NO change — they keep their values and simply
   take the new meaning (0: "Open" -> "Needs action", 1: "Awaiting response"
   -> "Open").

   This script only retires the legacy Approved(2) / Rejected(3) / Responded(5)
   rows: they all represented finished conversations, so they become Closed(4),
   taking the most truthful close date already on the row (an existing close
   date, else the response date, else the issue date, else the raised stamp).

   Touches ONLY dbo.Requests, ONLY rows with Status IN (2,3,5), ONLY the
   Status and ClosedAt columns (ClosedAt only where currently NULL).

   Idempotent: a second run (or the matching EF migration
   20260723090000_ConsolidateRequestStatuses running at API startup after
   this) matches zero rows and changes nothing.

   Run:
     sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
            -i scripts/update-request-statuses.sql
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* --- Before: what will change ---------------------------------------------- */
PRINT 'Rows per status BEFORE:';
SELECT [Status], COUNT(*) AS [Rows]
FROM [Requests]
GROUP BY [Status]
ORDER BY [Status];

BEGIN TRANSACTION;

UPDATE [Requests]
SET [ClosedAt] = COALESCE([ClosedAt], [RespondedAt], [IssuedAt], [RaisedAt]),
    [Status]   = 4
WHERE [Status] IN (2, 3, 5);

PRINT CONCAT('Rows moved to Closed: ', @@ROWCOUNT);

COMMIT TRANSACTION;

/* --- After: confirm only 0 / 1 / 4 (and any 6) remain ---------------------- */
PRINT 'Rows per status AFTER:';
SELECT [Status], COUNT(*) AS [Rows]
FROM [Requests]
GROUP BY [Status]
ORDER BY [Status];
