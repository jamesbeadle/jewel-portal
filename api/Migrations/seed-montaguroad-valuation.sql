-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: 72 Montagu Road -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : 72 Montagu Road SL3 9DY
-- ProjectId: resolved at run time by site-name matcher '72montaguroad'
--
-- Seeds the ORIGINAL contract scope only, taken from the "Revised Montagu Road
-- Valuation 13 - August 25" workbook. Three blocks make up the Contract Sum,
-- as per the By France / Albany Mews pattern:
--
--     Contract works    GBP 632,512.00   (200 lines, incl. Addendum to Tender)
--     Provisional Sums  GBP 126,718.00   (PS SUMS block 14.01-14.15)
--     Contingency        GBP 60,000.00
--     ----------------------------------
--     Contract Sum      GBP 819,230.00
--
-- Variations (V01..V44, net GBP -138,496.50; Revised Contract Sum
-- GBP 680,733.50) are NOT seeded here -- they belong in
-- seed-montaguroad-variations.sql. Per-valuation claim history
-- (Valuation 01..13, retention) is claim data, not bill structure.
--
-- The workbook is a QS schedule of works with no NRM2 codes; SectionCode is
-- assigned sequentially ('01'..'14') per top-level Section heading in workbook
-- order (the workbook's own Section numbering is non-contiguous and repeats
-- "Section 2", so it is not used): 01 Preliminaries, 02 Demolition,
-- 03 Building Works, 04 Joinery, 05 Ironmongery & Door Gear (no priced lines
-- -- its only item is a PC-sum placeholder), 06 Sanitaryware (inc Plumbing)
-- and Below Ground Drainage, 07 Mechanical Electrical & Heating Installation,
-- 08 Decoration, 09 Flooring, 10 External Works, 11 Specialist Equipment,
-- 12 Completion, 13 Information, 14 Addendum to Tender. The PS SUMS block
-- keeps its workbook refs (14.01..14.15) as SectionCode.
--
-- Lines claimed at 0.00% with their value sitting in the workbook's Balance
-- column are still priced contract scope and ARE seeded (LineType 0).
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
-- "Tender - Omit item" lines (6.11.5, 6.11.7) stay priced; their omission is
-- carried by the Addendum's negative line 'Omit item 6.11.5, 6.11.7'.
-- The Addendum's negative tender-adjustment lines (SEC 3 / SEC 6 / SEC 14 /
-- Section 7.3.1 & 7.4.1 omits and the cat 6 credit) are LineType 2 (Omit,
-- negative LineAmount) -- they are part of the stated Contract Sum.
-- Two Addendum rows the workbook prices but labels "PC SUM" (solar roof
-- strengthening GBP 2,500; masonry BBQ GBP 1,000) are inline provisional sums
-- (LineType 1).
--
-- Workbook rows SKIPPED (no contract value / priced elsewhere):
--   1.11    Demolish existing garage/utility/kitchen  -- narrative row, no amount
--   1.13.1  M&E strip out                             -- priced in Section 6
--   1.13.2  Plumbing strip out                        -- priced in Section 5
--   1.13.4  Strip wall mounted M&E                    -- priced in Section 6
--   2.4.1   Plasterwork to internal walls             -- "Included in 2.3.1 & 2.3.2"
--   2.6.2   Structural steelwork                      -- PC Sum (priced 14.01)
--   3.6.6   Storage unit kitchen                      -- in kitchen PC sum (14.06)
--   3.6.7   Storage unit dining                       -- PC SUM (priced 14.02)
--   3.6.9   Storage unit lounge (main)                -- PC SUM (priced 14.03)
--   3.6.10  Gaming unit lounge (PN)                   -- PC SUM (priced 14.04)
--   3.7.2   Kitchen supply                            -- PC SUM (priced 14.06)
--   3.7.3   Utility supply                            -- PC SUM (priced 14.07)
--   4.1.1   Internal door furniture                   -- PC SUM (priced 14.08)
--   5.4.1   Existing drainage condition check         -- "Included elsewhere 1.14.1"
--   6.2.3   Air conditioning PS                       -- PC SUM (priced 14.09)
--   6.10.1  CCTV                                      -- "included in 6.6 above"
--   6.11.6  Replacement gas fire                      -- PC SUM (priced 14.11)
--   6.11.8  Remaining MEP items                       -- narrative row, no amount
--   9.6.1   Hot-tub hoist                             -- PC SUMS (priced 14.12)
--   9.6.2   Storage shed                              -- PC SUMS (priced 14.13)
--   9.7.1   Planting PS                               -- PC SUMS (priced 14.14)
--   10.3    Swim spa refurbishment                    -- PC SUMS (priced 14.15)
--
-- One description was truncated: the Addendum item 7 front-door row (mr-cw-188)
-- ends at "NB: exact door to be confirmed by client." -- the remaining
-- installer/manufacturer spec listing and URL exceeded a sensible length.
-- One row keeps the workbook AMOUNT over qty x rate: 15.6 (mr-cw-187,
-- SEC 14 ironmongery omit) shows rate 2,718.00 but amount -2,718.00; seeded
-- as Quantity 1 x Rate -2,718.00.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (mr-cw-NNN / mr-ps-NN
-- / mr-cont-NN). A re-run refreshes every field via MERGE. Variation lines for
-- this project are left untouched. Safe to run repeatedly.
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
MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'mr-cw-001', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 1),
    (N'mr-cw-002', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Management and staff', N'weeks', 32.0000, 750.0000, 24000.0000, N'', 2),
    (N'mr-cw-003', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Site accommodation', N'weeks', 32.0000, 90.0000, 2880.0000, N'', 3),
    (N'mr-cw-004', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SET', N'Services and facilities', N'item', 1.0000, 500.0000, 500.0000, N'', 4),
    (N'mr-cw-005', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SET', N'Mechanical Plant', N'item', 1.0000, 600.0000, 600.0000, N'', 5),
    (N'mr-cw-006', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-TMP', N'Temporary works', N'item', 1.0000, 2500.0000, 2500.0000, N'', 6),
    (N'mr-cw-007', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health & Safety', N'weeks', 32.0000, 125.0000, 4000.0000, N'', 7),
    (N'mr-cw-008', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'PRELIMS-SET', N'The property will be not be occupied during the contract period. The main contractor is to provide a photographic and written survey record of the entire property & site including; internal & external of building(s), external hard & soft landscaped areas (inc, all boundaries and walls etc) and external highways adjacent to the property.', N'item', 1.0000, 300.0000, 300.0000, N'', 8),
    (N'mr-cw-009', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Allow for isolating central heating and water supplies and removing all plumbing/heating/gas services as required to areas affected by demolitions and adjustments. Include for removal of all redundant services. This element should be priced independently here and is separate from the works noted in Plumbing section of this Schedule (Section 6).', N'item', 1.0000, 600.0000, 600.0000, N'', 9),
    (N'mr-cw-010', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'UTIL-STD', N'Identify all incoming services locations around the property and allow for costs for adjustment and/or connection as appropriate at the time of Tendering. Cost shall include for any applicable statutory undertakers / utility company costs.', N'item', 1.0000, 15000.0000, 15000.0000, N'Omit item V05', 10),
    (N'mr-cw-011', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'MEC-PLM', N'Prior to works commencing allow for carrying out a gas safety check on any existing gas installations. Checks must be undertaken by an engineer approved under the Gas Safety Register. This element should be priced independently here and is separate from the works noted in Plumbing section of this Schedule (Section 6).', N'item', 1.0000, 400.0000, 400.0000, N'', 11),
    (N'mr-cw-012', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-SKP', N'Cart all removed waste material off site unless noted otherwise on the drawings.', N'skips', 16.0000, 380.0000, 6080.0000, N'', 12),
    (N'mr-cw-013', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-STS', N'temporary propping/supports - provide all temporary supports as necessary to remove all sections of load bearing and non load bearing walls as indicated on the aforementioned drawing. MC to assess load bearing status when tendering (during site visit). Include for Health & Safety assessing and provide full method statement and risk assessment for works. Include for all costs in disposal.', N'item', 1.0000, 1550.0000, 1550.0000, N'', 13),
    (N'mr-cw-014', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove all blinds, curtains/poles etc where noted on drawing and discard from site', N'item', 1.0000, 150.0000, 150.0000, N'', 14),
    (N'mr-cw-015', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-ASB', N'whole house intrusive asbestos survey required prior to demolition works commencing. MC to price for survey & controlled removal of any ACM''s.', N'item', 1.0000, 2000.0000, 2000.0000, N'', 15),
    (N'mr-cw-016', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'PRELIMS-PRO', N'MC to temporarily protect existing & retained elements of structure & finishes and make good prior to completion.', N'item', 1.0000, 420.0000, 420.0000, N'', 16),
    (N'mr-cw-017', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'SUB-EXC', N'prior to start on site the main contractor will open up & expose the existing foundations for SE to inspect.', N'item', 1.0000, 250.0000, 250.0000, N'', 17),
    (N'mr-cw-018', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'breakout existing ground floor construction (believed to be solid construction) to facilitate installation of new beam and block flooring', N'm2', 72.0000, 42.0000, 3024.0000, N'', 18),
    (N'mr-cw-019', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove all floor finishes throughout the house (at ground and first)', N'item', 1.0000, 450.0000, 450.0000, N'', 19),
    (N'mr-cw-020', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'demolish existing internal walls at ground & first floors as shown on demo plan', N'm2', 18.0000, 36.0000, 648.0000, N'', 20),
    (N'mr-cw-021', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'break-out new internal door openings and widen existing internal structural door openings in preparation for installation of new wide doors sets. NB: new lintels priced in 2.6.1', N'nr', 5.0000, 95.0000, 475.0000, N'', 21),
    (N'mr-cw-022', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove all wall tiling from all walls within the property', N'item', 1.0000, 120.0000, 120.0000, N'', 22),
    (N'mr-cw-023', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove all wallpaper from all walls within the property', N'item', 1.0000, 800.0000, 800.0000, N'', 23),
    (N'mr-cw-024', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove entire ceiling within existing kitchen, WC, study & part of the hall at ground floor', N'm2', 32.0000, 12.0000, 384.0000, N'', 24),
    (N'mr-cw-025', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove entire ceiling within existing bedroom 4 and only part of the ceiling within bedroom 3.', N'm2', 12.0000, 12.0000, 144.0000, N'', 25),
    (N'mr-cw-026', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'all artex ceilings to be scrapped in preparation for over skimming. NB: asbestos survey required prior to undertaking any ceiling works.', N'm2', 130.0000, 6.0000, 780.0000, N'', 26),
    (N'mr-cw-027', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove existing skirting boards as shown on the demolition drawing', N'item', 1.0000, 100.0000, 100.0000, N'', 27),
    (N'mr-cw-028', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove existing architraves & door linings as shown/noted on the demo drawing', N'nr', 13.0000, 18.0000, 234.0000, N'', 28),
    (N'mr-cw-029', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove existing window boards as noted on the demo drawing', N'item', 1.0000, 80.0000, 80.0000, N'', 29),
    (N'mr-cw-030', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove all existing sanitaryware fixtures & fittings from within the house bathroom, WC & ensuite (bed 1)', N'nr', 2.0000, 240.0000, 480.0000, N'', 30),
    (N'mr-cw-031', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Carefully remove existing fixtures and fittings to existing kitchens. All kitchen units, appliances and the like will remain in the ownership of the client for their sale or re-use and shall only be disposed of given express permission of the client. This will be clarified prior to commencement. NB: MC to photo - in detail - existing kitchen (inc existing damage) prior to removal and present to the CA.', N'item', 1.0000, 220.0000, 220.0000, N'', 31),
    (N'mr-cw-032', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove all existing fixtures and fittings from the existing utility and cart off site.', N'item', 1.0000, 120.0000, 120.0000, N'', 32),
    (N'mr-cw-033', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'include for remove all external windows and doors as noted on the demolition drawing.', N'item', 1.0000, 450.0000, 450.0000, N'', 33),
    (N'mr-cw-034', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'demolish existing external walls noted on demolition plans. Also include for raising the head height of the existing study window as noted on the plan and rear elevation. NB: new lintels priced in 2.6.1', N'm2', 42.0000, 75.0000, 3150.0000, N'', 34),
    (N'mr-cw-035', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'strip existing pitched roof to facilitate the construction of the new pitched roof. Works to be undertaken at such a time as to maintain the weather integrity of the house for as long as possible (ie: just prior to construction of new pitched roof).', N'm2', 26.0000, 22.0000, 572.0000, N'', 35),
    (N'mr-cw-036', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'strip existing flat roof finish to main entrance canopy and remove existing fascia''s. NB: existing soffits to be retained', N'm2', 58.0000, 12.0000, 696.0000, N'', 36),
    (N'mr-cw-037', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove existing tarmac surfacing, associated pin kerbs to front of property etc as shown on external layout plan', N'm2', 100.0000, 14.0000, 1400.0000, N'', 37),
    (N'mr-cw-038', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove existing landscaped features to front lawn and also remodel the grassed area to suit the new parking layout.', N'item', 1.0000, 450.0000, 450.0000, N'', 38),
    (N'mr-cw-039', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'remove side gate & wall, demolish existing steps, perimeter footpath, pond, dwarf wall and low level decking to rear of property etc as shown on external layout plan', N'item', 1.0000, 1288.0000, 1288.0000, N'', 39),
    (N'mr-cw-040', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'PRELIMS-PRO', N'Existing fire place to be retained and protected (ply boxing) during construction.', N'item', 1.0000, 150.0000, 150.0000, N'', 40),
    (N'mr-cw-041', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'MC to carefully remove the existing motorised awning to the rear of the property. MC shall check - and confirm - its condition prior to removal', N'item', 1.0000, 100.0000, 100.0000, N'', 41),
    (N'mr-cw-042', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'MEC-DRN', N'MC to cost for CCTV conditions survey & detailed report of existing below ground drainage system.', N'item', 1.0000, 2000.0000, 2000.0000, N'', 42),
    (N'mr-cw-043', @ProjectId, 0, N'02', N'Demolition', N'', N'', 0, N'MEC-DRN', N'Existing below ground drainage scheme - in part - to be removed in preparation for connection of new scheme.', N'item', 1.0000, 300.0000, 300.0000, N'', 43),
    (N'mr-cw-044', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-1FX', N'Proposed ensuite wetroom floor build-up at first floor. Allow for cutting out of existing floor deck and possible joist structure (TBC after opening up) to facilitate installation of new level shower tray. Refer to drawing 12978-15 for layout & spec.', N'm2', 2.0000, 125.0000, 250.0000, N'', 44),
    (N'mr-cw-045', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'FLR-SCR', N'Patch fill any areas of pockets/depressions etc as a result of the demolition works. Refer to section 8 for further works to existing floors.', N'item', 1.0000, 200.0000, 200.0000, N'', 45),
    (N'mr-cw-046', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'INT-PLB', N'Line existing walls as noted in IW-E on drawing 12978-04 & 05. include MR board to wet areas.', N'm2', 24.0000, 38.0000, 912.0000, N'', 46),
    (N'mr-cw-047', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'INT-PLS', N'All new plasterwork to altered works be 13mm Thistle or similar (except where described individually) or 12.5mm British Gypsum plasterboard or similar with skim finish to match surrounding existing fabric. Contractor to allow for all new plasterwork required including all finishing and patching to altered works. Renew any debonded plaster etc.', N'm2', 238.0000, 38.0000, 9044.0000, N'', 47),
    (N'mr-cw-048', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'INT-PLS', N'existing artex ceilings to receive new skim finish as ref IC-02 noted on drawing 12978-04 & 05', N'm2', 130.0000, 28.0000, 3640.0000, N'', 48),
    (N'mr-cw-049', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'MASON-BRK', N'Allow here for all necessary lintels throughout the scheme in accordance with drawing 12978-04 & 05.', N'm', 12.0000, 142.0000, 1704.0000, N'', 49),
    (N'mr-cw-050', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-TLO', N'MC to cost for brushing down & inspecting existing pitched roof tiles which are to remain.', N'item', 1.0000, 500.0000, 500.0000, N'', 50),
    (N'mr-cw-051', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-GRU', N'existing gutters and downpipes to be cleaned and tested.', N'item', 1.0000, 250.0000, 250.0000, N'', 51),
    (N'mr-cw-052', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-2FX', N'new loft ladder required, fixed back to existing hatch (existing hatch condition and operation to be checked prior to installation)', N'item', 1.0000, 475.0000, 475.0000, N'Omit item V34', 52),
    (N'mr-cw-053', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-TLO', N'additional venting required to existing pitched roof. MC to ascertain existing situation and provide proposals for additional vents', N'item', 1.0000, 300.0000, 300.0000, N'', 53),
    (N'mr-cw-054', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-CUT', N'reports of sagging roof timbers existing roof structure to be assessed by SE. in absence of SE info the tendering main contractor shall asses the existing roof structure at mid-tender site visit and assign a notional cost', N'item', 1.0000, 1000.0000, 1000.0000, N'', 54),
    (N'mr-cw-055', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'INT-INC', N'additional insulation installed within existing roof space as noted in section PR-01:12', N'm2', 86.0000, 38.0000, 3268.0000, N'', 55),
    (N'mr-cw-056', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'SUB-EXC', N'refer to spec ref FO-GN on drawing 12978-08 for general notes to price to. Foundations to be designed by SE prior to construction.', N'm', 34.0000, 220.0000, 7480.0000, N'Omit item V04', 56),
    (N'mr-cw-057', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'SUB-CON', N'New concrete base for lift priced as detail EW.01b on drawing 12978-07 & spec FO-GN:10 on drawing 12978-08', N'm2', 2.5000, 398.0000, 995.0000, N'Omit item V04', 57),
    (N'mr-cw-058', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'SUB-CON', N'price for new beam and block floor as section IF-01 on drawing 12978-04', N'm2', 106.0000, 282.0000, 29892.0000, N'Omit - V04', 58),
    (N'mr-cw-059', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-1FX', N'price for new timber floor as section IF-02 on drawing 12978-05', N'm2', 26.0000, 168.0000, 4368.0000, N'', 59),
    (N'mr-cw-060', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-1FX', N'price for new internal walls as noted in section IW-01 on drawing 12978-04 & 05.', N'm2', 24.0000, 80.0000, 1920.0000, N'', 60),
    (N'mr-cw-061', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-1FX', N'price for new internal walls as noted in section IW-02 on drawing 12978-04 & 05.', N'm2', 12.0000, 116.0000, 1392.0000, N'', 61),
    (N'mr-cw-062', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-1FX', N'price for new internal walls as noted in section IW-02a on drawing 12978-04 & 05.', N'm2', 10.0000, 124.0000, 1240.0000, N'', 62),
    (N'mr-cw-063', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-1FX', N'price for new internal walls as noted in section IW-03 on drawing 12978-04 & 05.', N'm2', 22.0000, 120.0000, 2640.0000, N'', 63),
    (N'mr-cw-064', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'MASON-BRK', N'price for new external walls (including internal plaster finish) as noted in section EW-GN & EW-01 on drawing 12978-04 & 05.', N'm2', 118.0000, 278.0000, 32804.0000, N'', 64),
    (N'mr-cw-065', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'INT-PLB', N'price for new internal ceilings as noted in section IC-01 on drawing 12978-04 & 05.', N'm2', 106.0000, 40.0000, 4240.0000, N'Omit item V39', 65),
    (N'mr-cw-066', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'INT-PLB', N'price for new internal beam boxing as noted in section IC-03 on drawing 12978-04 & 05.', N'm2', 8.0000, 56.0000, 448.0000, N'', 66),
    (N'mr-cw-067', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'CARP-1FX', N'price for new internal ceiling strengthening as noted in section IC-04 on drawing 12978-04 & 05.', N'm2', 20.0000, 82.0000, 1640.0000, N'Omit item V40', 67),
    (N'mr-cw-068', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-FLT', N'new flat roof priced as section FR-01 on drawing 12978-06', N'm2', 58.0000, 288.0000, 16704.0000, N'', 68),
    (N'mr-cw-069', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-FLT', N'new flat roofing to existing canopy roof priced as section FR-01 on drawing 12978-06. NB: assume new ply required but reuse existing canopy support framing.', N'm2', 10.0000, 180.0000, 1800.0000, N'', 69),
    (N'mr-cw-070', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-TLN', N'new pitched roof priced as section PR-01 on drawing 12978-06', N'm2', 68.0000, 248.0000, 16864.0000, N'', 70),
    (N'mr-cw-071', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-TLO', N're-roofing to existing pitched roof priced as section PR-01 on drawing 12978-06', N'm2', 122.0000, 154.0000, 18788.0000, N'', 71),
    (N'mr-cw-072', @ProjectId, 0, N'03', N'Building Works', N'', N'', 0, N'ROOF-FSU', N'New fascia / soffit throughout', N'item', 1.0000, 3420.0000, 3420.0000, N'', 72),
    (N'mr-cw-073', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-DOR', N'MC to price in this section for supply and installation for new INTERNAL doors, linings, frame and architrave as shown on the drawings and door schedule.', N'item', 1.0000, 10115.0000, 10115.0000, N'Omit item Section 15.6', 73),
    (N'mr-cw-074', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-DOR', N'MC to price separately in this section for works to ID-04 (existing bi-parting doors).', N'item', 1.0000, 300.0000, 300.0000, N'Omit item Section 15.7', 74),
    (N'mr-cw-075', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'WDR-TIM', N'ED-01 - main entrance door', N'nr', 1.0000, 2075.0000, 2075.0000, N'Omit item section 15.7', 75),
    (N'mr-cw-076', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'WDR-ALU', N'ED-02 - lounge bi-folds', N'nr', 1.0000, 4602.0000, 4602.0000, N'', 76),
    (N'mr-cw-077', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'WDR-ALU', N'ED-03 - utility door', N'nr', 1.0000, 2418.0000, 2418.0000, N'', 77),
    (N'mr-cw-078', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'WDR-ALU', N'ED-04 - kitchen bi-folds', N'nr', 1.0000, 5915.0000, 5915.0000, N'', 78),
    (N'mr-cw-079', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'WDR-UPV', N'MC specialist to undertake conditions survey of existing windows to ascertain if any works are required to existing windows other than that noted below.', N'item', 1.0000, 300.0000, 300.0000, N'', 79),
    (N'mr-cw-080', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'WDR-UPV', N'MC to install new trickle vents within remaining existing windows', N'nr', 10.0000, 125.0000, 1250.0000, N'', 80),
    (N'mr-cw-081', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'WDR-UPV', N'MC to price all new windows in this section.', N'item', 1.0000, 7695.0000, 7695.0000, N'', 81),
    (N'mr-cw-082', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-2FX', N'MC to price all new window boards in this section. Window boards required to all new windows', N'm', 12.0000, 42.0000, 504.0000, N'', 82),
    (N'mr-cw-083', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-2FX', N'MC to price all new skirting boards in this section. refer to finishes drawing 12978-8 for layout & 12978-13 for spec & details.', N'm', 122.0000, 24.0000, 2928.0000, N'', 83),
    (N'mr-cw-084', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-2FX', N'MC to price all new architraves in this section.', N'm', 170.0000, 12.0000, 2040.0000, N'', 84),
    (N'mr-cw-085', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-JNR', N'price IPS / storage unit in Accessible WC. refer to detail 03 on drawing 12978-13 for information.', N'item', 1.0000, 600.0000, 600.0000, N'', 85),
    (N'mr-cw-086', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-JNR', N'price storage unit in therapy. refer to detail 06 on drawing 12978-13 for information.', N'item', 1.0000, 1000.0000, 1000.0000, N'', 86),
    (N'mr-cw-087', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-WRD', N'price storage / wardrobe unit in bedroom (PN). refer to detail 09 on drawing 12978-13 for information.', N'item', 1.0000, 2000.0000, 2000.0000, N'', 87),
    (N'mr-cw-088', @ProjectId, 0, N'04', N'Joinery', N'', N'', 0, N'CARP-JNR', N'height adjustable desk in PN bedroom. refer to detail 11 on drawing 12978-13 for information.', N'item', 1.0000, 1200.0000, 1200.0000, N'', 88),
    (N'mr-cw-089', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'SUP-SAN', N'accessible WC - refer to drawing 12978-15 for list of requirements', N'item', 1.0000, 2988.0000, 2988.0000, N'Omit item V23', 89),
    (N'mr-cw-090', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'SUP-SAN', N'accessible ensuite - refer to drawing 12978-15 for list of requirements', N'item', 1.0000, 5320.0000, 5320.0000, N'Omit item V23', 90),
    (N'mr-cw-091', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'SUP-SAN', N'house bathroom - refer to drawing 12978-16 for list of requirements', N'item', 1.0000, 4278.0000, 4278.0000, N'Omit item V23', 91),
    (N'mr-cw-092', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'SUP-SAN', N'bed1 ensuite - refer to drawing 12978-16 for list of requirements', N'item', 1.0000, 4342.0000, 4342.0000, N'Omit item V23', 92),
    (N'mr-cw-093', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'SUB-DRN', N'price below ground foul water drainage system (including gullies etc)', N'item', 1.0000, 3988.0000, 3988.0000, N'', 93),
    (N'mr-cw-094', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'SUB-DRN', N'price below ground surface water drainage system including all gullies, drainage channels etc as required.', N'item', 1.0000, 3746.0000, 3746.0000, N'', 94),
    (N'mr-cw-095', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'MEC-DRN', N'MC to price for CCTV survey of new drainage installation. Report to be issued prior to handover to document drainage works at handover.', N'item', 1.0000, 1000.0000, 1000.0000, N'', 95),
    (N'mr-cw-096', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'MEC-PLM', N'MC to include for all demolition associated with plumbing works included in this section', N'item', 1.0000, 300.0000, 300.0000, N'', 96),
    (N'mr-cw-097', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'MEC-PLM', N'water pressure reportedly low and shall be be checked by specialist to ascertain if any works are required to address the situation.', N'item', 1.0000, 250.0000, 250.0000, N'', 97),
    (N'mr-cw-098', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'MEC-PLM', N'Provide hot and cold water services to all new sanitaryware, kitchens etc. refer to drawings 12978-15 & 16 for bathroom layouts & 12978-13 for kitchen & utility layouts. Also refer to drawing 12978-08 for supporting technical notes', N'nr', 26.0000, 160.0000, 4160.0000, N'', 98),
    (N'mr-cw-099', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'MEC-DRN', N'allow for all new above ground foul/waste drainage to bathrooms, kitchens & utility. This includes soil stacks, AAV, SVPs etc.', N'item', 1.0000, 3024.0000, 3024.0000, N'', 99),
    (N'mr-cw-100', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'MEC-PLM', N'Price here for external taps.', N'nr', 3.0000, 145.0000, 435.0000, N'', 100),
    (N'mr-cw-101', @ProjectId, 0, N'06', N'Sanitaryware (inc Plumbing) and Below Ground Drainage', N'', N'', 0, N'ROOF-GRU', N'Allow for new rainwater goods (RWPs, guttering, fixings etc) in this section', N'item', 1.0000, 2652.0000, 2652.0000, N'', 101),
    (N'mr-cw-102', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'MC to include here cost to isolate relevant electrical services and remove redundant electrical items (including disposal).', N'item', 1.0000, 500.0000, 500.0000, N'', 102),
    (N'mr-cw-103', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'MEC-PLM', N'cost for demolition & strip out of existing mechanical items shall be included here. Refer to demo drawing (12978-03) for detailed info.', N'item', 1.0000, 250.0000, 250.0000, N'', 103),
    (N'mr-cw-104', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'MEC-VNT', N'Allow for mechanical extraction as shown in the MEP drawing (12978-10), also refer to technical notes on 12978-08.', N'nr', 6.0000, 275.0000, 1650.0000, N'', 104),
    (N'mr-cw-105', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'cost for demolition & strip out of existing electric system strip - inc all redundant wall mounted M&E items from face of building - shall be included here. Refer to demo drawing (12978-03) for detailed info.', N'item', 1.0000, 250.0000, 250.0000, N'', 105),
    (N'mr-cw-106', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'NEW POWER: Price for new electrical outlets points/fittings as indicated and described on drawing TBC1.05 & 06 These are provisional locations at tender stage as room layouts are not yet know - final positioning to be strictly agreed with client. Include here additional power spurs for specialist equipment, ceiling hoists, fans, duel fuel towel radiators etc', N'nr', 90.0000, 120.0000, 10800.0000, N'', 106),
    (N'mr-cw-107', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'Include for renewal and relocation of the electric meter position into a new external cabinet - indicative location as shown in MEP plan', N'item', 1.0000, 1500.0000, 1500.0000, N'', 107),
    (N'mr-cw-108', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price here for all power to external window & doors an also internal doors etc (this is futureproofing for possible automation)', N'item', 1.0000, 1250.0000, 1250.0000, N'', 108),
    (N'mr-cw-109', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for new spotlights only', N'nr', 64.0000, 35.0000, 2240.0000, N'', 109),
    (N'mr-cw-110', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for new wiring, controls etc associated with spotlight installation', N'nr', 64.0000, 75.0000, 4800.0000, N'', 110),
    (N'mr-cw-111', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for new pendants only', N'nr', 20.0000, 50.0000, 1000.0000, N'', 111),
    (N'mr-cw-112', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for new wiring, controls etc associated with pendant installation', N'nr', 20.0000, 75.0000, 1500.0000, N'', 112),
    (N'mr-cw-113', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for new wall lights only', N'nr', 2.0000, 55.0000, 110.0000, N'', 113),
    (N'mr-cw-114', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for new wiring, controls etc associated with wall light installation', N'nr', 2.0000, 75.0000, 150.0000, N'', 114),
    (N'mr-cw-115', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-FIR', N'price for new Grade A Cat LD2 mains interlinked fire detection and fire alarm system', N'nr', 7.0000, 145.0000, 1015.0000, N'Omit item V16', 115),
    (N'mr-cw-116', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-ALM', N'Price for a new alarm system including CCTV capabilities - indicative layout shown on plans.', N'item', 1.0000, 3500.0000, 3500.0000, N'Omit item V12', 116),
    (N'mr-cw-117', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-FIR', N'Price for new system, layout as shown on layout drawing', N'nr', 4.0000, 180.0000, 720.0000, N'', 117),
    (N'mr-cw-118', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-AV', N'Exact requirements will be confirmed by client, but Include here for all costs associated with communications and entertainment systems as shown on drawing', N'item', 1.0000, 1500.0000, 1500.0000, N'Omit item V10', 118),
    (N'mr-cw-119', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for low level decking lights to - play & spa deck. Also Include for all installation, wiring, control etc.', N'nr', 8.0000, 175.0000, 1400.0000, N'', 119),
    (N'mr-cw-120', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for mid level decking lights to - terrace deck allow £50 per lighting unit (supply) & also Include for all installation, wiring, control etc.', N'nr', 21.0000, 185.0000, 3885.0000, N'', 120),
    (N'mr-cw-121', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for mid level decking lights to - planter/building frontage allow £50 per lighting unit (supply) & also Include for all installation, wiring, control etc.', N'nr', 10.0000, 185.0000, 1850.0000, N'', 121),
    (N'mr-cw-122', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for mid level decking lights to - spa deck allow £50 per lighting unit (supply) & also Include for all installation, wiring, control etc..', N'nr', 7.0000, 185.0000, 1295.0000, N'', 122),
    (N'mr-cw-123', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for all bollard lights to front & rear. allow £100 per lighting unit (supply) & also Include for all installation, wiring, control etc.', N'nr', 6.0000, 290.0000, 1740.0000, N'', 123),
    (N'mr-cw-124', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for remaining external lighting (ie: to 2 no storage sheds, building lighting etc)', N'item', 1.0000, 1820.0000, 1820.0000, N'', 124),
    (N'mr-cw-125', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-STD', N'price for external power supplies as noted on drawings', N'nr', 6.0000, 135.0000, 810.0000, N'', 125),
    (N'mr-cw-126', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'ELE-AV', N'price for new wifi and boosters', N'item', 1.0000, 800.0000, 800.0000, N'', 126),
    (N'mr-cw-127', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'MEC-PLM', N'Cost for demolition & strip out of existing heating system shall be included here. Refer to demo drawing (12978-03) for detailed info.', N'item', 1.0000, 300.0000, 300.0000, N'', 127),
    (N'mr-cw-128', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'MEC-BLR', N'existing redundant flue to be lined, swept & tested by a registered person. A modified terminal may be required.', N'item', 1.0000, 900.0000, 900.0000, N'Omit item V35', 128),
    (N'mr-cw-129', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'MEC-PLM', N'price here for all new duel fuel tower radiators and associated pipework', N'nr', 4.0000, 495.0000, 1980.0000, N'Omit item V09', 129),
    (N'mr-cw-130', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'MEC-BLR', N'price here for all new wet radiator system and associated pipework, boiler etc', N'item', 1.0000, 9850.0000, 9850.0000, N'Tender - Omit item', 130),
    (N'mr-cw-131', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'MEC-UFH', N'price for new underfloor heating system', N'm2', 74.0000, 150.0000, 11100.0000, N'Tender - Omit item', 131),
    (N'mr-cw-132', @ProjectId, 0, N'07', N'Mechanical, Electrical & Heating Installation', N'', N'', 0, N'UTIL-STD', N'Include for renewal and relocation of the gas meter position into a new external cabinet', N'item', 1.0000, 1500.0000, 1500.0000, N'Omit item V05', 132),
    (N'mr-cw-133', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'price for new internal wall paint finish, spec ref WP1', N'm2', 482.0000, 16.0000, 7712.0000, N'Omit item section 10', 133),
    (N'mr-cw-134', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'price for new internal specialist wall paint finish, spec ref WP2', N'm2', 48.0000, 18.0000, 864.0000, N'', 134),
    (N'mr-cw-135', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'price for new internal ceiling paint finish, spec ref CP1', N'm2', 270.0000, 18.0000, 4860.0000, N'Omit item section 10', 135),
    (N'mr-cw-136', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'price for new internal specialist ceiling paint finish, spec ref CP2', N'm2', 20.0000, 20.0000, 400.0000, N'', 136),
    (N'mr-cw-137', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'price for new internal joinery paint finish, spec ref JC1', N'item', 1.0000, 4250.0000, 4250.0000, N'', 137),
    (N'mr-cw-138', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'price for new external joinery paint finish, spec ref JC2', N'item', 1.0000, 2055.0000, 2055.0000, N'', 138),
    (N'mr-cw-139', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'TIL-STD', N'price for new internal wall tiling to kitchen, spec ref WT1', N'm2', 5.0000, 129.5000, 647.5000, N'Omit supply item V24', 139),
    (N'mr-cw-140', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'TIL-STD', N'price for new internal wall tiling to utility, spec ref WT1', N'm2', 5.0000, 129.5000, 647.5000, N'Omit supply item V24', 140),
    (N'mr-cw-141', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'TIL-STD', N'price for new internal wall tiling to accessible WC, spec ref WT1', N'm2', 1.0000, 129.5000, 129.5000, N'Omit supply item V24', 141),
    (N'mr-cw-142', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'TIL-STD', N'price for new internal wall tiling to PN wetroom, spec ref WT1', N'm2', 18.0000, 129.5000, 2331.0000, N'Omit supply item V24', 142),
    (N'mr-cw-143', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'TIL-STD', N'price for new internal wall tiling to house bathroom, spec ref WT1', N'm2', 18.0000, 129.5000, 2331.0000, N'Omit supply item V24', 143),
    (N'mr-cw-144', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'TIL-STD', N'price for new internal wall tiling to Bed 1 en-suite, spec ref WT1', N'm2', 18.0000, 129.5000, 2331.0000, N'Omit supply item V24', 144),
    (N'mr-cw-145', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'clean and paint existing high level timber fascia and soffit, refer to finishes drawing for more detail. Spec notes as JC2 on drawing 12978-09', N'item', 1.0000, 1898.0000, 1898.0000, N'', 145),
    (N'mr-cw-146', @ProjectId, 0, N'08', N'Decoration', N'', N'', 0, N'DEC-STD', N'clean and paint existing entrance canopy soffit, refer to finishes drawing for more detail. Spec notes as JC2 on drawing 12978-10', N'item', 1.0000, 550.0000, 550.0000, N'', 146),
    (N'mr-cw-147', @ProjectId, 0, N'09', N'Flooring', N'', N'', 0, N'FLR-SLF', N'Allow for provision of self levelling screed over all existing solid floors to receive new vinyl flooring. Assume at tender stage that this will apply to all existing ground floor areas.', N'm2', 166.0000, 32.0000, 5312.0000, N'', 147),
    (N'mr-cw-148', @ProjectId, 0, N'09', N'Flooring', N'', N'', 0, N'FLR-WD', N'Timber floors: Allow to provide and fix 6mm ply to all existing areas to provide level and flush FFL ready to received new floor finishes. Assume at tender stage that this will apply to all existing first floor areas.', N'm2', 96.0000, 14.0000, 1344.0000, N'', 148),
    (N'mr-cw-149', @ProjectId, 0, N'09', N'Flooring', N'', N'', 0, N'FLR-LVT', N'Price here for all areas as spec note FT2 shown on drawing 12978-09', N'm2', 258.0000, 100.0000, 25800.0000, N'', 149),
    (N'mr-cw-150', @ProjectId, 0, N'09', N'Flooring', N'', N'', 0, N'TIL-STD', N'Price here for all areas as spec note FT1 shown on drawing 12978-09', N'm2', 24.0000, 69.0000, 1656.0000, N'Omit item V24', 150),
    (N'mr-cw-151', @ProjectId, 0, N'09', N'Flooring', N'', N'', 0, N'FLR-CPT', N'Price here for all areas as spec note FT3 shown on drawing 12978-09', N'm2', 6.0000, 73.5000, 441.0000, N'', 151),
    (N'mr-cw-152', @ProjectId, 0, N'09', N'Flooring', N'', N'', 0, N'FLR-LVT', N'Price here for all transition strip as spec note TS-01 shown on drawing 12978-09', N'item', 1.0000, 500.0000, 500.0000, N'', 152),
    (N'mr-cw-153', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'HAND-CLE', N'Contractor to allow for thoroughly cleaning all paths, patio, driveways etc. on completion of the works and prior to handover.', N'item', 1.0000, 500.0000, 500.0000, N'', 153),
    (N'mr-cw-154', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-PAV', N'price new pedestrian paving''s to rear of building as described on aforementioned drawing', N'm2', 70.0000, 145.0000, 10150.0000, N'', 154),
    (N'mr-cw-155', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-DEK', N'price new elevated terrace decking as described on aforementioned drawing', N'm2', 80.0000, 132.0000, 10560.0000, N'', 155),
    (N'mr-cw-156', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-DEK', N'price new ''play'' decking as described on aforementioned drawing', N'm2', 28.0000, 150.0000, 4200.0000, N'', 156),
    (N'mr-cw-157', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-DEK', N'price new decking to spa area as described on aforementioned drawing', N'm2', 40.0000, 150.0000, 6000.0000, N'', 157),
    (N'mr-cw-158', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-DEK', N'price new handrail to elevated terrace decking as described on aforementioned drawing - detail 05', N'm', 46.0000, 85.0000, 3910.0000, N'', 158),
    (N'mr-cw-159', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-DEK', N'price new handrail to spa decking as described on aforementioned drawing - detail 05', N'm', 10.0000, 85.0000, 850.0000, N'', 159),
    (N'mr-cw-160', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-LND', N'price new gravel bed as described on aforementioned drawing', N'item', 1.0000, 600.0000, 600.0000, N'', 160),
    (N'mr-cw-161', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-PAV', N'price new vehicular hardstanding to front of building as described on aforementioned drawing', N'm2', 80.0000, 115.0000, 9200.0000, N'', 161),
    (N'mr-cw-162', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-PAV', N'price new ''vehicular'' paving''s to front of building - steps & ramp', N'm2', 20.0000, 125.0000, 2500.0000, N'', 162),
    (N'mr-cw-163', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-FEN', N'price here for new timber fencing and pedestrian gates described on the drawing', N'item', 1.0000, 520.0000, 520.0000, N'', 163),
    (N'mr-cw-164', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-LND', N'price here for new planter - for spec refer to detail 03 on the aforementioned drawing', N'item', 1.0000, 2000.0000, 2000.0000, N'', 164),
    (N'mr-cw-165', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'STR-MRL', N'price here for handrailing associated with planter & steps - refer to detail 05 on the aforementioned drawing', N'item', 1.0000, 1500.0000, 1500.0000, N'', 165),
    (N'mr-cw-166', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'STR-MRL', N'price for new railing infill to spa deck', N'item', 1.0000, 400.0000, 400.0000, N'', 166),
    (N'mr-cw-167', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-LND', N'Existing trees to be assessed by a registered arboriculturist to ascertain condition and if any works are required.', N'item', 1.0000, 250.0000, 250.0000, N'', 167),
    (N'mr-cw-168', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-TRF', N'price here for new grassed area', N'm2', 130.0000, 44.0000, 5720.0000, N'', 168),
    (N'mr-cw-169', @ProjectId, 0, N'10', N'External Works', N'', N'', 0, N'EXTW-TRF', N'price here for new seeding to existing lawn(s)', N'item', 1.0000, 600.0000, 600.0000, N'', 169),
    (N'mr-cw-170', @ProjectId, 0, N'11', N'Specialist Equipment', N'', N'', 0, N'SPEC-LFT', N'LIFT SPEC - MC to price for supply & installation of new passenger lift & associated BWIC with such. lift spec as noted on drawing 12978-08: This sum does not cover the lift shaft or pit which are to be priced elsewhere in this document.', N'item', 1.0000, 27076.0000, 27076.0000, N'Omit item V14', 170),
    (N'mr-cw-171', @ProjectId, 0, N'11', N'Specialist Equipment', N'', N'', 0, N'SPEC-SPA', N'hot tub replacement works - MC to price for removing existing hot tub and installation of client supply hot-tub (currently in use at the clients current property). MC to allow for disposal of site hot tub, collecting client hot tub, transportation and re-installation within the spa deck.', N'item', 1.0000, 2000.0000, 2000.0000, N'', 171),
    (N'mr-cw-172', @ProjectId, 0, N'12', N'Completion', N'', N'', 0, N'HAND-CLI', N'The property is to be thoroughly cleaned to the satisfaction of the CA and client. All services to be tested and adjusted. All locks etc. to be tested, adjusted and where applicable lubricated. Contractor to allow for contract cleaners full sparkle clean throughout. No ''builders cleans'' will be accepted. The property will be left in full working order. All lightbulbs to be fully operational.', N'item', 1.0000, 750.0000, 750.0000, N'', 172),
    (N'mr-cw-173', @ProjectId, 0, N'12', N'Completion', N'', N'', 0, N'ENABLE-SKP', N'Remove all debris from site, including all equipment and fittings that are deemed to be redundant.', N'item', 1.0000, 400.0000, 400.0000, N'', 173),
    (N'mr-cw-174', @ProjectId, 0, N'12', N'Completion', N'', N'', 0, N'HAND-CLE', N'Adjacent paths, roads, garden areas etc. to be cleaned and if necessary repaired to a reasonable standard.', N'item', 1.0000, 350.0000, 350.0000, N'', 174),
    (N'mr-cw-175', @ProjectId, 0, N'13', N'Information', N'', N'', 0, N'HAND-MSC', N'The client will be handed originals of all guarantees (in the client’s name and referring to the property), operating instructions etc. and will be shown any operations required to installed equipment. Contractor is to prepare a handover pack containing all information including all contact numbers and addresses of contactors/sub-contractors/suppliers who have worked on the project.', N'item', 1.0000, 300.0000, 300.0000, N'', 175),
    (N'mr-cw-176', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'MEC-SOL', N'new solar panels (PV) installed on both new & existing roof to rear of property. System - to specialist design - shall include battery bank, inverter etc. Tender contractors to confirm overall size roof panels will take up, total weight and also confirm the output achieved. NB: system to form part of an holistic hot water & heating strategy designed by a specialist (i.e.: incorporating items 1, 2, 3 & 5 on this sheet).', N'item', 1.0000, 10000.0000, 10000.0000, N'Omit item V09', 176),
    (N'mr-cw-177', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 1, N'CARP-CUT', N'Additional roof strengthening associated with installation of new solar panels', N'item', 1.0000, 2500.0000, 2500.0000, N'PC SUM', 177),
    (N'mr-cw-178', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'MEC-HTS', N'Tendering contractor to price for new air source heat pump located in newly formed external plant area. System to be designed by specialist. also refer to item 3 regarding existing heating system NB: system to form part of an holistic hot water & heating strategy designed by a specialist (i.e.: incorporating items 1, 2, 3 & 5 on this sheet).', N'item', 1.0000, 20000.0000, 20000.0000, N'Omit item V09', 178),
    (N'mr-cw-179', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'MEC-AC', N'Tendering contractor to price for air conditioning / heating & cooling system to all rooms within the house (apart from bathrooms, ensuites & WC). System to be designed by specialist NB: system to form part of an holistic hot water & heating strategy designed by a specialist (i.e.: incorporating items 1, 2, 3 & 5 on this sheet).', N'nr', 9.0000, 3000.0000, 27000.0000, N'Omit item V09', 179),
    (N'mr-cw-180', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 2, N'MEC-HTS', N'SEC 6 - tendering contractor to include here for any items works to be omitted from their priced schedule of works which are associated with inclusion of the above (ie:6.11.5, 6.11.7 if this conflicts with the holistic heating design). MC to ensure any omissions in this section do not double up with section 5 below.', N'item', 1.0000, -20950.0000, -20950.0000, N'Omit item 6.11.5, 6.11.7', 180),
    (N'mr-cw-181', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'EXTW-FEN', N'Tendering contractor to price for new timber fencing around new external plant area. New timber fencing to match existing (feater edged close boarded timber).', N'm', 50.0000, 110.0000, 5500.0000, N'', 181),
    (N'mr-cw-182', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'MEC-PLM', N'Tendering contractor to price for inclusion of a new electric hot water cylinder. System to be designed by specialist NB: system to form part of an holistic hot water & heating strategy designed by a specialist (i.e.: incorporating items 1, 2, 3 & 5 on this sheet).', N'nr', 1.0000, 2455.0000, 2455.0000, N'Omit item V09', 182),
    (N'mr-cw-183', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'SUP-DOR', N'Tendering contractor to allow a PS sum of £425 per internal door (all doors) for the supply only of a complete door set (including; internal timber door, ironmongery, door frame, architraves). refer to drawing schedule 12978-12. also refer to link below for guidance on the type of door in question. NB: exact door to be confirmed by client. https://antbs.co.uk/shop/diy-and-home-improvement/internal-doors-and-accessories/verte-home-j-6-hawana-oak/', N'nr', 17.0000, 425.0000, 7225.0000, N'Omit item V18', 183),
    (N'mr-cw-184', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'SUP-DOR', N'OHP on above', N'nr', 17.0000, 42.5000, 722.5000, N'Omit item V18', 184),
    (N'mr-cw-185', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'CARP-DOR', N'Installation cost associated with fitting the above doors', N'nr', 17.0000, 175.0000, 2975.0000, N'', 185),
    (N'mr-cw-186', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 2, N'CARP-DOR', N'SEC 3 - tendering contractor to include here for any items works to be omitted from their priced schedule of works which are associated with inclusion of the above.(ie: doors, architraves, linings, OHP, installation etc)', N'item', 1.0000, -10415.0000, -10415.0000, N'Omit item', 186),
    (N'mr-cw-187', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 2, N'SUP-IRO', N'SEC 14 - omit ironmongery Provisional Sum section 14.08', N'item', 1.0000, -2718.0000, -2718.0000, N'Omit item', 187),
    (N'mr-cw-188', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'WDR-TIM', N'Tendering contractor to allow a PS sum of £3000 the supply & installation (by approved installer) of a complete external front door set. refer to link below for guidance on the type of door in question. NB: exact door to be confirmed by client.', N'item', 1.0000, 7488.0000, 7488.0000, N'Omit item V19', 188),
    (N'mr-cw-189', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'WDR-TIM', N'OHP on above', N'item', 1.0000, 300.0000, 300.0000, N'Omit item V19', 189),
    (N'mr-cw-190', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 2, N'WDR-TIM', N'SEC 3 - tendering contractor to include here for any items works to be omitted from their priced schedule of works which are associated with inclusion of the above.(ie: doors, side lights, OHP, installation etc)', N'item', 1.0000, -2075.0000, -2075.0000, N'Omit item - Section 3.4', 190),
    (N'mr-cw-191', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'SUP-KIT', N'Supply and installation of new Quooker Flex PRO3', N'nr', 1.0000, 1550.0000, 1550.0000, N'', 191),
    (N'mr-cw-192', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'SPEC-GAZ', N'tendering contractor to price for new powder coated, aluminium ''box-section'' framed external canopy to rear of house (approx. dimensions = 14m x 3m), sides and front to be open, roof to be glass with remote controlled underside blinds tendering contractors to approach their supply chain and for competitive pricing. Link below to the contemporary style on which contractors are to base their cost; - https://www.nationwideltd.co.uk/lp/new/verandas.php', N'item', 1.0000, 15000.0000, 15000.0000, N'Omit item V20', 192),
    (N'mr-cw-193', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'SPEC-GAZ', N'Foundations and below deck support structure associated with incorporating new canopy onto the elevated deck.', N'item', 1.0000, 2500.0000, 2500.0000, N'Omit item V20', 193),
    (N'mr-cw-194', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'DEC-STD', N'tendering contractor to price for new dulux diamond to all new walls & ceilings. Refer to ''omit'' below for schedule of work references which this affects', N'm2', 752.0000, 20.0000, 15040.0000, N'', 194),
    (N'mr-cw-195', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 2, N'DEC-STD', N'schedule of work references 7.3.1: wall paint type 1 (WP1) & 7.4.1: ceiling paint type 1 (CP1) to areas shown on finishes drawing (12978-09)', N'item', 1.0000, -12572.0000, -12572.0000, N'Omit item - Section 7.3.1 & 7.4.1', 195),
    (N'mr-cw-196', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'ELE-AV', N'new cat 8 cabling to all AV & network locations etc shown on drawing no 12978-10', N'item', 1.0000, 1228.0000, 1228.0000, N'Omit item V10', 196),
    (N'mr-cw-197', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 2, N'ELE-AV', N'cat 6 cabling to all AV & network locations etc shown on drawing no 12978-10', N'item', 1.0000, -750.0000, -750.0000, N'', 197),
    (N'mr-cw-198', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 1, N'EXTW-BBQ', N'tendering contractors to allow a PS sum for new masonry barbecue located on the elevated rear decking (final location to be agreed with client)', N'item', 1.0000, 1000.0000, 1000.0000, N'PC SUM', 198),
    (N'mr-cw-199', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'EXTW-BBQ', N'OHP on above', N'item', 1.0000, 100.0000, 100.0000, N'', 199),
    (N'mr-cw-200', @ProjectId, 0, N'14', N'Addendum to Tender', N'', N'', 0, N'EXTW-BBQ', N'foundations and below deck support structure associated with incorporating new masonry BBQ on to the elevated deck.', N'item', 1.0000, 400.0000, 400.0000, N'', 200),
    (N'mr-ps-01', @ProjectId, 1, N'14.01', N'Provisional Sums', N'', N'', 1, N'STR-STL', N'(2.6.2) steelwork', N'PS', 1.0000, 25500.0000, 25500.0000, N'Omit item V04', 1),
    (N'mr-ps-02', @ProjectId, 1, N'14.02', N'Provisional Sums', N'', N'', 1, N'CARP-JNR', N'(3.6.7) storage unit in dining.', N'PS', 1.0000, 2500.0000, 2500.0000, N'Omit item V31', 2),
    (N'mr-ps-03', @ProjectId, 1, N'14.03', N'Provisional Sums', N'', N'', 1, N'CARP-JNR', N'(3.6.9) storage unit in lounge (main).', N'PS', 1.0000, 2500.0000, 2500.0000, N'Omit item V31', 3),
    (N'mr-ps-04', @ProjectId, 1, N'14.04', N'Provisional Sums', N'', N'', 1, N'CARP-JNR', N'(3.6.10) gaming unit in lounge (PN).', N'PS', 1.0000, 2500.0000, 2500.0000, N'Omit item V31', 4),
    (N'mr-ps-05', @ProjectId, 1, N'14.05', N'Provisional Sums', N'', N'', 1, N'CARP-WRD', N'(3.6.11) dressing / wardrobe unit in bedroom (PN).', N'PS', 1.0000, 1350.0000, 1350.0000, N'Omit item V31', 5),
    (N'mr-ps-06', @ProjectId, 1, N'14.06', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'(3.7.2) new kitchen', N'PS', 1.0000, 31000.0000, 31000.0000, N'Omit item V17', 6),
    (N'mr-ps-07', @ProjectId, 1, N'14.07', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'(3.7.3) new utility', N'PS', 1.0000, 6000.0000, 6000.0000, N'Omit item V17', 7),
    (N'mr-ps-08', @ProjectId, 1, N'14.08', N'Provisional Sums', N'', N'', 1, N'SUP-IRO', N'(4.1.1) ironmongery (£60 per internal door)', N'PS', 1.0000, 2718.0000, 2718.0000, N'Omit item 15.6', 8),
    (N'mr-ps-09', @ProjectId, 1, N'14.09', N'Provisional Sums', N'', N'', 1, N'MEC-AC', N'(6.2.3) client suggested air-conditioning system', N'PS', 1.0000, 13500.0000, 13500.0000, N'Omit item V07', 9),
    (N'mr-ps-10', @ProjectId, 1, N'14.10', N'Provisional Sums', N'', N'', 1, N'ELE-SPE', N'(6.3.4) smart home & environmental controls system', N'PS', 1.0000, 13500.0000, 13500.0000, N'Omit item V11', 10),
    (N'mr-ps-11', @ProjectId, 1, N'14.11', N'Provisional Sums', N'', N'', 1, N'DEC-FIR', N'(6.11.6) supply & install replacement gas fire', N'PS', 1.0000, 1500.0000, 1500.0000, N'', 11),
    (N'mr-ps-12', @ProjectId, 1, N'14.12', N'Provisional Sums', N'', N'', 1, N'SPEC-SPA', N'(9.6.1) hot-tub hoist', N'PS', 1.0000, 8100.0000, 8100.0000, N'', 12),
    (N'mr-ps-13', @ProjectId, 1, N'14.13', N'Provisional Sums', N'', N'', 1, N'EXTW-SHD', N'(9.6.2) shed', N'PS', 1.0000, 11750.0000, 11750.0000, N'', 13),
    (N'mr-ps-14', @ProjectId, 1, N'14.14', N'Provisional Sums', N'', N'', 1, N'EXTW-LND', N'(9.7.1) new shrub planting', N'PS', 1.0000, 1600.0000, 1600.0000, N'', 14),
    (N'mr-ps-15', @ProjectId, 1, N'14.15', N'Provisional Sums', N'', N'', 1, N'SPEC-SPA', N'(10.3) swim spa re-furb', N'PS', 1.0000, 2700.0000, 2700.0000, N'', 15),
    (N'mr-cont-01', @ProjectId, 2, N'', N'Contingency', N'', N'', 0, N'HAND-MSC', N'Contingency Budget', N'item', 1.0000, 60000.0000, 60000.0000, N'Omit item V15', 1)
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

-- Sanity check: the three seeded blocks should reconcile to the workbook.
SELECT
    SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 632512.00
    SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         -- 126718.00
    SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --  60000.00
    SUM(LineAmount) AS ContractSum                                               -- 819230.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
  AND LineType NOT IN (3, 4);

    PRINT '72 Montagu Road: valuation lines merged.';
    COMMIT TRAN;
END
GO
