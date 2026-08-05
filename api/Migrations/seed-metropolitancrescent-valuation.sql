-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql. If a code is not in that master it is NOT a cost
-- code.
-- Seed: Metropolitan Crescent -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : Flat 1, 3 Metropolitan Crescent, Crescent Lane, London, SW4 9BF
-- ProjectId: resolved at run time by site-name matcher 'metropolitancrescent'
--
-- Seeds the ORIGINAL contract scope only, taken from the "Valuation 7 -
-- 12 Month Retention" workbook. Three blocks make up the Contract Sum, as per
-- the Albany Mews pattern:
--
--     Contract works    GBP  88,514.00
--     Provisional Sums  GBP  34,500.00   (PC01..PC09)
--     Contingency       GBP  15,000.00
--     ----------------------------------
--     Contract Sum      GBP 138,014.00
--
-- Variations (V01..V37, net GBP 25,264.20) are NOT seeded here -- they belong
-- in seed-metropolitancrescent-variations.sql. Per-valuation claim history
-- (Valuation 01..07, retention) is claim data, not bill structure.
--
-- SectionCode/SectionName retain the workbook's NRM-style references; PS lines
-- retain their PC codes. The workbook's own numeric codes (0001..0024) are
-- dropped, per the Albany precedent. Two inline "Provisional Sum" bill lines
-- (external bifold door removal, aluminium bifolding door D02) stay in the
-- Contract works block as LineType 1.
--
-- "Omit Item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
--
-- Skipped workbook rows: none -- every priced bill row carries contract value.
-- (Blank spacer rows and the register's stray #DIV/0! formula row are not
-- data rows.)
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (mc-cw-NNN / mc-ps-NN
-- / mc-cont-NN). A re-run refreshes every field via MERGE. Variation lines for
-- this project are left untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'metropolitancrescent'
       OR LOWER(REPLACE(Name, ' ', '')) = 'metropolitancrescent'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'metropolitancrescent' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Metropolitan Crescent -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'mc-cw-001', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site Supervision', N'week', 14.0000, 1250.0000, 17500.0000, N'', 1),
        (N'mc-cw-002', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-LAB', N'Labour for clearing and curbside deliveries', N'week', 6.0000, 750.0000, 4500.0000, N'', 2),
        (N'mc-cw-003', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'skip', 3.0000, 345.0000, 1035.0000, N'', 3),
        (N'mc-cw-004', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection', N'item', 1.0000, 300.0000, 300.0000, N'', 4),
        (N'mc-cw-005', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet - assumes use old for duration not allowed', N'week', 2.0000, 50.0000, 100.0000, N'', 5),
        (N'mc-cw-006', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ELE-STD', N'Temporary plumbing & electrics', N'item', 1.0000, 200.0000, 200.0000, N'', 6),
        (N'mc-cw-007', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SET', N'Parking', N'week', 14.0000, 475.0000, 6650.0000, N'', 7),
        (N'mc-cw-008', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 14.0000, 75.0000, 1050.0000, N'', 8),
        (N'mc-cw-009', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 500.0000, 500.0000, N'', 9),
        (N'mc-cw-010', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 300.0000, 300.0000, N'', 10),
        (N'mc-cw-011', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 300.0000, 300.0000, N'', 11),
        (N'mc-cw-012', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 765.0000, 765.0000, N'', 12),
        (N'mc-cw-013', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove floor and wall tiling to 2 bathrooms', N'm2', 50.0000, 24.0000, 1200.0000, N'', 13),
        (N'mc-cw-014', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove kitchen units, worktops & appliances', N'item', 1.0000, 200.0000, 200.0000, N'', 14),
        (N'mc-cw-015', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items from ensuites & bathroom', N'item', 1.0000, 400.0000, 400.0000, N'', 15),
        (N'mc-cw-016', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove carfully built in joinery / wardrobes', N'item', 1.0000, 150.0000, 150.0000, N'', 16),
        (N'mc-cw-017', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 7.0000, 22.0000, 154.0000, N'', 17),
        (N'mc-cw-018', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal single skin walls', N'm2', 10.0000, 23.0000, 230.0000, N'', 18),
        (N'mc-cw-019', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 1, N'ENABLE-DEM', N'Remove external bifold door - Provisional Sum', N'item', 1.0000, 465.0000, 465.0000, N'Omit Item V29', 19),
        (N'mc-cw-020', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Break up section of floor screed for sunken trays - assumes not reinforced,', N'm2', 3.0000, 55.0000, 165.0000, N'', 20),
        (N'mc-cw-021', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 2.0000, 130.0000, 260.0000, N'', 21),
        (N'mc-cw-022', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Wastes connections - assumes accessible', N'nr', 6.0000, 88.0000, 528.0000, N'', 22),
        (N'mc-cw-023', @ProjectId, 0, N'B50', N'General structural requirements', N'', N'', 0, N'CARP-1FX', N'38 x 89 mm timber internal stud walls', N'm2', 14.0000, 62.0000, 868.0000, N'', 23),
        (N'mc-cw-024', @ProjectId, 0, N'B50', N'General structural requirements', N'', N'', 0, N'CARP-1FX', N'50 mm rockwool insulation between stud walls', N'm2', 14.0000, 18.0000, 252.0000, N'', 24),
        (N'mc-cw-025', @ProjectId, 0, N'B50', N'General structural requirements', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork & studs', N'm2', 24.0000, 22.0000, 528.0000, N'', 25),
        (N'mc-cw-026', @ProjectId, 0, N'B50', N'General structural requirements', N'', N'', 0, N'SUB-CON', N'Naylor lintels over widened door openings - included to make good plaster works', N'nr', 5.0000, 455.0000, 2275.0000, N'', 26),
        (N'mc-cw-027', @ProjectId, 0, N'K11', N'Rigid sheet flooring/sheathing/decking', N'', N'', 0, N'CARP-1FX', N'18 mm plywood to stud walls', N'm2', 24.0000, 24.0000, 576.0000, N'', 27),
        (N'mc-cw-028', @ProjectId, 0, N'K20', N'Timber board flooring/ sarking linings/ casings', N'', N'', 0, N'FLR-WD', N'Make good retained timber flooring to hallway', N'item', 1.0000, 250.0000, 250.0000, N'Omit Item V15', 28),
        (N'mc-cw-029', @ProjectId, 0, N'L20', N'Doors', N'', N'', 1, N'WDR-ALU', N'3775 x 2340 mm aluminium bifolding door - D02 - to be put together on site due to access - Provisional Sum', N'nr', 1.0000, 7750.0000, 7750.0000, N'Omit Item V03', 29),
        (N'mc-cw-030', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'CARP-2FX', N'Make good / level threshold to apartment entrance door with timber ramp', N'nr', 1.0000, 150.0000, 150.0000, N'Omit Item V18', 30),
        (N'mc-cw-031', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'CARP-2FX', N'Make good to block entrance doorway with exisitng floor cill removal and drop down cill to main door - Aco drainage TBD', N'nr', 1.0000, 375.0000, 375.0000, N'Omit Item V23', 31),
        (N'mc-cw-032', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'CARP-DOR', N'926 x 2040 mm internal FD30 fire door set - assumes opening allows with no further wall modifications', N'nr', 6.0000, 475.0000, 2850.0000, N'Omit Item V18 x 1 door', 32),
        (N'mc-cw-033', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'CARP-DOR', N'Utility cupboard bifolding doors', N'nr', 2.0000, 380.0000, 760.0000, N'Omit Item V22', 33),
        (N'mc-cw-034', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames (£6/m supply)', N'm', 60.0000, 12.0000, 720.0000, N'', 34),
        (N'mc-cw-035', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 8.0000, 24.0000, 192.0000, N'', 35),
        (N'mc-cw-036', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Hot & cold water supply modifications to existing', N'nr', 12.0000, 90.0000, 1080.0000, N'', 36),
        (N'mc-cw-037', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Water softener', N'nr', 1.0000, 855.0000, 855.0000, N'Omit Item V24', 37),
        (N'mc-cw-038', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - WC', N'nr', 2.0000, 295.0000, 590.0000, N'', 38),
        (N'mc-cw-039', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - Wash hand basin', N'nr', 1.0000, 280.0000, 280.0000, N'', 39),
        (N'mc-cw-040', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - Existng Wash hand basin & mirror', N'nr', 1.0000, 350.0000, 350.0000, N'', 40),
        (N'mc-cw-041', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - Showers / baths - assumes accessible', N'nr', 2.0000, 500.0000, 1000.0000, N'', 41),
        (N'mc-cw-042', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - Mirrors, towel rails, hooks, etc', N'item', 1.0000, 450.0000, 450.0000, N'', 42),
        (N'mc-cw-043', @ProjectId, 0, N'U90', N'General ventilation', N'', N'', 0, N'MEC-VNT', N'Extract fan - assumes existing vent re-attachable to external source', N'nr', 3.0000, 275.0000, 825.0000, N'', 43),
        (N'mc-cw-044', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Alterations to electrics following door widening', N'nr', 1.0000, 750.0000, 750.0000, N'', 44),
        (N'mc-cw-045', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 2.0000, 100.0000, 200.0000, N'', 45),
        (N'mc-cw-046', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 2.0000, 95.0000, 190.0000, N'', 46),
        (N'mc-cw-047', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Recessed light fitting', N'nr', 7.0000, 108.0000, 756.0000, N'', 47),
        (N'mc-cw-048', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 2.0000, 40.0000, 80.0000, N'', 48),
        (N'mc-cw-049', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 3.0000, 95.0000, 285.0000, N'', 49),
        (N'mc-cw-050', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 2.0000, 150.0000, 300.0000, N'', 50),
        (N'mc-cw-051', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Carbon monoxide detector', N'nr', 1.0000, 118.0000, 118.0000, N'', 51),
        (N'mc-cw-052', @ProjectId, 0, N'M10', N'Cement based levelling/wearing screed', N'', N'', 0, N'FLR-SLF', N'Self Levelling adhesive to internal change of bifold area - assumes 10mm', N'm2', 1.0000, 37.0000, 37.0000, N'Omit Item V15', 52),
        (N'mc-cw-053', @ProjectId, 0, N'M10', N'Cement based levelling/wearing screed', N'', N'', 0, N'FLR-SLF', N'Self Levelling adhesive to below shower tray reccess - assumes 10mm', N'm2', 3.0000, 37.0000, 111.0000, N'', 53),
        (N'mc-cw-054', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'Make good existing ceilings & walls affected by demolition', N'm2', 5.0000, 45.0000, 225.0000, N'', 54),
        (N'mc-cw-055', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'3 mm skim to new walls only', N'm2', 24.0000, 20.0000, 480.0000, N'', 55),
        (N'mc-cw-056', @ProjectId, 0, N'M40', N'Ceramic tiles', N'', N'', 0, N'TIL-STD', N'Fix only - Wall tiles to wet room & shower excluding ditramat/tanking for wet room - assumes existing structure is flat and sound following existing tile removal', N'm2', 50.0000, 90.0000, 4500.0000, N'', 56),
        (N'mc-cw-057', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Karndean vinyl flooring (£40 supply) - assumes no self level', N'm2', 36.0000, 95.0000, 3420.0000, N'Omit Item V15', 57),
        (N'mc-cw-058', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Altro vinyl flooring (£40 supply) - assumes no self level', N'm2', 14.0000, 95.0000, 1330.0000, N'', 58),
        (N'mc-cw-059', @ProjectId, 0, N'M51', N'Carpet', N'', N'', 0, N'FLR-CPT', N'Underlay & carpet (£45 supply)', N'm2', 19.0000, 70.0000, 1330.0000, N'', 59),
        (N'mc-cw-060', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 90.0000, 18.0000, 1620.0000, N'', 60),
        (N'mc-cw-061', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 184.0000, 16.0000, 2944.0000, N'', 61),
        (N'mc-cw-062', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Prepare & decorate doors', N'm2', 26.0000, 32.0000, 832.0000, N'', 62),
        (N'mc-cw-063', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Frames, architrave, window board & skirtings', N'm', 142.0000, 9.0000, 1278.0000, N'', 63),
        (N'mc-cw-064', @ProjectId, 0, N'Q20', N'Sub-bases to pavings', N'', N'', 0, N'EXTW-PAV', N'Remove existing paving and sub base', N'm2', 40.0000, 35.0000, 1400.0000, N'', 64),
        (N'mc-cw-065', @ProjectId, 0, N'Q20', N'Sub-bases to pavings', N'', N'', 0, N'SUB-CON', N'Sub base for new - no retaining walls', N'm2', 40.0000, 45.0000, 1800.0000, N'', 65),
        (N'mc-cw-066', @ProjectId, 0, N'Q24', N'Paving', N'', N'', 0, N'EXTW-PAV', N'New paving slabs to patio area (£45/m supply)', N'm2', 40.0000, 115.0000, 4600.0000, N'', 66),
        (N'mc-ps-01', @ProjectId, 1, N'PC01', N'Provisional Sums', N'', N'', 1, N'SUP-TIL', N'Wall tiling (BASED ON 50m2 @ £70.00 / M2)', N'item', 1.0000, 3500.0000, 3500.0000, N'Omit item V08', 1),
        (N'mc-ps-02', @ProjectId, 1, N'PC02', N'Provisional Sums', N'', N'', 1, N'SUP-IRO', N'Internal door ironmongery', N'item', 1.0000, 1500.0000, 1500.0000, N'Omit Item V25', 2),
        (N'mc-ps-03', @ProjectId, 1, N'PC03', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'New Foldable shower seat (2 of.)', N'item', 1.0000, 4000.0000, 4000.0000, N'Omit Item V05', 3),
        (N'mc-ps-04', @ProjectId, 1, N'PC04', N'Provisional Sums', N'', N'', 1, N'MEC-DRN', N'Below ground drainage and CCTV survey', N'item', 1.0000, 5000.0000, 5000.0000, N'Omit Item V02', 4),
        (N'mc-ps-05', @ProjectId, 1, N'PC05', N'Provisional Sums', N'', N'', 1, N'MEC-PLM', N'Radiators, allow for relocation of 4no.', N'item', 1.0000, 1000.0000, 1000.0000, N'Omit Item V19', 5),
        (N'mc-ps-06', @ProjectId, 1, N'PC06', N'Provisional Sums', N'', N'', 1, N'CARP-JNR', N'Utility Cupboard', N'item', 1.0000, 2500.0000, 2500.0000, N'Omit Item V21', 6),
        (N'mc-ps-07', @ProjectId, 1, N'PC07', N'Provisional Sums', N'', N'', 1, N'WIN-BLD', N'Blinds and curtains', N'item', 1.0000, 7000.0000, 7000.0000, N'Omit Item V27', 7),
        (N'mc-ps-08', @ProjectId, 1, N'PC08', N'Provisional Sums', N'', N'', 1, N'EXTW-LND', N'Soft landscaping', N'item', 1.0000, 5000.0000, 5000.0000, N'Omit Item V26', 8),
        (N'mc-ps-09', @ProjectId, 1, N'PC09', N'Provisional Sums', N'', N'', 1, N'CARP-WRD', N'Built-in Wardrobes, new kitchen, utility, remedial works', N'item', 1.0000, 5000.0000, 5000.0000, N'Omit Item V20', 9),
        (N'mc-cont-01', @ProjectId, 2, N'', N'Contingency', N'', N'', 0, N'HAND-MSC', N'Contingency Budget', N'item', 1.0000, 15000.0000, 15000.0000, N'Omit Item V28', 1)
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

    PRINT 'Metropolitan Crescent: valuation lines merged.';

    -- Sanity check: the three seeded blocks should reconcile to the workbook.
    SELECT
        SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  --  88514.00
        SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --  34500.00
        SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --  15000.00
        SUM(LineAmount) AS ContractSum                                               -- 138014.00
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
      AND LineType NOT IN (3, 4);

    COMMIT TRAN;
END
GO
