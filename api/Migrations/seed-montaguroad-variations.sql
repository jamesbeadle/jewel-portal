-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: 72 Montagu Road -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : 72 Montagu Road SL3 9DY
-- ProjectId: resolved at run time by site-name matcher '72montaguroad'
--
-- Companion to seed-montaguroad-valuation.sql, which seeds ONLY the original
-- contract scope (Contract works / PC Sums / Contingency = Contract Sum
-- GBP 819,230.00). This file adds the post-contract VARIATION ORDERS from
-- "Revised Montagu Road Valuation 13 - August 25", reconciling to the
-- workbook's variations register:
--
--     Contract Sum            GBP 819,230.00
--     Net Variations          GBP -138,496.50
--     ----------------------------------------
--     Revised Contract Sum    GBP 680,733.50
--
-- MODEL NOTE (unified variation orders, post-20260723120000_UnifyVariationOrders)
-- Each workbook VO is split into multiple priced lines (omits of contract/PS
-- scope as negatives, new items as positives). On the JPMS valuation report a
-- VO shows as a SINGLE summary line, so we seed ONE ValuationLineItem per
-- APPROVED VO whose LineAmount is the NET of that VO's workbook lines
-- (Quantity 1 x Rate = net). Each VO is ONE row in VariationOrderQuotes
-- (the [VariationOrders] table no longer exists).
--
-- 33 VOs are APPROVED (Status 2): every VO whose rows carry priced amounts in
-- the register (including those still claimed at 0.00% whose value sits in
-- the Balance column, e.g. V02/V23/V24). Their nets sum to GBP -138,496.50
-- EXACTLY -- no penny adjustment was needed.
--
-- 11 VOs are PENDING QUOTES (Status 0 = Quoting): V09, V21, V22, V32, V33,
-- V36, V37, V39, V40, V42, V43. The workbook lists their rows with rates but
-- leaves the amount column empty, so they carry NO value in the register net.
-- They are seeded as Quoting with a best-effort EstimatedValue (sum of
-- qty x rate), Value 0, no VariationRef and NO ValuationLineItem row.
-- None of the register's VOs is marked Declined.
--
-- Judgement calls:
--   * V05a (Thames Water 32mm upgrade + design fee, +GBP 3,445.00) is the
--     workbook's only sub-lettered VO; it is folded into V05's net
--     (-11,375.00 + 3,445.00 = -7,930.00) so Number/Reference stay aligned
--     with V01..V44. Its later reversal sits in V44 as the workbook shows.
--   * V06 is a cost-neutral instruction (GBP 0.00, claimed at 0 in
--     Valuation 2); seeded Approved with net 0.
--   * Approval dates are derived from each VO's first claimed valuation month
--     (Valuation 1 = Aug 2024 ... Valuation 13 = Aug 2025): created ~20th of
--     the prior month, issued +7 days, approved +21 days. VOs claimed only in
--     the Balance column are dated against Valuation 13.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net >= 0 -> Priced (addition)
--   * net <  0 -> Omit   (net reduction; stored as a negative LineAmount)
--
-- Idempotent: keyed on stable ids (mr-voq-vNN / mr-vo-vNN) via MERGE. The
-- contract/PC/contingency lines seeded by seed-montaguroad-valuation.sql are
-- left untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '72montaguroad'
       OR LOWER(REPLACE(Name, ' ', '')) = '72montaguroad'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '72montaguroad' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  72 Montagu Road -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
MERGE INTO [dbo].[VariationOrderQuotes] AS target
USING (VALUES
    (N'mr-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Asbestos removal as per quote', N'Asbestos removal as per quote', 2, NULL, NULL, 4386.0000, N'V01', 4386.0000, N'ENABLE-ASB', '2024-07-20', N'seed@jewelgroup.co.uk', '2024-07-27', '2024-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'12.5mm plasterboard to ceilings - asbestos removal areas', N'12.5mm plasterboard to ceilings - asbestos removal areas', 2, NULL, NULL, 1160.0000, N'V02', 1160.0000, N'INT-PLB', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'EOT-01 - prelims extension 6 weeks (management, accommodation, H&S)', N'EOT-01 - prelims extension 6 weeks (management, accommodation, H&S)', 2, NULL, NULL, 5790.0000, N'V03', 5790.0000, N'PRELIMS-SMG', '2025-03-20', N'seed@jewelgroup.co.uk', '2025-03-27', '2025-04-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Foundation redesign - screw piles, RC raft, core drilling & structural steel', N'Foundation redesign - screw piles, RC raft, core drilling & structural steel', 2, NULL, NULL, 23595.5000, N'V04', 23595.5000, N'SUB-GWK', '2024-08-20', N'seed@jewelgroup.co.uk', '2024-08-27', '2024-09-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'Incoming services - trenching, SSE connection & gas disconnection (incl. V05a Thames Water 32mm upgrade)', N'Incoming services - trenching, SSE connection & gas disconnection (incl. V05a Thames Water 32mm upgrade)', 2, NULL, NULL, -7930.0000, N'V05', -7930.0000, N'UTIL-STD', '2024-08-20', N'seed@jewelgroup.co.uk', '2024-08-27', '2024-09-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Bathroom walls - 12978-09 rev B finishes - cost neutral', N'Bathroom walls - 12978-09 rev B finishes - cost neutral', 2, NULL, NULL, 0.0000, N'V06', 0.0000, N'INT-PLB', '2024-08-20', N'seed@jewelgroup.co.uk', '2024-08-27', '2024-09-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Omit client suggested air-conditioning PS (6.2.3)', N'Omit client suggested air-conditioning PS (6.2.3)', 2, NULL, NULL, -13500.0000, N'V07', -13500.0000, N'MEC-AC', '2024-08-20', N'seed@jewelgroup.co.uk', '2024-08-27', '2024-09-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'EOT-02 - prelims extension 4 weeks', N'EOT-02 - prelims extension 4 weeks', 2, NULL, NULL, 3860.0000, N'V08', 3860.0000, N'PRELIMS-SMG', '2025-05-20', N'seed@jewelgroup.co.uk', '2025-05-27', '2025-06-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Holistic heating & renewables - Baystar heat pump, air conditioning, solar & UFH (quote pending)', N'Holistic heating & renewables - Baystar heat pump, air conditioning, solar & UFH (quote pending)', 0, NULL, NULL, 23049.0000, NULL, 0.0000, NULL, '2025-05-20', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Electrical revisions - power, lighting, AV & data points', N'Electrical revisions - power, lighting, AV & data points', 2, NULL, NULL, 5327.0000, N'V10', 5327.0000, N'ELE-STD', '2025-05-20', N'seed@jewelgroup.co.uk', '2025-05-27', '2025-06-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'Omit smart home & environmental controls PS (6.3.4)', N'Omit smart home & environmental controls PS (6.3.4)', 2, NULL, NULL, -13500.0000, N'V11', -13500.0000, N'ELE-SPE', '2025-03-20', N'seed@jewelgroup.co.uk', '2025-03-27', '2025-04-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Intruder alarm & CCTV - supply & installation revision', N'Intruder alarm & CCTV - supply & installation revision', 2, NULL, NULL, 1605.0000, N'V12', 1605.0000, N'ELE-ALM', '2025-02-20', N'seed@jewelgroup.co.uk', '2025-02-27', '2025-03-13', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Structural steel revisions - masonry dwgs 24-056-100 P6 & 24-056-110 P4', N'Structural steel revisions - masonry dwgs 24-056-100 P6 & 24-056-110 P4', 2, NULL, NULL, 2650.0000, N'V13', 2650.0000, N'STR-STL', '2025-02-20', N'seed@jewelgroup.co.uk', '2025-02-27', '2025-03-13', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'Passenger lift omit - Gartec lift attendance & rubbish removal', N'Passenger lift omit - Gartec lift attendance & rubbish removal', 2, NULL, NULL, -25666.0000, N'V14', -25666.0000, N'SPEC-LFT', '2025-04-20', N'seed@jewelgroup.co.uk', '2025-04-27', '2025-05-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Omit Contingency Budget', N'Omit Contingency Budget', 2, NULL, NULL, -60000.0000, N'V15', -60000.0000, N'HAND-MSC', '2025-01-20', N'seed@jewelgroup.co.uk', '2025-01-27', '2025-02-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'Fire detection & alarm system revision - Cat LD2', N'Fire detection & alarm system revision - Cat LD2', 2, NULL, NULL, -175.0000, N'V16', -175.0000, N'ELE-FIR', '2025-02-20', N'seed@jewelgroup.co.uk', '2025-02-27', '2025-03-13', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'Omit kitchen & utility PS sums (3.7.2, 3.7.3)', N'Omit kitchen & utility PS sums (3.7.2, 3.7.3)', 2, NULL, NULL, -37000.0000, N'V17', -37000.0000, N'SUP-KIT', '2025-04-20', N'seed@jewelgroup.co.uk', '2025-04-27', '2025-05-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'Internal doors - Deanta prefinished supply in lieu of PS', N'Internal doors - Deanta prefinished supply in lieu of PS', 2, NULL, NULL, -1212.5000, N'V18', -1212.5000, N'SUP-DOR', '2025-04-20', N'seed@jewelgroup.co.uk', '2025-04-27', '2025-05-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v19', @ProjectId, N'', 19, N'VOQ-0019', N'Entrance door - Domadeco supply & installation in lieu of PS', N'Entrance door - Domadeco supply & installation in lieu of PS', 2, NULL, NULL, -738.0000, N'V19', -738.0000, N'WDR-TIM', '2025-04-20', N'seed@jewelgroup.co.uk', '2025-04-27', '2025-05-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v20', @ProjectId, N'', 20, N'VOQ-0020', N'Omit external canopy to kitchen & dining decked area', N'Omit external canopy to kitchen & dining decked area', 2, NULL, NULL, -17500.0000, N'V20', -17500.0000, N'SPEC-GAZ', '2025-03-20', N'seed@jewelgroup.co.uk', '2025-03-27', '2025-04-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v21', @ProjectId, N'', 21, N'VOQ-0021', N'Internal CCTV option', N'Internal CCTV option', 0, NULL, NULL, 2535.0000, NULL, 0.0000, NULL, '2025-06-05', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v22', @ProjectId, N'', 22, N'VOQ-0022', N'Scaffolding allowance', N'Scaffolding allowance', 0, NULL, NULL, 16225.0000, NULL, 0.0000, NULL, '2025-06-05', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v23', @ProjectId, N'', 23, N'VOQ-0023', N'Bathroom & sanitaryware revisions - AI-06 11.04.25', N'Bathroom & sanitaryware revisions - AI-06 11.04.25', 2, NULL, NULL, -4073.0000, N'V23', -4073.0000, N'SUP-SAN', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v24', @ProjectId, N'', 24, N'VOQ-0024', N'Wall & floor tiling revisions - 15 rev C / 16 rev B', N'Wall & floor tiling revisions - 15 rev C / 16 rev B', 2, NULL, NULL, -3283.5000, N'V24', -3283.5000, N'TIL-STD', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v25', @ProjectId, N'', 25, N'VOQ-0025', N'Rear decking structure - 24-056-300_P3', N'Rear decking structure - 24-056-300_P3', 2, NULL, NULL, 790.0000, N'V25', 790.0000, N'EXTW-DEK', '2025-06-20', N'seed@jewelgroup.co.uk', '2025-06-27', '2025-07-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v26', @ProjectId, N'', 26, N'VOQ-0026', N'Strong-Tie restraint straps, angle brackets, wall plate & joisting', N'Strong-Tie restraint straps, angle brackets, wall plate & joisting', 2, NULL, NULL, 1495.0000, N'V26', 1495.0000, N'CARP-1FX', '2025-06-20', N'seed@jewelgroup.co.uk', '2025-06-27', '2025-07-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v27', @ProjectId, N'', 27, N'VOQ-0027', N'10KVA generator hire - 10 weeks', N'10KVA generator hire - 10 weeks', 2, NULL, NULL, 4200.0000, N'V27', 4200.0000, N'UTIL-STD', '2025-06-20', N'seed@jewelgroup.co.uk', '2025-06-27', '2025-07-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v28', @ProjectId, N'', 28, N'VOQ-0028', N'Kitchen wall demolition, blockwork & site welder', N'Kitchen wall demolition, blockwork & site welder', 2, NULL, NULL, 2120.0000, N'V28', 2120.0000, N'MASON-BRK', '2025-06-20', N'seed@jewelgroup.co.uk', '2025-06-27', '2025-07-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v29', @ProjectId, N'', 29, N'VOQ-0029', N'Excavation & removal of fill U/S of the slab', N'Excavation & removal of fill U/S of the slab', 2, NULL, NULL, 480.0000, N'V29', 480.0000, N'SUB-EXC', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v30', @ProjectId, N'', 30, N'VOQ-0030', N'Plumbing alterations - bathroom & ensuite pipework, utility points', N'Plumbing alterations - bathroom & ensuite pipework, utility points', 2, NULL, NULL, 1123.0000, N'V30', 1123.0000, N'MEC-PLM', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v31', @ProjectId, N'', 31, N'VOQ-0031', N'Omit joinery storage unit PS sums (14.02-14.05)', N'Omit joinery storage unit PS sums (14.02-14.05)', 2, NULL, NULL, -8850.0000, N'V31', -8850.0000, N'CARP-JNR', '2025-04-20', N'seed@jewelgroup.co.uk', '2025-04-27', '2025-05-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v32', @ProjectId, N'', 32, N'VOQ-0032', N'EOT-03 - prelims extension 25 weeks & rubbish removal', N'EOT-03 - prelims extension 25 weeks & rubbish removal', 0, NULL, NULL, 28685.0000, NULL, 0.0000, NULL, '2025-07-05', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v33', @ProjectId, N'', 33, N'VOQ-0033', N'Hot-tub hoist PS omit & hoist base works - excavation, concrete & steel post', N'Hot-tub hoist PS omit & hoist base works - excavation, concrete & steel post', 0, NULL, NULL, -4650.0000, NULL, 0.0000, NULL, '2025-07-05', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v34', @ProjectId, N'', 34, N'VOQ-0034', N'Loft ladder - Dolle supply & installation', N'Loft ladder - Dolle supply & installation', 2, NULL, NULL, 65.0000, N'V34', 65.0000, N'CARP-2FX', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v35', @ProjectId, N'', 35, N'VOQ-0035', N'Omit flue lining, sweep & test (6.11.3)', N'Omit flue lining, sweep & test (6.11.3)', 2, NULL, NULL, -900.0000, N'V35', -900.0000, N'MEC-BLR', '2025-06-20', N'seed@jewelgroup.co.uk', '2025-06-27', '2025-07-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v36', @ProjectId, N'', 36, N'VOQ-0036', N'Omit replacement gas fire PS (14.11)', N'Omit replacement gas fire PS (14.11)', 0, NULL, NULL, -1500.0000, NULL, 0.0000, NULL, '2025-07-05', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v37', @ProjectId, N'', 37, N'VOQ-0037', N'Omit swim spa refurbishment PS (14.15)', N'Omit swim spa refurbishment PS (14.15)', 0, NULL, NULL, -2700.0000, NULL, 0.0000, NULL, '2025-07-05', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v38', @ProjectId, N'', 38, N'VOQ-0038', N'Lighting & electrical revisions - wall lights, LED & sockets', N'Lighting & electrical revisions - wall lights, LED & sockets', 2, NULL, NULL, 770.0000, N'V38', 770.0000, N'ELE-STD', '2025-06-20', N'seed@jewelgroup.co.uk', '2025-06-27', '2025-07-11', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v39', @ProjectId, N'', 39, N'VOQ-0039', N'New ceilings IC-01 revision - suspended ceilings, fireline & insulation', N'New ceilings IC-01 revision - suspended ceilings, fireline & insulation', 0, NULL, NULL, 6147.0000, NULL, 0.0000, NULL, '2025-07-12', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v40', @ProjectId, N'', 40, N'VOQ-0040', N'Ceiling strengthening IC-04 revision - carpenter labour & materials', N'Ceiling strengthening IC-04 revision - carpenter labour & materials', 0, NULL, NULL, 550.0000, NULL, 0.0000, NULL, '2025-07-12', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v41', @ProjectId, N'', 41, N'VOQ-0041', N'Bedroom 1 - suspended ceiling IC-05 - Option A', N'Bedroom 1 - suspended ceiling IC-05 - Option A', 2, NULL, NULL, 1235.0000, N'V41', 1235.0000, N'INT-MFC', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'mr-voq-v42', @ProjectId, N'', 42, N'VOQ-0042', N'Remove pipework within slab, shutter & concrete pour', N'Remove pipework within slab, shutter & concrete pour', 0, NULL, NULL, 220.0000, NULL, 0.0000, NULL, '2025-07-19', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v43', @ProjectId, N'', 43, N'VOQ-0043', N'Carpenter labour & materials - 12978-15 rev E & 12978-16 rev C', N'Carpenter labour & materials - 12978-15 rev E & 12978-16 rev C', 0, NULL, NULL, 3340.0000, NULL, 0.0000, NULL, '2025-07-19', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
    (N'mr-voq-v44', @ProjectId, N'', 44, N'VOQ-0044', N'Omit Cadet gas disconnection & Thames Water 32mm upgrade', N'Omit Cadet gas disconnection & Thames Water 32mm upgrade', 2, NULL, NULL, -4820.0000, N'V44', -4820.0000, N'UTIL-STD', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL)
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
    (N'mr-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Asbestos removal as per quote', 0, N'ENABLE-ASB', N'', N'item', 1.0000, 4386.0000, 4386.0000, N'', 1),
    (N'mr-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'12.5mm plasterboard to ceilings - asbestos removal areas', 0, N'INT-PLB', N'', N'item', 1.0000, 1160.0000, 1160.0000, N'', 2),
    (N'mr-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'EOT-01 - prelims extension 6 weeks (management, accommodation, H&S)', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 5790.0000, 5790.0000, N'', 3),
    (N'mr-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Foundation redesign - screw piles, RC raft, core drilling & structural steel', 0, N'SUB-GWK', N'', N'item', 1.0000, 23595.5000, 23595.5000, N'', 4),
    (N'mr-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'Incoming services - trenching, SSE connection & gas disconnection (incl. V05a Thames Water 32mm upgrade)', 2, N'UTIL-STD', N'', N'item', 1.0000, -7930.0000, -7930.0000, N'', 5),
    (N'mr-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Bathroom walls - 12978-09 rev B finishes - cost neutral', 0, N'INT-PLB', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 6),
    (N'mr-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Omit client suggested air-conditioning PS (6.2.3)', 2, N'MEC-AC', N'', N'item', 1.0000, -13500.0000, -13500.0000, N'', 7),
    (N'mr-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'EOT-02 - prelims extension 4 weeks', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 3860.0000, 3860.0000, N'', 8),
    (N'mr-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Electrical revisions - power, lighting, AV & data points', 0, N'ELE-STD', N'', N'item', 1.0000, 5327.0000, 5327.0000, N'', 9),
    (N'mr-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'Omit smart home & environmental controls PS (6.3.4)', 2, N'ELE-SPE', N'', N'item', 1.0000, -13500.0000, -13500.0000, N'', 10),
    (N'mr-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Intruder alarm & CCTV - supply & installation revision', 0, N'ELE-ALM', N'', N'item', 1.0000, 1605.0000, 1605.0000, N'', 11),
    (N'mr-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Structural steel revisions - masonry dwgs 24-056-100 P6 & 24-056-110 P4', 0, N'STR-STL', N'', N'item', 1.0000, 2650.0000, 2650.0000, N'', 12),
    (N'mr-vo-v14', @ProjectId, 3, N'', N'', N'V14', N'Passenger lift omit - Gartec lift attendance & rubbish removal', 2, N'SPEC-LFT', N'', N'item', 1.0000, -25666.0000, -25666.0000, N'', 13),
    (N'mr-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Omit Contingency Budget', 2, N'HAND-MSC', N'', N'item', 1.0000, -60000.0000, -60000.0000, N'', 14),
    (N'mr-vo-v16', @ProjectId, 3, N'', N'', N'V16', N'Fire detection & alarm system revision - Cat LD2', 2, N'ELE-FIR', N'', N'item', 1.0000, -175.0000, -175.0000, N'', 15),
    (N'mr-vo-v17', @ProjectId, 3, N'', N'', N'V17', N'Omit kitchen & utility PS sums (3.7.2, 3.7.3)', 2, N'SUP-KIT', N'', N'item', 1.0000, -37000.0000, -37000.0000, N'', 16),
    (N'mr-vo-v18', @ProjectId, 3, N'', N'', N'V18', N'Internal doors - Deanta prefinished supply in lieu of PS', 2, N'SUP-DOR', N'', N'item', 1.0000, -1212.5000, -1212.5000, N'', 17),
    (N'mr-vo-v19', @ProjectId, 3, N'', N'', N'V19', N'Entrance door - Domadeco supply & installation in lieu of PS', 2, N'WDR-TIM', N'', N'item', 1.0000, -738.0000, -738.0000, N'', 18),
    (N'mr-vo-v20', @ProjectId, 3, N'', N'', N'V20', N'Omit external canopy to kitchen & dining decked area', 2, N'SPEC-GAZ', N'', N'item', 1.0000, -17500.0000, -17500.0000, N'', 19),
    (N'mr-vo-v23', @ProjectId, 3, N'', N'', N'V23', N'Bathroom & sanitaryware revisions - AI-06 11.04.25', 2, N'SUP-SAN', N'', N'item', 1.0000, -4073.0000, -4073.0000, N'', 20),
    (N'mr-vo-v24', @ProjectId, 3, N'', N'', N'V24', N'Wall & floor tiling revisions - 15 rev C / 16 rev B', 2, N'TIL-STD', N'', N'item', 1.0000, -3283.5000, -3283.5000, N'', 21),
    (N'mr-vo-v25', @ProjectId, 3, N'', N'', N'V25', N'Rear decking structure - 24-056-300_P3', 0, N'EXTW-DEK', N'', N'item', 1.0000, 790.0000, 790.0000, N'', 22),
    (N'mr-vo-v26', @ProjectId, 3, N'', N'', N'V26', N'Strong-Tie restraint straps, angle brackets, wall plate & joisting', 0, N'CARP-1FX', N'', N'item', 1.0000, 1495.0000, 1495.0000, N'', 23),
    (N'mr-vo-v27', @ProjectId, 3, N'', N'', N'V27', N'10KVA generator hire - 10 weeks', 0, N'UTIL-STD', N'', N'item', 1.0000, 4200.0000, 4200.0000, N'', 24),
    (N'mr-vo-v28', @ProjectId, 3, N'', N'', N'V28', N'Kitchen wall demolition, blockwork & site welder', 0, N'MASON-BRK', N'', N'item', 1.0000, 2120.0000, 2120.0000, N'', 25),
    (N'mr-vo-v29', @ProjectId, 3, N'', N'', N'V29', N'Excavation & removal of fill U/S of the slab', 0, N'SUB-EXC', N'', N'item', 1.0000, 480.0000, 480.0000, N'', 26),
    (N'mr-vo-v30', @ProjectId, 3, N'', N'', N'V30', N'Plumbing alterations - bathroom & ensuite pipework, utility points', 0, N'MEC-PLM', N'', N'item', 1.0000, 1123.0000, 1123.0000, N'', 27),
    (N'mr-vo-v31', @ProjectId, 3, N'', N'', N'V31', N'Omit joinery storage unit PS sums (14.02-14.05)', 2, N'CARP-JNR', N'', N'item', 1.0000, -8850.0000, -8850.0000, N'', 28),
    (N'mr-vo-v34', @ProjectId, 3, N'', N'', N'V34', N'Loft ladder - Dolle supply & installation', 0, N'CARP-2FX', N'', N'item', 1.0000, 65.0000, 65.0000, N'', 29),
    (N'mr-vo-v35', @ProjectId, 3, N'', N'', N'V35', N'Omit flue lining, sweep & test (6.11.3)', 2, N'MEC-BLR', N'', N'item', 1.0000, -900.0000, -900.0000, N'', 30),
    (N'mr-vo-v38', @ProjectId, 3, N'', N'', N'V38', N'Lighting & electrical revisions - wall lights, LED & sockets', 0, N'ELE-STD', N'', N'item', 1.0000, 770.0000, 770.0000, N'', 31),
    (N'mr-vo-v41', @ProjectId, 3, N'', N'', N'V41', N'Bedroom 1 - suspended ceiling IC-05 - Option A', 0, N'INT-MFC', N'', N'item', 1.0000, 1235.0000, 1235.0000, N'', 32),
    (N'mr-vo-v44', @ProjectId, 3, N'', N'', N'V44', N'Omit Cadet gas disconnection & Thames Water 32mm upgrade', 2, N'UTIL-STD', N'', N'item', 1.0000, -4820.0000, -4820.0000, N'', 33)
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

-- Sanity check: variation lines should reconcile to the workbook register.
SELECT
    COUNT(*) AS VariationLines,                                                       -- 33
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations  -- -138496.50
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId AND ElementType = 3;

-- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
SELECT
    SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 819230.00
    SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  -- -138496.50
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 680733.50
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId;

    PRINT '72 Montagu Road: variation orders & variation lines merged.';
    COMMIT TRAN;
END
GO
