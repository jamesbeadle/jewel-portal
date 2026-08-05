-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: 21 Chetwode Road -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : 21 Chetwode Road SW17 7RF
-- ProjectId: resolved at run time by site-name matcher '21chetwoderoad'
--            (XeroSiteName first, then Name; nothing touched if no match).
--
-- Seeds the ORIGINAL contract scope only, taken from the "Silvercrow Chetwode
-- Valuation 07 - April 24 REVISED" workbook (VAL 7, April 24). The workbook has
-- a single Contract Works bill (new build basement with traditional
-- construction above to create flats); its PC-sum / provisional-sum lines sit
-- INLINE in the bill (LineType 1) rather than in a separate PC block, and
-- there is no contingency block:
--
--     Contract works (incl. GBP 99,395.50 inline PC/provisional sums)
--     Contract Sum      GBP 826,141.23
--
-- Variations (V01..V19, net GBP 5,823.89) are NOT seeded here -- they belong
-- in seed-chetwoderoad-variations.sql. Per-valuation claim history (VAL.1..14,
-- retention, certified-to-date) is claim data, not bill structure.
--
-- The workbook has no NRM2 numbering; SectionCode is assigned sequentially
-- (01..18) from the workbook's unnumbered section headings, in workbook order;
-- sub-headings (e.g. "Pitched roof", "Front garden") are folded into their
-- parent section. CostCode maps each line to the Jewel cost-centre master.
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
--
-- Rates shown rounded to 2 dp in the workbook were restored to their exact
-- 4-dp values where Quantity x Rate reproduces the workbook amount (e.g.
-- 23.6250, 65.6250, 61.4250); where the product still differs by sub-penny
-- rounding (e.g. 262.42 x 55.65 = 14,603.673) the workbook AMOUNT is kept as
-- the truth.
--
-- ROUNDING RECONCILIATION: the workbook's row amounts displayed at 2 dp sum to
-- GBP 826,141.27, but its stated Contract Sum (GBP 826,141.23) totals the
-- UNROUNDED products (nine rows are exact x.xx5 products the sheet displays
-- rounded up). To reconcile EXACTLY to the stated figure, four of those x.xx5
-- rows are stored rounded half-DOWN instead (cd-cw-037 Padstones PS1
-- 1,246.87; cd-cw-040 Sacrificial screed 7,653.97; cd-cw-041 Celotex GA4000
-- 6,148.27; cd-cw-042 Perimeter insulation 804.82). Every line remains within
-- half a penny of Quantity x Rate.
--
-- SKIPPED workbook rows (no contract value):
--   * "Bespoke MDF wardrobes ... (provisional sum)" -- 30 Lm at 0.00,
--     marked "Omitted from Tender".
--   * Heading, subtotal, claim-history and retention/certified summary rows.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (cd-cw-NNN). A re-run
-- refreshes every field via MERGE (no WHEN NOT MATCHED BY SOURCE -- other
-- projects' and app-entered rows are never touched). Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '21chetwoderoad'
       OR LOWER(REPLACE(Name, ' ', '')) = '21chetwoderoad'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '21chetwoderoad' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  21 Chetwode Road — no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'cd-cw-001', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-PMG', N'Project Manager', N'weeks', 23.0000, 315.0000, 7245.0000, N'', 1),
    (N'cd-cw-002', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site Manager', N'weeks', 45.0000, 1575.0000, 70875.0000, N'', 2),
    (N'cd-cw-003', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-LAB', N'Labourer', N'weeks', 45.0000, 315.0000, 14175.0000, N'', 3),
    (N'cd-cw-004', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Skips (labour included elsewhere, not including soil removal)', N'item', 1.0000, 9450.0000, 9450.0000, N'', 4),
    (N'cd-cw-005', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection of all surfaces throughout the build', N'Item', 1.0000, 1575.0000, 1575.0000, N'', 5),
    (N'cd-cw-006', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-TMP', N'Small plant and tools, propping, temp supplies', N'weeks', 45.0000, 52.5000, 2362.5000, N'', 6),
    (N'cd-cw-007', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 1, N'SCAFF-STD', N'Scaffolding to include temporary roof (P. Sum pending design)', N'Item', 1.0000, 17800.0000, 17800.0000, N'Omit item V07', 7),
    (N'cd-cw-008', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SET', N'Allow for alterations to existing supplies for water and electric for temporary set up', N'Item', 1.0000, 525.0000, 525.0000, N'', 8),
    (N'cd-cw-009', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SET', N'Site set up to include hot running water and CCTV', N'Item', 1.0000, 472.5000, 472.5000, N'', 9),
    (N'cd-cw-010', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Welfare facilities to meet HSE requirements', N'weeks', 45.0000, 78.7500, 3543.7500, N'', 10),
    (N'cd-cw-011', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSO', N'Health and safety, to meet HSE requirements', N'weeks', 45.0000, 52.5000, 2362.5000, N'', 11),
    (N'cd-cw-012', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Builders clean', N'Item', 1.0000, 2100.0000, 2100.0000, N'', 12),
    (N'cd-cw-013', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'MASON-BRK', N'Facing brickwork, half lap stretcher bond, pointed AWP - bricks supplied by client, to include forming of window openings and all sundry items, cavity ties, closures, DPC, weep vents etc', N'm2', 360.0000, 94.5000, 34020.0000, N'Omit item V18', 13),
    (N'cd-cw-014', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'MASON-BRK', N'External Blockwork Rendered ( render measured elsewhere)', N'm2', 24.0000, 50.4000, 1209.6000, N'Omit item V18', 14),
    (N'cd-cw-015', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'INT-INW', N'Cavity wall insulation to building regs (PIR insulation)', N'm2', 384.0000, 23.6250, 9072.0000, N'Omit item V18', 15),
    (N'cd-cw-016', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'MASON-BRK', N'Internal blockwork; 100mm', N'm2', 384.0000, 50.4000, 19353.6000, N'Omit item V18', 16),
    (N'cd-cw-017', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'EXT-STC', N'Krend or similar approved through colour render', N'm2', 50.0000, 80.8500, 4042.5000, N'Amended to 50m', 17),
    (N'cd-cw-018', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'MASON-BRK', N'1B thick garden wall - bricks suplied by client', N'm2', 50.0000, 52.5000, 2625.0000, N'', 18),
    (N'cd-cw-019', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'MASON-BRK', N'Blockwork; 140mm ,10N, M4 Mortar', N'm2', 262.4200, 55.6500, 14603.6700, N'Omit item V18', 19),
    (N'cd-cw-020', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'MASON-BRK', N'Blockwork; 100mm ,10N, M4 Mortar', N'm2', 132.9200, 50.4000, 6699.1700, N'Omit item V18', 20),
    (N'cd-cw-021', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 0, N'MASON-BRK', N'Sytex detailing  installed to windows, Supplied by client', N'no.', 17.0000, 126.0000, 2142.0000, N'', 21),
    (N'cd-cw-022', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 1, N'EXT-STC-COP', N'Single weathered concrete coping stone (PC Sum)', N'm', 26.0000, 42.0000, 1092.0000, N'PC SUM', 22),
    (N'cd-cw-023', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 1, N'MASON-BRK', N'Lintels; upto 1500mm,NAYLOR ER2 LINTELS (PC Sum)', N'no.', 30.0000, 47.2500, 1417.5000, N'Omit Item V04', 23),
    (N'cd-cw-024', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 1, N'MASON-BRK', N'Lintels; upto 2500mm,NAYLOR ER2 LINTELS (PC Sum)', N'no', 14.0000, 52.5000, 735.0000, N'Omit Item V04', 24),
    (N'cd-cw-025', @ProjectId, 0, N'02', N'Superstructure', N'', N'', 1, N'MASON-BRK', N'Lintels; over 2500mm,NAYLOR ER2 LINTELS (PC Sum)', N'no', 1.0000, 63.0000, 63.0000, N'Omit Item V04', 25),
    (N'cd-cw-026', @ProjectId, 0, N'03', N'Upper floor construction', N'', N'', 0, N'CARP-1FX', N'Timber joists, 200mm x 50mm including all sundry item, installed at 300 centres', N'm2', 44.0000, 45.1500, 1986.6000, N'Omit item V08', 26),
    (N'cd-cw-027', @ProjectId, 0, N'03', N'Upper floor construction', N'', N'', 0, N'CARP-1FX', N'Timber joists, 200mm x 50mm including all sundry items , installed at 400 centres', N'm2', 89.0000, 45.1500, 4018.3500, N'Omit item V16', 27),
    (N'cd-cw-028', @ProjectId, 0, N'03', N'Upper floor construction', N'', N'', 0, N'CARP-1FX', N'Timber joists, 200mm x 75mm including all sundry items , installed at 600 centres', N'm2', 100.0000, 47.2500, 4725.0000, N'Omit item V08', 28),
    (N'cd-cw-029', @ProjectId, 0, N'03', N'Upper floor construction', N'', N'', 0, N'CARP-1FX', N'E.O for doubled up joists around openings', N'Lm', 25.0000, 15.7500, 393.7500, N'', 29),
    (N'cd-cw-030', @ProjectId, 0, N'03', N'Upper floor construction', N'', N'', 0, N'INT-INF', N'Insulation; 100mm rockwool', N'm2', 233.0000, 11.5500, 2691.1500, N'', 30),
    (N'cd-cw-031', @ProjectId, 0, N'03', N'Upper floor construction', N'', N'', 0, N'CARP-1FX', N'18mm structural plywood, glued and screwed', N'm2', 200.0000, 17.8500, 3570.0000, N'', 31),
    (N'cd-cw-032', @ProjectId, 0, N'03', N'Upper floor construction', N'', N'', 0, N'CARP-1FX', N'22mm structural plywood, glued and screwed', N'm2', 100.0000, 22.0500, 2205.0000, N'Omit item V08', 32),
    (N'cd-cw-033', @ProjectId, 0, N'04', N'GF garden structure (TBC)', N'', N'', 0, N'SUB-CON', N'Concrete lid to front entrance area, assume reinforced suspended concrete slab, 225mm thick', N'm2', 4.0000, 850.5000, 3402.0000, N'', 33),
    (N'cd-cw-034', @ProjectId, 0, N'05', N'Staircase - softwood', N'', N'', 1, N'STAIR-TIM', N'Supply of staircase; MDF, prebuilt off site (PC SUM)', N'no.', 6.0000, 1260.0000, 7560.0000, N'PC SUM', 34),
    (N'cd-cw-035', @ProjectId, 0, N'05', N'Staircase - softwood', N'', N'', 1, N'STAIR-TIM', N'Installation of staircase and timber handrails, base rails and spindles (to be painted) (PC SUM)', N'no.', 6.0000, 945.0000, 5670.0000, N'PC SUM', 35),
    (N'cd-cw-036', @ProjectId, 0, N'06', N'Structural work', N'', N'', 1, N'STR-STL', N'Allow for steels, connections and install - PC SUM', N'Tonne', 8.0000, 3990.0000, 31920.0000, N'Omit Item V01', 36),
    (N'cd-cw-037', @ProjectId, 0, N'07', N'Padstones', N'', N'', 0, N'SUB-CON', N'Padstones to engineers spec, PS1', N'no.', 19.0000, 65.6250, 1246.8700, N'', 37),
    (N'cd-cw-038', @ProjectId, 0, N'07', N'Padstones', N'', N'', 0, N'SUB-CON', N'Padstones to engineers spec, PS2', N'no.', 7.0000, 68.2500, 477.7500, N'', 38),
    (N'cd-cw-039', @ProjectId, 0, N'07', N'Padstones', N'', N'', 0, N'SUB-CON', N'Padstones to engineers spec, PS3', N'no.', 2.0000, 75.0750, 150.1500, N'', 39),
    (N'cd-cw-040', @ProjectId, 0, N'08', N'Slab works', N'', N'', 0, N'FLR-SCR', N'Sacrifical Screed, 75mm thick, leaving 125 x 75mm void for perimeter channel for waterproofing', N'm2', 239.0000, 32.0250, 7653.9700, N'', 40),
    (N'cd-cw-041', @ProjectId, 0, N'08', N'Slab works', N'', N'', 0, N'INT-INF', N'Insulation; Celotex GA4000 -140mm, allow for 1500 gauge membrane below', N'm2', 239.0000, 25.7250, 6148.2700, N'', 41),
    (N'cd-cw-042', @ProjectId, 0, N'08', N'Slab works', N'', N'', 0, N'INT-INF', N'Perimeter insulation; 25mm celotex', N'm', 73.0000, 11.0250, 804.8200, N'', 42),
    (N'cd-cw-043', @ProjectId, 0, N'08', N'Slab works', N'', N'', 0, N'FLR-SCR', N'Screed; 50mm liquid screed, allow for decoupling membrane between insulation and screed to contractor''s spec', N'm2', 239.0000, 38.8500, 9285.1500, N'', 43),
    (N'cd-cw-044', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'CARP-CUT', N'Rafters; 200mm x 75mm including all sundry items including forming of hips and valleys', N'm2', 122.0000, 54.6000, 6661.2000, N'', 44),
    (N'cd-cw-045', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Breather membrane', N'm2', 122.0000, 3.6750, 448.3500, N'', 45),
    (N'cd-cw-046', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Insulation; between rafters 150mm celotex', N'm2', 122.0000, 27.3000, 3330.6000, N'', 46),
    (N'cd-cw-047', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Insulation;below rafters', N'm2', 122.0000, 17.8500, 2177.7000, N'', 47),
    (N'cd-cw-048', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Battens; 25mm x 38mm', N'm2', 122.0000, 14.1750, 1729.3500, N'', 48),
    (N'cd-cw-049', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Textured eternit fibre slates', N'm2', 122.0000, 50.4000, 6148.8000, N'', 49),
    (N'cd-cw-050', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'CARP-CUT', N'Wall plate; 100mm x 50mm', N'm', 50.0000, 15.2250, 761.2500, N'', 50),
    (N'cd-cw-051', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Mechanically fixed ridge tiles between flat roof and pitch and hips', N'm', 20.0000, 30.9750, 619.5000, N'', 51),
    (N'cd-cw-052', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-LED', N'Forming of valley in lead', N'm', 6.0000, 25.2000, 151.2000, N'', 52),
    (N'cd-cw-053', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-LED', N'Apron detail to rear of main roof between flat and mansard', N'm', 15.0000, 25.2000, 378.0000, N'', 53),
    (N'cd-cw-054', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'CARP-CUT', N'Front timber structure including slates, ridge tiles and full construction as detailed above', N'Item', 2.0000, 1837.2200, 3674.4400, N'', 54),
    (N'cd-cw-055', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'200x75 C24 timber including sundry items to complete installation', N'm2', 84.0000, 55.1250, 4630.5000, N'', 55),
    (N'cd-cw-056', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'25 x 38mm battens/allowance for cross ventilation', N'm2', 84.0000, 16.2750, 1367.1000, N'', 56),
    (N'cd-cw-057', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Breather membrane', N'm2', 84.0000, 4.7250, 396.9000, N'', 57),
    (N'cd-cw-058', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Firrings', N'm2', 84.0000, 15.2250, 1278.9000, N'', 58),
    (N'cd-cw-059', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'18mm plywood', N'm2', 84.0000, 17.8500, 1499.4000, N'', 59),
    (N'cd-cw-060', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Vapour barrier', N'm2', 84.0000, 5.2500, 441.0000, N'', 60),
    (N'cd-cw-061', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Insulation to meet building regulations (2 no. layers)', N'm2', 84.0000, 77.7000, 6526.8000, N'', 61),
    (N'cd-cw-062', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Flat roof covering, contractors design, to include 10 year guarentee and IBG', N'm2', 84.0000, 118.1250, 9922.5000, N'', 62),
    (N'cd-cw-063', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Forming of upstands and roof covering to complete installation', N'no.', 6.0000, 199.5000, 1197.0000, N'', 63),
    (N'cd-cw-064', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Allow for penetrations through the roofs for cables or boiler flues to include collars and sundry items as required', N'no.', 4.0000, 49.3500, 197.4000, N'', 64),
    (N'cd-cw-065', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FSU', N'White painted timber soffits', N'm', 40.0000, 29.4000, 1176.0000, N'', 65),
    (N'cd-cw-066', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-FSU', N'White painted timber fascias', N'm', 40.0000, 29.4000, 1176.0000, N'', 66),
    (N'cd-cw-067', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-GRU', N'Cast iron effect gutters (assumes half round deepflow inc fittings)', N'm', 40.0000, 47.2500, 1890.0000, N'', 67),
    (N'cd-cw-068', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-GRU', N'Cast iron effect downpipes (assumes half round deepflow inc fittings)', N'm', 36.0000, 52.5000, 1890.0000, N'', 68),
    (N'cd-cw-069', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'ROOF-LED', N'Lead flashing', N'm', 15.0000, 75.6000, 1134.0000, N'', 69),
    (N'cd-cw-070', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'WDR-SPG', N'Installation only for roof windows - Velux, supplied with fixing kit', N'no.', 10.0000, 126.0000, 1260.0000, N'', 70),
    (N'cd-cw-071', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'WDR-SPG', N'Installation only for AOV in pitched roof', N'no.', 1.0000, 504.0000, 504.0000, N'', 71),
    (N'cd-cw-072', @ProjectId, 0, N'09', N'Roofing', N'', N'', 0, N'WDR-SPG', N'Installation only for flat roof rooflights', N'no.', 2.0000, 126.0000, 252.0000, N'', 72),
    (N'cd-cw-073', @ProjectId, 0, N'10', N'Windows & Doors', N'', N'', 0, N'WDR-TIM', N'Timber sash installation', N'no.', 25.0000, 168.0000, 4200.0000, N'Omit item V03', 73),
    (N'cd-cw-074', @ProjectId, 0, N'10', N'Windows & Doors', N'', N'', 0, N'WDR-ALU', N'Aluminium sliding doors to rear', N'no.', 8.0000, 367.5000, 2940.0000, N'Omit item V03', 74),
    (N'cd-cw-075', @ProjectId, 0, N'10', N'Windows & Doors', N'', N'', 0, N'WDR-TIM', N'Front entrance door (assumes locks and handles laready fitted)', N'no.', 1.0000, 420.0000, 420.0000, N'Omit item V03', 75),
    (N'cd-cw-076', @ProjectId, 0, N'10', N'Windows & Doors', N'', N'', 0, N'WDR-ALU', N'External patio doors doubles (aluminimum)', N'no.', 4.0000, 420.0000, 1680.0000, N'Omit item V03', 76),
    (N'cd-cw-077', @ProjectId, 0, N'10', N'Windows & Doors', N'', N'', 0, N'WDR-ALU', N'External patio doors singles (aluminimum)', N'no.', 4.0000, 367.5000, 1470.0000, N'Omit item V03', 77),
    (N'cd-cw-078', @ProjectId, 0, N'10', N'Windows & Doors', N'', N'', 0, N'WDR-ALU', N'External doors to basement front (aluminimum)', N'no.', 2.0000, 420.0000, 840.0000, N'Omit item V03', 78),
    (N'cd-cw-079', @ProjectId, 0, N'10', N'Windows & Doors', N'', N'', 1, N'STR-GRL', N'Glass balustrades, obscured, 1.8m high. To include supply install and Building control approved structural calculations (PC Sum)', N'Lm', 21.0000, 420.0000, 8820.0000, N'Omit item V03', 79),
    (N'cd-cw-080', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Metal stud and track, up to 3m high', N'm2', 250.0000, 24.1500, 6037.5000, N'', 80),
    (N'cd-cw-081', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-INW', N'Insulation; 70mm rockwool (assumes mineral wool)', N'm2', 250.0000, 7.8750, 1968.7500, N'', 81),
    (N'cd-cw-082', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLB', N'15mm standard plasterboard to each side', N'm2', 500.0000, 13.6500, 6825.0000, N'', 82),
    (N'cd-cw-083', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLS', N'3mm skim finish', N'm2', 500.0000, 12.6000, 6300.0000, N'', 83),
    (N'cd-cw-084', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Metal stud and track, up to 3m high', N'm2', 150.0000, 24.1500, 3622.5000, N'', 84),
    (N'cd-cw-085', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-INW', N'Insulation; 70mm rockwool (assumes mineral wool)', N'm2', 150.0000, 7.8750, 1181.2500, N'', 85),
    (N'cd-cw-086', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLB', N'15mm MR plasterboard to one side 15mm standard plasterboard to one side', N'm2', 300.0000, 15.7500, 4725.0000, N'', 86),
    (N'cd-cw-087', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Allow for false walls for cistern and shower fittings in each bathroom', N'item', 13.0000, 16.8000, 218.4000, N'', 87),
    (N'cd-cw-088', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Allow for recess in bathrooms walls within boxings', N'item', 13.0000, 252.0000, 3276.0000, N'', 88),
    (N'cd-cw-089', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLS', N'3mm skim finish', N'm2', 300.0000, 12.6000, 3780.0000, N'', 89),
    (N'cd-cw-090', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Metal stud and track, 2 x 70mm stud off set, total wall width 200mm, deflection head and acoustic strip to head and base track', N'm2', 50.0000, 45.1500, 2257.5000, N'', 90),
    (N'cd-cw-091', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Standard deflection head', N'item', 1.0000, 12.6000, 12.6000, N'', 91),
    (N'cd-cw-092', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Acoustic strip', N'item', 1.0000, 9.4500, 9.4500, N'', 92),
    (N'cd-cw-093', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-INW', N'Insulation; 70mm acoustic insulation snaked between studs', N'm2', 50.0000, 8.4000, 420.0000, N'', 93),
    (N'cd-cw-094', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLB', N'2 x 15mm DB plasterboard to each side', N'm2', 100.0000, 25.2000, 2520.0000, N'', 94),
    (N'cd-cw-095', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'FIRE-STP', N'Allowance for fire batt to head of partitions to be installed as per construction details with fire sealant between joists', N'Lm', 20.0000, 28.3500, 567.0000, N'', 95),
    (N'cd-cw-096', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLS', N'3mm skim finish', N'm2', 100.0000, 12.6000, 1260.0000, N'', 96),
    (N'cd-cw-097', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Gypline with 2 x DB plasterboard', N'm2', 524.8400, 51.4500, 27003.0200, N'', 97),
    (N'cd-cw-098', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-INW', N'Acoustic insulation in 30mm void', N'm2', 524.8400, 9.4500, 4959.7400, N'', 98),
    (N'cd-cw-099', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLS', N'3mm skim finish', N'm2', 524.8400, 12.6000, 6612.9800, N'', 99),
    (N'cd-cw-100', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Gypline to external walls as required, acoustic insulation and 2 x 15mm DB plasterboard', N'm2', 384.0000, 59.8500, 22982.4000, N'', 100),
    (N'cd-cw-101', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MGW', N'Gypline independant to external walls in the basement acoustic insulation and 2 x 15mm DB plasterboard', N'm2', 211.0000, 60.9000, 12849.9000, N'', 101),
    (N'cd-cw-102', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLS', N'3mm skim finish to all walls', N'm2', 595.0000, 12.6000, 7497.0000, N'', 102),
    (N'cd-cw-103', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MFC', N'2 x 15mm DB plasterboard suspended MF ceiling on acoustic hangers', N'm2', 233.0000, 55.6500, 12966.4500, N'', 103),
    (N'cd-cw-104', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-INC', N'Insulation; 100mm acoustic RWA45 insulation', N'm2', 233.0000, 11.5500, 2691.1500, N'', 104),
    (N'cd-cw-105', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLS', N'3mm skim finish', N'm2', 233.0000, 12.6000, 2935.8000, N'', 105),
    (N'cd-cw-106', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLB', N'12.5 standard wallboard', N'm2', 206.0000, 10.5000, 2163.0000, N'', 106),
    (N'cd-cw-107', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-MFC', N'Allowance for dropped ceiling to create void for cables etc, top hat to rafters (excluding boarding)', N'm2', 206.0000, 22.0500, 4542.3000, N'', 107),
    (N'cd-cw-108', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'INT-PLS', N'3mm skim finish', N'm2', 206.0000, 12.6000, 2595.6000, N'', 108),
    (N'cd-cw-109', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'CARP-2FX', N'Allow for construction of fire rated electrical intake cupboard. Include pair of flush fire doors, fire rated ironmongery (Client supplied Doors and ironmongary). Assumed full height and 1.2m wide', N'item', 1.0000, 1155.0000, 1155.0000, N'', 109),
    (N'cd-cw-110', @ProjectId, 0, N'11', N'Internal Walls and Partitions', N'', N'', 0, N'CARP-2FX', N'White painted plywood backing to rear of cupboard. To be painted prior to installation of any fixtures and fittings', N'item', 1.0000, 84.0000, 84.0000, N'', 110),
    (N'cd-cw-111', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-STD', N'Flat distribution board', N'no.', 8.0000, 341.2500, 2730.0000, N'', 111),
    (N'cd-cw-112', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-STD', N'First fix cabling to lights, sockets and switches', N'no.', 750.0000, 33.6000, 25200.0000, N'', 112),
    (N'cd-cw-113', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-STD', N'Second fix of client supplied fittings', N'no.', 750.0000, 31.5000, 23625.0000, N'', 113),
    (N'cd-cw-114', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-FIR', N'Installation of AOV, control panel supplied by client', N'item', 1.0000, 262.5000, 262.5000, N'', 114),
    (N'cd-cw-115', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-FIR', N'Smoke detection/fire alarm panel to building regulations to allow for interlinked system between flats', N'item', 1.0000, 2520.0000, 2520.0000, N'', 115),
    (N'cd-cw-116', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-STD', N'Fibre run from communal to ONT within each flat', N'no.', 8.0000, 210.0000, 1680.0000, N'', 116),
    (N'cd-cw-117', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-STD', N'Ryefield board and associated fittings and fixtures to complete installation to communal cupboard', N'item', 1.0000, 1785.0000, 1785.0000, N'', 117),
    (N'cd-cw-118', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-ENT', N'Aperta colour intercom system, video, audio and remote door release system', N'item', 1.0000, 3412.5000, 3412.5000, N'', 118),
    (N'cd-cw-119', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-STD', N'Builders work in connection', N'item', 1.0000, 2100.0000, 2100.0000, N'', 119),
    (N'cd-cw-120', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'ELE-STD', N'Testing and commissioning', N'item', 9.0000, 105.0000, 945.0000, N'', 120),
    (N'cd-cw-121', @ProjectId, 0, N'12', N'Electrics', N'', N'', 0, N'MEC-VNT', N'Allow for ventilation from bathrooms to external walls/ceilings and kitchens to external walls/ceilings as necessary. Fans and external grills provided by client. (Air Testing Required - To be rigid ducting and minimal flexi)', N'item', 21.0000, 288.7500, 6063.7500, N'', 121),
    (N'cd-cw-122', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 1, N'MEC-BLR', N'Vaillant boiler, sized by plumber per flat (Ecotec or equivlent) with all parts required and installed to provide a manufacturers 10 year guarantee PC Sum', N'no.', 8.0000, 2514.7500, 20118.0000, N'Omit item V02', 122),
    (N'cd-cw-123', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 1, N'MEC-PLM', N'Gas supply to boiler per flat - PC SUM', N'no.', 8.0000, 525.0000, 4200.0000, N'Omit item V02', 123),
    (N'cd-cw-124', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-PLM', N'New water supply in 25mm MDPE Pipe from boundary to incoming cupboard, supply to be installed with stopcock as TW new service requirements', N'no.', 8.0000, 157.5000, 1260.0000, N'Omit item V05', 124),
    (N'cd-cw-125', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-PLM', N'Hot water supply to all sanitaryware and kitchen', N'no.', 8.0000, 315.0000, 2520.0000, N'', 125),
    (N'cd-cw-126', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-PLM', N'Cold water supply to all sanitaryware and kitchen', N'no.', 8.0000, 315.0000, 2520.0000, N'', 126),
    (N'cd-cw-127', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-UFH', N'Include wet UFH track system in basement', N'm2', 208.0000, 65.1000, 13540.8000, N'', 127),
    (N'cd-cw-128', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-UFH', N'Include wet UFH XPS wet system above plywood on upper levels', N'm2', 300.0000, 87.1500, 26145.0000, N'', 128),
    (N'cd-cw-129', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-DRN', N'Waste plumbing and drainage including pipework and sundry items to completion', N'no.', 8.0000, 903.0000, 7224.0000, N'', 129),
    (N'cd-cw-130', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-PLM', N'Builders work in connection', N'item', 1.0000, 2100.0000, 2100.0000, N'', 130),
    (N'cd-cw-131', @ProjectId, 0, N'13', N'Mechanical', N'', N'', 0, N'MEC-PLM', N'Testing and commissioning', N'no.', 8.0000, 105.0000, 840.0000, N'', 131),
    (N'cd-cw-132', @ProjectId, 0, N'14', N'Joinery', N'', N'', 0, N'CARP-DOR', N'FD30 internal door installation, frame and smoke seals to be included. Ironmongery supplied by client', N'no.', 40.0000, 210.0000, 8400.0000, N'', 132),
    (N'cd-cw-133', @ProjectId, 0, N'14', N'Joinery', N'', N'', 0, N'CARP-DOR', N'FD30s flat front entrance door installation, frame and fire strips to be included. Ironmongery supplied by client including self closer', N'no.', 8.0000, 231.0000, 1848.0000, N'', 133),
    (N'cd-cw-134', @ProjectId, 0, N'14', N'Joinery', N'', N'', 0, N'CARP-2FX', N'Install client supplied MDF primed skirting', N'm', 644.0000, 10.5000, 6762.0000, N'', 134),
    (N'cd-cw-135', @ProjectId, 0, N'14', N'Joinery', N'', N'', 0, N'CARP-2FX', N'Install client supplied MDF primed architraves', N'm', 480.0000, 8.4000, 4032.0000, N'', 135),
    (N'cd-cw-136', @ProjectId, 0, N'14', N'Joinery', N'', N'', 0, N'CARP-2FX', N'Install client supplied MDF primed windowboards', N'm', 25.0000, 15.7500, 393.7500, N'', 136),
    (N'cd-cw-137', @ProjectId, 0, N'15', N'Kitchens & Bathrooms', N'', N'', 0, N'MEC-PLM', N'Plumber 2nd fix attendance ONLY to help with client Install only kitchen, to include patressing and any other works required to install', N'no.', 8.0000, 241.5000, 1932.0000, N'', 137),
    (N'cd-cw-138', @ProjectId, 0, N'15', N'Kitchens & Bathrooms', N'', N'', 0, N'SUP-SAN', N'Install only all fixtures and fittings to bathrooms', N'no.', 13.0000, 1050.0000, 13650.0000, N'', 138),
    (N'cd-cw-139', @ProjectId, 0, N'15', N'Kitchens & Bathrooms', N'', N'', 0, N'TIL-STD', N'Install tiles to walls to baths and showers (rectangle approx 70 x 250 tiles)', N'm2', 120.0000, 61.4250, 7371.0000, N'', 139),
    (N'cd-cw-140', @ProjectId, 0, N'15', N'Kitchens & Bathrooms', N'', N'', 0, N'TIL-STD', N'Tiles to recesses (Trims and no mitre of tiles)', N'item', 13.0000, 98.1750, 1276.2800, N'', 140),
    (N'cd-cw-141', @ProjectId, 0, N'16', N'Decoration', N'', N'', 0, N'DEC-STD', N'2 mist coat of matt emulsion', N'm2', 1958.8400, 7.8750, 15425.8700, N'', 141),
    (N'cd-cw-142', @ProjectId, 0, N'16', N'Decoration', N'', N'', 0, N'DEC-STD', N'Decoration to skirting architrave and window boards', N'Lm', 1149.0000, 8.9250, 10254.8300, N'', 142),
    (N'cd-cw-143', @ProjectId, 0, N'16', N'Decoration', N'', N'', 0, N'DEC-STD', N'Final coat colour emulsion including colour match (To completion)', N'm2', 1958.8400, 7.8750, 15425.8700, N'', 143),
    (N'cd-cw-144', @ProjectId, 0, N'16', N'Decoration', N'', N'', 0, N'DEC-STD', N'Decorations to internal pre primed doors', N'no', 48.0000, 52.5000, 2520.0000, N'', 144),
    (N'cd-cw-145', @ProjectId, 0, N'16', N'Decoration', N'', N'', 0, N'DEC-STD', N'Painting stringer, spindles and handrails of staircases', N'no', 8.0000, 315.0000, 2520.0000, N'', 145),
    (N'cd-cw-146', @ProjectId, 0, N'17', N'Floor Finishes', N'', N'', 0, N'FLR-WD', N'Install wooden floor to living rooms 450 x 90 herringbone. Flooring, thresholds and underlay supplied by client (Floating)', N'm2', 239.0000, 52.5000, 12547.5000, N'', 146),
    (N'cd-cw-147', @ProjectId, 0, N'17', N'Floor Finishes', N'', N'', 0, N'FLR-WD', N'Install wooden floor to hallways 180mm straight board. Flooring, thresholds and underlay supplied by client', N'm2', 33.0000, 31.5000, 1039.5000, N'', 147),
    (N'cd-cw-148', @ProjectId, 0, N'17', N'Floor Finishes', N'', N'', 0, N'TIL-STD', N'Install tile floor to bathroom hexagon tiles. Approx 150 x 150mm. Tiles only to be supplied by client  (Client supplied tiles and trims only)', N'm2', 47.0000, 61.4250, 2886.9800, N'', 148),
    (N'cd-cw-149', @ProjectId, 0, N'17', N'Floor Finishes', N'', N'', 0, N'TIL-STD', N'Ditra mat installed under tiles (Supply and install)', N'm2', 47.0000, 24.1500, 1135.0500, N'', 149),
    (N'cd-cw-150', @ProjectId, 0, N'18', N'Externals', N'', N'', 0, N'UTIL-TRN', N'Allow for utility trenches, 750mm deep, 5 meters long and 500mm wide', N'item', 1.0000, 147.0000, 147.0000, N'', 150),
    (N'cd-cw-151', @ProjectId, 0, N'18', N'Externals', N'', N'', 0, N'EXTW-PAV', N'Fit client supplied large format patio', N'm2', 6.0000, 68.2500, 409.5000, N'', 151),
    (N'cd-cw-152', @ProjectId, 0, N'18', N'Externals', N'', N'', 0, N'EXTW-LND', N'Soft landscaping to front garden, ready for install of grass, shrubs, bushes etc. inc weed membrane (clean and level)', N'm2', 10.0000, 14.7000, 147.0000, N'', 152),
    (N'cd-cw-153', @ProjectId, 0, N'18', N'Externals', N'', N'', 0, N'EXTW-PAV', N'Fit client supplied patio', N'm2', 15.0000, 57.7500, 866.2500, N'', 153),
    (N'cd-cw-154', @ProjectId, 0, N'18', N'Externals', N'', N'', 0, N'EXTW-TRF', N'Excavate and prepare subbase for artifical grass, install grass supplied by client', N'm2', 20.0000, 47.2500, 945.0000, N'', 154),
    (N'cd-cw-155', @ProjectId, 0, N'18', N'Externals', N'', N'', 0, N'EXTW-FEN', N'Client supplied fence to garden seperation', N'Lm', 20.0000, 36.7500, 735.0000, N'', 155)
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

    PRINT '21 Chetwode Road: 155 contract valuation lines merged.';

    -- Sanity check: the seeded block should reconcile to the workbook.
    SELECT
        SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 826141.23
        SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --      0.00 (PC sums are inline, LineType 1)
        SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --      0.00 (no contingency block)
        SUM(LineAmount) AS ContractSum                                               -- 826141.23
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
      AND LineType NOT IN (3, 4);

    COMMIT TRAN;
END
GO
