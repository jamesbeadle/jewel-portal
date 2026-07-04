/* ============================================================================
   Fix + reseed: 17a Abbot Road — RFI-009 (Velfac) and RFI-010..RFI-029
   ----------------------------------------------------------------------------
   SUPERSEDES scripts/seed-abbotroad-rfis-009-028.sql — do not run that script.

   What the first seed got wrong:
     - RFI-009 already existed (Velfac Window Position, created from the
       mailbox intake of Nigel's 02.07.2026 14:09 email). The seed skipped its
       request row but still attached the radiator items to it.
     - With Velfac correctly holding RFI-009, every seeded reference was off
       by one: RFI-010 held the staircase-walls content that belongs to
       RFI-011, and so on. The radiator RFI's request row was never inserted.

   What this script does (all guarded, safe to re-run):
     1. RFI-009 (Velfac): removes the two misattached radiator items, fills
        the official form sections from the Velfac email (only where NULL),
        and inserts the three Velfac items if the request has no items.
     2. Deletes the 19 off-by-one requests (RFI-010..RFI-028) and their items
        — matched by reference AND the wrong title, so corrected rows are
        never touched on a re-run.
     3. Reseeds the twenty email RFIs as RFI-010..RFI-029 (radiators = 010 …
        rear cladding = 029), requests + official form + itemised queries.
        Items are scoped to requests inserted THIS RUN via an OUTPUT capture,
        fixing the flaw that caused the misattachment.

   Conventions unchanged from the original seed:
     RaisedAt 2026-07-02, ResponseDue 2026-07-09, Status 0 (Open),
     Kind 0 (RFI), RaisedTo Design Team, RaisedByEmail nigel.reilly@,
     ImpliesVariation = 1 for new / out-of-scope items
     (now RFI-018/019/021/023/024/025).
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @RaisedBy  NVARCHAR(256)   = N'nigel.reilly@jewelgroup.co.uk';
DECLARE @ProjectId NVARCHAR(64)    = N'4ec1ad1ca3a440c69f32f46f73aea005';
DECLARE @RaisedAt  DATETIMEOFFSET  = '2026-07-02T00:00:00+00:00';
DECLARE @Due       DATETIMEOFFSET  = '2026-07-09T00:00:00+00:00';

/* --- Verify the project exists --------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
BEGIN
    RAISERROR(N'Project 4ec1ad1ca3a440c69f32f46f73aea005 (17a Abbot Road) not found in dbo.Projects. Seed aborted.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

/* ============================================================================
   1. RFI-009 — Velfac Window Position (existing request)
   ============================================================================ */
DECLARE @Rfi009 NVARCHAR(64) =
    (SELECT TOP 1 RequestId FROM dbo.Requests
     WHERE ProjectId = @ProjectId AND Reference = N'RFI-009');

IF @Rfi009 IS NULL
BEGIN
    RAISERROR(N'RFI-009 (Velfac Window Position) not found for the project. Fix aborted.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

/* 1a. Remove the two radiator items the first seed misattached. Matched by
       content so genuine Velfac items are never touched. */
DELETE FROM dbo.RequestItems
WHERE RequestId = @Rfi009
  AND (MemberArea = N'First Floor — sliding door / radiator and TV bracket'
       OR MemberArea = N'Ground Floor — radiator adjacent to staircase');

PRINT N'RFI-009 misattached radiator items removed: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* 1b. Official form sections from the Velfac email (only fill where empty). */
UPDATE dbo.Requests
SET BasisOfQueries = COALESCE(BasisOfQueries,
        N'Architect''s email and plans proposing the rear elevation Velfac window at 1050mm from FFL; contractor''s email of 02.07.2026 (14:09); the updated MF ceiling design detail on the revised plans; outstanding IQ-Glass finished head details for the bifold doors.'),
    ResponseActionRequired = COALESCE(ResponseActionRequired,
        N'Please confirm the rear elevation Velfac window position / head detail, noting only 10mm remains between the top of the window and the underside of the steel (the steels are in the correct position); confirm the MF ceiling design detail in relation to the window head; and provide the IQ-Glass exact finished head details for the bifold doors so the window can be aligned through framing.'),
    ImpactIfLate = COALESCE(ImpactIfLate,
        N'The timber framing cannot be secured and the window position cannot be set; knock-on effect to all ply, batten, felt and cladding works, which cannot continue.'),
    ResponseDue = COALESCE(ResponseDue, @Due)
WHERE RequestId = @Rfi009;

/* 1c. Velfac itemised queries — only if the request has no items. */
IF NOT EXISTS (SELECT 1 FROM dbo.RequestItems WHERE RequestId = @Rfi009)
BEGIN
    INSERT INTO dbo.RequestItems
        (RequestItemId, RequestId, Position, DrawingRef, MemberArea, Query, Response)
    VALUES
    (LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')), @Rfi009, 1,
     N'Architect''s plans / email — window at 1050mm FFL',
     N'Rear elevation — Velfac window head / steel interface',
     N'If we position the window 1050mm from FFL as your plans and email propose, we are left with 10mm spacing between the top of the window and the bottom of the steel. This leaves no fixing points to be able to secure the timber framing, so it secures nothing but the window. The steels are in the correct position. Please confirm the required window position / head detail.',
     NULL),
    (LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')), @Rfi009, 2,
     N'Updated plans — MF ceiling design detail',
     N'Ceiling at rear elevation window',
     N'Your updated plans show the following MF design detail to the ceiling — "12.5mm British Gypsum Soundbloc Plasterboard on MF5 ceiling section with 3mm plaster skim and painted white. 100 Isover Spacesaver insulation laid above ceiling board". Please confirm this detail in relation to the window head position.',
     NULL),
    (LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')), @Rfi009, 3,
     N'IQ-Glass fabrication details',
     N'Bifold doors — finished head / window alignment through framing',
     N'We require the IQ-Glass exact details for their finished head for the bifold doors to be able to align this window through framing. This has a knock-on effect to all ply, batten, felt and cladding works being able to continue.',
     NULL);

    PRINT N'RFI-009 Velfac items inserted: 3';
END

/* ============================================================================
   2. Delete the off-by-one seeded requests (RFI-010..RFI-028)
      Matched by reference AND the wrong title, so once corrected rows are in
      place a re-run deletes nothing.
   ============================================================================ */
DECLARE @Wrong TABLE (Reference NVARCHAR(64), WrongTitle NVARCHAR(256));
INSERT INTO @Wrong (Reference, WrongTitle) VALUES
(N'RFI-010', N'Staircase Walls (FF & SF) — Insulated Plasterboard Build-up Support'),
(N'RFI-011', N'Existing GF Stone Walling — Retention and Tie-in Detail'),
(N'RFI-012', N'Parapet Cold Bridging — 50mm Kingspan K106 Extent and Coping Impact'),
(N'RFI-013', N'Revised Drawings — Superseded SIKA and 25mm Therma Upstand References'),
(N'RFI-014', N'Vertical Steel Column Encasement — Blue Line Denotation'),
(N'RFI-015', N'Marmox Thermoblock Extent — GF Stud Works and IQ Glass Perimeter'),
(N'RFI-016', N'Sub-Floor Build-up Changes — Drawing Alignment'),
(N'RFI-017', N'Store Room Floor Support — SE/Design Input and Damp Investigation'),
(N'RFI-018', N'Existing Rainwater Goods (FF Rear & Side) — Replacement or Repair'),
(N'RFI-019', N'En-Suite Roof Vent — Proposed Location Conflict'),
(N'RFI-020', N'Existing Roof Tiles and Ridges (FF & GF Lower Section) — Condition Assessment'),
(N'RFI-021', N'External Tap Location'),
(N'RFI-022', N'Steel Column and Padstones — Inclusion in Revised Tender'),
(N'RFI-023', N'Kitchen Door Removal, Brick-up and Window Installation — Scope Confirmation'),
(N'RFI-024', N'Steel Encasement — Revised Item Confirmation'),
(N'RFI-025', N'Arris Rail to Flat Roof and Box Gutters'),
(N'RFI-026', N'Hanging 6x2 Ladder Frame Below Steel — Supporting Detail'),
(N'RFI-027', N'Log Burner Future Proofing — Roof Section'),
(N'RFI-028', N'Rear Cladding to Existing House and New Box Gutter — Weather Proofing Detail');

DELETE x
FROM dbo.RequestItems x
JOIN dbo.Requests r ON r.RequestId = x.RequestId
JOIN @Wrong w ON w.Reference = r.Reference AND w.WrongTitle = r.Title
WHERE r.ProjectId = @ProjectId AND r.RaisedByEmail = @RaisedBy;

PRINT N'Off-by-one items deleted: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

DELETE r
FROM dbo.Requests r
JOIN @Wrong w ON w.Reference = r.Reference AND w.WrongTitle = r.Title
WHERE r.ProjectId = @ProjectId AND r.RaisedByEmail = @RaisedBy;

PRINT N'Off-by-one requests deleted: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* ============================================================================
   3. Reseed correctly numbered RFI-010..RFI-029
   ============================================================================ */
DECLARE @Seed TABLE (
    Ord                    INT,
    Reference              NVARCHAR(64),
    Title                  NVARCHAR(256),
    Description            NVARCHAR(2048),
    ImpliesVariation       BIT,
    DrawingRef             NVARCHAR(256) NULL,
    RelatedDrawingSpec     NVARCHAR(512) NULL,
    BasisOfQueries         NVARCHAR(4000) NULL,
    ResponseActionRequired NVARCHAR(4000) NULL,
    ImpactIfLate           NVARCHAR(2048) NULL
);

INSERT INTO @Seed
    (Ord, Reference, Title, Description, ImpliesVariation, DrawingRef, RelatedDrawingSpec,
     BasisOfQueries, ResponseActionRequired, ImpactIfLate)
VALUES
( 1, N'RFI-010', N'FF & GF Radiator Locations — Sliding Door Clash and Staircase Design',
     N'Marked FF radiator (and TV bracket) fixing locations clash with the sliding door and its mechanism; GF radiator location to be reviewed against the latest agreed staircase design. Contract clause 2.11 / 2.20.6.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (14:17); site review of the marked radiator and TV bracket positions against the sliding door mechanism and the latest agreed staircase design.',
     N'Please provide revised locations / written confirmation for the FF radiator and TV bracket fixings clear of the sliding door and its mechanism, and confirm the GF radiator location against the latest agreed staircase design.',
     N'First-fix positions cannot be set out; proceeding with the marked locations risks damage to the sliding door and abortive works.'),

( 2, N'RFI-011', N'Staircase Walls (FF & SF) — Insulated Plasterboard Build-up Support',
     N'The insulated plasterboard build-up to all walls surrounding the staircase requires review as to whether it can support the staircase and its fixings. Applies to FF and SF. Contract clause 2.11 / 2.20.6.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (14:18); review of the proposed insulated plasterboard wall build-up against the staircase fixing requirements. Contractor''s standard approach in these situations is 50mm timber battens, insulation between, then ply prior to plasterboard.',
     N'Please confirm whether the specified insulated plasterboard build-up is adequate to support the staircase and its fixings at FF and SF, or instruct the contractor''s proposed battened and plied build-up.',
     N'Wall linings and staircase installation at FF and SF cannot proceed; risk of abortive lining works.'),

( 3, N'RFI-012', N'Existing GF Stone Walling — Retention and Tie-in Detail',
     N'Existing live stone walling on the GF requires Architect/SE input on the best process, including the design detail for tying into the new structure and finishing details.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (14:21); site observation of live existing stone walling at ground floor within the area of the new works.',
     N'Please provide Architect/SE instruction on the process for dealing with the existing GF stone walling, including the design detail for tying it into the new structure and the finishing details.',
     N'Works to and around the existing stone walling cannot proceed, with programme risk to the adjoining new structure.'),

( 4, N'RFI-013', N'Parapet Cold Bridging — 50mm Kingspan K106 Extent and Coping Impact',
     N'Section A-A Detail 2 requests additional 50mm Kingspan K106 insulation between battens for cold bridging (balcony end only); extent and knock-on effects on coping thickness and pergola spacing require confirmation. Note: batten was already installed and covered in cross-batten, now removed.',
     0, N'Section A-A, Detail 2', N'Section A-A, Detail 2',
     N'Revised drawing Section A-A Detail 2; contractor''s email of 02.07.2026 (14:28); site status — the batten in question had already been installed and covered in cross-batten and has now been removed.',
     N'Please confirm (a) the intended extent of the 50mm Kingspan K106 cold-bridging insulation — balcony end only as drawn, or also around the rear elevation above the bifold doors if intended for cold bridging to the steel throughout; and (b) the resulting coping thickness and setting out, noting the mixed 50mm/38mm batten sizes and the unequal module across the pergola spacing.',
     N'Parapet battening, insulation and copings cannot proceed; abortive works already incurred through removal of the installed batten, with further programme risk while the extent is unconfirmed.'),

( 5, N'RFI-014', N'Revised Drawings — Superseded SIKA and 25mm Therma Upstand References',
     N'Revised drawings still state SIKA roof covering and outlets, and 25mm Therma roof upstand, despite the agreed FLEXIPROOF substitution (ref RFI-005) and the agreed 18mm ply upstand.',
     0, N'Revised drawing issue', N'Revised drawing issue',
     N'Contractor''s review of the revised drawing issue against the agreed FLEXIPROOF substitution (ref RFI-005, accepted by client and LAA 15.06.2026) and the agreed 18mm ply roof upstand.',
     N'Please confirm the drawings will be revised to reflect the agreed FLEXIPROOF roof covering and outlets and the 18mm ply upstand, or confirm in writing that the contractor may proceed on the agreed basis notwithstanding the drawing references.',
     N'Ambiguity between the drawings and the agreed specification risks incorrect procurement and prejudices the roofing warranty position.'),

( 6, N'RFI-015', N'Vertical Steel Column Encasement — Blue Line Denotation',
     N'Clarification required as to why one drawing details an encasement for the vertical columns under the dashed blue line denotation.',
     0, N'Dashed blue line denotation', N'Dashed blue line denotation',
     N'Contractor''s email of 02.07.2026; review of the drawing note "Dashed blue lines denote steel columns to sit on steel beam and then beam encased in concrete to S.E. detail & specification" against the other contract drawings.',
     N'Please confirm why one drawing details, for the blue line denotation, an encasement for the vertical columns, and confirm whether the vertical columns require encasement and to what SE detail and specification.',
     N'Steelwork encasement and follow-on works cannot proceed while the requirement is unconfirmed.'),

( 7, N'RFI-016', N'Marmox Thermoblock Extent — GF Stud Works and IQ Glass Perimeter',
     N'Extent of Marmox Thermoblock installations requires confirmation: 100x140mm blocks at GF stud works around the staircase section, and the revised 207mm-wide blocks at the IQ Glass FF perimeter.',
     0, NULL, NULL,
     N'Drawing notes "100 x 140mm Marmox Thermoblock to run full length across top of 215mm blockwork full length" and "215mmW x 100mmH Marmox Thermoblock cut down to 207mm wide by taking 8mm off the one side only"; IQ Glass interface requirements.',
     N'Please confirm (a) whether the 100x140mm Marmox blocks are required on all GF stud works around the staircase section or only the face into the garden as detailed; and (b) whether the newly requested change in Marmox blocks to the IQ Glass is confirmed, and whether it applies around the entire perimeter where the FF glass sits rather than just in front of the balustrade SE support.',
     N'Blockwork and thermal-break installation cannot proceed; risk of abortive works at the FF glazing perimeter ahead of the IQ Glass installation.'),

( 8, N'RFI-017', N'Sub-Floor Build-up Changes — Drawing Alignment',
     N'Clarification required that the drawings align with all sub-floor build-up changes, as they appear to differ.',
     0, NULL, NULL,
     N'Contractor''s comparison of the revised drawings against the agreed sub-floor build-up changes.',
     N'Please clarify that the drawings align with all sub-floor build-up changes and confirm the correct build-up to work to, as the drawings seem to differ.',
     N'Floor build-ups cannot be procured or installed; risk of abortive works and knock-on finished-level discrepancies.'),

( 9, N'RFI-018', N'Store Room Floor Support — SE/Design Input and Damp Investigation',
     N'SE or design input required into the supporting of the store room floor (new request) to enable firm costings. Store room also smells very damp and may require investigation for rising damp prior to works.',
     1, NULL, NULL,
     N'New request for store room floor works; contractor''s site observation of a strong damp smell in the store room.',
     N'Please provide SE/design input into the supporting of the store room floor so firm costings can be provided, and instruct whether a damp investigation is required to ensure there is no rising damp prior to works.',
     N'Firm costings cannot be provided and the works cannot be programmed; risk of building over undiagnosed rising damp.'),

(10, N'RFI-019', N'Existing Rainwater Goods (FF Rear & Side) — Replacement or Repair',
     N'Existing rainwater goods to the FF at the rear and side appear very aged and in need of attention via replacement or repair; review and Architect input suggested.',
     1, NULL, NULL,
     N'Contractor''s site observation of the existing FF rainwater goods to the rear and side elevations.',
     N'Please review the existing FF rainwater goods to the rear and side elevations and provide your input / instruction on replacement or repair.',
     N'Aged rainwater goods risk water damage to completed works; the opportunity to attend from the current scaffold may be lost.'),

(11, N'RFI-020', N'En-Suite Roof Vent — Proposed Location Conflict',
     N'Due to the existing lead works, existing RWP and new box gutter, the proposed location of the en-suite roof vent is not believed workable.',
     0, NULL, NULL,
     N'Contractor''s site review of the proposed en-suite roof vent location against the existing lead works, existing RWP and the new box gutter.',
     N'Please confirm a revised location for the en-suite roof vent, taking account of the existing lead works, existing RWP and new box gutter.',
     N'The vent penetration cannot be formed; risk of clashes with the lead works and box gutter and of delaying the roof completion.'),

(12, N'RFI-021', N'Existing Roof Tiles and Ridges (FF & GF Lower Section) — Condition Assessment',
     N'A detailed assessment is required of the existing roof tiles and ridges on the FF & GF where the new works are being undertaken; contractor''s opinion is these are beyond patching in and would benefit from a complete overhaul.',
     1, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (14:45); site inspection of the existing roof tiles and ridges on the FF and GF lower sections adjacent to the new works.',
     N'Please arrange / instruct a detailed assessment of the existing roof tiles and ridges on the FF and GF lower sections where the new works are being undertaken, and confirm whether a complete overhaul is to be instructed in lieu of patching in.',
     N'Patching in to failed tiles and ridges risks poor junctions with the new works and rework at additional cost while scaffold remains in place.'),

(13, N'RFI-022', N'External Tap Location',
     N'Advice required on the external tap location outside.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (14:46).',
     N'Please advise on the external tap location outside.',
     N'First-fix plumbing runs cannot be finalised.'),

(14, N'RFI-023', N'Steel Column and Padstones — Inclusion in Revised Tender',
     N'Confirmation required that the identified column and padstone were included in the revised tender, as this appears to be a new item on the revised drawings.',
     1, N'Revised tender drawings', N'Revised tender drawings',
     N'Contractor''s email of 02.07.2026 (14:49); comparison of the revised drawings against the revised tender.',
     N'Please confirm that the identified steel column and padstones were included in the revised tender, as this seems to be a new item on the revised drawings; if not included, please confirm that a cost proposal is required.',
     N'Steel and padstone procurement is on hold and the cost position remains unconfirmed.'),

(15, N'RFI-024', N'Kitchen Door Removal, Brick-up and Window Installation — Scope Confirmation',
     N'Confirmation required whether the kitchen door removal, brick-up and window installation is a new item, as it does not appear on the schedule, valuation or the original tender drawings.',
     1, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (14:52); review against the contractor''s schedule, valuation and the original tender drawings.',
     N'Please confirm whether the kitchen door removal, brick-up and window installation is a new item, as it does not appear on our schedule, valuation or the original tender drawings; if new, please confirm that a cost proposal is required.',
     N'The works cannot be programmed or priced; risk of delay to the kitchen area sequence.'),

(16, N'RFI-025', N'Steel Encasement — Revised Item Confirmation',
     N'Confirmation required that the identified steel encasement is not a revised item, as it does not appear on the tender drawings.',
     1, NULL, NULL,
     N'Contractor''s email of 02.07.2026; comparison of the current drawings against the tender drawings.',
     N'Please confirm that the identified steel encasement is not a revised item, as it seems this was not on the tender drawings; if it is a revised item, please confirm that a cost proposal is required.',
     N'The encasement cannot be programmed or priced and follow-on works are at risk of delay.'),

(17, N'RFI-026', N'Arris Rail to Flat Roof and Box Gutters',
     N'Confirmation required whether the flat roof and all box gutters should also have arris rail.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (14:56); review of the flat roof and box gutter details.',
     N'Please confirm whether the flat roof and all box gutters should also have arris rail.',
     N'Roof perimeter detailing cannot be completed; risk of rework to membrane terminations.'),

(18, N'RFI-027', N'Hanging 6x2 Ladder Frame Below Steel — Supporting Detail',
     N'Supporting detail required for the hanging ladder frame (6x2 per subject; 4x2 per email body) to box and create the soffit design, following the SE input on the parapet.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (15:02); SE input on the parapet. Contractor would normally spit-gun and CT1 this fixing.',
     N'Please confirm the supporting detail for the hanging ladder frame below the steel to box and create the soffit design. Normally we would spit-gun this and CT1, but following the SE input on the parapet we will need clarification.',
     N'The soffit framing cannot proceed; risk of delay to the external envelope completion.'),

(19, N'RFI-028', N'Log Burner Future Proofing — Roof Section',
     N'Confirmation required of what is needed to future proof for the log burner to the roof section.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (15:05).',
     N'Please confirm what is required for future proofing for the log burner to the roof section (e.g. flue route, penetration and any structural provision) before the roof build-up is closed.',
     N'If not confirmed before the roof is closed up, later provision for the log burner will require opening up completed works at additional cost.'),

(20, N'RFI-029', N'Rear Cladding to Existing House and New Box Gutter — Weather Proofing Detail',
     N'Detail required for finishing the vertical section including new rear cladding to the existing house and the new box gutter, with weather proofing.',
     0, NULL, NULL,
     N'Contractor''s email of 02.07.2026 (15:09); interface between the new rear cladding to the existing house and the new box gutter.',
     N'Please provide the detail for finishing the vertical section, including the new rear cladding to the existing house and the new box gutter detail with weather proofing.',
     N'The cladding and box gutter junction cannot be completed and remains vulnerable to water ingress.');

/* --- Itemised queries (official sheet rows) --------------------------------- */
DECLARE @Items TABLE (
    ParentOrd  INT,
    ItemPos    INT,
    DrawingRef NVARCHAR(1024),
    MemberArea NVARCHAR(512),
    Query      NVARCHAR(4000)
);

INSERT INTO @Items (ParentOrd, ItemPos, DrawingRef, MemberArea, Query)
VALUES
-- RFI-010 radiators
( 1, 1, N'Marked-up radiator / TV bracket locations',
        N'First Floor — sliding door / radiator and TV bracket',
        N'The marked location of the FF radiator (and TV bracket) fixings will affect the sliding door and its mechanism, meaning the door itself will get damaged. Additionally, the frames for sliding doors are not designed to hang items like this on them. Please confirm revised fixing locations.'),
( 1, 2, N'Latest agreed staircase design',
        N'Ground Floor — radiator adjacent to staircase',
        N'The GF radiator location needs to be reviewed against the latest agreed staircase design. Please confirm the required location.'),

-- RFI-011 staircase walls
( 2, 1, N'Wall build-up specification',
        N'First & Second Floor — walls surrounding staircase',
        N'The design of the walling with insulated plasterboard to all surrounding walls where the staircase is to be installed requires review as to whether this will be able to support the staircase and its fixings into the walls. In these situations we would normally install 50mm timber battens, insulate between and then ply prior to plasterboard. Please confirm the required build-up for FF and SF.'),

-- RFI-012 stone walling
( 3, 1, N'Site observation',
        N'Ground Floor — existing stone walling',
        N'The existing stone walling on the GF is live and requires your/SE input into what the best process is. This will need to include the design detail on how to tie this into the new structure, including finishing details.'),

-- RFI-013 parapet cold bridging
( 4, 1, N'Section A-A, Detail 2',
        N'Parapet — balcony end / rear elevation above bifold doors',
        N'Only Section A-A Detail 2 requests new additional 50mm Kingspan K106 insulation between battens for cold bridging (this is the balcony end only). Should this not also be around the rear elevation above the bifold doors if it is for cold bridging to the steel throughout? Note: this batten was already installed and covered in cross-batten, and has now been removed.'),
( 4, 2, N'Section A-A, Detail 2',
        N'Parapet copings / pergola interface',
        N'This change in batten size will change the top coping thickness, which is no longer generic (some battens 50mm, some 38mm). Installing this will also mean it is not equal across the pergola spacing. Please confirm the required coping thickness and setting out.'),

-- RFI-014 superseded drawing references
( 5, 1, N'Revised drawing issue',
        N'Roof covering and outlets',
        N'Your revised drawings still state SIKA roof covering and outlets. Please confirm the drawings will be updated to reflect the agreed FLEXIPROOF substitution (ref RFI-005).'),
( 5, 2, N'Revised drawing issue',
        N'Roof upstands',
        N'Your revised drawings still state 25mm Therma roof upstand even though this is now being done in 18mm ply as agreed. Please confirm.'),

-- RFI-015 column encasement denotation
( 6, 1, N'Dashed blue line denotation',
        N'Vertical steel columns',
        N'Please confirm why one drawing details, for the blue line denotation, an encasement for the vertical columns — "Dashed blue lines denote steel columns to sit on steel beam and then beam encased in concrete to S.E. detail & specification".'),

-- RFI-016 Marmox extent
( 7, 1, N'Marmox Thermoblock drawing note',
        N'Ground Floor — stud works around staircase section',
        N'Are the 100 x 140mm Marmox blocks required on all stud works GF around the staircase section, and not just the face into the garden as detailed — "100 x 140mm Marmox Thermoblock to run full length across top of 215mm blockwork full length"?'),
( 7, 2, N'Marmox Thermoblock drawing note / IQ Glass interface',
        N'First Floor — glazing perimeter (IQ Glass)',
        N'Are the newly requested changes in Marmox blocks to the IQ Glass confirmed, and is this around the entire perimeter where the FF glass sits and not just in front of the balustrade SE support — "215mmW x 100mmH Marmox Thermoblock cut down to 207mm wide by taking 8mm off the one side only"?'),

-- RFI-017 sub-floor build-ups
( 8, 1, N'Revised drawing issue',
        N'Sub-floor build-ups',
        N'Could you please clarify that the drawings align with all sub-floor build-up changes, as they seem to differ.'),

-- RFI-018 store room
( 9, 1, N'New request',
        N'Store room — floor support',
        N'We will need SE or design input into the supporting of the store room floor (new request) to be able to provide firm costings.'),
( 9, 2, N'Site observation',
        N'Store room — potential rising damp',
        N'It smells very damp in the store room and this may require investigation to ensure there is no rising damp prior to works.'),

-- RFI-019 rainwater goods
(10, 1, N'Site observation',
        N'First Floor — rainwater goods, rear and side elevations',
        N'The existing rainwater goods to the FF at the rear and side seem to be very aged and in need of attention via replacement or repair. We would suggest a review and your input.'),

-- RFI-020 en-suite roof vent
(11, 1, N'Proposed roof vent location',
        N'En-suite roof vent / existing lead works, RWP and new box gutter',
        N'Due to the existing lead works, existing RWP and new box gutter, we do not believe the proposed location of the en-suite roof vent will work. Please confirm a revised location.'),

-- RFI-021 roof tiles and ridges
(12, 1, N'Site inspection',
        N'Existing roof tiles and ridges — FF & GF lower sections',
        N'A detailed assessment is required of the existing roof tiles and ridges on the FF & GF where the new works are being undertaken. It is our opinion that these are beyond just patching in and would benefit from a complete overhaul.'),

-- RFI-022 external tap
(13, 1, N'',
        N'External works — outside tap',
        N'Please advise on the external tap location outside.'),

-- RFI-023 steel column and padstones
(14, 1, N'Revised tender drawings',
        N'Steel column and padstones',
        N'Could you please confirm that the identified column and pad were included in the revised tender, as this seems to be a new item on the revised drawings.'),

-- RFI-024 kitchen door
(15, 1, N'Schedule / valuation / original tender drawings',
        N'Kitchen — door removal, brick-up and window installation',
        N'Could you please confirm if this kitchen door removal, brick-up and window installation is a new item, as it does not appear on our schedule, valuation or the original tender drawings.'),

-- RFI-025 steel encasement
(16, 1, N'Tender drawings',
        N'Steel encasement',
        N'Could you please confirm that this steel encasement is not a revised item, as it seems that this was not on the tender drawings.'),

-- RFI-026 arris rail
(17, 1, N'Roof details',
        N'Flat roof and box gutters',
        N'Should the flat roof and all box gutters not also have arris rail?'),

-- RFI-027 ladder frame
(18, 1, N'SE parapet input',
        N'Soffit — hanging ladder frame below steel',
        N'What is the supporting detail for the hanging ladder frame (6x2 per subject; 4x2 per email) to box and create the soffit design? Normally we would spit-gun this and CT1, but following the SE input on the parapet we will need clarification.'),

-- RFI-028 log burner
(19, 1, N'',
        N'Roof section — log burner provision',
        N'Please confirm what is required here for future proofing for the log burner to the roof section.'),

-- RFI-029 rear cladding / box gutter
(20, 1, N'',
        N'Rear elevation — cladding to existing house / new box gutter',
        N'What is the detail for finishing the vertical section, including the new rear cladding to the existing house and the new box gutter detail with weather proofing?');

/* --- Insert any RFI not already present (idempotent) ----------------------- */
DECLARE @Base INT = ISNULL((SELECT MAX(Number) FROM dbo.Requests), 0);

DECLARE @Inserted TABLE (RequestId NVARCHAR(64), Reference NVARCHAR(64));

INSERT INTO dbo.Requests
    (RequestId, ProjectId, Kind, Reference, Title, Description, Status, Value,
     RaisedByEmail, RaisedAt, RespondedAt, ResponseText, RespondedByEmail,
     ImpliesVariation, RaisedTo, DrawingRef, ResponseDue, RelatedDrawingSpec,
     InternalNotes, ClientNotes, BasisOfQueries, ResponseActionRequired,
     ImpactIfLate, Number, MailboxFolderId)
OUTPUT inserted.RequestId, inserted.Reference INTO @Inserted (RequestId, Reference)
SELECT
    LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')),   -- 32-char compact GUID, matches app format
    @ProjectId,
    0,                                                           -- Kind = RFI
    s.Reference,
    s.Title,
    s.Description,
    0,                                                           -- Status = Open (not yet issued)
    NULL,                                                        -- Value
    @RaisedBy,
    @RaisedAt,
    NULL,                                                        -- RespondedAt
    NULL,                                                        -- ResponseText
    NULL,                                                        -- RespondedByEmail
    s.ImpliesVariation,
    N'Design Team',
    s.DrawingRef,
    @Due,
    s.RelatedDrawingSpec,
    NULL,                                                        -- InternalNotes
    NULL,                                                        -- ClientNotes
    s.BasisOfQueries,
    s.ResponseActionRequired,
    s.ImpactIfLate,
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

/* --- Items: ONLY for requests inserted this run ----------------------------- */
INSERT INTO dbo.RequestItems
    (RequestItemId, RequestId, Position, DrawingRef, MemberArea, Query, Response)
SELECT
    LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')),
    ins.RequestId,
    i.ItemPos,
    i.DrawingRef,
    i.MemberArea,
    i.Query,
    NULL
FROM @Items i
JOIN @Seed s     ON s.Ord = i.ParentOrd
JOIN @Inserted ins ON ins.Reference = s.Reference;

PRINT N'Abbot Road RFI items inserted this run: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

COMMIT TRANSACTION;

/* --- Summary --------------------------------------------------------------- */
SELECT
    Total  = COUNT(*),
    [Open] = SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END),
    Closed = SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END)
FROM dbo.Requests
WHERE ProjectId = @ProjectId
  AND Kind = 0;

SELECT r.Reference, r.Title, Items = COUNT(x.RequestItemId)
FROM dbo.Requests r
LEFT JOIN dbo.RequestItems x ON x.RequestId = r.RequestId
WHERE r.ProjectId = @ProjectId AND r.Kind = 0
GROUP BY r.Reference, r.Title
ORDER BY r.Reference;
