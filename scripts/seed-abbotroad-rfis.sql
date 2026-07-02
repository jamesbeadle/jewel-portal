/* ============================================================================
   Seed: 17a Abbot Road — RFI register (8 RFIs)
   ----------------------------------------------------------------------------
   Loads the live RFI register (source: "RFI Register - 17a Abbot Road,
   Guildford GU1 3TA — Phase 2", Jewel BB RFI-REG-001, maintained by
   James Clark, issued 25/06/2026) into dbo.Requests.

   Only the eight formally numbered RFIs (RFI-01..RFI-08 in the register)
   are seeded; "Email Ref" rows are intentionally excluded.

   Safe to re-run: each RFI is only inserted if a request with the same
   Reference does not already exist for the project (NOT EXISTS guard).

   Mapping (register column -> dbo.Requests column):
     RFI No.                    -> Reference          (RFI-01 -> RFI-001)
     Subject                    -> Title
     Notes                      -> Description
     Date Issued                -> RaisedAt
     Response Due               -> ResponseDue
     Response Date              -> RespondedAt
     Raised To                  -> RaisedTo           (Design Team throughout)
     Drawing / Detail Ref       -> DrawingRef
     Related Drawing / Spec     -> RelatedDrawingSpec
     LAA RESPONSE - 26.06.2026  -> ResponseText
     Status                     -> Status             (Awaiting Response = 0,
                                                       Closed = 4)
   Fixed values:
     Kind             = 0      (RFI)
     Value            = NULL   (RFIs carry no monetary value)
     RaisedByEmail    = nigel.reilly@jewelgroup.co.uk
     ImpliesVariation = 0
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @RaisedBy  NVARCHAR(256) = N'nigel.reilly@jewelgroup.co.uk';
DECLARE @ProjectId NVARCHAR(64)  = N'4ec1ad1ca3a440c69f32f46f73aea005';

/* --- Verify the project exists --------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
BEGIN
    RAISERROR(N'Project 4ec1ad1ca3a440c69f32f46f73aea005 (17a Abbot Road) not found in dbo.Projects. Seed aborted.', 16, 1);
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
    RelatedDrawingSpec  NVARCHAR(512) NULL,
    ResponseText        NVARCHAR(2048) NULL
);

INSERT INTO @Seed
    (Ord, Reference, Title, Description, Status, RaisedAt, ResponseDue, RespondedAt, RaisedTo, DrawingRef, RelatedDrawingSpec, ResponseText)
VALUES
( 1, N'RFI-001', N'60-Minute Fire Protection to Structural Steel',
     N'Installation of joisting & framework to FF. Contract clause 2.11 / 2.20.6.',
     4, '2026-04-21T00:00:00+00:00', '2026-04-28T00:00:00+00:00', '2026-04-24T00:00:00+00:00',
     N'Design Team', N'A1986_140F / A1986_140D / 10313', N'Drawing Refs: A1986_140F / A1986_140D / 10313',
     NULL),

( 2, N'RFI-002', N'Rainwater Chains — Specification Clarification',
     N'Following review of the architectural drawings - specification for rainwater chains required. Roofing & Drainage works. Contract clause 2.11 / 2.20.6.',
     0, '2026-05-11T00:00:00+00:00', '2026-05-18T00:00:00+00:00', NULL,
     N'Design Team', N'A1986_120D / A1986_130H', N'Drawing A1986_120D / Drawing A1986_130H',
     N'The rain chain specification will be confirmed in due course. In the meeting with Client, Jewel and LAA on 25.06.2026 it was confirmed that this was not affecting the critical path. LAA requested that the critical path items were considered by Jewel in the first instance.'),

( 3, N'RFI-003', N'Pergola — Design, Fixing and Interface',
     N'Please confirm the required pergola design and structural fixing interface. Structural Works at roof level. Contract clause 2.11 / 2.20.6.',
     0, '2026-05-11T00:00:00+00:00', '2026-05-18T00:00:00+00:00', NULL,
     N'Design Team', N'A1986_111D / A1986_140F / A1986_141E', N'A1986_111D / A1986_140F / A1986_141E',
     N'This is currently in abeyance. The client has requested that the proposed fixed timber louvres were substituted for an operable canvas blind canopy structure. PR had relayed details to Socotec Building Control who had requested that the blinds were automated with a solar/overheating sensor. LP confirmed that the company could not offer this as a solution. PR requested what Socotec required, who requested that an overheating analysis was carried out if the substitution was to be agreed. PR to liaise with Socotec further.'),

( 4, N'RFI-004', N'JK Floor Heating Build-up',
     N'Please confirm the required JK Floor Heating build-up. Internal works and floor build up. Contract clause 2.11 / 2.20.6.',
     0, '2026-05-11T00:00:00+00:00', '2026-05-18T00:00:00+00:00', NULL,
     N'Design Team', N'A1986_110G / A1986_140F', N'Drawings A1986_110G and A1986_140F',
     N'The flooring specification has been confirmed on the revised drawing issue from 15.06.2026.'),

( 5, N'RFI-005', N'Sika Sarnafil Membrane Type & Warranty',
     N'Roof covering - Sika Sarnafil membrane type and warranty requirements confirmed.',
     4, '2026-05-29T00:00:00+00:00', '2026-06-05T00:00:00+00:00', '2026-05-29T00:00:00+00:00',
     N'Design Team', NULL, NULL,
     N'SIKA Sarnafil is no longer to be used for the single ply membrane, as per Jewel''s request to substitute the specification to a FLEXIPROOF product. Providing that the 25 year warranty was granted, both the client and LAA confirmed this was acceptable on 15.06.2026.'),

( 6, N'RFI-006', N'Lead Valley Gutter (West Elevation) — Existing Construction',
     N'Roofing - lead valley gutter detail at west elevation.',
     4, '2026-06-11T00:00:00+00:00', '2026-06-18T00:00:00+00:00', '2026-06-17T00:00:00+00:00',
     N'Design Team', N'Dwg 1986_111 Rev D / Dwg 1986_140 Rev F', N'Dwg 1986_111 Rev D / Dwg 1986_140 Rev F',
     N'Lead valley gutter to be completed as per our detail drawings issued on 15.06.2026. Dorking Roofing SIKA approval status N/A now that the client and LAA have accepted Jewel''s request for substitution to FLEXIPROOF. Confirmed repair to existing hip tiles on North Elevation only and existing ridge tiles closest to proposed extension.'),

( 7, N'RFI-007', N'Variation 04 - Scope of Works Confirmation',
     N'Decorations - scope of works confirmation for Variation 04 required.',
     0, '2026-06-25T00:00:00+00:00', '2026-07-02T00:00:00+00:00', NULL,
     N'Design Team', NULL, NULL,
     N'RFI 07 TBC in a separate email relating to Variation 04.'),

( 8, N'RFI-008', N'Access Arrangements for IQ Glass',
     N'Note IQ Glass PSC is booked for 21st July. Cladding & Ground Works - access arrangements required.',
     0, '2026-06-25T00:00:00+00:00', '2026-07-02T00:00:00+00:00', NULL,
     N'Design Team', N'Scaffolding', N'Scaffolding',
     N'TBC. PR could not get in contact with the IQ Glass project manager Hardik Gala over the phone. PR has sent an URGENT email on 26.06.2026 to Hardik Gala requesting earlier clarity on access. Client and Jewel copied in for reference.');

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
    s.ResponseText,
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

PRINT N'Abbot Road RFIs inserted this run: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* --- Summary --------------------------------------------------------------- */
SELECT
    Total  = COUNT(*),
    [Open] = SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END),
    Closed = SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END)
FROM dbo.Requests
WHERE ProjectId = @ProjectId
  AND Kind = 0;
