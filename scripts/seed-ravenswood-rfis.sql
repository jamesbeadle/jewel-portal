/* ============================================================================
   Seed: 64 Ravenswood Ave — RFI register (1 RFI)
   ----------------------------------------------------------------------------
   Loads the live RFI register (source: "RFI Register - 64 Ravenswood Avenue,
   Surbiton, KT6 7NP", Jewel BB RFI-REG, maintained by James Clark,
   issued 17/06/2026) into dbo.Requests.

   Safe to re-run: each RFI is only inserted if a request with the same
   Reference does not already exist for the project (NOT EXISTS guard).

   Mapping (register column -> dbo.Requests column):
     RFI No.                -> Reference
     Subject                -> Title
     Notes                  -> Description
     Date Issued            -> RaisedAt
     Response Due           -> ResponseDue
     Response Date          -> RespondedAt
     Raised To              -> RaisedTo
     Drawing / Detail Ref   -> DrawingRef
     Related Drawing / Spec -> RelatedDrawingSpec
     Status                 -> Status     (Open = 0, Closed = 4)

   Fixed values:
     Kind             = 0      (RFI)
     Value            = NULL   (RFIs carry no monetary value)
     RaisedByEmail    = nigel.reilly@jewelgroup.co.uk
     ImpliesVariation = 0
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @RaisedBy  NVARCHAR(256) = N'nigel.reilly@jewelgroup.co.uk';
DECLARE @ProjectId NVARCHAR(64)  = N'3bf6dcfa81764a248138fb5fd357aa84';

/* --- Verify the project exists --------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
BEGIN
    RAISERROR(N'Project 3bf6dcfa81764a248138fb5fd357aa84 (64 Ravenswood Ave) not found in dbo.Projects. Seed aborted.', 16, 1);
    RETURN;
END

/* --- Register rows --------------------------------------------------------- */
DECLARE @Seed TABLE (
    Ord                 INT,
    Reference           NVARCHAR(64),
    Title               NVARCHAR(256),
    Description         NVARCHAR(2048),
    Status              INT,
    RaisedAt            DATETIMEOFFSET,
    ResponseDue         DATETIMEOFFSET NULL,
    RespondedAt         DATETIMEOFFSET NULL,
    RaisedTo            NVARCHAR(256) NULL,
    DrawingRef          NVARCHAR(256) NULL,
    RelatedDrawingSpec  NVARCHAR(512) NULL
);

INSERT INTO @Seed
    (Ord, Reference, Title, Description, Status, RaisedAt, ResponseDue, RespondedAt, RaisedTo, DrawingRef, RelatedDrawingSpec)
VALUES
( 1, N'RFI-001', N'Architect Plans to Align with Structural Engineer Drawings',
     N'Architect''s plans to be reviewed and updated to align with structural engineer''s drawings.',
     4, '2026-05-27T00:00:00+00:00', '2026-06-03T00:00:00+00:00', NULL,
     N'Architect', N'Architect drawings / structural engineer drawings', N'Architect drawings / structural engineer drawings');

/* --- Insert any RFI not already present (idempotent) ----------------------- */
DECLARE @Base INT = ISNULL((SELECT MAX(Number) FROM dbo.Requests), 0);

INSERT INTO dbo.Requests
    (RequestId, ProjectId, Kind, Reference, Title, Description, Status, Value,
     RaisedByEmail, RaisedAt, IssuedAt, RespondedAt, ResponseText, RespondedByEmail,
     ImpliesVariation, RaisedTo, DrawingRef, ResponseDue, RelatedDrawingSpec,
     InternalNotes, ClientNotes, Number, MailboxFolderId)
SELECT
    LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')),   -- 32-char compact GUID, matches app format
    @ProjectId,
    0,                                                           -- Kind = RFI
    s.Reference,
    s.Title,
    s.Description,
    s.Status,
    NULL,                                                        -- Value
    @RaisedBy,
    s.RaisedAt,
    s.RaisedAt,                                                  -- IssuedAt: the register's issue date (same as the "Date Issued" column)
    s.RespondedAt,
    NULL,                                                        -- ResponseText
    NULL,                                                        -- RespondedByEmail
    0,                                                           -- ImpliesVariation
    s.RaisedTo,
    s.DrawingRef,
    s.ResponseDue,
    s.RelatedDrawingSpec,
    NULL,                                                        -- InternalNotes
    NULL,                                                        -- ClientNotes
    @Base + s.Ord,                                               -- sequential Number after current max
    NULL                                                         -- MailboxFolderId
FROM @Seed s
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Requests r
    WHERE r.ProjectId = @ProjectId
      AND r.Reference = s.Reference
);

PRINT N'64 Ravenswood Ave RFIs inserted this run: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* --- Summary --------------------------------------------------------------- */
SELECT
    Total  = COUNT(*),
    [Open] = SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END),
    Closed = SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END)
FROM dbo.Requests
WHERE ProjectId = @ProjectId
  AND Kind = 0;
