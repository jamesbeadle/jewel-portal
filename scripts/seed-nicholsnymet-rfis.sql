/* ============================================================================
   Seed: Nichols Nymet — RFI register (14 RFIs)
   ----------------------------------------------------------------------------
   Loads the live RFI register (source: "RFI Register - Nichols Nymet,
   Woodhouse Lane, Surrey, Dorking RH5", Jewel BB RFI-REG, maintained by
   James Clark, issued 17/06/2026) into dbo.Requests.

   Safe to re-run: each RFI is only inserted if a request with the same
   Reference does not already exist for the project (NOT EXISTS guard).

   Mapping (register column -> dbo.Requests column):
     RFI No.                -> Reference
     Subject                -> Title
     Notes                  -> Description
     Date Issued            -> RaisedAt
     Response Due           -> ResponseDue
     Response Date          -> RespondedAt
     Raised To              -> RaisedTo   (Malcolm Lelliot - MVL Architects)
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
DECLARE @ProjectId NVARCHAR(64)  = N'c16a737d8e1347f28917183b77360f1d';

/* --- Verify the project exists --------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
BEGIN
    RAISERROR(N'Project c16a737d8e1347f28917183b77360f1d (Nichols Nymet) not found in dbo.Projects. Seed aborted.', 16, 1);
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
( 1, N'RFI-001', N'Existing Lounge Steel',
     N'250mm depth with only 175mm floor joists. Raised on site and confirmed by SE.',
     4, '2025-10-24T00:00:00+00:00', '2025-10-31T00:00:00+00:00', '2025-10-31T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-01', N'Existing Lounge Steel'),

( 2, N'RFI-002', N'Structure - Existing Rainwater Tank',
     N'Location of rainwater tank in relation to retaining wall foundation.',
     4, '2025-10-28T00:00:00+00:00', '2025-11-04T00:00:00+00:00', '2025-10-28T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-02', N'Structure - Existing Rainwater Tank'),

( 3, N'RFI-003', N'M&E Service Routes - L-1730-602E / L-1730-603E',
     N'Stack and extract duct routes throughout the building. NB Lighting plan to relate to M&E layout.',
     4, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', '2026-01-26T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'L-1730-602E / L-1730-603E', N'L-1730-602E Proposed Ground Floor Heating & Plumbing / L-1730-603E'),

( 4, N'RFI-004', N'Electrics - L-1730-600E / L-1730-601E',
     N'NB Lighting plan finalised and to relate to M&E layout.',
     4, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', '2026-01-26T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'L-1730-600E / L-1730-601E', N'L-1730-600E Proposed Ground Floor Electrical / L-1730-601E'),

( 5, N'RFI-005', N'Tile Layout - GF Internal/External',
     N'Tile layout for ground floor internal/external areas required.',
     0, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', NULL,
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-05', N'Tile Layout - GF Internal/External'),

( 6, N'RFI-006', N'CP Hart - Bathroom Layout/Design',
     N'Potential impact to service routes if sanitary ware positions change.',
     4, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', '2026-05-14T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-06', N'CP Hart Bathroom Layout/Design'),

( 7, N'RFI-007', N'Fluid Glass FGP382 Rev.B - Survey Drawings',
     N'Fluid Glass survey drawings and specification confirmed.',
     4, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', '2026-04-17T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'FGP382 Rev.B', N'Fluid Glass FGP382 Rev.B Survey Drawings'),

( 8, N'RFI-008', N'Tom Howley Survey Drawings',
     N'Tom Howley survey drawings signed off for fabrication.',
     4, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', '2026-01-08T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-08', N'Tom Howley Survey Drawings'),

( 9, N'RFI-009', N'CP Hart - 1st & 2nd Fix Sanitary Ware Delivery',
     N'1st fix items W/C 2nd March. 2nd fix items W/C confirmed.',
     4, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', '2025-11-10T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-09', N'CP Hart Sanitary Ware Delivery'),

(10, N'RFI-010', N'Glass Balustrade - Survey & Specification',
     N'Glass balustrade survey and specification confirmed.',
     4, '2025-11-03T00:00:00+00:00', '2025-11-10T00:00:00+00:00', '2026-01-06T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-10', N'Glass Balustrade Survey & Specification'),

(11, N'RFI-011', N'Fluid Glass - Detail D-01 & 05 - A-A Typical',
     N'Please provide updated single/twin track perimeter frame details.',
     4, '2026-01-08T00:00:00+00:00', '2026-01-15T00:00:00+00:00', '2026-05-11T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'SSA 23-040-100_P10 / MVL L-1730-400G', N'SSA 23-040-100_P10; MVL L-1730-400G Details'),

(12, N'RFI-012', N'Setting Out Height - WC Window',
     N'Please provide setting out height for the WC window.',
     4, '2026-01-08T00:00:00+00:00', '2026-01-15T00:00:00+00:00', '2026-01-26T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Buildertrend RFI-012', N'WC window setting out height'),

(13, N'RFI-013', N'Fleece-Backed EPDM: Sheet Layout & Seam Joints',
     N'Confirm whether to proceed with RubberBond fleece-backed EPDM and seam joint detail.',
     4, '2026-04-22T00:00:00+00:00', '2026-04-29T00:00:00+00:00', '2026-05-11T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'RubberBond FR EPDM / Flat Roof Plan', N'Manufacturer product literature; RubberBond FR EPDM'),

(14, N'RFI-014', N'Roof Insulation Fixing Method: Mechanical Fix or Adhered',
     N'Confirm approved fixing method for roof insulation. Closed 27/05/2026.',
     4, '2026-05-26T00:00:00+00:00', '2026-06-02T00:00:00+00:00', '2026-05-26T00:00:00+00:00',
     N'Malcolm Lelliot - MVL Architects', N'Radmat ProTherm PIR ADHERED / roof build-up', N'Radmat ProTherm PIR ADHERED Product Data Sheet');

/* --- Insert any RFI not already present (idempotent) ----------------------- */
DECLARE @Base INT = ISNULL((SELECT MAX(Number) FROM dbo.Requests), 0);

INSERT INTO dbo.Requests
    (RequestId, ProjectId, Kind, Reference, Title, Description, Status, Value,
     RaisedByEmail, RaisedAt, RespondedAt, ResponseText, RespondedByEmail,
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

PRINT N'Nichols Nymet RFIs inserted this run: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* --- Summary --------------------------------------------------------------- */
SELECT
    Total  = COUNT(*),
    [Open] = SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END),
    Closed = SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END)
FROM dbo.Requests
WHERE ProjectId = @ProjectId
  AND Kind = 0;
