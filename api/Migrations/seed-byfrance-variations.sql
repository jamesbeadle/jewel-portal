-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql. Remapped from the original JBB-* buckets on
-- 2026-07-07 -- audit trail: scripts/cost-code-remap-review.csv.
-- Seed: By France -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : By France, Leas Green, Chislehurst, BR7 6HD  (JBB-2026-001)
-- ProjectId: 3490f944b29545c4b8d5a04130f42ab8
--
-- Companion to seed-byfrance-valuation.sql, which seeds ONLY the original
-- contract scope (Contract works / PC Sums / Contingency = Contract Sum
-- GBP 1,780,455.00). This file adds the post-contract VARIATION ORDERS from
-- "By France - Valuation 18 - June 26", reconciling to the workbook's
-- variations register (cells F343:F725):
--
--     Net Variations          GBP   215,737.58
--     Contract Sum            GBP 1,780,455.00
--     -----------------------------------------
--     Revised Contract Sum    GBP 1,996,192.58
--
-- MODEL NOTE
-- In the workbook each VO is split into multiple priced lines (and four VOs
-- have their own detail tabs: V69, V72, V73, V76). On the JPMS valuation
-- report a VO shows as a SINGLE summary line; the per-line detail and
-- per-VO % complete will be driven by click-through functionality built
-- later. We therefore seed ONE ValuationLineItem per VO, whose LineAmount is
-- the NET of that VO's workbook lines (Quantity 1 x Rate = net). VariationRef
-- (V01..V76) is the code shown on the report; VariationTitle is the VO's
-- headline description from the register.
--
-- EXCEPTION -- V48 (2026-07-14): seeded as its FOUR workbook detail lines
-- (bf-vo-v48a..d) instead of one net line, so each omit carries its correct
-- cost code (joinery vs fireplaces vs decorating). The VOQ/VO records remain
-- a single V48 at the -60,175.00 net. The retired single line bf-vo-v48 (and
-- its Claim 18 line) is deleted below before the MERGE.
--
-- EXCEPTION -- V10 (2026-07-22): seeded as TWO trade lines (bf-vo-v10a..b)
-- instead of one net line: Groundworks 10,010.00 (SUB-GWK -- ground gas /
-- drainage omits -15,150.00 plus surface water & foul drainage new items
-- 25,160.00) and Plumber -4,894.00 (MEC-PLM -- stub stack, soil vent &
-- wastes omits). The VOQ/VO records remain a single V10 at the 5,116.00
-- net. The retired single line bf-vo-v10 (and its Claim 18 line) is deleted
-- below before the MERGE.
--
-- EXCEPTION -- V42 (2026-07-21): seeded as TWO detail lines (bf-vo-v42a..b)
-- instead of one net line: the Tile Supply Only line 14,435.00 (SUP-TIL,
-- revised in V69 with omit and add back) and the remaining tiling /
-- splashback / shower-room lines netting -29,012.00 (TIL-STD, omits moved to
-- V66/V76). The VOQ/VO records remain a single V42 at the -14,577.00 net.
-- The retired single line bf-vo-v42 (and its Claim 18 line) is deleted below
-- before the MERGE.
--
-- EXCEPTION -- V06 (2026-07-21): seeded as TWO detail lines (bf-vo-v06a..b)
-- instead of one net line: Site Supervision 750.00 (PRELIMS-SMG) and
-- Temporary toilet & health, safety and welfare 860.00 (PRELIMS-WEL), so the
-- welfare spend no longer sits under the site-manager cost code. The VOQ/VO
-- records remain a single V06 at the 1,610.00 net. The retired single line
-- bf-vo-v06 (and its Claim 18 line) is deleted below before the MERGE.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--   * net = 0  -> Declined / Tbc (recorded, not priced into totals)
-- Declined / Tbc lines do NOT count toward Net Variations (matches
-- ValuationLineItem.CountsTowardTotals); every such VO here nets to 0 so the
-- workbook total is unaffected. NOTE: V69/V72/V73/V76 carry a "TBC" annotation
-- in the workbook but ARE included in its F343:F725 net, so they are seeded as
-- priced/omit (counting) to reconcile to GBP 215,737.58.
--
-- Idempotent: keyed on stable ids (bf-vo-vNN valuation lines, bf-voq-vNN /
-- bf-vord-vNN VOQ and VO records -- see the VARIATION ORDER RECORDS section
-- below). A re-run refreshes every field via MERGE. The contract/PC/contingency lines seeded by
-- seed-byfrance-valuation.sql are left untouched. Safe to run repeatedly.
-- ============================================================================

-- Retire the original single-net V48 line (replaced by bf-vo-v48a..d below).
DELETE FROM [dbo].[ClaimLines]         WHERE ValuationLineItemId = N'bf-vo-v48';
DELETE FROM [dbo].[ValuationLineItems] WHERE ValuationLineItemId = N'bf-vo-v48';
GO

-- Retire the original single-net V06 line (replaced by bf-vo-v06a..b below).
DELETE FROM [dbo].[ClaimLines]         WHERE ValuationLineItemId = N'bf-vo-v06';
DELETE FROM [dbo].[ValuationLineItems] WHERE ValuationLineItemId = N'bf-vo-v06';
GO

-- Retire the original single-net V10 line (replaced by bf-vo-v10a..b below).
DELETE FROM [dbo].[ClaimLines]         WHERE ValuationLineItemId = N'bf-vo-v10';
DELETE FROM [dbo].[ValuationLineItems] WHERE ValuationLineItemId = N'bf-vo-v10';
GO

-- Retire the original single-net V42 line (replaced by bf-vo-v42a..b below).
DELETE FROM [dbo].[ClaimLines]         WHERE ValuationLineItemId = N'bf-vo-v42';
DELETE FROM [dbo].[ValuationLineItems] WHERE ValuationLineItemId = N'bf-vo-v42';
GO

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'bf-vo-v01', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V01', N'Removal of Asbestos', 2, N'ENABLE-ASB', N'', N'item', 1.0000, -5500.0000, -5500.0000, N'', 1),
    (N'bf-vo-v03', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V03', N'All assosiated work - Provisional sum', 0, N'UTIL-TRN', N'', N'item', 1.0000, 27797.0000, 27797.0000, N'', 2),
    (N'bf-vo-v04', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V04', N'Temporary Works - Demolition Phase 1& 2 - TW-200', 0, N'ENABLE-STS', N'', N'item', 1.0000, 1135.0000, 1135.0000, N'', 3),
    (N'bf-vo-v05', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V05', N'Site Supervision 8 days - July 24', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 2860.0000, 2860.0000, N'', 4),
    (N'bf-vo-v06a', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V06', N'Site Supervision 3 days - August 24', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 750.0000, 750.0000, N'', 5),
    (N'bf-vo-v06b', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V06', N'Temporary toilet & health, safety and welfare - August 24', 0, N'PRELIMS-WEL', N'', N'item', 1.0000, 860.0000, 860.0000, N'', 6),
    (N'bf-vo-v07', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V07', N'Site Supervision 3 days - Sept 24', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 1360.0000, 1360.0000, N'', 7),
    (N'bf-vo-v08', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V08', N'Site Supervision 2 days - Oct 24', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 1360.0000, 1360.0000, N'', 8),
    (N'bf-vo-v09', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V09', N'Site Supervision 2 days - Nov 24', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 1360.0000, 1360.0000, N'', 9),
    (N'bf-vo-v10a', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V10', N'Groundworks - Ground gas, drainage & surface water works (omits + new items)', 0, N'SUB-GWK', N'', N'item', 1.0000, 10010.0000, 10010.0000, N'', 10),
    (N'bf-vo-v10b', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V10', N'Plumber - Stub stack, soil vent pipework & wastes connections omits', 2, N'MEC-PLM', N'', N'item', 1.0000, -4894.0000, -4894.0000, N'', 11),
    (N'bf-vo-v11', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V11', N'Connection Details - Steel Fabrication', 0, N'STR-STL', N'', N'item', 1.0000, 4125.0000, 4125.0000, N'', 12),
    (N'bf-vo-v12', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V12', N'Tree Protection - STEM Arb Report', 0, N'PRELIMS-SET', N'', N'item', 1.0000, 2100.0000, 2100.0000, N'', 13),
    (N'bf-vo-v13', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V13', N'Thames Sewer Connections - MC0476 Model1-CIV10', 0, N'SUB-DRN', N'', N'item', 1.0000, 6396.0000, 6396.0000, N'', 14),
    (N'bf-vo-v14', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V14', N'Driveway Porous Stone - MC0476 Model1-CIV10', 0, N'EXTW-PAV', N'', N'item', 1.0000, 42936.0000, 42936.0000, N'', 15),
    (N'bf-vo-v15', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V15', N'Site Supervision - EOT-01', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 5860.0000, 5860.0000, N'', 16),
    (N'bf-vo-v16', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V16', N'Pond', 0, N'EXTW-LND', N'', N'item', 1.0000, 26351.0000, 26351.0000, N'', 17),
    (N'bf-vo-v17', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V17', N'100mm Ducting to the entrance gates', 0, N'EXTW-FEN', N'', N'item', 1.0000, 1720.0000, 1720.0000, N'', 18),
    (N'bf-vo-v18', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V18', N'Aluminium windows - throughout', 0, N'WDR-ALU', N'', N'item', 1.0000, 11474.1000, 11474.1000, N'', 19),
    (N'bf-vo-v19', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V19', N'Additional Drainage TBC', 4, N'MEC-DRN', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 20),
    (N'bf-vo-v20', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V20', N'4 No Canopies', 2, N'SPEC-GAZ', N'', N'item', 1.0000, -22200.0000, -22200.0000, N'', 21),
    (N'bf-vo-v21', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V21', N'W-07 Rebuild Opening - PRO-064-(WD)-P-004 Rev G', 0, N'MASON-BRK', N'', N'item', 1.0000, 400.0000, 400.0000, N'', 22),
    (N'bf-vo-v22', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V22', N'SW.1 - G11 - SITE WORKS - Rev B', 0, N'EXTW-LND', N'', N'item', 1.0000, 7600.0000, 7600.0000, N'', 23),
    (N'bf-vo-v23', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V23', N'Chandelier', 0, N'ELE-STD', N'', N'item', 1.0000, 29533.0000, 29533.0000, N'', 24),
    (N'bf-vo-v24', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V24', N'Wet underfloor heating to ground floor (carers only)', 0, N'MEC-UFH', N'', N'item', 1.0000, 4093.0000, 4093.0000, N'', 25),
    (N'bf-vo-v25', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V25', N'Site Supervision - EOT-02', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 3516.0000, 3516.0000, N'', 26),
    (N'bf-vo-v26', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V26', N'Air conditioning', 0, N'MEC-AC', N'', N'item', 1.0000, 8925.0000, 8925.0000, N'', 27),
    (N'bf-vo-v27', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V27', N'Intruder Alarm', 2, N'ELE-ALM', N'', N'item', 1.0000, -6640.0000, -6640.0000, N'', 28),
    (N'bf-vo-v28', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V28', N'Fire and smoke alarm', 2, N'ELE-FIR', N'', N'item', 1.0000, -1310.0000, -1310.0000, N'', 29),
    (N'bf-vo-v29', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V29', N'Invoice 6764 - Croft Structural Engineers', 0, N'HAND-SPE', N'', N'item', 1.0000, 330.0000, 330.0000, N'', 30),
    (N'bf-vo-v30', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V30', N'10 mm render to internal blockwork walls', 0, N'INT-RDR', N'', N'item', 1.0000, 29185.0000, 29185.0000, N'', 31),
    (N'bf-vo-v31', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V31', N'Sanitary ware for Cloakroom', 2, N'SUP-SAN', N'', N'item', 1.0000, -12240.0000, -12240.0000, N'', 32),
    (N'bf-vo-v32', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V32', N'Supply of feature staircase', 2, N'STAIR-TIM', N'', N'item', 1.0000, -1665.0000, -1665.0000, N'', 33),
    (N'bf-vo-v33', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V33', N'Structural Steel Works - SL30 Rev 9 & SL40 Rev 8', 0, N'STR-STL', N'', N'item', 1.0000, 1870.0000, 1870.0000, N'', 34),
    (N'bf-vo-v34', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V34', N'Fix only - WC', 0, N'SUP-SAN', N'', N'item', 1.0000, 11965.0000, 11965.0000, N'', 35),
    (N'bf-vo-v35', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V35', N'Site Supervision - EOT-03', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 8790.0000, 8790.0000, N'', 36),
    (N'bf-vo-v36', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V36', N'Annex kitchen/Utility room', 0, N'SUP-KIT', N'', N'item', 1.0000, 17935.0000, 17935.0000, N'', 37),
    (N'bf-vo-v37', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V37', N'GF - ZZ Bathroom - On the Level Shower Former', 0, N'SUP-SAN', N'', N'item', 1.0000, 4245.0000, 4245.0000, N'', 38),
    (N'bf-vo-v38', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V38', N'New 6ft timber fencing', 0, N'EXTW-FEN', N'', N'item', 1.0000, 23650.0000, 23650.0000, N'', 39),
    (N'bf-vo-v39', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V39', N'Demolition - Removal of Fencing & Existing trees', 3, N'ENABLE-DEM', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 40),
    (N'bf-vo-v40', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V40', N'Electrics - P-151 Rev P Electrical Plan FF', 0, N'ELE-STD', N'', N'item', 1.0000, 790.0000, 790.0000, N'', 41),
    (N'bf-vo-v41', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V41', N'Karndean flooring (£50/m supply)', 0, N'FLR-LVT', N'', N'item', 1.0000, 19067.0000, 19067.0000, N'', 42),
    (N'bf-vo-v42a', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V42', N'Tile Supply Only - As per Thompson & Leigh Quote 27/11/25 - NOW REVISED IN V69 WITH OMIT AND ADD BACK', 0, N'SUP-TIL', N'', N'item', 1.0000, 14435.0000, 14435.0000, N'', 43),
    (N'bf-vo-v42b', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V42', N'Wall & floor tiling, splashbacks & shower room omits (Install now V76 / Omit V66)', 2, N'TIL-STD', N'', N'item', 1.0000, -29012.0000, -29012.0000, N'', 44),
    (N'bf-vo-v43', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V43', N'Ceiling track hoists', 2, N'SPEC-LFT', N'', N'item', 1.0000, -19800.0000, -19800.0000, N'', 45),
    (N'bf-vo-v44', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V44', N'Radiators with TRVs', 0, N'MEC-PLM', N'', N'item', 1.0000, 2435.0000, 2435.0000, N'', 46),
    (N'bf-vo-v45', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V45', N'Contingency Budget', 2, N'HAND-MSC', N'', N'item', 1.0000, -50000.0000, -50000.0000, N'', 47),
    (N'bf-vo-v46', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V46', N'Eve Access Hatches P-009 Rev H', 0, N'CARP-1FX', N'', N'item', 1.0000, 4950.0000, 4950.0000, N'', 48),
    (N'bf-vo-v47', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V47', N'Double Socket Outlets - P-151 Rev Q / P-152 Rev H', 0, N'ELE-STD', N'', N'item', 1.0000, 575.0000, 575.0000, N'', 49),
    (N'bf-vo-v48a', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V48', N'Fix only - Bespoke joinery', 2, N'CARP-JNR', N'', N'nr', 1.0000, -9000.0000, -9000.0000, N'', 50),
    (N'bf-vo-v48b', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V48', N'Fix only - Decorative fire place', 2, N'DEC-STD', N'', N'item', 1.0000, -1575.0000, -1575.0000, N'', 51),
    (N'bf-vo-v48c', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V48', N'Decorative Fireplaces for media walls', 2, N'DEC-FIR', N'', N'item', 1.0000, -7600.0000, -7600.0000, N'', 52),
    (N'bf-vo-v48d', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V48', N'Dressing rooms, Wardrobes and fitted storage', 2, N'CARP-JNR', N'', N'item', 1.0000, -42000.0000, -42000.0000, N'', 53),
    (N'bf-vo-v49', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V49', N'Gazebo', 2, N'SPEC-GAZ', N'', N'item', 1.0000, -5000.0000, -5000.0000, N'', 54),
    (N'bf-vo-v50', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V50', N'Window coverings', 2, N'WDR-TIM', N'', N'item', 1.0000, -16500.0000, -16500.0000, N'', 55),
    (N'bf-vo-v51', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V51', N'Internal Door ironmongery', 2, N'SUP-IRO', N'', N'item', 1.0000, -2560.0000, -2560.0000, N'', 56),
    (N'bf-vo-v52', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V52', N'Mineral insulation to loft space', 0, N'INT-INC', N'', N'item', 1.0000, 8393.0000, 8393.0000, N'', 57),
    (N'bf-vo-v53', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V53', N'12.5mm plasterboard', 0, N'INT-PLB', N'', N'item', 1.0000, 6210.0000, 6210.0000, N'', 58),
    (N'bf-vo-v54', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V54', N'FFL/FCL TBC', 3, N'HAND-MSC', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 59),
    (N'bf-vo-v55', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V55', N'Double Sockets - P-152 Rev J, Electrical Plan SF', 0, N'ELE-STD', N'', N'item', 1.0000, 230.0000, 230.0000, N'', 60),
    (N'bf-vo-v56', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V56', N'P-205 Rev H Ensuite 1 & P-206 Rev H Ensuite 2', 0, N'SUP-SAN', N'', N'item', 1.0000, 750.0000, 750.0000, N'', 61),
    (N'bf-vo-v57', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V57', N'Site Supervision - EOT-04', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 29300.0000, 29300.0000, N'', 62),
    (N'bf-vo-v58', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V58', N'Manifold - Carers', 0, N'MEC-UFH', N'', N'item', 1.0000, 3907.0000, 3907.0000, N'', 63),
    (N'bf-vo-v59', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V59', N'PRO-064-(WD)-P-703  Rev —  Rear Canopies', 3, N'SPEC-GAZ', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 64),
    (N'bf-vo-v60', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V60', N'Croft SL-20 rev 11', 0, N'HAND-SPE', N'', N'item', 1.0000, 2940.0000, 2940.0000, N'', 65),
    (N'bf-vo-v61', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V61', N'Acoustic boxing to SVP pipe through eaves space', 0, N'CARP-1FX', N'', N'item', 1.0000, 438.0000, 438.0000, N'', 66),
    (N'bf-vo-v62', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V62', N'PRO-064-(WD)-P-706 Rev F — Ground Floor RCP', 0, N'ELE-STD', N'', N'item', 1.0000, 5771.5000, 5771.5000, N'', 67),
    (N'bf-vo-v63', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V63', N'PRO-064-(WD)-P-150 Rev Q — Electrical Plan GF', 0, N'ELE-STD', N'', N'item', 1.0000, 3565.0000, 3565.0000, N'', 68),
    (N'bf-vo-v64', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V64', N'926 mm Internal door lining & single door ( £200 supply )', 0, N'SUP-DOR', N'', N'item', 1.0000, 25351.3700, 25351.3700, N'', 69),
    (N'bf-vo-v65', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V65', N'Staircase Omit Item', 3, N'STAIR-TIM', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 70),
    (N'bf-vo-v66', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V66', N'Supply - Howdens Utility', 2, N'SUP-KIT', N'', N'item', 1.0000, -23845.0000, -23845.0000, N'', 71),
    (N'bf-vo-v67', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V67', N'2nd Fix Carpentry Omit', 3, N'CARP-2FX', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 72),
    (N'bf-vo-v68', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V68', N'Mist & 2 coats of Dulux emulsion to ceilings', 3, N'DEC-STD', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 73),
    (N'bf-vo-v69', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V69', N'Tile Supply - REVISED WITH V42 OMIT', 0, N'SUP-TIL', N'', N'item', 1.0000, 1548.1200, 1548.1200, N'', 74),
    (N'bf-vo-v70', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V70', N'External Taps - Declined', 4, N'MEC-PLM', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 75),
    (N'bf-vo-v71', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V71', N'Generation - Entrance Door', 4, N'WDR-TIM', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 76),
    (N'bf-vo-v72', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V72', N'Coffer Details and Insulation ZZ bedroom - Works Completed', 0, N'INT-INC', N'', N'item', 1.0000, 2285.0000, 2285.0000, N'', 77),
    (N'bf-vo-v73', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V73', N'Revised Coving and LED lighting Details', 0, N'INT-COV', N'', N'item', 1.0000, 4781.4900, 4781.4900, N'', 78),
    (N'bf-vo-v76', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V76', N'Tile Installation & Enabling Works', 0, N'TIL-STD', N'', N'item', 1.0000, 38865.0000, 38865.0000, N'', 79)
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
    COUNT(*) AS VariationLines,                                                       -- 79 (73 VOs; V48 split into 4, V06/V10/V42 into 2 detail lines each)
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, -- 215737.58
    SUM(LineAmount) AS GrossOfAllVoLines                                             -- 215737.58
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8' AND ElementType = 3;
GO

-- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
SELECT
    SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 1780455.00
    SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  215737.58
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                        -- 1996192.58
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8';
GO


-- ============================================================================
-- VARIATION ORDER RECORDS (VariationOrderQuotes + VariationOrders)
-- ----------------------------------------------------------------------------
-- The ValuationLineItems above are only the report's DISPLAY lines. The
-- Variations tab, the email-triage "Variation Order" picker and the record-
-- link tags all read the real VariationOrders table (and its parent VOQs), so
-- those records are seeded here too, matching the register on VariationRef.
--
-- Status mapping follows the register's line types:
--   * Priced / Omit lines (64) -> VOQ Approved (4) + VO Issued (1), Value = net
--   * Declined lines      (6)  -> VOQ Rejected (5), no VO   (V39 V54 V59 V65 V67 V68)
--   * TBC lines           (3)  -> VOQ Tendering (2), no VO  (V19 V70 V71)
-- Declined/TBC variations were never approved, so they exist only as quotes --
-- exactly why they carry no value on the valuation report. RequestId is empty
-- (no originating RFQ email exists for seeded history). Approval dates are
-- spread ~3 VOs/month from Jul 2024 (live delivery start) to Jun 2026
-- (Valuation 18).
--
-- VOQ Status: 0=Draft 1=Inviting 2=Tendering 3=Selected 4=Approved 5=Rejected
-- VO  Status: 0=Approved 1=Issued 2=Cancelled
--
-- Idempotent: keyed on bf-voq-vNN / bf-vord-vNN via MERGE.
-- ============================================================================

MERGE INTO [dbo].[VariationOrderQuotes] AS target
USING (VALUES
    (N'bf-voq-v01', N'3490f944b29545c4b8d5a04130f42ab8', N'', 1, N'VOQ-0001', N'Removal of Asbestos', N'Removal of Asbestos', 4, NULL, NULL, -5500.0000, '2024-06-24', N'seed@jewelgroup.co.uk', '2024-07-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v03', N'3490f944b29545c4b8d5a04130f42ab8', N'', 3, N'VOQ-0003', N'All assosiated work - Provisional sum', N'All assosiated work - Provisional sum', 4, NULL, NULL, 27797.0000, '2024-06-24', N'seed@jewelgroup.co.uk', '2024-07-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v04', N'3490f944b29545c4b8d5a04130f42ab8', N'', 4, N'VOQ-0004', N'Temporary Works - Demolition Phase 1& 2 - TW-200', N'Temporary Works - Demolition Phase 1& 2 - TW-200', 4, NULL, NULL, 1135.0000, '2024-06-24', N'seed@jewelgroup.co.uk', '2024-07-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v05', N'3490f944b29545c4b8d5a04130f42ab8', N'', 5, N'VOQ-0005', N'Site Supervision 8 days - July 24', N'Site Supervision 8 days - July 24', 4, NULL, NULL, 2860.0000, '2024-07-25', N'seed@jewelgroup.co.uk', '2024-08-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v06', N'3490f944b29545c4b8d5a04130f42ab8', N'', 6, N'VOQ-0006', N'Site Supervision 3 days - August 24', N'Site Supervision 3 days - August 24', 4, NULL, NULL, 1610.0000, '2024-07-25', N'seed@jewelgroup.co.uk', '2024-08-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v07', N'3490f944b29545c4b8d5a04130f42ab8', N'', 7, N'VOQ-0007', N'Site Supervision 3 days - Sept 24', N'Site Supervision 3 days - Sept 24', 4, NULL, NULL, 1360.0000, '2024-07-25', N'seed@jewelgroup.co.uk', '2024-08-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v08', N'3490f944b29545c4b8d5a04130f42ab8', N'', 8, N'VOQ-0008', N'Site Supervision 2 days - Oct 24', N'Site Supervision 2 days - Oct 24', 4, NULL, NULL, 1360.0000, '2024-08-25', N'seed@jewelgroup.co.uk', '2024-09-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v09', N'3490f944b29545c4b8d5a04130f42ab8', N'', 9, N'VOQ-0009', N'Site Supervision 2 days - Nov 24', N'Site Supervision 2 days - Nov 24', 4, NULL, NULL, 1360.0000, '2024-08-25', N'seed@jewelgroup.co.uk', '2024-09-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v10', N'3490f944b29545c4b8d5a04130f42ab8', N'', 10, N'VOQ-0010', N'Ground gas collection & ventilated systems - Provisional sum', N'Ground gas collection & ventilated systems - Provisional sum', 4, NULL, NULL, 5116.0000, '2024-08-25', N'seed@jewelgroup.co.uk', '2024-09-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v11', N'3490f944b29545c4b8d5a04130f42ab8', N'', 11, N'VOQ-0011', N'Connection Details - Steel Fabrication', N'Connection Details - Steel Fabrication', 4, NULL, NULL, 4125.0000, '2024-09-24', N'seed@jewelgroup.co.uk', '2024-10-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v12', N'3490f944b29545c4b8d5a04130f42ab8', N'', 12, N'VOQ-0012', N'Tree Protection - STEM Arb Report', N'Tree Protection - STEM Arb Report', 4, NULL, NULL, 2100.0000, '2024-09-24', N'seed@jewelgroup.co.uk', '2024-10-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v13', N'3490f944b29545c4b8d5a04130f42ab8', N'', 13, N'VOQ-0013', N'Thames Sewer Connections - MC0476 Model1-CIV10', N'Thames Sewer Connections - MC0476 Model1-CIV10', 4, NULL, NULL, 6396.0000, '2024-09-24', N'seed@jewelgroup.co.uk', '2024-10-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v14', N'3490f944b29545c4b8d5a04130f42ab8', N'', 14, N'VOQ-0014', N'Driveway Porous Stone - MC0476 Model1-CIV10', N'Driveway Porous Stone - MC0476 Model1-CIV10', 4, NULL, NULL, 42936.0000, '2024-10-25', N'seed@jewelgroup.co.uk', '2024-11-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v15', N'3490f944b29545c4b8d5a04130f42ab8', N'', 15, N'VOQ-0015', N'Site Supervision - EOT-01', N'Site Supervision - EOT-01', 4, NULL, NULL, 5860.0000, '2024-10-25', N'seed@jewelgroup.co.uk', '2024-11-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v16', N'3490f944b29545c4b8d5a04130f42ab8', N'', 16, N'VOQ-0016', N'Pond', N'Pond', 4, NULL, NULL, 26351.0000, '2024-10-25', N'seed@jewelgroup.co.uk', '2024-11-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v17', N'3490f944b29545c4b8d5a04130f42ab8', N'', 17, N'VOQ-0017', N'100mm Ducting to the entrance gates', N'100mm Ducting to the entrance gates', 4, NULL, NULL, 1720.0000, '2024-11-24', N'seed@jewelgroup.co.uk', '2024-12-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v18', N'3490f944b29545c4b8d5a04130f42ab8', N'', 18, N'VOQ-0018', N'Aluminium windows - throughout', N'Aluminium windows - throughout', 4, NULL, NULL, 11474.1000, '2024-11-24', N'seed@jewelgroup.co.uk', '2024-12-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v19', N'3490f944b29545c4b8d5a04130f42ab8', N'', 19, N'VOQ-0019', N'Additional Drainage TBC', N'Additional Drainage TBC', 2, NULL, NULL, NULL, '2024-11-24', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v20', N'3490f944b29545c4b8d5a04130f42ab8', N'', 20, N'VOQ-0020', N'4 No Canopies', N'4 No Canopies', 4, NULL, NULL, -22200.0000, '2024-12-25', N'seed@jewelgroup.co.uk', '2025-01-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v21', N'3490f944b29545c4b8d5a04130f42ab8', N'', 21, N'VOQ-0021', N'W-07 Rebuild Opening - PRO-064-(WD)-P-004 Rev G', N'W-07 Rebuild Opening - PRO-064-(WD)-P-004 Rev G', 4, NULL, NULL, 400.0000, '2024-12-25', N'seed@jewelgroup.co.uk', '2025-01-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v22', N'3490f944b29545c4b8d5a04130f42ab8', N'', 22, N'VOQ-0022', N'SW.1 - G11 - SITE WORKS - Rev B', N'SW.1 - G11 - SITE WORKS - Rev B', 4, NULL, NULL, 7600.0000, '2024-12-25', N'seed@jewelgroup.co.uk', '2025-01-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v23', N'3490f944b29545c4b8d5a04130f42ab8', N'', 23, N'VOQ-0023', N'Chandelier', N'Chandelier', 4, NULL, NULL, 29533.0000, '2025-01-25', N'seed@jewelgroup.co.uk', '2025-02-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v24', N'3490f944b29545c4b8d5a04130f42ab8', N'', 24, N'VOQ-0024', N'Wet underfloor heating to ground floor (carers only)', N'Wet underfloor heating to ground floor (carers only)', 4, NULL, NULL, 4093.0000, '2025-01-25', N'seed@jewelgroup.co.uk', '2025-02-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v25', N'3490f944b29545c4b8d5a04130f42ab8', N'', 25, N'VOQ-0025', N'Site Supervision - EOT-02', N'Site Supervision - EOT-02', 4, NULL, NULL, 3516.0000, '2025-01-25', N'seed@jewelgroup.co.uk', '2025-02-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v26', N'3490f944b29545c4b8d5a04130f42ab8', N'', 26, N'VOQ-0026', N'Air conditioning', N'Air conditioning', 4, NULL, NULL, 8925.0000, '2025-02-22', N'seed@jewelgroup.co.uk', '2025-03-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v27', N'3490f944b29545c4b8d5a04130f42ab8', N'', 27, N'VOQ-0027', N'Intruder Alarm', N'Intruder Alarm', 4, NULL, NULL, -6640.0000, '2025-02-22', N'seed@jewelgroup.co.uk', '2025-03-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v28', N'3490f944b29545c4b8d5a04130f42ab8', N'', 28, N'VOQ-0028', N'Fire and smoke alarm', N'Fire and smoke alarm', 4, NULL, NULL, -1310.0000, '2025-02-22', N'seed@jewelgroup.co.uk', '2025-03-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v29', N'3490f944b29545c4b8d5a04130f42ab8', N'', 29, N'VOQ-0029', N'Invoice 6764 - Croft Structural Engineers', N'Invoice 6764 - Croft Structural Engineers', 4, NULL, NULL, 330.0000, '2025-03-25', N'seed@jewelgroup.co.uk', '2025-04-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v30', N'3490f944b29545c4b8d5a04130f42ab8', N'', 30, N'VOQ-0030', N'10 mm render to internal blockwork walls', N'10 mm render to internal blockwork walls', 4, NULL, NULL, 29185.0000, '2025-03-25', N'seed@jewelgroup.co.uk', '2025-04-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v31', N'3490f944b29545c4b8d5a04130f42ab8', N'', 31, N'VOQ-0031', N'Sanitary ware for Cloakroom', N'Sanitary ware for Cloakroom', 4, NULL, NULL, -12240.0000, '2025-03-25', N'seed@jewelgroup.co.uk', '2025-04-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v32', N'3490f944b29545c4b8d5a04130f42ab8', N'', 32, N'VOQ-0032', N'Supply of feature staircase', N'Supply of feature staircase', 4, NULL, NULL, -1665.0000, '2025-04-24', N'seed@jewelgroup.co.uk', '2025-05-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v33', N'3490f944b29545c4b8d5a04130f42ab8', N'', 33, N'VOQ-0033', N'Structural Steel Works - SL30 Rev 9 & SL40 Rev 8', N'Structural Steel Works - SL30 Rev 9 & SL40 Rev 8', 4, NULL, NULL, 1870.0000, '2025-04-24', N'seed@jewelgroup.co.uk', '2025-05-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v34', N'3490f944b29545c4b8d5a04130f42ab8', N'', 34, N'VOQ-0034', N'Fix only - WC', N'Fix only - WC', 4, NULL, NULL, 11965.0000, '2025-04-24', N'seed@jewelgroup.co.uk', '2025-05-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v35', N'3490f944b29545c4b8d5a04130f42ab8', N'', 35, N'VOQ-0035', N'Site Supervision - EOT-03', N'Site Supervision - EOT-03', 4, NULL, NULL, 8790.0000, '2025-05-25', N'seed@jewelgroup.co.uk', '2025-06-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v36', N'3490f944b29545c4b8d5a04130f42ab8', N'', 36, N'VOQ-0036', N'Annex kitchen/Utility room', N'Annex kitchen/Utility room', 4, NULL, NULL, 17935.0000, '2025-05-25', N'seed@jewelgroup.co.uk', '2025-06-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v37', N'3490f944b29545c4b8d5a04130f42ab8', N'', 37, N'VOQ-0037', N'GF - ZZ Bathroom - On the Level Shower Former', N'GF - ZZ Bathroom - On the Level Shower Former', 4, NULL, NULL, 4245.0000, '2025-05-25', N'seed@jewelgroup.co.uk', '2025-06-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v38', N'3490f944b29545c4b8d5a04130f42ab8', N'', 38, N'VOQ-0038', N'New 6ft timber fencing', N'New 6ft timber fencing', 4, NULL, NULL, 23650.0000, '2025-06-24', N'seed@jewelgroup.co.uk', '2025-07-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v39', N'3490f944b29545c4b8d5a04130f42ab8', N'', 39, N'VOQ-0039', N'Demolition - Removal of Fencing & Existing trees', N'Demolition - Removal of Fencing & Existing trees', 5, NULL, NULL, NULL, '2025-06-24', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v40', N'3490f944b29545c4b8d5a04130f42ab8', N'', 40, N'VOQ-0040', N'Electrics - P-151 Rev P Electrical Plan FF', N'Electrics - P-151 Rev P Electrical Plan FF', 4, NULL, NULL, 790.0000, '2025-06-24', N'seed@jewelgroup.co.uk', '2025-07-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v41', N'3490f944b29545c4b8d5a04130f42ab8', N'', 41, N'VOQ-0041', N'Karndean flooring (£50/m supply)', N'Karndean flooring (£50/m supply)', 4, NULL, NULL, 19067.0000, '2025-07-25', N'seed@jewelgroup.co.uk', '2025-08-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v42', N'3490f944b29545c4b8d5a04130f42ab8', N'', 42, N'VOQ-0042', N'Wall tiling (BASED ON 212M2 @ £80.00 / M2)', N'Wall tiling (BASED ON 212M2 @ £80.00 / M2)', 4, NULL, NULL, -14577.0000, '2025-07-25', N'seed@jewelgroup.co.uk', '2025-08-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v43', N'3490f944b29545c4b8d5a04130f42ab8', N'', 43, N'VOQ-0043', N'Ceiling track hoists', N'Ceiling track hoists', 4, NULL, NULL, -19800.0000, '2025-07-25', N'seed@jewelgroup.co.uk', '2025-08-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v44', N'3490f944b29545c4b8d5a04130f42ab8', N'', 44, N'VOQ-0044', N'Radiators with TRVs', N'Radiators with TRVs', 4, NULL, NULL, 2435.0000, '2025-08-25', N'seed@jewelgroup.co.uk', '2025-09-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v45', N'3490f944b29545c4b8d5a04130f42ab8', N'', 45, N'VOQ-0045', N'Contingency Budget', N'Contingency Budget', 4, NULL, NULL, -50000.0000, '2025-08-25', N'seed@jewelgroup.co.uk', '2025-09-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v46', N'3490f944b29545c4b8d5a04130f42ab8', N'', 46, N'VOQ-0046', N'Eve Access Hatches P-009 Rev H', N'Eve Access Hatches P-009 Rev H', 4, NULL, NULL, 4950.0000, '2025-08-25', N'seed@jewelgroup.co.uk', '2025-09-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v47', N'3490f944b29545c4b8d5a04130f42ab8', N'', 47, N'VOQ-0047', N'Double Socket Outlets - P-151 Rev Q / P-152 Rev H', N'Double Socket Outlets - P-151 Rev Q / P-152 Rev H', 4, NULL, NULL, 575.0000, '2025-09-24', N'seed@jewelgroup.co.uk', '2025-10-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v48', N'3490f944b29545c4b8d5a04130f42ab8', N'', 48, N'VOQ-0048', N'Fix only - Bespoke joinery', N'Fix only - Bespoke joinery', 4, NULL, NULL, -60175.0000, '2025-09-24', N'seed@jewelgroup.co.uk', '2025-10-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v49', N'3490f944b29545c4b8d5a04130f42ab8', N'', 49, N'VOQ-0049', N'Gazebo', N'Gazebo', 4, NULL, NULL, -5000.0000, '2025-09-24', N'seed@jewelgroup.co.uk', '2025-10-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v50', N'3490f944b29545c4b8d5a04130f42ab8', N'', 50, N'VOQ-0050', N'Window coverings', N'Window coverings', 4, NULL, NULL, -16500.0000, '2025-10-25', N'seed@jewelgroup.co.uk', '2025-11-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v51', N'3490f944b29545c4b8d5a04130f42ab8', N'', 51, N'VOQ-0051', N'Internal Door ironmongery', N'Internal Door ironmongery', 4, NULL, NULL, -2560.0000, '2025-10-25', N'seed@jewelgroup.co.uk', '2025-11-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v52', N'3490f944b29545c4b8d5a04130f42ab8', N'', 52, N'VOQ-0052', N'Mineral insulation to loft space', N'Mineral insulation to loft space', 4, NULL, NULL, 8393.0000, '2025-10-25', N'seed@jewelgroup.co.uk', '2025-11-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v53', N'3490f944b29545c4b8d5a04130f42ab8', N'', 53, N'VOQ-0053', N'12.5mm plasterboard', N'12.5mm plasterboard', 4, NULL, NULL, 6210.0000, '2025-11-24', N'seed@jewelgroup.co.uk', '2025-12-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v54', N'3490f944b29545c4b8d5a04130f42ab8', N'', 54, N'VOQ-0054', N'FFL/FCL TBC', N'FFL/FCL TBC', 5, NULL, NULL, NULL, '2025-11-24', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v55', N'3490f944b29545c4b8d5a04130f42ab8', N'', 55, N'VOQ-0055', N'Double Sockets - P-152 Rev J, Electrical Plan SF', N'Double Sockets - P-152 Rev J, Electrical Plan SF', 4, NULL, NULL, 230.0000, '2025-11-24', N'seed@jewelgroup.co.uk', '2025-12-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v56', N'3490f944b29545c4b8d5a04130f42ab8', N'', 56, N'VOQ-0056', N'P-205 Rev H Ensuite 1 & P-206 Rev H Ensuite 2', N'P-205 Rev H Ensuite 1 & P-206 Rev H Ensuite 2', 4, NULL, NULL, 750.0000, '2025-12-25', N'seed@jewelgroup.co.uk', '2026-01-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v57', N'3490f944b29545c4b8d5a04130f42ab8', N'', 57, N'VOQ-0057', N'Site Supervision - EOT-04', N'Site Supervision - EOT-04', 4, NULL, NULL, 29300.0000, '2025-12-25', N'seed@jewelgroup.co.uk', '2026-01-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v58', N'3490f944b29545c4b8d5a04130f42ab8', N'', 58, N'VOQ-0058', N'Manifold - Carers', N'Manifold - Carers', 4, NULL, NULL, 3907.0000, '2025-12-25', N'seed@jewelgroup.co.uk', '2026-01-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v59', N'3490f944b29545c4b8d5a04130f42ab8', N'', 59, N'VOQ-0059', N'PRO-064-(WD)-P-703  Rev —  Rear Canopies', N'PRO-064-(WD)-P-703  Rev —  Rear Canopies', 5, NULL, NULL, NULL, '2026-01-25', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v60', N'3490f944b29545c4b8d5a04130f42ab8', N'', 60, N'VOQ-0060', N'Croft SL-20 rev 11', N'Croft SL-20 rev 11', 4, NULL, NULL, 2940.0000, '2026-01-25', N'seed@jewelgroup.co.uk', '2026-02-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v61', N'3490f944b29545c4b8d5a04130f42ab8', N'', 61, N'VOQ-0061', N'Acoustic boxing to SVP pipe through eaves space', N'Acoustic boxing to SVP pipe through eaves space', 4, NULL, NULL, 438.0000, '2026-01-25', N'seed@jewelgroup.co.uk', '2026-02-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v62', N'3490f944b29545c4b8d5a04130f42ab8', N'', 62, N'VOQ-0062', N'PRO-064-(WD)-P-706 Rev F — Ground Floor RCP', N'PRO-064-(WD)-P-706 Rev F — Ground Floor RCP', 4, NULL, NULL, 5771.5000, '2026-02-22', N'seed@jewelgroup.co.uk', '2026-03-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v63', N'3490f944b29545c4b8d5a04130f42ab8', N'', 63, N'VOQ-0063', N'PRO-064-(WD)-P-150 Rev Q — Electrical Plan GF', N'PRO-064-(WD)-P-150 Rev Q — Electrical Plan GF', 4, NULL, NULL, 3565.0000, '2026-02-22', N'seed@jewelgroup.co.uk', '2026-03-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v64', N'3490f944b29545c4b8d5a04130f42ab8', N'', 64, N'VOQ-0064', N'926 mm Internal door lining & single door ( £200 supply )', N'926 mm Internal door lining & single door ( £200 supply )', 4, NULL, NULL, 25351.3700, '2026-02-22', N'seed@jewelgroup.co.uk', '2026-03-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v65', N'3490f944b29545c4b8d5a04130f42ab8', N'', 65, N'VOQ-0065', N'Staircase Omit Item', N'Staircase Omit Item', 5, NULL, NULL, NULL, '2026-03-25', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v66', N'3490f944b29545c4b8d5a04130f42ab8', N'', 66, N'VOQ-0066', N'Supply - Howdens Utility', N'Supply - Howdens Utility', 4, NULL, NULL, -23845.0000, '2026-03-25', N'seed@jewelgroup.co.uk', '2026-04-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v67', N'3490f944b29545c4b8d5a04130f42ab8', N'', 67, N'VOQ-0067', N'2nd Fix Carpentry Omit', N'2nd Fix Carpentry Omit', 5, NULL, NULL, NULL, '2026-03-25', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v68', N'3490f944b29545c4b8d5a04130f42ab8', N'', 68, N'VOQ-0068', N'Mist & 2 coats of Dulux emulsion to ceilings', N'Mist & 2 coats of Dulux emulsion to ceilings', 5, NULL, NULL, NULL, '2026-04-24', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v69', N'3490f944b29545c4b8d5a04130f42ab8', N'', 69, N'VOQ-0069', N'Tile Supply - REVISED WITH V42 OMIT', N'Tile Supply - REVISED WITH V42 OMIT', 4, NULL, NULL, 1548.1200, '2026-04-24', N'seed@jewelgroup.co.uk', '2026-05-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v70', N'3490f944b29545c4b8d5a04130f42ab8', N'', 70, N'VOQ-0070', N'External Taps - Declined', N'External Taps - Declined', 2, NULL, NULL, NULL, '2026-04-24', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v71', N'3490f944b29545c4b8d5a04130f42ab8', N'', 71, N'VOQ-0071', N'Generation - Entrance Door', N'Generation - Entrance Door', 2, NULL, NULL, NULL, '2026-04-24', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'bf-voq-v72', N'3490f944b29545c4b8d5a04130f42ab8', N'', 72, N'VOQ-0072', N'Coffer Details and Insulation ZZ bedroom - Works Completed', N'Coffer Details and Insulation ZZ bedroom - Works Completed', 4, NULL, NULL, 2285.0000, '2026-04-24', N'seed@jewelgroup.co.uk', '2026-05-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v73', N'3490f944b29545c4b8d5a04130f42ab8', N'', 73, N'VOQ-0073', N'Revised Coving and LED lighting Details', N'Revised Coving and LED lighting Details', 4, NULL, NULL, 4781.4900, '2026-04-24', N'seed@jewelgroup.co.uk', '2026-05-15', N'seed@jewelgroup.co.uk'),
    (N'bf-voq-v76', N'3490f944b29545c4b8d5a04130f42ab8', N'', 76, N'VOQ-0076', N'Tile Installation & Enabling Works', N'Tile Installation & Enabling Works', 4, NULL, NULL, 38865.0000, '2026-04-24', N'seed@jewelgroup.co.uk', '2026-05-15', N'seed@jewelgroup.co.uk')
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
    (N'bf-vord-v01', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v01', N'', 1, N'V01', N'Removal of Asbestos', N'Removal of Asbestos', 1, -5500.0000, NULL, N'', '2024-07-15', N'seed@jewelgroup.co.uk', '2024-07-22', NULL),
    (N'bf-vord-v03', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v03', N'', 3, N'V03', N'All assosiated work - Provisional sum', N'All assosiated work - Provisional sum', 1, 27797.0000, NULL, N'', '2024-07-15', N'seed@jewelgroup.co.uk', '2024-07-22', NULL),
    (N'bf-vord-v04', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v04', N'', 4, N'V04', N'Temporary Works - Demolition Phase 1& 2 - TW-200', N'Temporary Works - Demolition Phase 1& 2 - TW-200', 1, 1135.0000, NULL, N'', '2024-07-15', N'seed@jewelgroup.co.uk', '2024-07-22', NULL),
    (N'bf-vord-v05', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v05', N'', 5, N'V05', N'Site Supervision 8 days - July 24', N'Site Supervision 8 days - July 24', 1, 2860.0000, NULL, N'', '2024-08-15', N'seed@jewelgroup.co.uk', '2024-08-22', NULL),
    (N'bf-vord-v06', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v06', N'', 6, N'V06', N'Site Supervision 3 days - August 24', N'Site Supervision 3 days - August 24', 1, 1610.0000, NULL, N'', '2024-08-15', N'seed@jewelgroup.co.uk', '2024-08-22', NULL),
    (N'bf-vord-v07', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v07', N'', 7, N'V07', N'Site Supervision 3 days - Sept 24', N'Site Supervision 3 days - Sept 24', 1, 1360.0000, NULL, N'', '2024-08-15', N'seed@jewelgroup.co.uk', '2024-08-22', NULL),
    (N'bf-vord-v08', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v08', N'', 8, N'V08', N'Site Supervision 2 days - Oct 24', N'Site Supervision 2 days - Oct 24', 1, 1360.0000, NULL, N'', '2024-09-15', N'seed@jewelgroup.co.uk', '2024-09-22', NULL),
    (N'bf-vord-v09', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v09', N'', 9, N'V09', N'Site Supervision 2 days - Nov 24', N'Site Supervision 2 days - Nov 24', 1, 1360.0000, NULL, N'', '2024-09-15', N'seed@jewelgroup.co.uk', '2024-09-22', NULL),
    (N'bf-vord-v10', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v10', N'', 10, N'V10', N'Ground gas collection & ventilated systems - Provisional sum', N'Ground gas collection & ventilated systems - Provisional sum', 1, 5116.0000, NULL, N'', '2024-09-15', N'seed@jewelgroup.co.uk', '2024-09-22', NULL),
    (N'bf-vord-v11', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v11', N'', 11, N'V11', N'Connection Details - Steel Fabrication', N'Connection Details - Steel Fabrication', 1, 4125.0000, NULL, N'', '2024-10-15', N'seed@jewelgroup.co.uk', '2024-10-22', NULL),
    (N'bf-vord-v12', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v12', N'', 12, N'V12', N'Tree Protection - STEM Arb Report', N'Tree Protection - STEM Arb Report', 1, 2100.0000, NULL, N'', '2024-10-15', N'seed@jewelgroup.co.uk', '2024-10-22', NULL),
    (N'bf-vord-v13', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v13', N'', 13, N'V13', N'Thames Sewer Connections - MC0476 Model1-CIV10', N'Thames Sewer Connections - MC0476 Model1-CIV10', 1, 6396.0000, NULL, N'', '2024-10-15', N'seed@jewelgroup.co.uk', '2024-10-22', NULL),
    (N'bf-vord-v14', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v14', N'', 14, N'V14', N'Driveway Porous Stone - MC0476 Model1-CIV10', N'Driveway Porous Stone - MC0476 Model1-CIV10', 1, 42936.0000, NULL, N'', '2024-11-15', N'seed@jewelgroup.co.uk', '2024-11-22', NULL),
    (N'bf-vord-v15', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v15', N'', 15, N'V15', N'Site Supervision - EOT-01', N'Site Supervision - EOT-01', 1, 5860.0000, NULL, N'', '2024-11-15', N'seed@jewelgroup.co.uk', '2024-11-22', NULL),
    (N'bf-vord-v16', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v16', N'', 16, N'V16', N'Pond', N'Pond', 1, 26351.0000, NULL, N'', '2024-11-15', N'seed@jewelgroup.co.uk', '2024-11-22', NULL),
    (N'bf-vord-v17', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v17', N'', 17, N'V17', N'100mm Ducting to the entrance gates', N'100mm Ducting to the entrance gates', 1, 1720.0000, NULL, N'', '2024-12-15', N'seed@jewelgroup.co.uk', '2024-12-22', NULL),
    (N'bf-vord-v18', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v18', N'', 18, N'V18', N'Aluminium windows - throughout', N'Aluminium windows - throughout', 1, 11474.1000, NULL, N'', '2024-12-15', N'seed@jewelgroup.co.uk', '2024-12-22', NULL),
    (N'bf-vord-v20', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v20', N'', 20, N'V20', N'4 No Canopies', N'4 No Canopies', 1, -22200.0000, NULL, N'', '2025-01-15', N'seed@jewelgroup.co.uk', '2025-01-22', NULL),
    (N'bf-vord-v21', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v21', N'', 21, N'V21', N'W-07 Rebuild Opening - PRO-064-(WD)-P-004 Rev G', N'W-07 Rebuild Opening - PRO-064-(WD)-P-004 Rev G', 1, 400.0000, NULL, N'', '2025-01-15', N'seed@jewelgroup.co.uk', '2025-01-22', NULL),
    (N'bf-vord-v22', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v22', N'', 22, N'V22', N'SW.1 - G11 - SITE WORKS - Rev B', N'SW.1 - G11 - SITE WORKS - Rev B', 1, 7600.0000, NULL, N'', '2025-01-15', N'seed@jewelgroup.co.uk', '2025-01-22', NULL),
    (N'bf-vord-v23', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v23', N'', 23, N'V23', N'Chandelier', N'Chandelier', 1, 29533.0000, NULL, N'', '2025-02-15', N'seed@jewelgroup.co.uk', '2025-02-22', NULL),
    (N'bf-vord-v24', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v24', N'', 24, N'V24', N'Wet underfloor heating to ground floor (carers only)', N'Wet underfloor heating to ground floor (carers only)', 1, 4093.0000, NULL, N'', '2025-02-15', N'seed@jewelgroup.co.uk', '2025-02-22', NULL),
    (N'bf-vord-v25', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v25', N'', 25, N'V25', N'Site Supervision - EOT-02', N'Site Supervision - EOT-02', 1, 3516.0000, NULL, N'', '2025-02-15', N'seed@jewelgroup.co.uk', '2025-02-22', NULL),
    (N'bf-vord-v26', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v26', N'', 26, N'V26', N'Air conditioning', N'Air conditioning', 1, 8925.0000, NULL, N'', '2025-03-15', N'seed@jewelgroup.co.uk', '2025-03-22', NULL),
    (N'bf-vord-v27', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v27', N'', 27, N'V27', N'Intruder Alarm', N'Intruder Alarm', 1, -6640.0000, NULL, N'', '2025-03-15', N'seed@jewelgroup.co.uk', '2025-03-22', NULL),
    (N'bf-vord-v28', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v28', N'', 28, N'V28', N'Fire and smoke alarm', N'Fire and smoke alarm', 1, -1310.0000, NULL, N'', '2025-03-15', N'seed@jewelgroup.co.uk', '2025-03-22', NULL),
    (N'bf-vord-v29', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v29', N'', 29, N'V29', N'Invoice 6764 - Croft Structural Engineers', N'Invoice 6764 - Croft Structural Engineers', 1, 330.0000, NULL, N'', '2025-04-15', N'seed@jewelgroup.co.uk', '2025-04-22', NULL),
    (N'bf-vord-v30', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v30', N'', 30, N'V30', N'10 mm render to internal blockwork walls', N'10 mm render to internal blockwork walls', 1, 29185.0000, NULL, N'', '2025-04-15', N'seed@jewelgroup.co.uk', '2025-04-22', NULL),
    (N'bf-vord-v31', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v31', N'', 31, N'V31', N'Sanitary ware for Cloakroom', N'Sanitary ware for Cloakroom', 1, -12240.0000, NULL, N'', '2025-04-15', N'seed@jewelgroup.co.uk', '2025-04-22', NULL),
    (N'bf-vord-v32', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v32', N'', 32, N'V32', N'Supply of feature staircase', N'Supply of feature staircase', 1, -1665.0000, NULL, N'', '2025-05-15', N'seed@jewelgroup.co.uk', '2025-05-22', NULL),
    (N'bf-vord-v33', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v33', N'', 33, N'V33', N'Structural Steel Works - SL30 Rev 9 & SL40 Rev 8', N'Structural Steel Works - SL30 Rev 9 & SL40 Rev 8', 1, 1870.0000, NULL, N'', '2025-05-15', N'seed@jewelgroup.co.uk', '2025-05-22', NULL),
    (N'bf-vord-v34', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v34', N'', 34, N'V34', N'Fix only - WC', N'Fix only - WC', 1, 11965.0000, NULL, N'', '2025-05-15', N'seed@jewelgroup.co.uk', '2025-05-22', NULL),
    (N'bf-vord-v35', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v35', N'', 35, N'V35', N'Site Supervision - EOT-03', N'Site Supervision - EOT-03', 1, 8790.0000, NULL, N'', '2025-06-15', N'seed@jewelgroup.co.uk', '2025-06-22', NULL),
    (N'bf-vord-v36', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v36', N'', 36, N'V36', N'Annex kitchen/Utility room', N'Annex kitchen/Utility room', 1, 17935.0000, NULL, N'', '2025-06-15', N'seed@jewelgroup.co.uk', '2025-06-22', NULL),
    (N'bf-vord-v37', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v37', N'', 37, N'V37', N'GF - ZZ Bathroom - On the Level Shower Former', N'GF - ZZ Bathroom - On the Level Shower Former', 1, 4245.0000, NULL, N'', '2025-06-15', N'seed@jewelgroup.co.uk', '2025-06-22', NULL),
    (N'bf-vord-v38', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v38', N'', 38, N'V38', N'New 6ft timber fencing', N'New 6ft timber fencing', 1, 23650.0000, NULL, N'', '2025-07-15', N'seed@jewelgroup.co.uk', '2025-07-22', NULL),
    (N'bf-vord-v40', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v40', N'', 40, N'V40', N'Electrics - P-151 Rev P Electrical Plan FF', N'Electrics - P-151 Rev P Electrical Plan FF', 1, 790.0000, NULL, N'', '2025-07-15', N'seed@jewelgroup.co.uk', '2025-07-22', NULL),
    (N'bf-vord-v41', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v41', N'', 41, N'V41', N'Karndean flooring (£50/m supply)', N'Karndean flooring (£50/m supply)', 1, 19067.0000, NULL, N'', '2025-08-15', N'seed@jewelgroup.co.uk', '2025-08-22', NULL),
    (N'bf-vord-v42', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v42', N'', 42, N'V42', N'Wall tiling (BASED ON 212M2 @ £80.00 / M2)', N'Wall tiling (BASED ON 212M2 @ £80.00 / M2)', 1, -14577.0000, NULL, N'', '2025-08-15', N'seed@jewelgroup.co.uk', '2025-08-22', NULL),
    (N'bf-vord-v43', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v43', N'', 43, N'V43', N'Ceiling track hoists', N'Ceiling track hoists', 1, -19800.0000, NULL, N'', '2025-08-15', N'seed@jewelgroup.co.uk', '2025-08-22', NULL),
    (N'bf-vord-v44', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v44', N'', 44, N'V44', N'Radiators with TRVs', N'Radiators with TRVs', 1, 2435.0000, NULL, N'', '2025-09-15', N'seed@jewelgroup.co.uk', '2025-09-22', NULL),
    (N'bf-vord-v45', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v45', N'', 45, N'V45', N'Contingency Budget', N'Contingency Budget', 1, -50000.0000, NULL, N'', '2025-09-15', N'seed@jewelgroup.co.uk', '2025-09-22', NULL),
    (N'bf-vord-v46', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v46', N'', 46, N'V46', N'Eve Access Hatches P-009 Rev H', N'Eve Access Hatches P-009 Rev H', 1, 4950.0000, NULL, N'', '2025-09-15', N'seed@jewelgroup.co.uk', '2025-09-22', NULL),
    (N'bf-vord-v47', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v47', N'', 47, N'V47', N'Double Socket Outlets - P-151 Rev Q / P-152 Rev H', N'Double Socket Outlets - P-151 Rev Q / P-152 Rev H', 1, 575.0000, NULL, N'', '2025-10-15', N'seed@jewelgroup.co.uk', '2025-10-22', NULL),
    (N'bf-vord-v48', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v48', N'', 48, N'V48', N'Fix only - Bespoke joinery', N'Fix only - Bespoke joinery', 1, -60175.0000, NULL, N'', '2025-10-15', N'seed@jewelgroup.co.uk', '2025-10-22', NULL),
    (N'bf-vord-v49', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v49', N'', 49, N'V49', N'Gazebo', N'Gazebo', 1, -5000.0000, NULL, N'', '2025-10-15', N'seed@jewelgroup.co.uk', '2025-10-22', NULL),
    (N'bf-vord-v50', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v50', N'', 50, N'V50', N'Window coverings', N'Window coverings', 1, -16500.0000, NULL, N'', '2025-11-15', N'seed@jewelgroup.co.uk', '2025-11-22', NULL),
    (N'bf-vord-v51', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v51', N'', 51, N'V51', N'Internal Door ironmongery', N'Internal Door ironmongery', 1, -2560.0000, NULL, N'', '2025-11-15', N'seed@jewelgroup.co.uk', '2025-11-22', NULL),
    (N'bf-vord-v52', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v52', N'', 52, N'V52', N'Mineral insulation to loft space', N'Mineral insulation to loft space', 1, 8393.0000, NULL, N'', '2025-11-15', N'seed@jewelgroup.co.uk', '2025-11-22', NULL),
    (N'bf-vord-v53', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v53', N'', 53, N'V53', N'12.5mm plasterboard', N'12.5mm plasterboard', 1, 6210.0000, NULL, N'', '2025-12-15', N'seed@jewelgroup.co.uk', '2025-12-22', NULL),
    (N'bf-vord-v55', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v55', N'', 55, N'V55', N'Double Sockets - P-152 Rev J, Electrical Plan SF', N'Double Sockets - P-152 Rev J, Electrical Plan SF', 1, 230.0000, NULL, N'', '2025-12-15', N'seed@jewelgroup.co.uk', '2025-12-22', NULL),
    (N'bf-vord-v56', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v56', N'', 56, N'V56', N'P-205 Rev H Ensuite 1 & P-206 Rev H Ensuite 2', N'P-205 Rev H Ensuite 1 & P-206 Rev H Ensuite 2', 1, 750.0000, NULL, N'', '2026-01-15', N'seed@jewelgroup.co.uk', '2026-01-22', NULL),
    (N'bf-vord-v57', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v57', N'', 57, N'V57', N'Site Supervision - EOT-04', N'Site Supervision - EOT-04', 1, 29300.0000, NULL, N'', '2026-01-15', N'seed@jewelgroup.co.uk', '2026-01-22', NULL),
    (N'bf-vord-v58', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v58', N'', 58, N'V58', N'Manifold - Carers', N'Manifold - Carers', 1, 3907.0000, NULL, N'', '2026-01-15', N'seed@jewelgroup.co.uk', '2026-01-22', NULL),
    (N'bf-vord-v60', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v60', N'', 60, N'V60', N'Croft SL-20 rev 11', N'Croft SL-20 rev 11', 1, 2940.0000, NULL, N'', '2026-02-15', N'seed@jewelgroup.co.uk', '2026-02-22', NULL),
    (N'bf-vord-v61', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v61', N'', 61, N'V61', N'Acoustic boxing to SVP pipe through eaves space', N'Acoustic boxing to SVP pipe through eaves space', 1, 438.0000, NULL, N'', '2026-02-15', N'seed@jewelgroup.co.uk', '2026-02-22', NULL),
    (N'bf-vord-v62', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v62', N'', 62, N'V62', N'PRO-064-(WD)-P-706 Rev F — Ground Floor RCP', N'PRO-064-(WD)-P-706 Rev F — Ground Floor RCP', 1, 5771.5000, NULL, N'', '2026-03-15', N'seed@jewelgroup.co.uk', '2026-03-22', NULL),
    (N'bf-vord-v63', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v63', N'', 63, N'V63', N'PRO-064-(WD)-P-150 Rev Q — Electrical Plan GF', N'PRO-064-(WD)-P-150 Rev Q — Electrical Plan GF', 1, 3565.0000, NULL, N'', '2026-03-15', N'seed@jewelgroup.co.uk', '2026-03-22', NULL),
    (N'bf-vord-v64', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v64', N'', 64, N'V64', N'926 mm Internal door lining & single door ( £200 supply )', N'926 mm Internal door lining & single door ( £200 supply )', 1, 25351.3700, NULL, N'', '2026-03-15', N'seed@jewelgroup.co.uk', '2026-03-22', NULL),
    (N'bf-vord-v66', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v66', N'', 66, N'V66', N'Supply - Howdens Utility', N'Supply - Howdens Utility', 1, -23845.0000, NULL, N'', '2026-04-15', N'seed@jewelgroup.co.uk', '2026-04-22', NULL),
    (N'bf-vord-v69', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v69', N'', 69, N'V69', N'Tile Supply - REVISED WITH V42 OMIT', N'Tile Supply - REVISED WITH V42 OMIT', 1, 1548.1200, NULL, N'', '2026-05-15', N'seed@jewelgroup.co.uk', '2026-05-22', NULL),
    (N'bf-vord-v72', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v72', N'', 72, N'V72', N'Coffer Details and Insulation ZZ bedroom - Works Completed', N'Coffer Details and Insulation ZZ bedroom - Works Completed', 1, 2285.0000, NULL, N'', '2026-05-15', N'seed@jewelgroup.co.uk', '2026-05-22', NULL),
    (N'bf-vord-v73', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v73', N'', 73, N'V73', N'Revised Coving and LED lighting Details', N'Revised Coving and LED lighting Details', 1, 4781.4900, NULL, N'', '2026-05-15', N'seed@jewelgroup.co.uk', '2026-05-22', NULL),
    (N'bf-vord-v76', N'3490f944b29545c4b8d5a04130f42ab8', N'bf-voq-v76', N'', 76, N'V76', N'Tile Installation & Enabling Works', N'Tile Installation & Enabling Works', 1, 38865.0000, NULL, N'', '2026-05-15', N'seed@jewelgroup.co.uk', '2026-05-22', NULL)
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

-- Sanity check: VO records should mirror the register -- 64 issued VOs netting
-- to the workbook total, 73 VOQs in all, and every VO backed by a report line.
SELECT
    (SELECT COUNT(*) FROM [dbo].[VariationOrders]      WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8') AS VariationOrders,      -- 64
    (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8') AS VariationOrderQuotes, -- 73
    (SELECT SUM(Value) FROM [dbo].[VariationOrders]    WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8' AND Status <> 2) AS NetVoValue, -- 215737.58
    (SELECT COUNT(*)
       FROM [dbo].[VariationOrders] vo
       LEFT JOIN [dbo].[ValuationLineItems] li
         ON li.ProjectId = vo.ProjectId AND li.ElementType = 3 AND li.VariationRef = vo.VariationRef
      WHERE vo.ProjectId = N'3490f944b29545c4b8d5a04130f42ab8' AND li.ValuationLineItemId IS NULL) AS VosMissingReportLine; -- 0
GO
