-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: 67 Beresford Road -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : 67 Beresford Road, Sutton, SM2 6ER
-- ProjectId: resolved at run time by site-name matcher '67beresfordroadsutton'
--
-- Companion to seed-beresfordroad-valuation.sql, which seeds ONLY the original
-- contract scope (Contract Sum GBP 264,504.00). This file adds the
-- post-contract VARIATION ORDERS from the "Valuation 9 - Completion" workbook,
-- reconciling to the register:
--
--     Net Variations           GBP  21,673.44
--     Contract Sum             GBP 264,504.00
--     -----------------------------------------
--     Revised (Live Build) Sum GBP 286,177.44
--
-- MODEL NOTE (unified variation orders, post-20260723120000_UnifyVariationOrders)
-- Each workbook VO is split into multiple priced lines (omits of contract
-- scope as negatives, new items as positives). On the JPMS valuation report a
-- VO shows as a SINGLE summary line, so we seed ONE ValuationLineItem per
-- APPROVED VO whose LineAmount is the NET of that VO's workbook lines
-- (Quantity 1 x Rate = net), plus ONE VariationOrderQuotes row per VO (the
-- unified variation-order record; the old [VariationOrders] table is gone).
--
-- V14, V16, V20 and V25 are wholly DECLINED (Status 3): they carry their
-- quoted amount as EstimatedValue, Value 0, no VariationRef, no CostCode and
-- NO ValuationLineItem row, so they never count toward totals.
--
-- The register's own footer shows the correct GBP 21,673.44 next to a 106.15%
-- claim-percentage oddity and a GBP 23,007.44 claim-column cross-cast; both
-- are workbook display artefacts -- the VO VALUES below reconcile to
-- GBP 21,673.44 exactly, with no penny adjustment needed. V04's "Radiators"
-- row (+1,820.00, later reversed by V28's "Radiators V04" -1,820.00) is
-- included in V04's net as the register does.
--
-- Approval dates are seeded ~monthly, each VO created just before its first
-- claimed valuation month (Val 1 taken as Dec 2025 .. Val 9 as Aug 2026);
-- declined VOs are dated with their register neighbours.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all lines here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--
-- VOQ Status : Quoting=0 Issued=1 Approved=2 Declined=3
--
-- Idempotent: keyed on stable ids (br-voq-vNN / br-vo-vNN). A re-run refreshes
-- every field via MERGE. The contract lines seeded by
-- seed-beresfordroad-valuation.sql are left untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '67beresfordroadsutton'
       OR LOWER(REPLACE(Name, ' ', '')) = '67beresfordroadsutton'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '67beresfordroadsutton' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  67 Beresford Road -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'br-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Plasterboard ceiling removal - omit', N'Plasterboard ceiling removal - omit', 2, NULL, NULL, -420.0000, N'V01', -420.0000, N'ENABLE-DEM', '2025-12-20', N'seed@jewelgroup.co.uk', '2025-12-27', '2026-01-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Supply & install drainage & soakaway', N'Supply & install drainage & soakaway', 2, NULL, NULL, 1450.0000, N'V02', 1450.0000, N'SUB-DRN', '2026-01-20', N'seed@jewelgroup.co.uk', '2026-01-27', '2026-02-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'External wall respec - 20N brick & 7N block cavity walls', N'External wall respec - 20N brick & 7N block cavity walls', 2, NULL, NULL, 1425.0000, N'V03', 1425.0000, N'MASON-BRK', '2026-01-20', N'seed@jewelgroup.co.uk', '2026-01-27', '2026-02-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Living room floor demolition & overlay UFH system', N'Living room floor demolition & overlay UFH system', 2, NULL, NULL, 4970.0000, N'V04', 4970.0000, N'MEC-UFH', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'Blockwork screen wall & entrance door brickwork', N'Blockwork screen wall & entrance door brickwork', 2, NULL, NULL, 916.0000, N'V05', 916.0000, N'MASON-BRK', '2026-01-20', N'seed@jewelgroup.co.uk', '2026-01-27', '2026-02-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Windows & doors respec - Generation Windows', N'Windows & doors respec - Generation Windows', 2, NULL, NULL, -4701.0000, N'V06', -4701.0000, N'WDR-UPV', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Overlay wet UFH - master & walk-in', N'Overlay wet UFH - master & walk-in', 2, NULL, NULL, 3085.0000, N'V07', 3085.0000, N'MEC-UFH', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Velux respec - triple glazed electric window & blinds', N'Velux respec - triple glazed electric window & blinds', 2, NULL, NULL, 34.8400, N'V08', 34.8400, N'WDR-SPG', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Loft insulation, counter battens, T&G flooring & hatch', N'Loft insulation, counter battens, T&G flooring & hatch', 2, NULL, NULL, 4950.0000, N'V09', 4950.0000, N'CARP-1FX', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Velux rigid sun tunnels', N'Velux rigid sun tunnels', 2, NULL, NULL, 1450.0000, N'V10', 1450.0000, N'WDR-SPG', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'Electrical additions - sockets, lighting, data & security', N'Electrical additions - sockets, lighting, data & security', 2, NULL, NULL, 7715.0000, N'V11', 7715.0000, N'ELE-STD', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'New staircase supply & install', N'New staircase supply & install', 2, NULL, NULL, 2655.0000, N'V12', 2655.0000, N'STAIR-TIM', '2026-02-20', N'seed@jewelgroup.co.uk', '2026-02-27', '2026-03-13', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Electrical - GF & FF dual rads', N'Electrical - GF & FF dual rads', 2, NULL, NULL, 480.0000, N'V13', 480.0000, N'ELE-STD', '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', '2026-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'False ceiling to the kitchen/dining room', N'False ceiling to the kitchen/dining room', 3, NULL, NULL, 750.0000, NULL, 0.0000, NULL, '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', NULL, NULL, '2026-04-03'),
        (N'br-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Kitchen & living room electrical additions', N'Kitchen & living room electrical additions', 2, NULL, NULL, 448.0000, N'V15', 448.0000, N'ELE-STD', '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', '2026-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'Decoration of the staircase', N'Decoration of the staircase', 3, NULL, NULL, 925.0000, NULL, 0.0000, NULL, '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', NULL, NULL, '2026-04-03'),
        (N'br-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'Fascia & soffit to main roof', N'Fascia & soffit to main roof', 2, NULL, NULL, 5125.0000, N'V17', 5125.0000, N'ROOF-FSU', '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', '2026-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'Tiling respec - floor & wall tiles, tanking & access hatches', N'Tiling respec - floor & wall tiles, tanking & access hatches', 2, NULL, NULL, 11779.0000, N'V18', 11779.0000, N'TIL-STD', '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', '2026-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v19', @ProjectId, N'', 19, N'VOQ-0019', N'Master en-suite ceiling - shower area', N'Master en-suite ceiling - shower area', 2, NULL, NULL, 485.0000, N'V19', 485.0000, N'INT-PLB', '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', '2026-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v20', @ProjectId, N'', 20, N'VOQ-0020', N'Headboard joinery unit', N'Headboard joinery unit', 3, NULL, NULL, 2645.0000, NULL, 0.0000, NULL, '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', NULL, NULL, '2026-05-04'),
        (N'br-voq-v21', @ProjectId, N'', 21, N'VOQ-0021', N'Flooring respec - engineered timber, ply base & adhesive', N'Flooring respec - engineered timber, ply base & adhesive', 2, NULL, NULL, -1941.4000, N'V21', -1941.4000, N'FLR-WD', '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', '2026-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v22', @ProjectId, N'', 22, N'VOQ-0022', N'Extended preliminaries - 3 weeks', N'Extended preliminaries - 3 weeks', 2, NULL, NULL, 1830.0000, N'V22', 1830.0000, N'PRELIMS-SMG', '2026-06-20', N'seed@jewelgroup.co.uk', '2026-06-27', '2026-07-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v23', @ProjectId, N'', 23, N'VOQ-0023', N'Clay paint respec to walls', N'Clay paint respec to walls', 2, NULL, NULL, 1136.0000, N'V23', 1136.0000, N'DEC-STD', '2026-05-20', N'seed@jewelgroup.co.uk', '2026-05-27', '2026-06-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v24', @ProjectId, N'', 24, N'VOQ-0024', N'Utility & store alterations, electrics & water main upgrade', N'Utility & store alterations, electrics & water main upgrade', 2, NULL, NULL, 1260.0000, N'V24', 1260.0000, N'HAND-MSC', '2026-05-20', N'seed@jewelgroup.co.uk', '2026-05-27', '2026-06-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v25', @ProjectId, N'', 25, N'VOQ-0025', N'Sealing granite tiles', N'Sealing granite tiles', 3, NULL, NULL, 1215.0000, NULL, 0.0000, NULL, '2026-05-20', N'seed@jewelgroup.co.uk', '2026-05-27', NULL, NULL, '2026-06-03'),
        (N'br-voq-v26', @ProjectId, N'', 26, N'VOQ-0026', N'Omit driveway & external paving', N'Omit driveway & external paving', 2, NULL, NULL, -13410.0000, N'V26', -13410.0000, N'EXTW-PAV', '2026-06-20', N'seed@jewelgroup.co.uk', '2026-06-27', '2026-07-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v27', @ProjectId, N'', 27, N'VOQ-0027', N'Block paving border', N'Block paving border', 2, NULL, NULL, 715.0000, N'V27', 715.0000, N'EXTW-PAV', '2026-06-20', N'seed@jewelgroup.co.uk', '2026-06-27', '2026-07-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v28', @ProjectId, N'', 28, N'VOQ-0028', N'Completion adjustments - radiators, lights, doors & asbestos', N'Completion adjustments - radiators, lights, doors & asbestos', 2, NULL, NULL, -8429.0000, N'V28', -8429.0000, N'MEC-PLM', '2026-06-20', N'seed@jewelgroup.co.uk', '2026-06-27', '2026-07-11', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v29', @ProjectId, N'', 29, N'VOQ-0029', N'Kitchen extractor', N'Kitchen extractor', 2, NULL, NULL, 300.0000, N'V29', 300.0000, N'MEC-VNT', '2026-07-20', N'seed@jewelgroup.co.uk', '2026-07-27', '2026-08-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v30', @ProjectId, N'', 30, N'VOQ-0030', N'Omit electric blackout blinds', N'Omit electric blackout blinds', 2, NULL, NULL, -517.0000, N'V30', -517.0000, N'WIN-BLD', '2026-07-20', N'seed@jewelgroup.co.uk', '2026-07-27', '2026-08-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v31', @ProjectId, N'', 31, N'VOQ-0031', N'Omit pebble dash & render to retained areas', N'Omit pebble dash & render to retained areas', 2, NULL, NULL, -1417.0000, N'V31', -1417.0000, N'INT-RDR', '2026-07-20', N'seed@jewelgroup.co.uk', '2026-07-27', '2026-08-10', N'seed@jewelgroup.co.uk', NULL),
        (N'br-voq-v32', @ProjectId, N'', 32, N'VOQ-0032', N'Carpentry - day rate', N'Carpentry - day rate', 2, NULL, NULL, 300.0000, N'V32', 300.0000, N'CARP-2FX', '2026-07-20', N'seed@jewelgroup.co.uk', '2026-07-27', '2026-08-10', N'seed@jewelgroup.co.uk', NULL)
    ) AS source (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
                 Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
                 VariationRef, Value, CostCode, CreatedAt, CreatedByEmail, IssuedAt,
                 ApprovedAt, ApprovedByEmail, RejectedAt)
    ON target.VariationOrderQuoteId = source.VariationOrderQuoteId
    WHEN MATCHED THEN UPDATE SET
        ProjectId               = source.ProjectId,
        RequestId               = source.RequestId,
        Number                  = source.Number,
        Reference               = source.Reference,
        Title                   = source.Title,
        Description             = source.Description,
        Status                  = source.Status,
        SelectedBidPackageId    = source.SelectedBidPackageId,
        SelectedSubcontractorId = source.SelectedSubcontractorId,
        EstimatedValue          = source.EstimatedValue,
        VariationRef            = source.VariationRef,
        Value                   = source.Value,
        CostCode                = source.CostCode,
        CreatedAt               = source.CreatedAt,
        CreatedByEmail          = source.CreatedByEmail,
        IssuedAt                = source.IssuedAt,
        ApprovedAt              = source.ApprovedAt,
        ApprovedByEmail         = source.ApprovedByEmail,
        RejectedAt              = source.RejectedAt
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
                Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
                VariationRef, Value, CostCode, CreatedAt, CreatedByEmail, IssuedAt,
                ApprovedAt, ApprovedByEmail, RejectedAt)
        VALUES (source.VariationOrderQuoteId, source.ProjectId, source.RequestId, source.Number,
                source.Reference, source.Title, source.Description, source.Status,
                source.SelectedBidPackageId, source.SelectedSubcontractorId, source.EstimatedValue,
                source.VariationRef, source.Value, source.CostCode, source.CreatedAt,
                source.CreatedByEmail, source.IssuedAt, source.ApprovedAt,
                source.ApprovedByEmail, source.RejectedAt);

    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'br-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Plasterboard ceiling removal - omit', 2, N'ENABLE-DEM', N'', N'item', 1.0000, -420.0000, -420.0000, N'', 1),
        (N'br-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Supply & install drainage & soakaway', 0, N'SUB-DRN', N'', N'item', 1.0000, 1450.0000, 1450.0000, N'', 2),
        (N'br-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'External wall respec - 20N brick & 7N block cavity walls', 0, N'MASON-BRK', N'', N'item', 1.0000, 1425.0000, 1425.0000, N'', 3),
        (N'br-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Living room floor demolition & overlay UFH system', 0, N'MEC-UFH', N'', N'item', 1.0000, 4970.0000, 4970.0000, N'', 4),
        (N'br-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'Blockwork screen wall & entrance door brickwork', 0, N'MASON-BRK', N'', N'item', 1.0000, 916.0000, 916.0000, N'', 5),
        (N'br-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Windows & doors respec - Generation Windows', 2, N'WDR-UPV', N'', N'item', 1.0000, -4701.0000, -4701.0000, N'', 6),
        (N'br-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Overlay wet UFH - master & walk-in', 0, N'MEC-UFH', N'', N'item', 1.0000, 3085.0000, 3085.0000, N'', 7),
        (N'br-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Velux respec - triple glazed electric window & blinds', 0, N'WDR-SPG', N'', N'item', 1.0000, 34.8400, 34.8400, N'', 8),
        (N'br-vo-v09', @ProjectId, 3, N'', N'', N'V09', N'Loft insulation, counter battens, T&G flooring & hatch', 0, N'CARP-1FX', N'', N'item', 1.0000, 4950.0000, 4950.0000, N'', 9),
        (N'br-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Velux rigid sun tunnels', 0, N'WDR-SPG', N'', N'item', 1.0000, 1450.0000, 1450.0000, N'', 10),
        (N'br-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'Electrical additions - sockets, lighting, data & security', 0, N'ELE-STD', N'', N'item', 1.0000, 7715.0000, 7715.0000, N'', 11),
        (N'br-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'New staircase supply & install', 0, N'STAIR-TIM', N'', N'item', 1.0000, 2655.0000, 2655.0000, N'', 12),
        (N'br-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Electrical - GF & FF dual rads', 0, N'ELE-STD', N'', N'item', 1.0000, 480.0000, 480.0000, N'', 13),
        (N'br-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Kitchen & living room electrical additions', 0, N'ELE-STD', N'', N'item', 1.0000, 448.0000, 448.0000, N'', 14),
        (N'br-vo-v17', @ProjectId, 3, N'', N'', N'V17', N'Fascia & soffit to main roof', 0, N'ROOF-FSU', N'', N'item', 1.0000, 5125.0000, 5125.0000, N'', 15),
        (N'br-vo-v18', @ProjectId, 3, N'', N'', N'V18', N'Tiling respec - floor & wall tiles, tanking & access hatches', 0, N'TIL-STD', N'', N'item', 1.0000, 11779.0000, 11779.0000, N'', 16),
        (N'br-vo-v19', @ProjectId, 3, N'', N'', N'V19', N'Master en-suite ceiling - shower area', 0, N'INT-PLB', N'', N'item', 1.0000, 485.0000, 485.0000, N'', 17),
        (N'br-vo-v21', @ProjectId, 3, N'', N'', N'V21', N'Flooring respec - engineered timber, ply base & adhesive', 2, N'FLR-WD', N'', N'item', 1.0000, -1941.4000, -1941.4000, N'', 18),
        (N'br-vo-v22', @ProjectId, 3, N'', N'', N'V22', N'Extended preliminaries - 3 weeks', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 1830.0000, 1830.0000, N'', 19),
        (N'br-vo-v23', @ProjectId, 3, N'', N'', N'V23', N'Clay paint respec to walls', 0, N'DEC-STD', N'', N'item', 1.0000, 1136.0000, 1136.0000, N'', 20),
        (N'br-vo-v24', @ProjectId, 3, N'', N'', N'V24', N'Utility & store alterations, electrics & water main upgrade', 0, N'HAND-MSC', N'', N'item', 1.0000, 1260.0000, 1260.0000, N'', 21),
        (N'br-vo-v26', @ProjectId, 3, N'', N'', N'V26', N'Omit driveway & external paving', 2, N'EXTW-PAV', N'', N'item', 1.0000, -13410.0000, -13410.0000, N'', 22),
        (N'br-vo-v27', @ProjectId, 3, N'', N'', N'V27', N'Block paving border', 0, N'EXTW-PAV', N'', N'item', 1.0000, 715.0000, 715.0000, N'', 23),
        (N'br-vo-v28', @ProjectId, 3, N'', N'', N'V28', N'Completion adjustments - radiators, lights, doors & asbestos', 2, N'MEC-PLM', N'', N'item', 1.0000, -8429.0000, -8429.0000, N'', 24),
        (N'br-vo-v29', @ProjectId, 3, N'', N'', N'V29', N'Kitchen extractor', 0, N'MEC-VNT', N'', N'item', 1.0000, 300.0000, 300.0000, N'', 25),
        (N'br-vo-v30', @ProjectId, 3, N'', N'', N'V30', N'Omit electric blackout blinds', 2, N'WIN-BLD', N'', N'item', 1.0000, -517.0000, -517.0000, N'', 26),
        (N'br-vo-v31', @ProjectId, 3, N'', N'', N'V31', N'Omit pebble dash & render to retained areas', 2, N'INT-RDR', N'', N'item', 1.0000, -1417.0000, -1417.0000, N'', 27),
        (N'br-vo-v32', @ProjectId, 3, N'', N'', N'V32', N'Carpentry - day rate', 0, N'CARP-2FX', N'', N'item', 1.0000, 300.0000, 300.0000, N'', 28)
    ) AS source (ValuationLineItemId, ProjectId, ElementType, SectionCode, SectionName,
                 VariationRef, VariationTitle, LineType, CostCode, Description, Unit,
                 Quantity, Rate, LineAmount, Comments, DisplayOrder)
    ON target.ValuationLineItemId = source.ValuationLineItemId
    WHEN MATCHED THEN UPDATE SET
        ProjectId      = source.ProjectId,
        ElementType    = source.ElementType,
        SectionCode    = source.SectionCode,
        SectionName    = source.SectionName,
        VariationRef   = source.VariationRef,
        VariationTitle = source.VariationTitle,
        LineType       = source.LineType,
        CostCode       = source.CostCode,
        Description    = source.Description,
        Unit           = source.Unit,
        Quantity       = source.Quantity,
        Rate           = source.Rate,
        LineAmount     = source.LineAmount,
        Comments       = source.Comments,
        DisplayOrder   = source.DisplayOrder
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (ValuationLineItemId, ProjectId, ElementType, SectionCode, SectionName,
                VariationRef, VariationTitle, LineType, CostCode, Description, Unit,
                Quantity, Rate, LineAmount, Comments, DisplayOrder)
        VALUES (source.ValuationLineItemId, source.ProjectId, source.ElementType,
                source.SectionCode, source.SectionName, source.VariationRef,
                source.VariationTitle, source.LineType, source.CostCode,
                source.Description, source.Unit, source.Quantity, source.Rate,
                source.LineAmount, source.Comments, source.DisplayOrder);

    PRINT '67 Beresford Road: variation orders & variation lines merged.';
    COMMIT TRAN;
END

-- Sanity check: variation lines should reconcile to the workbook register.
-- (@ProjectId is still in scope -- same batch.)
SELECT
    COUNT(*) AS VariationLines,                                                       -- 28 (V14/V16/V20/V25 declined, no lines)
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations  -- 21673.44
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId AND ElementType = 3;

-- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
SELECT
    SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 264504.00
    SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  21673.44
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 286177.44
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId;

-- VOQ mirror check: 32 VOs, 28 approved summing to the register net.
SELECT
    COUNT(*)                                            AS VariationOrders,  -- 32
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END)         AS ApprovedVOs,      -- 28
    SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END)         AS DeclinedVOs,      --  4
    SUM(CASE WHEN Status = 2 THEN Value ELSE 0 END)     AS NetVoValue        -- 21673.44
FROM [dbo].[VariationOrderQuotes]
WHERE ProjectId = @ProjectId;
GO
