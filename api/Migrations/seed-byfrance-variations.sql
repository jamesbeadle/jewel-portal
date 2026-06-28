-- ============================================================================
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
-- Idempotent: keyed on stable ValuationLineItemId values (bf-vo-vNN). A re-run
-- refreshes every field via MERGE. The contract/PC/contingency lines seeded by
-- seed-byfrance-valuation.sql are left untouched. Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'bf-vo-v01', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V01', N'Removal of Asbestos', 2, N'', N'', N'item', 1.0000, -5500.0000, -5500.0000, N'', 1),
    (N'bf-vo-v03', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V03', N'All assosiated work - Provisional sum', 0, N'', N'', N'item', 1.0000, 27797.0000, 27797.0000, N'', 2),
    (N'bf-vo-v04', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V04', N'Temporary Works - Demolition Phase 1& 2 - TW-200', 0, N'', N'', N'item', 1.0000, 1135.0000, 1135.0000, N'', 3),
    (N'bf-vo-v05', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V05', N'Site Supervision 8 days - July 24', 0, N'', N'', N'item', 1.0000, 2860.0000, 2860.0000, N'', 4),
    (N'bf-vo-v06', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V06', N'Site Supervision 3 days - August 24', 0, N'', N'', N'item', 1.0000, 1610.0000, 1610.0000, N'', 5),
    (N'bf-vo-v07', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V07', N'Site Supervision 3 days - Sept 24', 0, N'', N'', N'item', 1.0000, 1360.0000, 1360.0000, N'', 6),
    (N'bf-vo-v08', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V08', N'Site Supervision 2 days - Oct 24', 0, N'', N'', N'item', 1.0000, 1360.0000, 1360.0000, N'', 7),
    (N'bf-vo-v09', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V09', N'Site Supervision 2 days - Nov 24', 0, N'', N'', N'item', 1.0000, 1360.0000, 1360.0000, N'', 8),
    (N'bf-vo-v10', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V10', N'Ground gas collection & ventilated systems - Provisional sum', 0, N'', N'', N'item', 1.0000, 5116.0000, 5116.0000, N'', 9),
    (N'bf-vo-v11', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V11', N'Connection Details - Steel Fabrication', 0, N'', N'', N'item', 1.0000, 4125.0000, 4125.0000, N'', 10),
    (N'bf-vo-v12', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V12', N'Tree Protection - STEM Arb Report', 0, N'', N'', N'item', 1.0000, 2100.0000, 2100.0000, N'', 11),
    (N'bf-vo-v13', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V13', N'Thames Sewer Connections - MC0476 Model1-CIV10', 0, N'', N'', N'item', 1.0000, 6396.0000, 6396.0000, N'', 12),
    (N'bf-vo-v14', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V14', N'Driveway Porous Stone - MC0476 Model1-CIV10', 0, N'', N'', N'item', 1.0000, 42936.0000, 42936.0000, N'', 13),
    (N'bf-vo-v15', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V15', N'Site Supervision - EOT-01', 0, N'', N'', N'item', 1.0000, 5860.0000, 5860.0000, N'', 14),
    (N'bf-vo-v16', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V16', N'Pond', 0, N'', N'', N'item', 1.0000, 26351.0000, 26351.0000, N'', 15),
    (N'bf-vo-v17', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V17', N'100mm Ducting to the entrance gates', 0, N'', N'', N'item', 1.0000, 1720.0000, 1720.0000, N'', 16),
    (N'bf-vo-v18', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V18', N'Aluminium windows - throughout', 0, N'', N'', N'item', 1.0000, 11474.1000, 11474.1000, N'', 17),
    (N'bf-vo-v19', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V19', N'Additional Drainage TBC', 4, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 18),
    (N'bf-vo-v20', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V20', N'4 No Canopies', 2, N'', N'', N'item', 1.0000, -22200.0000, -22200.0000, N'', 19),
    (N'bf-vo-v21', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V21', N'W-07 Rebuild Opening - PRO-064-(WD)-P-004 Rev G', 0, N'', N'', N'item', 1.0000, 400.0000, 400.0000, N'', 20),
    (N'bf-vo-v22', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V22', N'SW.1 - G11 - SITE WORKS - Rev B', 0, N'', N'', N'item', 1.0000, 7600.0000, 7600.0000, N'', 21),
    (N'bf-vo-v23', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V23', N'Chandelier', 0, N'', N'', N'item', 1.0000, 29533.0000, 29533.0000, N'', 22),
    (N'bf-vo-v24', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V24', N'Wet underfloor heating to ground floor (carers only)', 0, N'', N'', N'item', 1.0000, 4093.0000, 4093.0000, N'', 23),
    (N'bf-vo-v25', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V25', N'Site Supervision - EOT-02', 0, N'', N'', N'item', 1.0000, 3516.0000, 3516.0000, N'', 24),
    (N'bf-vo-v26', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V26', N'Air conditioning', 0, N'', N'', N'item', 1.0000, 8925.0000, 8925.0000, N'', 25),
    (N'bf-vo-v27', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V27', N'Intruder Alarm', 2, N'', N'', N'item', 1.0000, -6640.0000, -6640.0000, N'', 26),
    (N'bf-vo-v28', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V28', N'Fire and smoke alarm', 2, N'', N'', N'item', 1.0000, -1310.0000, -1310.0000, N'', 27),
    (N'bf-vo-v29', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V29', N'Invoice 6764 - Croft Structural Engineers', 0, N'', N'', N'item', 1.0000, 330.0000, 330.0000, N'', 28),
    (N'bf-vo-v30', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V30', N'10 mm render to internal blockwork walls', 0, N'', N'', N'item', 1.0000, 29185.0000, 29185.0000, N'', 29),
    (N'bf-vo-v31', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V31', N'Sanitary ware for Cloakroom', 2, N'', N'', N'item', 1.0000, -12240.0000, -12240.0000, N'', 30),
    (N'bf-vo-v32', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V32', N'Supply of feature staircase', 2, N'', N'', N'item', 1.0000, -1665.0000, -1665.0000, N'', 31),
    (N'bf-vo-v33', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V33', N'Structural Steel Works - SL30 Rev 9 & SL40 Rev 8', 0, N'', N'', N'item', 1.0000, 1870.0000, 1870.0000, N'', 32),
    (N'bf-vo-v34', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V34', N'Fix only - WC', 0, N'', N'', N'item', 1.0000, 11965.0000, 11965.0000, N'', 33),
    (N'bf-vo-v35', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V35', N'Site Supervision - EOT-03', 0, N'', N'', N'item', 1.0000, 8790.0000, 8790.0000, N'', 34),
    (N'bf-vo-v36', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V36', N'Annex kitchen/Utility room', 0, N'', N'', N'item', 1.0000, 17935.0000, 17935.0000, N'', 35),
    (N'bf-vo-v37', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V37', N'GF - ZZ Bathroom - On the Level Shower Former', 0, N'', N'', N'item', 1.0000, 4245.0000, 4245.0000, N'', 36),
    (N'bf-vo-v38', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V38', N'New 6ft timber fencing', 0, N'', N'', N'item', 1.0000, 23650.0000, 23650.0000, N'', 37),
    (N'bf-vo-v39', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V39', N'Demolition - Removal of Fencing & Existing trees', 3, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 38),
    (N'bf-vo-v40', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V40', N'Electrics - P-151 Rev P Electrical Plan FF', 0, N'', N'', N'item', 1.0000, 790.0000, 790.0000, N'', 39),
    (N'bf-vo-v41', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V41', N'Karndean flooring (£50/m supply)', 0, N'', N'', N'item', 1.0000, 19067.0000, 19067.0000, N'', 40),
    (N'bf-vo-v42', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V42', N'Wall tiling (BASED ON 212M2 @ £80.00 / M2)', 2, N'', N'', N'item', 1.0000, -14577.0000, -14577.0000, N'', 41),
    (N'bf-vo-v43', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V43', N'Ceiling track hoists', 2, N'', N'', N'item', 1.0000, -19800.0000, -19800.0000, N'', 42),
    (N'bf-vo-v44', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V44', N'Radiators with TRVs', 0, N'', N'', N'item', 1.0000, 2435.0000, 2435.0000, N'', 43),
    (N'bf-vo-v45', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V45', N'Contingency Budget', 2, N'', N'', N'item', 1.0000, -50000.0000, -50000.0000, N'', 44),
    (N'bf-vo-v46', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V46', N'Eve Access Hatches P-009 Rev H', 0, N'', N'', N'item', 1.0000, 4950.0000, 4950.0000, N'', 45),
    (N'bf-vo-v47', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V47', N'Double Socket Outlets - P-151 Rev Q / P-152 Rev H', 0, N'', N'', N'item', 1.0000, 575.0000, 575.0000, N'', 46),
    (N'bf-vo-v48', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V48', N'Fix only - Bespoke joinery', 2, N'', N'', N'item', 1.0000, -60175.0000, -60175.0000, N'', 47),
    (N'bf-vo-v49', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V49', N'Gazebo', 2, N'', N'', N'item', 1.0000, -5000.0000, -5000.0000, N'', 48),
    (N'bf-vo-v50', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V50', N'Window coverings', 2, N'', N'', N'item', 1.0000, -16500.0000, -16500.0000, N'', 49),
    (N'bf-vo-v51', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V51', N'Internal Door ironmongery', 2, N'', N'', N'item', 1.0000, -2560.0000, -2560.0000, N'', 50),
    (N'bf-vo-v52', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V52', N'Mineral insulation to loft space', 0, N'', N'', N'item', 1.0000, 8393.0000, 8393.0000, N'', 51),
    (N'bf-vo-v53', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V53', N'12.5mm plasterboard', 0, N'', N'', N'item', 1.0000, 6210.0000, 6210.0000, N'', 52),
    (N'bf-vo-v54', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V54', N'FFL/FCL TBC', 3, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 53),
    (N'bf-vo-v55', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V55', N'Double Sockets - P-152 Rev J, Electrical Plan SF', 0, N'', N'', N'item', 1.0000, 230.0000, 230.0000, N'', 54),
    (N'bf-vo-v56', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V56', N'P-205 Rev H Ensuite 1 & P-206 Rev H Ensuite 2', 0, N'', N'', N'item', 1.0000, 750.0000, 750.0000, N'', 55),
    (N'bf-vo-v57', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V57', N'Site Supervision - EOT-04', 0, N'', N'', N'item', 1.0000, 29300.0000, 29300.0000, N'', 56),
    (N'bf-vo-v58', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V58', N'Manifold - Carers', 0, N'', N'', N'item', 1.0000, 3907.0000, 3907.0000, N'', 57),
    (N'bf-vo-v59', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V59', N'PRO-064-(WD)-P-703  Rev —  Rear Canopies', 3, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 58),
    (N'bf-vo-v60', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V60', N'Croft SL-20 rev 11', 0, N'', N'', N'item', 1.0000, 2940.0000, 2940.0000, N'', 59),
    (N'bf-vo-v61', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V61', N'Acoustic boxing to SVP pipe through eaves space', 0, N'', N'', N'item', 1.0000, 438.0000, 438.0000, N'', 60),
    (N'bf-vo-v62', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V62', N'PRO-064-(WD)-P-706 Rev F — Ground Floor RCP', 0, N'', N'', N'item', 1.0000, 5771.5000, 5771.5000, N'', 61),
    (N'bf-vo-v63', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V63', N'PRO-064-(WD)-P-150 Rev Q — Electrical Plan GF', 0, N'', N'', N'item', 1.0000, 3565.0000, 3565.0000, N'', 62),
    (N'bf-vo-v64', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V64', N'926 mm Internal door lining & single door ( £200 supply )', 0, N'', N'', N'item', 1.0000, 25351.3700, 25351.3700, N'', 63),
    (N'bf-vo-v65', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V65', N'Staircase Omit Item', 3, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 64),
    (N'bf-vo-v66', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V66', N'Supply - Howdens Utility', 2, N'', N'', N'item', 1.0000, -23845.0000, -23845.0000, N'', 65),
    (N'bf-vo-v67', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V67', N'2nd Fix Carpentry Omit', 3, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 66),
    (N'bf-vo-v68', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V68', N'Mist & 2 coats of Dulux emulsion to ceilings', 3, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 67),
    (N'bf-vo-v69', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V69', N'Tile Supply - REVISED WITH V42 OMIT', 0, N'', N'', N'item', 1.0000, 1548.1200, 1548.1200, N'', 68),
    (N'bf-vo-v70', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V70', N'External Taps - Declined', 4, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 69),
    (N'bf-vo-v71', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V71', N'Generation - Entrance Door', 4, N'', N'', N'item', 1.0000, 0.0000, 0.0000, N'', 70),
    (N'bf-vo-v72', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V72', N'Coffer Details and Insulation ZZ bedroom - Works Completed', 0, N'', N'', N'item', 1.0000, 2285.0000, 2285.0000, N'', 71),
    (N'bf-vo-v73', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V73', N'Revised Coving and LED lighting Details', 0, N'', N'', N'item', 1.0000, 4781.4900, 4781.4900, N'', 72),
    (N'bf-vo-v76', N'3490f944b29545c4b8d5a04130f42ab8', 3, N'', N'', N'V76', N'Tile Installation & Enabling Works', 0, N'', N'', N'item', 1.0000, 38865.0000, 38865.0000, N'', 73)
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
    COUNT(*) AS VariationLines,                                                       -- 73
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
