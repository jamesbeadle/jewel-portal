-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: 67 Beresford Road -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : 67 Beresford Road, Sutton, SM2 6ER
-- ProjectId: resolved at run time by site-name matcher '67beresfordroadsutton'
--
-- Seeds the CONTRACT SCOPE only, taken from the "Valuation 9 - Completion"
-- workbook. A single Contract Works block makes up the Contract Sum; the
-- workbook's "Glazing - Provisional allowance" section sits inline in the
-- bill as provisional-sum lines (ElementType 0, LineType 1) rather than in a
-- separate PC block. There is no separate PS block and no Contingency block.
--
--     Contract works (incl. GBP 15,941.00 inline glazing provisional sums)
--     Contract Sum      GBP 264,504.00
--     Net Variations    GBP  21,673.44   (seeded by seed-beresfordroad-variations.sql)
--     Live Build Sum    GBP 286,177.44
--
-- The workbook has no NRM2 section numbering; SectionCode is assigned
-- sequentially (01..18) in workbook order (Ravenswood pattern) and the
-- workbook's own numeric codes (0001..0024) are dropped. CostCode maps each
-- line to the Jewel cost-centre master (seed-cost-centers.sql), consistent
-- with the Ravenswood/Albany seeds.
--
-- Zero-value rows marked "Omitted from tender" are NOT seeded (they carry no
-- contract value):
--     - 203 x 133 x 25 kg steel beam  (Structural steels; Omitted from tender)
--     - Velux sun tunnels  (Glazing - Provisional allowance; Omitted from tender)
--     - Fan isolator switches  (Electrical installation; Omitted from tender)
--
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (br-cw-NNN). A re-run
-- refreshes every field via MERGE. Variation lines for this project are left
-- untouched. Safe to run repeatedly.
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
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'br-cw-001', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site manager', N'week', 28.0000, 395.0000, 11060.0000, N'', 1),
        (N'br-cw-002', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 14.0000, 380.0000, 5320.0000, N'', 2),
        (N'br-cw-003', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 28.0000, 90.0000, 2520.0000, N'', 3),
        (N'br-cw-004', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 4),
        (N'br-cw-005', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 28.0000, 125.0000, 3500.0000, N'', 5),
        (N'br-cw-006', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'SCAFF-STD', N'Scaffolding', N'm²', 55.0000, 42.0000, 2310.0000, N'', 6),
        (N'br-cw-007', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 350.0000, 350.0000, N'', 7),
        (N'br-cw-008', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 350.0000, 350.0000, N'', 8),
        (N'br-cw-009', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 375.0000, 375.0000, N'', 9),
        (N'br-cw-010', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 200.0000, 200.0000, N'', 10),
        (N'br-cw-011', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove kitchen units, worktops & appliances', N'item', 1.0000, 175.0000, 175.0000, N'', 11),
        (N'br-cw-012', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary from first floor bathroom & WC', N'item', 1.0000, 220.0000, 220.0000, N'', 12),
        (N'br-cw-013', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 11.0000, 18.0000, 198.0000, N'', 13),
        (N'br-cw-014', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal walls to form new layout', N'm²', 22.0000, 34.0000, 748.0000, N'Amended', 14),
        (N'br-cw-015', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove plasterboard ceilings to areas required', N'm²', 172.0000, 10.0000, 1720.0000, N'Amended V01', 15),
        (N'br-cw-016', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove windows & external doors', N'nr', 8.0000, 45.0000, 360.0000, N'', 16),
        (N'br-cw-017', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-STS', N'Erect temporary propping to existing construction', N'm', 15.0000, 70.0000, 1050.0000, N'', 17),
        (N'br-cw-018', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls to form new layout', N'm²', 20.0000, 85.0000, 1700.0000, N'', 18),
        (N'br-cw-019', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish chimney breast & breast', N'item', 1.0000, 2250.0000, 2250.0000, N'', 19),
        (N'br-cw-020', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove section of pitched roof covering & construction', N'm²', 24.0000, 24.0000, 576.0000, N'', 20),
        (N'br-cw-021', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'MEC-DRN', N'Grub out redundant drainage runs', N'item', 1.0000, 250.0000, 250.0000, N'', 21),
        (N'br-cw-022', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove glass canopy', N'item', 1.0000, 120.0000, 120.0000, N'', 22),
        (N'br-cw-023', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove existing paving & hardstanding to new areas', N'm²', 24.0000, 10.0000, 240.0000, N'', 23),
        (N'br-cw-024', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove garden shed', N'item', 1.0000, 120.0000, 120.0000, N'', 24),
        (N'br-cw-025', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-ASB', N'Asbestos removal', N'item', 1.0000, 1500.0000, 1500.0000, N'Omit item V28', 25),
        (N'br-cw-026', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Hack off pebble dash', N'm²', 130.0000, 24.0000, 3120.0000, N'', 26),
        (N'br-cw-027', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate to reduce levels & remove spoil', N'm³', 10.0000, 105.0000, 1050.0000, N'', 27),
        (N'br-cw-028', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate foundations 500 x 1000 mm & remove spoil', N'm³', 11.0000, 145.0000, 1595.0000, N'', 28),
        (N'br-cw-029', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate pad foundations & remove spoil', N'm³', 1.5000, 175.0000, 262.5000, N'', 29),
        (N'br-cw-030', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'Concrete in foundations', N'm³', 13.0000, 160.0000, 2080.0000, N'', 30),
        (N'br-cw-031', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in two skins of engineering brickwork & lean mix cavity gill', N'm²', 9.0000, 228.0000, 2052.0000, N'', 31),
        (N'br-cw-032', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Damp proof course', N'm', 21.0000, 16.0000, 336.0000, N'', 32),
        (N'br-cw-033', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'150 mm hardcore blinded with sand', N'm²', 38.0000, 32.0000, 1216.0000, N'', 33),
        (N'br-cw-034', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'150 mm bed of concrete', N'm³', 5.5000, 285.0000, 1567.5000, N'Amended', 34),
        (N'br-cw-035', @ProjectId, 0, N'04', N'External work & drainage', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 3500.0000, 3500.0000, N'', 35),
        (N'br-cw-036', @ProjectId, 0, N'04', N'External work & drainage', N'', N'', 0, N'MEC-DRN', N'Provide drainage from new rainwater pipe, back inlet gulley & connection to existing drainage', N'item', 1.0000, 240.0000, 240.0000, N'', 36),
        (N'br-cw-037', @ProjectId, 0, N'04', N'External work & drainage', N'', N'', 0, N'MEC-DRN', N'Make connection of new drainage to existing runs', N'item', 1.0000, 250.0000, 250.0000, N'', 37),
        (N'br-cw-038', @ProjectId, 0, N'04', N'External work & drainage', N'', N'', 0, N'MEC-DRN', N'Relocate manhole', N'nr', 1.0000, 645.0000, 645.0000, N'', 38),
        (N'br-cw-039', @ProjectId, 0, N'04', N'External work & drainage', N'', N'', 0, N'MEC-DRN', N'Make good damaged areas', N'item', 1.0000, 250.0000, 250.0000, N'', 39),
        (N'br-cw-040', @ProjectId, 0, N'05', N'External walls & lintels', N'', N'', 0, N'INT-INW', N'Cavity wall in two skins of 100 mm blockwork & 75 mm Kingspan insulation to cavity', N'm²', 40.0000, 192.0000, 7680.0000, N'Omit item V03', 40),
        (N'br-cw-041', @ProjectId, 0, N'05', N'External walls & lintels', N'', N'', 0, N'MASON-BRK', N'Form new window / door openings & make good reveals', N'item', 1.0000, 500.0000, 500.0000, N'', 41),
        (N'br-cw-042', @ProjectId, 0, N'05', N'External walls & lintels', N'', N'', 0, N'MASON-BRK', N'Wall extension profiles', N'm', 22.0000, 32.0000, 704.0000, N'', 42),
        (N'br-cw-043', @ProjectId, 0, N'05', N'External walls & lintels', N'', N'', 0, N'MASON-BRK', N'Thermabate cavity closers', N'm', 36.0000, 22.0000, 792.0000, N'', 43),
        (N'br-cw-044', @ProjectId, 0, N'05', N'External walls & lintels', N'', N'', 0, N'MASON-BRK', N'Naylor R5 & Catnic CSS lintels over openings', N'm', 10.0000, 178.0000, 1780.0000, N'', 44),
        (N'br-cw-045', @ProjectId, 0, N'05', N'External walls & lintels', N'', N'', 0, N'MASON-BRK', N'Naylor S4 lintels', N'm', 6.0000, 122.0000, 732.0000, N'', 45),
        (N'br-cw-046', @ProjectId, 0, N'05', N'External walls & lintels', N'', N'', 0, N'MASON-BRK', N'100 mm blockwork internal walls', N'm²', 5.0000, 82.0000, 410.0000, N'Omit item V03', 46),
        (N'br-cw-047', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'203 x 102 x 23 kg steel beam', N'kg', 220.0000, 7.0000, 1540.0000, N'', 47),
        (N'br-cw-048', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'203 x 203 x 46 kg steel beams & columns', N'kg', 480.0000, 7.0000, 3360.0000, N'', 48),
        (N'br-cw-049', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'SUB-CON', N'Cut out & cast concrete padstones', N'nr', 3.0000, 98.0000, 294.0000, N'', 49),
        (N'br-cw-050', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'Base plate & hold down brackets', N'nr', 3.0000, 60.0000, 180.0000, N'', 50),
        (N'br-cw-051', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'PRELIMS-PRO', N'Fireline protection to steel beams', N'item', 1.0000, 300.0000, 300.0000, N'', 51),
        (N'br-cw-052', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Timber wall plate bolted to wall', N'm', 12.0000, 36.0000, 432.0000, N'', 52),
        (N'br-cw-053', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-CUT', N'50 x 150 mm timber roof rafters', N'm', 178.0000, 30.0000, 5340.0000, N'', 53),
        (N'br-cw-054', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Joist hangers', N'nr', 62.0000, 7.0000, 434.0000, N'', 54),
        (N'br-cw-055', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Galvanised restraint straps', N'nr', 22.0000, 18.0000, 396.0000, N'', 55),
        (N'br-cw-056', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Repair existing floor boards', N'item', 1.0000, 500.0000, 500.0000, N'', 56),
        (N'br-cw-057', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber internal stud walls', N'm²', 20.0000, 68.0000, 1360.0000, N'', 57),
        (N'br-cw-058', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Extra for forming pocket door reveals', N'item', 1.0000, 300.0000, 300.0000, N'', 58),
        (N'br-cw-059', @ProjectId, 0, N'08', N'Screed', N'', N'', 0, N'WPF-DMP', N'Damp proof membranes 1200 g', N'm²', 38.0000, 14.0000, 532.0000, N'', 59),
        (N'br-cw-060', @ProjectId, 0, N'08', N'Screed', N'', N'', 0, N'INT-INF', N'100 mm Kingspan floor insulation', N'm²', 48.0000, 40.0000, 1920.0000, N'', 60),
        (N'br-cw-061', @ProjectId, 0, N'08', N'Screed', N'', N'', 0, N'INT-INF', N'25 mm ditto to perimeter', N'm', 34.0000, 12.0000, 408.0000, N'', 61),
        (N'br-cw-062', @ProjectId, 0, N'08', N'Screed', N'', N'', 0, N'FLR-SCR', N'65 mm sand / cement floor screed (throughout ground floor)', N'm²', 112.0000, 65.0000, 7280.0000, N'', 62),
        (N'br-cw-063', @ProjectId, 0, N'09', N'Render', N'', N'', 0, N'INT-RDR', N'20 mm painted sand / cement render - throughout', N'm²', 170.0000, 85.0000, 14450.0000, N'', 63),
        (N'br-cw-064', @ProjectId, 0, N'10', N'Roof covering & rainwater goods', N'', N'', 0, N'SUB-CON', N'Breatherable membrane, battens & matching tiles', N'm²', 66.0000, 102.0000, 6732.0000, N'', 64),
        (N'br-cw-065', @ProjectId, 0, N'10', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-RFR', N'Separate cost for supply of tiles (£0.6 each)', N'm²', 66.0000, 36.0000, 2376.0000, N'', 65),
        (N'br-cw-066', @ProjectId, 0, N'10', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-RFR', N'Ridge / hip tiles', N'm', 4.0000, 72.0000, 288.0000, N'', 66),
        (N'br-cw-067', @ProjectId, 0, N'10', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining', N'm', 24.0000, 55.0000, 1320.0000, N'', 67),
        (N'br-cw-068', @ProjectId, 0, N'10', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-FSU', N'Upvc fascia / soffit', N'm', 28.0000, 44.0000, 1232.0000, N'', 68),
        (N'br-cw-069', @ProjectId, 0, N'10', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-GRU', N'Guttering', N'm', 22.0000, 32.0000, 704.0000, N'', 69),
        (N'br-cw-070', @ProjectId, 0, N'10', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-GRU', N'Rainwater pipework', N'm', 16.0000, 34.0000, 544.0000, N'', 70),
        (N'br-cw-071', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-TIM', N'Composite front entrance door with side light', N'nr', 1.0000, 1850.0000, 1850.0000, N'Omit item V06', 71),
        (N'br-cw-072', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'800 x 2100 mm upvc external door', N'nr', 1.0000, 1295.0000, 1295.0000, N'Omit item V06', 72),
        (N'br-cw-073', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-ALU', N'3475 x 2100 mm aluminium sliding door with fan light', N'nr', 1.0000, 4950.0000, 4950.0000, N'Omit item V06', 73),
        (N'br-cw-074', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'1230 x 1660 mm upvc window', N'nr', 1.0000, 920.0000, 920.0000, N'Omit item V06', 74),
        (N'br-cw-075', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'2205 x 900 mm upvc window', N'nr', 1.0000, 1095.0000, 1095.0000, N'Omit item V06', 75),
        (N'br-cw-076', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'1160 x 900 mm upvc window', N'nr', 1.0000, 580.0000, 580.0000, N'Omit item V06', 76),
        (N'br-cw-077', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'2290 x 1225 mm upvc window', N'nr', 1.0000, 1580.0000, 1580.0000, N'Omit item V06', 77),
        (N'br-cw-078', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'1100 x 985 mm upvc window', N'nr', 1.0000, 600.0000, 600.0000, N'Omit item V06', 78),
        (N'br-cw-079', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'520 x 915 mm upvc window', N'nr', 2.0000, 268.0000, 536.0000, N'Omit item V06', 79),
        (N'br-cw-080', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-UPV', N'335 x 700 mm upvc window', N'nr', 1.0000, 255.0000, 255.0000, N'Omit item V06', 80),
        (N'br-cw-081', @ProjectId, 0, N'11', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-SPG', N'Velux pitched roof lights', N'nr', 2.0000, 1140.0000, 2280.0000, N'Omit item V08', 81),
        (N'br-cw-082', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'INT-INF', N'100 mm Kingspan insulation between floor joists', N'm²', 74.0000, 40.0000, 2960.0000, N'', 82),
        (N'br-cw-083', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'CARP-CUT', N'52.5 mm Kingspan plasterboard to rafters', N'm²', 48.0000, 42.0000, 2016.0000, N'', 83),
        (N'br-cw-084', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings', N'm²', 188.0000, 20.0000, 3760.0000, N'', 84),
        (N'br-cw-085', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'INT-PLS', N'3 mm skim to ceilings', N'm²', 188.0000, 18.0000, 3384.0000, N'', 85),
        (N'br-cw-086', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'52.5 mm Limegreen plasterboard to existing external walls', N'm²', 80.0000, 42.0000, 3360.0000, N'', 86),
        (N'br-cw-087', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'INT-INW', N'50 mm rockwool insulation between stud walls', N'm²', 24.0000, 14.0000, 336.0000, N'', 87),
        (N'br-cw-088', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork & studs', N'm²', 122.0000, 18.0000, 2196.0000, N'', 88),
        (N'br-cw-089', @ProjectId, 0, N'12', N'Insulation & Plastering', N'', N'', 0, N'INT-PLS', N'3 mm skim to walls', N'm²', 224.0000, 18.0000, 4032.0000, N'', 89),
        (N'br-cw-090', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'Replica Tudor boards', N'item', 1.0000, 1000.0000, 1000.0000, N'', 90),
        (N'br-cw-091', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF dado rail to play area', N'm', 14.0000, 28.0000, 392.0000, N'Omit item V28', 91),
        (N'br-cw-092', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-DOR', N'Fix only - Internal door lining, door & ironmongery', N'nr', 14.0000, 195.0000, 2730.0000, N'', 92),
        (N'br-cw-093', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-DOR', N'Sliding pocket door system', N'nr', 3.0000, 450.0000, 1350.0000, N'Omit item V28 - No.2', 93),
        (N'br-cw-094', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames', N'm', 140.0000, 14.0000, 1960.0000, N'', 94),
        (N'br-cw-095', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 112.0000, 28.0000, 3136.0000, N'', 95),
        (N'br-cw-096', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 8.0000, 36.0000, 288.0000, N'', 96),
        (N'br-cw-097', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-1FX', N'Plywood boxing to internal pipework', N'item', 1.0000, 500.0000, 500.0000, N'', 97),
        (N'br-cw-098', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'STAIR-TIM', N'Remove existing balustrade & install new', N'item', 1.0000, 1100.0000, 1100.0000, N'Omit item V12', 98),
        (N'br-cw-099', @ProjectId, 0, N'13', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-KIT', N'Fix only - Kitchen & utility units, worktops & appliances', N'item', 1.0000, 4000.0000, 4000.0000, N'', 99),
        (N'br-cw-100', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'New consumer unit', N'nr', 1.0000, 975.0000, 975.0000, N'', 100),
        (N'br-cw-101', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 32.0000, 110.0000, 3520.0000, N'', 101),
        (N'br-cw-102', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 3.0000, 100.0000, 300.0000, N'', 102),
        (N'br-cw-103', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 5.0000, 100.0000, 500.0000, N'', 103),
        (N'br-cw-104', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Cooker switch', N'nr', 1.0000, 115.0000, 115.0000, N'', 104),
        (N'br-cw-105', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Recessed light fittings', N'nr', 52.0000, 108.0000, 5616.0000, N'', 105),
        (N'br-cw-106', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Pendant lights', N'nr', 11.0000, 84.0000, 924.0000, N'Omit 1 - V11', 106),
        (N'br-cw-107', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Wall lights', N'nr', 5.0000, 112.0000, 560.0000, N'Omit 1 - V11', 107),
        (N'br-cw-108', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Track lights', N'nr', 2.0000, 225.0000, 450.0000, N'', 108),
        (N'br-cw-109', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'External lights', N'nr', 4.0000, 128.0000, 512.0000, N'Omit item V28', 109),
        (N'br-cw-110', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 16.0000, 38.0000, 608.0000, N'', 110),
        (N'br-cw-111', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'MEC-VNT', N'Fix only - Extractor fans', N'nr', 5.0000, 120.0000, 600.0000, N'', 111),
        (N'br-cw-112', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 4.0000, 145.0000, 580.0000, N'', 112),
        (N'br-cw-113', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'TV / data points', N'item', 1.0000, 120.0000, 120.0000, N'', 113),
        (N'br-cw-114', @ProjectId, 0, N'14', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 114),
        (N'br-cw-115', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-BLR', N'New boiler & Megaflo hot water cylinder', N'nr', 1.0000, 6250.0000, 6250.0000, N'', 115),
        (N'br-cw-116', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-UFH', N'Wet underfloor heating', N'm²', 60.0000, 155.0000, 9300.0000, N'', 116),
        (N'br-cw-117', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'New radiators throughout', N'nr', 10.0000, 455.0000, 4550.0000, N'Omit Item V28', 117),
        (N'br-cw-118', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Thermostat / manifolds', N'item', 1.0000, 500.0000, 500.0000, N'', 118),
        (N'br-cw-119', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Towel rails with TRVs', N'nr', 3.0000, 400.0000, 1200.0000, N'Omit item V28', 119),
        (N'br-cw-120', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 3.0000, 130.0000, 390.0000, N'', 120),
        (N'br-cw-121', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 550.0000, 550.0000, N'', 121),
        (N'br-cw-122', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Hot & cold supplies to kitchen & sanitary fittings', N'nr', 22.0000, 165.0000, 3630.0000, N'', 122),
        (N'br-cw-123', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Wastes to ditto', N'nr', 19.0000, 88.0000, 1672.0000, N'', 123),
        (N'br-cw-124', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - WC', N'nr', 3.0000, 320.0000, 960.0000, N'', 124),
        (N'br-cw-125', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin', N'nr', 3.0000, 310.0000, 930.0000, N'', 125),
        (N'br-cw-126', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - Showers / baths', N'nr', 3.0000, 495.0000, 1485.0000, N'', 126),
        (N'br-cw-127', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - Bathroom mirror / screens', N'nr', 1.0000, 180.0000, 180.0000, N'', 127),
        (N'br-cw-128', @ProjectId, 0, N'15', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Builders work in connection with plumbing & heating installation', N'item', 1.0000, 500.0000, 500.0000, N'', 128),
        (N'br-cw-129', @ProjectId, 0, N'16', N'Floor Finishes', N'', N'', 0, N'TIL-STD', N'Fix only - Floor tiles (with self leveling & matting)', N'm²', 32.0000, 118.0000, 3776.0000, N'Omit item V18', 129),
        (N'br-cw-130', @ProjectId, 0, N'16', N'Floor Finishes', N'', N'', 0, N'TIL-STD', N'Fix only - Wall tiles', N'm²', 30.0000, 80.0000, 2400.0000, N'Omit item V18', 130),
        (N'br-cw-131', @ProjectId, 0, N'16', N'Floor Finishes', N'', N'', 0, N'FLR-WD', N'Fix only - Engineered timber flooring', N'm²', 146.0000, 36.0000, 5256.0000, N'Omit item V21', 131),
        (N'br-cw-132', @ProjectId, 0, N'16', N'Floor Finishes', N'', N'', 0, N'FLR-LVT', N'Fix only - Vinyl flooring', N'm²', 12.0000, 22.0000, 264.0000, N'Omit item V21', 132),
        (N'br-cw-133', @ProjectId, 0, N'17', N'Decorations & finishes - Throughout', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm²', 182.0000, 16.0000, 2912.0000, N'', 133),
        (N'br-cw-134', @ProjectId, 0, N'17', N'Decorations & finishes - Throughout', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm²', 402.0000, 14.0000, 5628.0000, N'', 134),
        (N'br-cw-135', @ProjectId, 0, N'17', N'Decorations & finishes - Throughout', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm²', 49.0000, 30.0000, 1470.0000, N'', 135),
        (N'br-cw-136', @ProjectId, 0, N'17', N'Decorations & finishes - Throughout', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 378.0000, 8.0000, 3024.0000, N'', 136),
        (N'br-cw-137', @ProjectId, 0, N'18', N'Landscape', N'', N'', 0, N'MASON-BRK', N'Brick dwarf wall, foundation & copping', N'm', 6.0000, 425.0000, 2550.0000, N'', 137),
        (N'br-cw-138', @ProjectId, 0, N'18', N'Landscape', N'', N'', 0, N'EXTW-PAV', N'Charcoal block paving to driveway', N'm²', 45.0000, 98.0000, 4410.0000, N'Omit item V26', 138),
        (N'br-cw-139', @ProjectId, 0, N'18', N'Landscape', N'', N'', 0, N'EXTW-PAV', N'Outdoor tiles / paving to rear & side', N'm²', 60.0000, 150.0000, 9000.0000, N'Omit item V26', 139)
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

    PRINT '67 Beresford Road: valuation lines merged.';
    COMMIT TRAN;
END

-- Sanity check: the seeded block should reconcile to the workbook.
-- (@ProjectId is still in scope -- same batch.)
SELECT
    SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 264504.00
    SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --      0.00 (none)
    SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --      0.00 (none)
    SUM(LineAmount) AS ContractSum                                               -- 264504.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
  AND LineType NOT IN (3, 4);
GO
