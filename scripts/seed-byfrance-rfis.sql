/* ============================================================================
   Seed: By France — RFI register (48 RFIs)
   ----------------------------------------------------------------------------
   Loads the live RFI register (source: "RFI Register - By France", maintained by
   James Clark) into dbo.Requests so a fresh JPMS database starts level with the
   live system.

   Safe to re-run: each RFI is only inserted if a request with the same
   Reference does not already exist for the By France project (NOT EXISTS guard).

   Mapping (register column -> dbo.Requests column):
     RFI No.               -> Reference            (e.g. RFI-001)
     Subject               -> Title
     Notes                 -> Description           (full text from the source
                                                     spreadsheet)
     Date Issued           -> RaisedAt
     Response Due          -> ResponseDue
     Response Date         -> RespondedAt           (populated for 32 closed RFIs)
     Raised To             -> RaisedTo              (PLG Architects throughout)
     Drawing / Detail Ref  -> DrawingRef
     Related Drawing / Spec -> RelatedDrawingSpec
     Status                -> Status                (Open = 0, Closed = 4)

   Fixed values:
     Kind             = 0      (RFI)
     Value            = NULL   (RFIs carry no monetary value)
     RaisedByEmail    = nigel.reilly@jewelgroup.co.uk
     ImpliesVariation = 0
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @RaisedBy NVARCHAR(256) = N'nigel.reilly@jewelgroup.co.uk';

/* --- Resolve the By France project by name -------------------------------- */
DECLARE @ProjectId NVARCHAR(64);

SELECT @ProjectId = ProjectId
FROM dbo.Projects
WHERE Name LIKE N'%By France%';

IF @ProjectId IS NULL
BEGIN
    RAISERROR(N'No project matching "%%By France%%" found in dbo.Projects. Seed aborted.', 16, 1);
    RETURN;
END

IF (SELECT COUNT(*) FROM dbo.Projects WHERE Name LIKE N'%By France%') > 1
BEGIN
    RAISERROR(N'More than one project matches "%%By France%%". Set @ProjectId explicitly and re-run.', 16, 1);
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
( 1, N'RFI-001', N'Drainage Runs & Thames Water Agreement', N'Please confirm direction of drainage runs and Thames Water agreement requirements.', 4, '2024-05-01T00:00:00+00:00', '2024-05-08T00:00:00+00:00', '2025-02-12T00:00:00+00:00', N'PLG Architects', NULL, NULL),
( 2, N'RFI-002', N'Tree Protection Works', N'Tree protection report and method statement required.', 4, '2024-05-01T00:00:00+00:00', '2024-05-08T00:00:00+00:00', '2024-06-06T00:00:00+00:00', N'PLG Architects', NULL, NULL),
( 3, N'RFI-003', N'Face Brickwork Selection', N'Please confirm face brickwork selection and specification.', 4, '2024-05-01T00:00:00+00:00', '2024-05-08T00:00:00+00:00', '2025-03-31T00:00:00+00:00', N'PLG Architects', NULL, NULL),
( 4, N'RFI-004', N'Temporary Works Design - Demolition', N'', 4, '2024-05-03T00:00:00+00:00', '2024-05-10T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
( 5, N'RFI-005', N'Trial Pit Locations', N'Trial pit locations to confirm existing foundation depths.', 4, '2024-05-30T00:00:00+00:00', '2024-06-06T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
( 6, N'RFI-006', N'Surface Water Drainage - Rev D GF DRAINAGE', N'Relates to drawing - PRO-064 GF Drainage Rev D.', 4, '2024-12-12T00:00:00+00:00', '2024-12-19T00:00:00+00:00', '2025-02-12T00:00:00+00:00', N'PLG Architects', NULL, NULL),
( 7, N'RFI-007', N'Pond Detail - PRO-064-(WD)-P-700', N'Excavation, structural and waterproofing detail for pond.', 4, '2024-12-19T00:00:00+00:00', '2024-12-26T00:00:00+00:00', '2026-06-22T00:00:00+00:00', N'PLG Architects', N'PRO-064 / P-700', N'PRO-064 / P-700'),
( 8, N'RFI-008', N'SE - Steel Connection Details', N'SE to provide connection details for structural steel.', 4, '2025-01-07T00:00:00+00:00', '2025-01-14T00:00:00+00:00', '2025-01-30T00:00:00+00:00', N'PLG Architects', NULL, NULL),
( 9, N'RFI-009', N'Dimensions to the Drainage Plan', N'Dimensions of the pop up drain locations on drainage plan.', 4, '2025-01-20T00:00:00+00:00', '2025-01-27T00:00:00+00:00', '2025-02-12T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(10, N'RFI-010', N'Drainage Strategy - Tree Protection Zone', N'MC0476 Model1-CIV10 - drainage strategy within TPZ.', 4, '2025-01-30T00:00:00+00:00', '2025-02-06T00:00:00+00:00', '2025-02-12T00:00:00+00:00', N'PLG Architects', N'MC0476 Model1-CIV10', N'MC0476 Model1-CIV10'),
(11, N'RFI-011', N'Masonry Movement Joints', N'Please confirm location of masonry movement joints.', 4, '2025-03-07T00:00:00+00:00', '2025-03-14T00:00:00+00:00', '2025-03-12T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(12, N'RFI-012', N'Setout Dimensions for the Pond', N'Setout dimensions for the fish pond required.', 0, '2025-03-07T00:00:00+00:00', '2025-03-14T00:00:00+00:00', NULL, N'PLG Architects', N'PRO-064-(WD)-P-701 Fish Pond - REV F', NULL),
(13, N'RFI-013', N'Setout Dimensions for the Front Driveway', N'Dimension and setout for entrance driveway.', 4, '2025-03-07T00:00:00+00:00', '2025-03-14T00:00:00+00:00', '2026-06-25T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(14, N'RFI-014', N'Entrance Door Specification', N'Can you please provide the entrance door specification.', 0, '2025-03-13T00:00:00+00:00', '2025-03-20T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
(15, N'RFI-015', N'Truss Roof Detail - SD-15 rev 5', N'SE - SD-15 rev 5 truss roof connection detail.', 4, '2025-03-20T00:00:00+00:00', '2025-03-27T00:00:00+00:00', NULL, N'PLG Architects', N'SD-15 rev 5', N'SD-15 rev 5'),
(16, N'RFI-016', N'Internal Door Schedule - P-500 Rev A / P-004 Rev F', N'Internal door finishes, hardware and ironmongery schedule.', 4, '2025-03-25T00:00:00+00:00', '2025-04-01T00:00:00+00:00', NULL, N'PLG Architects', N'P-500 Rev A / P-004 Rev F', N'P-500 Rev A / P-004 Rev F'),
(17, N'RFI-017', N'Masonry Junction Detail - P-007 REV L / P-004 Rev F', N'Detail between face brickwork and internal blockwork junction.', 4, '2025-03-26T00:00:00+00:00', '2025-04-02T00:00:00+00:00', '2026-06-08T00:00:00+00:00', N'PLG Architects', N'P-007 REV L / P-004 Rev F', N'P-007 REV L / P-004 Rev F'),
(18, N'RFI-018', N'Suspended Ceiling & Wall Details', N'RFI relates to suspended ceiling and wall construction details.', 4, '2025-04-24T00:00:00+00:00', '2025-05-01T00:00:00+00:00', '2026-06-08T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(19, N'RFI-019', N'Damp Tray Details for the First Floor', N'Section detail for the damp tray at first floor level.', 4, '2025-04-24T00:00:00+00:00', '2025-05-01T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
(20, N'RFI-020', N'Electric, Heating & Drainage Floor Plans', N'REV T Electrical Plans issued June 2026', 4, '2025-05-08T00:00:00+00:00', '2025-05-15T00:00:00+00:00', '2026-06-08T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(21, N'RFI-021', N'SL-20 rev 10 - Double Joist Setout', N'Dimension for the double joist setout.', 4, '2025-06-03T00:00:00+00:00', '2025-06-10T00:00:00+00:00', NULL, N'PLG Architects', N'SL-20 rev 10', N'SL-20 rev 10'),
(22, N'RFI-022', N'Steel & Padstone Heights FF', N'PRO-064-(WD)-P-005 Rev D steel and padstone heights at first floor.', 4, '2025-06-20T00:00:00+00:00', '2025-06-27T00:00:00+00:00', NULL, N'PLG Architects', N'PRO-064 / P-005 Rev D', N'PRO-064 / P-005 Rev D'),
(23, N'RFI-023', N'Control 4 System - Electrics', N'Please confirm whether Control 4 system is included in scope.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
(24, N'RFI-024', N'Sanitary Ware Schedule - Bathroom Layouts', N'Confirmation of sanitary ware schedule and bathroom layouts.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', '2026-06-02T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(25, N'RFI-025', N'Specialist Equipment/Hoist Specification', N'Supplier of specialist hoist equipment and specification required.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
(26, N'RFI-026', N'Staircase Design', N'Design of the internal staircase and balustrade required.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', '2025-12-11T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(27, N'RFI-027', N'Tile Supplier', N'Tile supplier details required so procurement can be initiated.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', '2025-11-12T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(28, N'RFI-028', N'Kitchen & Utility Design & Plan', N'CAD design drawings from kitchen designer required.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', '2025-09-15T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(29, N'RFI-029', N'Entrance Gate Design & Specification', N'PRO-064-(WD)-P-700 Rev B - entrance gate design and specification.', 0, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', NULL, N'PLG Architects', N'PRO-064 / P-700 Rev B', N'PRO-064 / P-700 Rev B'),
(30, N'RFI-030', N'Bespoke Joinery Design', N'Omitted from the contract - bespoke joinery design required.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
(31, N'RFI-031', N'Floor Finishes', N'', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', '2026-04-24T00:00:00+00:00', N'PLG Architects', NULL, N'PRO-064-(WD)-P-015 Rev E PROPOSED FINISHES'),
(32, N'RFI-032', N'Landscape Plan - P-700 Rev B External Works', N'Landscape Plan - P-700 Rev B external works design and specification.', 0, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', NULL, N'PLG Architects', N'P-700 Rev B / P-700', NULL),
(33, N'RFI-033', N'Canopies P-703 REAR CANOPIES SKETCH', N'Omitted from the contract - rear canopy sketch and specification.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', '2026-04-23T00:00:00+00:00', N'PLG Architects', N'P-703', N'P-703'),
(34, N'RFI-034', N'Gazebo PS', N'Omitted from the contract - gazebo design and specification.', 4, '2025-07-10T00:00:00+00:00', '2025-07-17T00:00:00+00:00', '2026-04-23T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(35, N'RFI-035', N'Annex Kitchen & Utility - Design & Specification', N'Omitted from the contract - annex kitchen and utility design.', 4, '2025-08-21T00:00:00+00:00', '2025-08-28T00:00:00+00:00', '2025-10-07T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(36, N'RFI-036', N'Setout for Fixing Details - Ground Floor', N'', 4, '2026-01-09T00:00:00+00:00', '2026-01-16T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
(37, N'RFI-037', N'Finishes Schedule (paint, hardware, etc.)', N'', 4, '2026-03-05T00:00:00+00:00', '2026-03-12T00:00:00+00:00', '2026-05-05T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(38, N'RFI-038', N'Window Coverings PS', N'Omitted from the contract - window coverings specification.', 4, '2026-03-05T00:00:00+00:00', '2026-03-12T00:00:00+00:00', '2026-04-23T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(39, N'RFI-039', N'Ironmongery PS', N'Ironmongery to suit FR Doors - specification required.', 0, '2026-03-05T00:00:00+00:00', '2026-03-12T00:00:00+00:00', NULL, N'PLG Architects', NULL, NULL),
(40, N'RFI-040', N'Tile Layout Plan for Each Bathroom', N'The bathroom drawings and Thompson & Leigh quote define tile types and quantities, but tile setting out is indicative. Please provide a coordinated tile layout for each bathroom confirming final tile locations, heights, datum and setting-out lines, feature wall limits, trims, finishes, floor falls, tray and waste interfaces.', 4, '2026-04-28T00:00:00+00:00', '2026-05-05T00:00:00+00:00', '2026-05-15T00:00:00+00:00', N'PLG Architects', N'PRO-064-(WD)-P-201 Rev F / P-202 Rev H / P-203 Rev I / P-204 Rev G / P-205 Rev H / P-206 Rev H / P-207 Rev F / P-209 Rev D', N'Tile layouts / bathroom drawings / Thompson & Leigh quotation 09SQU00000426'),
(41, N'RFI-041', N'FF Curtain Track / Concealed Track Requirements', N'Omitted from the contract - first floor curtain track specification.', 4, '2026-05-12T00:00:00+00:00', '2026-05-19T00:00:00+00:00', '2026-06-04T00:00:00+00:00', N'PLG Architects', N'PRO-064-(WD)-P-008 Rev J', NULL),
(42, N'RFI-042', N'Ceiling Sections / RCP Alignment and Coffer Straight Edges', N'Updated drawings provided - ceiling section detail and RCP alignment.', 4, '2026-05-12T00:00:00+00:00', '2026-05-19T00:00:00+00:00', '2026-05-18T00:00:00+00:00', N'PLG Architects', N'PRO-064-(WD)-P-707 / PRO-064-(WD)-P-706 Rev G', N'PRO-064-(WD)-P-706 Rev H / PRO-064-(WD)-P-707 Rev A / PRO-064-(WD)-P-710 Ceiling details'),
(43, N'RFI-043', N'U/S of Flat Canopies at LGF Level - Board Material and Format', N'Cream eurocell soffit board confirmed.', 4, '2026-05-12T00:00:00+00:00', '2026-05-19T00:00:00+00:00', '2026-05-20T00:00:00+00:00', N'PLG Architects', N'Detail extract / mark-up', NULL),
(44, N'RFI-044', N'RWP Locations in Relation to External Lights at GF Level', N'', 4, '2026-05-12T00:00:00+00:00', '2026-05-19T00:00:00+00:00', '2026-06-04T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(45, N'RFI-045', N'External Taps - Quantity and Locations', N'Additional taps to the boundary to be costed separately.', 4, '2026-05-12T00:00:00+00:00', '2026-05-19T00:00:00+00:00', '2026-06-04T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(46, N'RFI-046', N'ACO Drains - Design and Specification', N'Detail issued but to be discussed on site.', 4, '2026-05-26T00:00:00+00:00', '2026-06-02T00:00:00+00:00', '2026-06-09T00:00:00+00:00', N'PLG Architects', NULL, NULL),
(47, N'RFI-047', N'Thomas & Leigh Tile Quote - Validity & Compliance', N'Email confirmation from PLG that quantities need to be verified.', 4, '2026-06-01T00:00:00+00:00', '2026-06-08T00:00:00+00:00', '2026-06-01T00:00:00+00:00', N'PLG Architects', N'T&L Quote 09SQU00000426', NULL),
(48, N'RFI-048', N'Ground Floor RCP — Ceiling Heights and Drawing Coordination', N'PRO-064-(WD)-P-706 Rev J Ground Floor RCP issued.', 4, '2026-06-02T00:00:00+00:00', '2026-06-09T00:00:00+00:00', '2026-06-06T00:00:00+00:00', N'PLG Architects', N'Ground Floor Reflected Ceiling Plan PRO-064-(WD)-P-706', NULL);
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

PRINT N'By France RFIs inserted this run: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* --- Summary --------------------------------------------------------------- */
SELECT
    Total  = COUNT(*),
    [Open] = SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END),
    Closed = SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END)
FROM dbo.Requests
WHERE ProjectId = @ProjectId
  AND Kind = 0;
