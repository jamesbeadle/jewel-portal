-- ============================================================================
-- NOTE: CostCode values use the canonical JBB Cost Code Master codes (v2.1, trade-prefixed) seeded
-- by seed-cost-centers.sql. Remapped from the original JBB-* buckets on
-- 2026-07-07 -- audit trail: scripts/cost-code-remap-review.csv.
-- Seed: Albany Mews -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : 2 Albany Mews  (JBB-2026-006)
-- ProjectId: 6642a865d657422fa51c8bf4c800e973
--
-- Companion to seed-albanymews-valuation.sql, which seeds ONLY the original
-- contract scope (Contract works / PC Sums / Contingency = Contract Sum
-- GBP 414,464.00). This file adds the post-contract VARIATION ORDERS from
-- "2 Albany Mews - Valuation 11 - April 26", reconciling to the workbook's
-- variations register:
--
--     Net Variations          GBP  40,823.67
--     Contract Sum            GBP 414,464.00
--     ----------------------------------------
--     Revised Contract Sum    GBP 455,287.67
--
-- MODEL NOTE
-- As with By France, each workbook VO is split into multiple priced lines
-- (omits of contract/PS scope as negatives, new items as positives). On the
-- JPMS valuation report a VO shows as a SINGLE summary line, so we seed ONE
-- ValuationLineItem per VO whose LineAmount is the NET of that VO's workbook
-- lines (Quantity 1 x Rate = net). VariationRef (V01..V41) is the code shown
-- on the report; VariationTitle is a headline for the VO's scope.
--
-- Rows the workbook marks "Decline" or "Complete FOC" (all within V40) carry
-- no contract-sum value and are excluded from that VO's net, matching the
-- register total. Items claimed at 0% (e.g. V40's BC works, V41) still carry
-- their value in the register and ARE included, so the file reconciles to
-- GBP 40,823.67. The V35/V40 anti-slip matting swap is netted per the
-- register's own labelling (the -450.00 reversal sits in V35, the -600.00 in
-- V40).
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--
-- This file seeds THREE tables so the variations exist as first-class records,
-- not just report lines:
--   1. ValuationLineItems     -- the Variations block on the valuation report
--   2. VariationOrderQuotes   -- the VOQ each VO was approved from (Approved)
--   3. VariationOrders        -- the real VO records the Variations tab, email
--                                triage picker and record-link tags read
--
-- Idempotent: keyed on stable ids (am-vo-vNN / am-voq-vNN / am-vord-vNN). A
-- re-run refreshes every field via MERGE. The contract/PC/contingency lines
-- seeded by seed-albanymews-valuation.sql are left untouched. Safe to run
-- repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'am-vo-v01', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V01', N'Asbestos removal - Goodbye Asbestos', 2, N'ENABLE-ASB', N'', N'item', 1.0000, -1835.0000, -1835.0000, N'', 1),
    (N'am-vo-v02', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V02', N'Structural steels - supply & installation', 2, N'STR-STL', N'', N'item', 1.0000, -805.0000, -805.0000, N'', 2),
    (N'am-vo-v03', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V03', N'Groundworks redesign - drainage, block & beam, foundations', 2, N'MEC-DRN', N'', N'item', 1.0000, -6.0000, -6.0000, N'', 3),
    (N'am-vo-v04', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V04', N'Relocate electrical board & gas meter', 2, N'UTIL-STD', N'', N'item', 1.0000, -160.0000, -160.0000, N'', 4),
    (N'am-vo-v05', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V05', N'Intruder / fire & smoke alarm revision', 2, N'ELE-FIR', N'', N'item', 1.0000, -3600.0000, -3600.0000, N'', 5),
    (N'am-vo-v06', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V06', N'Site Supervision - EOT-01', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 5860.0000, 5860.0000, N'', 6),
    (N'am-vo-v07', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V07', N'Windows, Doors & Entrance Door - Quote 9585', 0, N'WDR-TIM', N'', N'item', 1.0000, 16322.1700, 16322.1700, N'', 7),
    (N'am-vo-v08', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V08', N'Stiltz Homelift - Supply & Installation', 2, N'SPEC-LFT', N'', N'item', 1.0000, -2200.0000, -2200.0000, N'', 8),
    (N'am-vo-v09', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V09', N'Howden Kitchen, Worktop & Installation', 2, N'SUP-KIT', N'', N'item', 1.0000, -680.0000, -680.0000, N'', 9),
    (N'am-vo-v10', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V10', N'Split Level Air Conditioning', 2, N'MEC-AC', N'', N'item', 1.0000, -2415.0000, -2415.0000, N'', 10),
    (N'am-vo-v11', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V11', N'D&P Healthcare specialist sanitary ware', 2, N'SUP-SAN', N'', N'item', 1.0000, -18858.5000, -18858.5000, N'', 11),
    (N'am-vo-v12', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V12', N'Boiler adaptation works & radiators', 0, N'MEC-BLR', N'', N'item', 1.0000, 2030.0000, 2030.0000, N'', 12),
    (N'am-vo-v13', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V13', N'Structural steel & timber revisions (PS1/PS2, lintels)', 0, N'STR-STL', N'', N'item', 1.0000, 4865.0000, 4865.0000, N'', 13),
    (N'am-vo-v14', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V14', N'Site Supervision - EOT-02', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 4395.0000, 4395.0000, N'', 14),
    (N'am-vo-v15', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V15', N'Electrical - Velux SFS & blinds, staircase lighting', 0, N'WDR-SPG', N'', N'item', 1.0000, 2680.0000, 2680.0000, N'', 15),
    (N'am-vo-v16', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V16', N'GF Lounge alterations & fire lining', 0, N'FIRE-PSV', N'', N'item', 1.0000, 7126.0000, 7126.0000, N'', 16),
    (N'am-vo-v17', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V17', N'Continuous Timber Mopstick - Existing Staircase', 0, N'STAIR-TIM', N'', N'item', 1.0000, 2250.0000, 2250.0000, N'', 17),
    (N'am-vo-v18', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V18', N'Velux rooflights & blinds', 0, N'WDR-SPG', N'', N'item', 1.0000, 4500.0000, 4500.0000, N'', 18),
    (N'am-vo-v19', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V19', N'Electrical uplifts - sockets & external camera', 0, N'ELE-STD', N'', N'item', 1.0000, 1130.0000, 1130.0000, N'', 19),
    (N'am-vo-v20', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V20', N'GF cloakroom sanitary ware & accessories', 0, N'SUP-SAN', N'', N'item', 1.0000, 910.0000, 910.0000, N'', 20),
    (N'am-vo-v21', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V21', N'FF bathroom - BetteLux bath & fittings', 0, N'SUP-SAN', N'', N'item', 1.0000, 7495.0000, 7495.0000, N'', 21),
    (N'am-vo-v22', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V22', N'SF shower room refit', 0, N'SUP-SAN', N'', N'item', 1.0000, 7035.0000, 7035.0000, N'', 22),
    (N'am-vo-v23', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V23', N'Electrical additions - therapy room & GF lounge', 0, N'ELE-STD', N'', N'item', 1.0000, 2015.0000, 2015.0000, N'', 23),
    (N'am-vo-v24', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V24', N'Wall & floor tiling - PRO 131-APPENDIX 07 Walkington Rev C', 2, N'TIL-STD', N'', N'item', 1.0000, -1125.0000, -1125.0000, N'', 24),
    (N'am-vo-v25', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V25', N'Flooring - Karndean, Altro wetroom & staircase carpet', 0, N'STAIR-TIM', N'', N'item', 1.0000, 5540.0000, 5540.0000, N'', 25),
    (N'am-vo-v26', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V26', N'Double sockets - GF hallway & external', 0, N'ELE-STD', N'', N'item', 1.0000, 305.0000, 305.0000, N'', 26),
    (N'am-vo-v27', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V27', N'Entrance gate, clearance & brickwork', 0, N'EXTW-FEN', N'', N'item', 1.0000, 2300.0000, 2300.0000, N'', 27),
    (N'am-vo-v28', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V28', N'Velux GGU MK08 0070', 0, N'WDR-SPG', N'', N'item', 1.0000, 3300.0000, 3300.0000, N'', 28),
    (N'am-vo-v29', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V29', N'Dining & lounge electrical additions', 0, N'ELE-STD', N'', N'item', 1.0000, 705.0000, 705.0000, N'', 29),
    (N'am-vo-v30', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V30', N'External landscaping - Marshalls paving & Aco drain', 2, N'EXTW-PAV', N'', N'item', 1.0000, -620.0000, -620.0000, N'', 30),
    (N'am-vo-v31', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V31', N'Howdens door ironmongery & mag lock', 0, N'SUP-KIT', N'', N'item', 1.0000, 285.0000, 285.0000, N'', 31),
    (N'am-vo-v32', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V32', N'Blinds and curtains - omit provisional sum', 2, N'WIN-BLD', N'', N'item', 1.0000, -7000.0000, -7000.0000, N'', 32),
    (N'am-vo-v33', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V33', N'Contingency Budget - omit', 2, N'HAND-MSC', N'', N'item', 1.0000, -30000.0000, -30000.0000, N'', 33),
    (N'am-vo-v34', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V34', N'Utility room - Howdens supply, installation & structural works', 0, N'SUP-KIT', N'', N'item', 1.0000, 7485.0000, 7485.0000, N'', 34),
    (N'am-vo-v35', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V35', N'Balcony & sliding door remedial works', 0, N'WDR-ALU', N'', N'item', 1.0000, 4290.0000, 4290.0000, N'', 35),
    (N'am-vo-v36', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V36', N'Additional Electrics - GF Socket', 0, N'ELE-STD', N'', N'item', 1.0000, 450.0000, 450.0000, N'', 36),
    (N'am-vo-v37', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V37', N'Supply & Install Shower Screen', 0, N'SUP-SAN', N'', N'item', 1.0000, 535.0000, 535.0000, N'', 37),
    (N'am-vo-v38', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V38', N'Grab rails & bathroom accessories - FF', 0, N'SUP-SAN', N'', N'item', 1.0000, 700.0000, 700.0000, N'', 38),
    (N'am-vo-v39', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V39', N'Parcel box, decal sticker & radiator cover', 0, N'MEC-PLM', N'', N'item', 1.0000, 480.0000, 480.0000, N'', 39),
    (N'am-vo-v40', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V40', N'Building Control works - therapy room, utility & external', 0, N'UTIL-STD', N'', N'item', 1.0000, 13915.0000, 13915.0000, N'', 40),
    (N'am-vo-v41', N'6642a865d657422fa51c8bf4c800e973', 3, N'', N'', N'V41', N'Additional Plumbing works', 0, N'MEC-PLM', N'', N'item', 1.0000, 1225.0000, 1225.0000, N'', 41)
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
GO

-- Sanity check: variation lines should reconcile to the workbook register.
SELECT
    COUNT(*) AS VariationLines,                                                       -- 41
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, -- 40823.67
    SUM(LineAmount) AS GrossOfAllVoLines                                              -- 40823.67
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'6642a865d657422fa51c8bf4c800e973' AND ElementType = 3;
GO

-- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
SELECT
    SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 414464.00
    SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  40823.67
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 455287.67
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'6642a865d657422fa51c8bf4c800e973';
GO


-- ============================================================================
-- VARIATION ORDER RECORDS (VariationOrderQuotes + VariationOrders)
-- ----------------------------------------------------------------------------
-- The ValuationLineItems above are only the report's DISPLAY lines. The
-- Variations tab, the email-triage "Variation Order" picker and the record-
-- link tags all read the real VariationOrders table (and its parent VOQs), so
-- those records are seeded here too, one per VO, with matching VariationRef
-- (V01..V41) and net Value.
--
-- Every VOQ is seeded Approved (status 4) and every VO Issued (status 1) --
-- these are historical, instructed variations from the register. RequestId is
-- empty (no originating RFQ email exists for seeded history). Approval dates
-- are spread ~4 VOs/month from Jun 2025 to match the claim periods the
-- register shows the variations landing in.
--
-- VOQ Status: 0=Draft 1=Inviting 2=Tendering 3=Selected 4=Approved 5=Rejected
-- VO  Status: 0=Approved 1=Issued 2=Cancelled
--
-- Idempotent: keyed on am-voq-vNN / am-vord-vNN via MERGE.
-- ============================================================================

MERGE INTO [dbo].[VariationOrderQuotes] AS target
USING (VALUES
    (N'am-voq-v01', N'6642a865d657422fa51c8bf4c800e973', N'', 1, N'VOQ-0001', N'Asbestos removal - Goodbye Asbestos', N'Asbestos removal - Goodbye Asbestos', 4, NULL, NULL, -1835.0000, '2025-05-25', N'seed@jewelgroup.co.uk', '2025-06-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v02', N'6642a865d657422fa51c8bf4c800e973', N'', 2, N'VOQ-0002', N'Structural steels - supply & installation', N'Structural steels - supply & installation', 4, NULL, NULL, -805.0000, '2025-05-25', N'seed@jewelgroup.co.uk', '2025-06-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v03', N'6642a865d657422fa51c8bf4c800e973', N'', 3, N'VOQ-0003', N'Groundworks redesign - drainage, block & beam, foundations', N'Groundworks redesign - drainage, block & beam, foundations', 4, NULL, NULL, -6.0000, '2025-05-25', N'seed@jewelgroup.co.uk', '2025-06-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v04', N'6642a865d657422fa51c8bf4c800e973', N'', 4, N'VOQ-0004', N'Relocate electrical board & gas meter', N'Relocate electrical board & gas meter', 4, NULL, NULL, -160.0000, '2025-05-25', N'seed@jewelgroup.co.uk', '2025-06-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v05', N'6642a865d657422fa51c8bf4c800e973', N'', 5, N'VOQ-0005', N'Intruder / fire & smoke alarm revision', N'Intruder / fire & smoke alarm revision', 4, NULL, NULL, -3600.0000, '2025-06-24', N'seed@jewelgroup.co.uk', '2025-07-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v06', N'6642a865d657422fa51c8bf4c800e973', N'', 6, N'VOQ-0006', N'Site Supervision - EOT-01', N'Site Supervision - EOT-01', 4, NULL, NULL, 5860.0000, '2025-06-24', N'seed@jewelgroup.co.uk', '2025-07-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v07', N'6642a865d657422fa51c8bf4c800e973', N'', 7, N'VOQ-0007', N'Windows, Doors & Entrance Door - Quote 9585', N'Windows, Doors & Entrance Door - Quote 9585', 4, NULL, NULL, 16322.1700, '2025-06-24', N'seed@jewelgroup.co.uk', '2025-07-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v08', N'6642a865d657422fa51c8bf4c800e973', N'', 8, N'VOQ-0008', N'Stiltz Homelift - Supply & Installation', N'Stiltz Homelift - Supply & Installation', 4, NULL, NULL, -2200.0000, '2025-06-24', N'seed@jewelgroup.co.uk', '2025-07-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v09', N'6642a865d657422fa51c8bf4c800e973', N'', 9, N'VOQ-0009', N'Howden Kitchen, Worktop & Installation', N'Howden Kitchen, Worktop & Installation', 4, NULL, NULL, -680.0000, '2025-07-25', N'seed@jewelgroup.co.uk', '2025-08-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v10', N'6642a865d657422fa51c8bf4c800e973', N'', 10, N'VOQ-0010', N'Split Level Air Conditioning', N'Split Level Air Conditioning', 4, NULL, NULL, -2415.0000, '2025-07-25', N'seed@jewelgroup.co.uk', '2025-08-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v11', N'6642a865d657422fa51c8bf4c800e973', N'', 11, N'VOQ-0011', N'D&P Healthcare specialist sanitary ware', N'D&P Healthcare specialist sanitary ware', 4, NULL, NULL, -18858.5000, '2025-07-25', N'seed@jewelgroup.co.uk', '2025-08-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v12', N'6642a865d657422fa51c8bf4c800e973', N'', 12, N'VOQ-0012', N'Boiler adaptation works & radiators', N'Boiler adaptation works & radiators', 4, NULL, NULL, 2030.0000, '2025-07-25', N'seed@jewelgroup.co.uk', '2025-08-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v13', N'6642a865d657422fa51c8bf4c800e973', N'', 13, N'VOQ-0013', N'Structural steel & timber revisions (PS1/PS2, lintels)', N'Structural steel & timber revisions (PS1/PS2, lintels)', 4, NULL, NULL, 4865.0000, '2025-08-25', N'seed@jewelgroup.co.uk', '2025-09-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v14', N'6642a865d657422fa51c8bf4c800e973', N'', 14, N'VOQ-0014', N'Site Supervision - EOT-02', N'Site Supervision - EOT-02', 4, NULL, NULL, 4395.0000, '2025-08-25', N'seed@jewelgroup.co.uk', '2025-09-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v15', N'6642a865d657422fa51c8bf4c800e973', N'', 15, N'VOQ-0015', N'Electrical - Velux SFS & blinds, staircase lighting', N'Electrical - Velux SFS & blinds, staircase lighting', 4, NULL, NULL, 2680.0000, '2025-08-25', N'seed@jewelgroup.co.uk', '2025-09-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v16', N'6642a865d657422fa51c8bf4c800e973', N'', 16, N'VOQ-0016', N'GF Lounge alterations & fire lining', N'GF Lounge alterations & fire lining', 4, NULL, NULL, 7126.0000, '2025-08-25', N'seed@jewelgroup.co.uk', '2025-09-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v17', N'6642a865d657422fa51c8bf4c800e973', N'', 17, N'VOQ-0017', N'Continuous Timber Mopstick - Existing Staircase', N'Continuous Timber Mopstick - Existing Staircase', 4, NULL, NULL, 2250.0000, '2025-09-24', N'seed@jewelgroup.co.uk', '2025-10-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v18', N'6642a865d657422fa51c8bf4c800e973', N'', 18, N'VOQ-0018', N'Velux rooflights & blinds', N'Velux rooflights & blinds', 4, NULL, NULL, 4500.0000, '2025-09-24', N'seed@jewelgroup.co.uk', '2025-10-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v19', N'6642a865d657422fa51c8bf4c800e973', N'', 19, N'VOQ-0019', N'Electrical uplifts - sockets & external camera', N'Electrical uplifts - sockets & external camera', 4, NULL, NULL, 1130.0000, '2025-09-24', N'seed@jewelgroup.co.uk', '2025-10-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v20', N'6642a865d657422fa51c8bf4c800e973', N'', 20, N'VOQ-0020', N'GF cloakroom sanitary ware & accessories', N'GF cloakroom sanitary ware & accessories', 4, NULL, NULL, 910.0000, '2025-09-24', N'seed@jewelgroup.co.uk', '2025-10-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v21', N'6642a865d657422fa51c8bf4c800e973', N'', 21, N'VOQ-0021', N'FF bathroom - BetteLux bath & fittings', N'FF bathroom - BetteLux bath & fittings', 4, NULL, NULL, 7495.0000, '2025-10-25', N'seed@jewelgroup.co.uk', '2025-11-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v22', N'6642a865d657422fa51c8bf4c800e973', N'', 22, N'VOQ-0022', N'SF shower room refit', N'SF shower room refit', 4, NULL, NULL, 7035.0000, '2025-10-25', N'seed@jewelgroup.co.uk', '2025-11-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v23', N'6642a865d657422fa51c8bf4c800e973', N'', 23, N'VOQ-0023', N'Electrical additions - therapy room & GF lounge', N'Electrical additions - therapy room & GF lounge', 4, NULL, NULL, 2015.0000, '2025-10-25', N'seed@jewelgroup.co.uk', '2025-11-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v24', N'6642a865d657422fa51c8bf4c800e973', N'', 24, N'VOQ-0024', N'Wall & floor tiling - PRO 131-APPENDIX 07 Walkington Rev C', N'Wall & floor tiling - PRO 131-APPENDIX 07 Walkington Rev C', 4, NULL, NULL, -1125.0000, '2025-10-25', N'seed@jewelgroup.co.uk', '2025-11-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v25', N'6642a865d657422fa51c8bf4c800e973', N'', 25, N'VOQ-0025', N'Flooring - Karndean, Altro wetroom & staircase carpet', N'Flooring - Karndean, Altro wetroom & staircase carpet', 4, NULL, NULL, 5540.0000, '2025-11-24', N'seed@jewelgroup.co.uk', '2025-12-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v26', N'6642a865d657422fa51c8bf4c800e973', N'', 26, N'VOQ-0026', N'Double sockets - GF hallway & external', N'Double sockets - GF hallway & external', 4, NULL, NULL, 305.0000, '2025-11-24', N'seed@jewelgroup.co.uk', '2025-12-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v27', N'6642a865d657422fa51c8bf4c800e973', N'', 27, N'VOQ-0027', N'Entrance gate, clearance & brickwork', N'Entrance gate, clearance & brickwork', 4, NULL, NULL, 2300.0000, '2025-11-24', N'seed@jewelgroup.co.uk', '2025-12-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v28', N'6642a865d657422fa51c8bf4c800e973', N'', 28, N'VOQ-0028', N'Velux GGU MK08 0070', N'Velux GGU MK08 0070', 4, NULL, NULL, 3300.0000, '2025-11-24', N'seed@jewelgroup.co.uk', '2025-12-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v29', N'6642a865d657422fa51c8bf4c800e973', N'', 29, N'VOQ-0029', N'Dining & lounge electrical additions', N'Dining & lounge electrical additions', 4, NULL, NULL, 705.0000, '2025-12-25', N'seed@jewelgroup.co.uk', '2026-01-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v30', N'6642a865d657422fa51c8bf4c800e973', N'', 30, N'VOQ-0030', N'External landscaping - Marshalls paving & Aco drain', N'External landscaping - Marshalls paving & Aco drain', 4, NULL, NULL, -620.0000, '2025-12-25', N'seed@jewelgroup.co.uk', '2026-01-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v31', N'6642a865d657422fa51c8bf4c800e973', N'', 31, N'VOQ-0031', N'Howdens door ironmongery & mag lock', N'Howdens door ironmongery & mag lock', 4, NULL, NULL, 285.0000, '2025-12-25', N'seed@jewelgroup.co.uk', '2026-01-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v32', N'6642a865d657422fa51c8bf4c800e973', N'', 32, N'VOQ-0032', N'Blinds and curtains - omit provisional sum', N'Blinds and curtains - omit provisional sum', 4, NULL, NULL, -7000.0000, '2025-12-25', N'seed@jewelgroup.co.uk', '2026-01-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v33', N'6642a865d657422fa51c8bf4c800e973', N'', 33, N'VOQ-0033', N'Contingency Budget - omit', N'Contingency Budget - omit', 4, NULL, NULL, -30000.0000, '2026-01-25', N'seed@jewelgroup.co.uk', '2026-02-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v34', N'6642a865d657422fa51c8bf4c800e973', N'', 34, N'VOQ-0034', N'Utility room - Howdens supply, installation & structural works', N'Utility room - Howdens supply, installation & structural works', 4, NULL, NULL, 7485.0000, '2026-01-25', N'seed@jewelgroup.co.uk', '2026-02-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v35', N'6642a865d657422fa51c8bf4c800e973', N'', 35, N'VOQ-0035', N'Balcony & sliding door remedial works', N'Balcony & sliding door remedial works', 4, NULL, NULL, 4290.0000, '2026-01-25', N'seed@jewelgroup.co.uk', '2026-02-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v36', N'6642a865d657422fa51c8bf4c800e973', N'', 36, N'VOQ-0036', N'Additional Electrics - GF Socket', N'Additional Electrics - GF Socket', 4, NULL, NULL, 450.0000, '2026-01-25', N'seed@jewelgroup.co.uk', '2026-02-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v37', N'6642a865d657422fa51c8bf4c800e973', N'', 37, N'VOQ-0037', N'Supply & Install Shower Screen', N'Supply & Install Shower Screen', 4, NULL, NULL, 535.0000, '2026-02-22', N'seed@jewelgroup.co.uk', '2026-03-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v38', N'6642a865d657422fa51c8bf4c800e973', N'', 38, N'VOQ-0038', N'Grab rails & bathroom accessories - FF', N'Grab rails & bathroom accessories - FF', 4, NULL, NULL, 700.0000, '2026-02-22', N'seed@jewelgroup.co.uk', '2026-03-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v39', N'6642a865d657422fa51c8bf4c800e973', N'', 39, N'VOQ-0039', N'Parcel box, decal sticker & radiator cover', N'Parcel box, decal sticker & radiator cover', 4, NULL, NULL, 480.0000, '2026-02-22', N'seed@jewelgroup.co.uk', '2026-03-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v40', N'6642a865d657422fa51c8bf4c800e973', N'', 40, N'VOQ-0040', N'Building Control works - therapy room, utility & external', N'Building Control works - therapy room, utility & external', 4, NULL, NULL, 13915.0000, '2026-02-22', N'seed@jewelgroup.co.uk', '2026-03-15', N'seed@jewelgroup.co.uk'),
    (N'am-voq-v41', N'6642a865d657422fa51c8bf4c800e973', N'', 41, N'VOQ-0041', N'Additional Plumbing works', N'Additional Plumbing works', 4, NULL, NULL, 1225.0000, '2026-03-25', N'seed@jewelgroup.co.uk', '2026-04-15', N'seed@jewelgroup.co.uk')
) AS source (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
             Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
             CreatedAt, CreatedByEmail, ApprovedAt, ApprovedByEmail)
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
    CreatedAt               = source.CreatedAt,
    CreatedByEmail          = source.CreatedByEmail,
    ApprovedAt              = source.ApprovedAt,
    ApprovedByEmail         = source.ApprovedByEmail
WHEN NOT MATCHED BY TARGET THEN
    INSERT (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
            Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
            CreatedAt, CreatedByEmail, ApprovedAt, ApprovedByEmail)
    VALUES (source.VariationOrderQuoteId, source.ProjectId, source.RequestId, source.Number,
            source.Reference, source.Title, source.Description, source.Status,
            source.SelectedBidPackageId, source.SelectedSubcontractorId, source.EstimatedValue,
            source.CreatedAt, source.CreatedByEmail, source.ApprovedAt, source.ApprovedByEmail);
GO

MERGE INTO [dbo].[VariationOrders] AS target
USING (VALUES
    (N'am-vord-v01', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v01', N'', 1, N'V01', N'Asbestos removal - Goodbye Asbestos', N'Asbestos removal - Goodbye Asbestos', 1, -1835.0000, NULL, N'', '2025-06-15', N'seed@jewelgroup.co.uk', '2025-06-22', NULL),
    (N'am-vord-v02', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v02', N'', 2, N'V02', N'Structural steels - supply & installation', N'Structural steels - supply & installation', 1, -805.0000, NULL, N'', '2025-06-15', N'seed@jewelgroup.co.uk', '2025-06-22', NULL),
    (N'am-vord-v03', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v03', N'', 3, N'V03', N'Groundworks redesign - drainage, block & beam, foundations', N'Groundworks redesign - drainage, block & beam, foundations', 1, -6.0000, NULL, N'', '2025-06-15', N'seed@jewelgroup.co.uk', '2025-06-22', NULL),
    (N'am-vord-v04', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v04', N'', 4, N'V04', N'Relocate electrical board & gas meter', N'Relocate electrical board & gas meter', 1, -160.0000, NULL, N'', '2025-06-15', N'seed@jewelgroup.co.uk', '2025-06-22', NULL),
    (N'am-vord-v05', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v05', N'', 5, N'V05', N'Intruder / fire & smoke alarm revision', N'Intruder / fire & smoke alarm revision', 1, -3600.0000, NULL, N'', '2025-07-15', N'seed@jewelgroup.co.uk', '2025-07-22', NULL),
    (N'am-vord-v06', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v06', N'', 6, N'V06', N'Site Supervision - EOT-01', N'Site Supervision - EOT-01', 1, 5860.0000, NULL, N'', '2025-07-15', N'seed@jewelgroup.co.uk', '2025-07-22', NULL),
    (N'am-vord-v07', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v07', N'', 7, N'V07', N'Windows, Doors & Entrance Door - Quote 9585', N'Windows, Doors & Entrance Door - Quote 9585', 1, 16322.1700, NULL, N'', '2025-07-15', N'seed@jewelgroup.co.uk', '2025-07-22', NULL),
    (N'am-vord-v08', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v08', N'', 8, N'V08', N'Stiltz Homelift - Supply & Installation', N'Stiltz Homelift - Supply & Installation', 1, -2200.0000, NULL, N'', '2025-07-15', N'seed@jewelgroup.co.uk', '2025-07-22', NULL),
    (N'am-vord-v09', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v09', N'', 9, N'V09', N'Howden Kitchen, Worktop & Installation', N'Howden Kitchen, Worktop & Installation', 1, -680.0000, NULL, N'', '2025-08-15', N'seed@jewelgroup.co.uk', '2025-08-22', NULL),
    (N'am-vord-v10', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v10', N'', 10, N'V10', N'Split Level Air Conditioning', N'Split Level Air Conditioning', 1, -2415.0000, NULL, N'', '2025-08-15', N'seed@jewelgroup.co.uk', '2025-08-22', NULL),
    (N'am-vord-v11', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v11', N'', 11, N'V11', N'D&P Healthcare specialist sanitary ware', N'D&P Healthcare specialist sanitary ware', 1, -18858.5000, NULL, N'', '2025-08-15', N'seed@jewelgroup.co.uk', '2025-08-22', NULL),
    (N'am-vord-v12', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v12', N'', 12, N'V12', N'Boiler adaptation works & radiators', N'Boiler adaptation works & radiators', 1, 2030.0000, NULL, N'', '2025-08-15', N'seed@jewelgroup.co.uk', '2025-08-22', NULL),
    (N'am-vord-v13', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v13', N'', 13, N'V13', N'Structural steel & timber revisions (PS1/PS2, lintels)', N'Structural steel & timber revisions (PS1/PS2, lintels)', 1, 4865.0000, NULL, N'', '2025-09-15', N'seed@jewelgroup.co.uk', '2025-09-22', NULL),
    (N'am-vord-v14', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v14', N'', 14, N'V14', N'Site Supervision - EOT-02', N'Site Supervision - EOT-02', 1, 4395.0000, NULL, N'', '2025-09-15', N'seed@jewelgroup.co.uk', '2025-09-22', NULL),
    (N'am-vord-v15', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v15', N'', 15, N'V15', N'Electrical - Velux SFS & blinds, staircase lighting', N'Electrical - Velux SFS & blinds, staircase lighting', 1, 2680.0000, NULL, N'', '2025-09-15', N'seed@jewelgroup.co.uk', '2025-09-22', NULL),
    (N'am-vord-v16', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v16', N'', 16, N'V16', N'GF Lounge alterations & fire lining', N'GF Lounge alterations & fire lining', 1, 7126.0000, NULL, N'', '2025-09-15', N'seed@jewelgroup.co.uk', '2025-09-22', NULL),
    (N'am-vord-v17', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v17', N'', 17, N'V17', N'Continuous Timber Mopstick - Existing Staircase', N'Continuous Timber Mopstick - Existing Staircase', 1, 2250.0000, NULL, N'', '2025-10-15', N'seed@jewelgroup.co.uk', '2025-10-22', NULL),
    (N'am-vord-v18', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v18', N'', 18, N'V18', N'Velux rooflights & blinds', N'Velux rooflights & blinds', 1, 4500.0000, NULL, N'', '2025-10-15', N'seed@jewelgroup.co.uk', '2025-10-22', NULL),
    (N'am-vord-v19', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v19', N'', 19, N'V19', N'Electrical uplifts - sockets & external camera', N'Electrical uplifts - sockets & external camera', 1, 1130.0000, NULL, N'', '2025-10-15', N'seed@jewelgroup.co.uk', '2025-10-22', NULL),
    (N'am-vord-v20', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v20', N'', 20, N'V20', N'GF cloakroom sanitary ware & accessories', N'GF cloakroom sanitary ware & accessories', 1, 910.0000, NULL, N'', '2025-10-15', N'seed@jewelgroup.co.uk', '2025-10-22', NULL),
    (N'am-vord-v21', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v21', N'', 21, N'V21', N'FF bathroom - BetteLux bath & fittings', N'FF bathroom - BetteLux bath & fittings', 1, 7495.0000, NULL, N'', '2025-11-15', N'seed@jewelgroup.co.uk', '2025-11-22', NULL),
    (N'am-vord-v22', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v22', N'', 22, N'V22', N'SF shower room refit', N'SF shower room refit', 1, 7035.0000, NULL, N'', '2025-11-15', N'seed@jewelgroup.co.uk', '2025-11-22', NULL),
    (N'am-vord-v23', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v23', N'', 23, N'V23', N'Electrical additions - therapy room & GF lounge', N'Electrical additions - therapy room & GF lounge', 1, 2015.0000, NULL, N'', '2025-11-15', N'seed@jewelgroup.co.uk', '2025-11-22', NULL),
    (N'am-vord-v24', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v24', N'', 24, N'V24', N'Wall & floor tiling - PRO 131-APPENDIX 07 Walkington Rev C', N'Wall & floor tiling - PRO 131-APPENDIX 07 Walkington Rev C', 1, -1125.0000, NULL, N'', '2025-11-15', N'seed@jewelgroup.co.uk', '2025-11-22', NULL),
    (N'am-vord-v25', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v25', N'', 25, N'V25', N'Flooring - Karndean, Altro wetroom & staircase carpet', N'Flooring - Karndean, Altro wetroom & staircase carpet', 1, 5540.0000, NULL, N'', '2025-12-15', N'seed@jewelgroup.co.uk', '2025-12-22', NULL),
    (N'am-vord-v26', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v26', N'', 26, N'V26', N'Double sockets - GF hallway & external', N'Double sockets - GF hallway & external', 1, 305.0000, NULL, N'', '2025-12-15', N'seed@jewelgroup.co.uk', '2025-12-22', NULL),
    (N'am-vord-v27', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v27', N'', 27, N'V27', N'Entrance gate, clearance & brickwork', N'Entrance gate, clearance & brickwork', 1, 2300.0000, NULL, N'', '2025-12-15', N'seed@jewelgroup.co.uk', '2025-12-22', NULL),
    (N'am-vord-v28', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v28', N'', 28, N'V28', N'Velux GGU MK08 0070', N'Velux GGU MK08 0070', 1, 3300.0000, NULL, N'', '2025-12-15', N'seed@jewelgroup.co.uk', '2025-12-22', NULL),
    (N'am-vord-v29', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v29', N'', 29, N'V29', N'Dining & lounge electrical additions', N'Dining & lounge electrical additions', 1, 705.0000, NULL, N'', '2026-01-15', N'seed@jewelgroup.co.uk', '2026-01-22', NULL),
    (N'am-vord-v30', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v30', N'', 30, N'V30', N'External landscaping - Marshalls paving & Aco drain', N'External landscaping - Marshalls paving & Aco drain', 1, -620.0000, NULL, N'', '2026-01-15', N'seed@jewelgroup.co.uk', '2026-01-22', NULL),
    (N'am-vord-v31', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v31', N'', 31, N'V31', N'Howdens door ironmongery & mag lock', N'Howdens door ironmongery & mag lock', 1, 285.0000, NULL, N'', '2026-01-15', N'seed@jewelgroup.co.uk', '2026-01-22', NULL),
    (N'am-vord-v32', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v32', N'', 32, N'V32', N'Blinds and curtains - omit provisional sum', N'Blinds and curtains - omit provisional sum', 1, -7000.0000, NULL, N'', '2026-01-15', N'seed@jewelgroup.co.uk', '2026-01-22', NULL),
    (N'am-vord-v33', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v33', N'', 33, N'V33', N'Contingency Budget - omit', N'Contingency Budget - omit', 1, -30000.0000, NULL, N'', '2026-02-15', N'seed@jewelgroup.co.uk', '2026-02-22', NULL),
    (N'am-vord-v34', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v34', N'', 34, N'V34', N'Utility room - Howdens supply, installation & structural works', N'Utility room - Howdens supply, installation & structural works', 1, 7485.0000, NULL, N'', '2026-02-15', N'seed@jewelgroup.co.uk', '2026-02-22', NULL),
    (N'am-vord-v35', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v35', N'', 35, N'V35', N'Balcony & sliding door remedial works', N'Balcony & sliding door remedial works', 1, 4290.0000, NULL, N'', '2026-02-15', N'seed@jewelgroup.co.uk', '2026-02-22', NULL),
    (N'am-vord-v36', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v36', N'', 36, N'V36', N'Additional Electrics - GF Socket', N'Additional Electrics - GF Socket', 1, 450.0000, NULL, N'', '2026-02-15', N'seed@jewelgroup.co.uk', '2026-02-22', NULL),
    (N'am-vord-v37', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v37', N'', 37, N'V37', N'Supply & Install Shower Screen', N'Supply & Install Shower Screen', 1, 535.0000, NULL, N'', '2026-03-15', N'seed@jewelgroup.co.uk', '2026-03-22', NULL),
    (N'am-vord-v38', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v38', N'', 38, N'V38', N'Grab rails & bathroom accessories - FF', N'Grab rails & bathroom accessories - FF', 1, 700.0000, NULL, N'', '2026-03-15', N'seed@jewelgroup.co.uk', '2026-03-22', NULL),
    (N'am-vord-v39', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v39', N'', 39, N'V39', N'Parcel box, decal sticker & radiator cover', N'Parcel box, decal sticker & radiator cover', 1, 480.0000, NULL, N'', '2026-03-15', N'seed@jewelgroup.co.uk', '2026-03-22', NULL),
    (N'am-vord-v40', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v40', N'', 40, N'V40', N'Building Control works - therapy room, utility & external', N'Building Control works - therapy room, utility & external', 1, 13915.0000, NULL, N'', '2026-03-15', N'seed@jewelgroup.co.uk', '2026-03-22', NULL),
    (N'am-vord-v41', N'6642a865d657422fa51c8bf4c800e973', N'am-voq-v41', N'', 41, N'V41', N'Additional Plumbing works', N'Additional Plumbing works', 1, 1225.0000, NULL, N'', '2026-04-15', N'seed@jewelgroup.co.uk', '2026-04-22', NULL)
) AS source (VariationOrderId, ProjectId, VariationOrderQuoteId, RequestId, Number, VariationRef,
             Title, Description, Status, Value, SubcontractorId, CostCode,
             ApprovedAt, ApprovedByEmail, IssuedAt, CancelledAt)
ON target.VariationOrderId = source.VariationOrderId
WHEN MATCHED THEN UPDATE SET
    ProjectId             = source.ProjectId,
    VariationOrderQuoteId = source.VariationOrderQuoteId,
    RequestId             = source.RequestId,
    Number                = source.Number,
    VariationRef          = source.VariationRef,
    Title                 = source.Title,
    Description           = source.Description,
    Status                = source.Status,
    Value                 = source.Value,
    SubcontractorId       = source.SubcontractorId,
    CostCode              = source.CostCode,
    ApprovedAt            = source.ApprovedAt,
    ApprovedByEmail       = source.ApprovedByEmail,
    IssuedAt              = source.IssuedAt,
    CancelledAt           = source.CancelledAt
WHEN NOT MATCHED BY TARGET THEN
    INSERT (VariationOrderId, ProjectId, VariationOrderQuoteId, RequestId, Number, VariationRef,
            Title, Description, Status, Value, SubcontractorId, CostCode,
            ApprovedAt, ApprovedByEmail, IssuedAt, CancelledAt)
    VALUES (source.VariationOrderId, source.ProjectId, source.VariationOrderQuoteId,
            source.RequestId, source.Number, source.VariationRef, source.Title,
            source.Description, source.Status, source.Value, source.SubcontractorId,
            source.CostCode, source.ApprovedAt, source.ApprovedByEmail,
            source.IssuedAt, source.CancelledAt);
GO

-- Sanity check: VO records should mirror the valuation report's variation lines
-- exactly -- one per line, same refs, same net total.
SELECT
    (SELECT COUNT(*) FROM [dbo].[VariationOrders]      WHERE ProjectId = N'6642a865d657422fa51c8bf4c800e973') AS VariationOrders,      -- 41
    (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = N'6642a865d657422fa51c8bf4c800e973') AS VariationOrderQuotes, -- 41
    (SELECT SUM(Value) FROM [dbo].[VariationOrders]    WHERE ProjectId = N'6642a865d657422fa51c8bf4c800e973' AND Status <> 2) AS NetVoValue, -- 40823.67
    (SELECT COUNT(*)
       FROM [dbo].[VariationOrders] vo
       LEFT JOIN [dbo].[ValuationLineItems] li
         ON li.ProjectId = vo.ProjectId AND li.ElementType = 3 AND li.VariationRef = vo.VariationRef
      WHERE vo.ProjectId = N'6642a865d657422fa51c8bf4c800e973' AND li.ValuationLineItemId IS NULL) AS VosMissingReportLine; -- 0
GO
