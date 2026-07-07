-- ============================================================================
-- NOTE: CostCode values use the canonical JBB Cost Code Master codes (v2.1, trade-prefixed) seeded
-- by seed-cost-centers.sql. Remapped from the original JBB-* buckets on
-- 2026-07-07 -- audit trail: scripts/cost-code-remap-review.csv.
-- Seed: Coombe Lane -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : 149a Coombe Lane West  (JBB-2026-005)
--           Adaptation to dwelling including alteration to existing indoor pool
-- ProjectId: d64c8a72f4b14eebbabd251b54564ef5
--
-- Seeds the ORIGINAL contract scope only, taken from the Valuation 20 workbook
-- (Retention Release). Three blocks make up the Contract Sum, as per the
-- By France pattern:
--
--     Contract works    GBP 478,174.60
--     Provisional Sums  GBP 204,965.00
--     Contingency        GBP 20,000.00
--     ----------------------------------
--     Contract Sum      GBP 703,139.60
--
-- Variations (V01..V56, net GBP 152,462.81) are NOT seeded here -- they belong
-- in a separate variations seed. Per-valuation claim history (VAL.1..VAL.20,
-- forecasts, retention release) is claim data (ValuationClaims/ClaimLines),
-- not bill structure.
--
-- SectionCode/SectionName retain the workbook's NRM2-style references; the
-- "Swimming Pool - Subcontractor" section has no code in the workbook and is
-- seeded with an empty SectionCode (costed JBB-SEQ). PS SUMS lines carry no
-- PC codes in this workbook, so SectionCode is empty for those too. Lines
-- priced "PS" in the rate column are LineType 1 with Rate = LineAmount.
-- CostCode maps to the Jewel cost-centre master (JBB-*); the workbook's own
-- numeric codes (0001..0028) are dropped. "Omit item Vnn" comments are
-- informational: those omits live in the variations register.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (cl-cw-NNN / cl-ps-NN
-- / cl-cont-NN). A re-run refreshes every field via MERGE. Variation lines for
-- this project are left untouched. Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'cl-cw-001', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site Supervision', N'week', 20.0000, 1250.0000, 25000.0000, N'', 1),
    (N'cl-cw-002', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'A10', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 16.0000, 380.0000, 6080.0000, N'', 2),
    (N'cl-cw-003', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection', N'item', 1.0000, 600.0000, 600.0000, N'', 3),
    (N'cl-cw-004', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 20.0000, 90.0000, 1800.0000, N'', 4),
    (N'cl-cw-005', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 5),
    (N'cl-cw-006', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 20.0000, 125.0000, 2500.0000, N'', 6),
    (N'cl-cw-007', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'A10', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 500.0000, 500.0000, N'', 7),
    (N'cl-cw-008', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 400.0000, 400.0000, N'', 8),
    (N'cl-cw-009', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 550.0000, 550.0000, N'', 9),
    (N'cl-cw-010', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Decomision & remove ground source heat pump', N'item', 1.0000, 1000.0000, 1000.0000, N'', 10),
    (N'cl-cw-011', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'm2', 156.0000, 4.0000, 624.0000, N'', 11),
    (N'cl-cw-012', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove utility units, worktops & appliances', N'item', 1.0000, 180.0000, 180.0000, N'', 12),
    (N'cl-cw-013', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items from WCs & bathrooms', N'item', 1.0000, 400.0000, 400.0000, N'', 13),
    (N'cl-cw-014', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 6.0000, 22.0000, 132.0000, N'', 14),
    (N'cl-cw-015', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal single skin walls', N'm2', 30.0000, 44.0000, 1320.0000, N'', 15),
    (N'cl-cw-016', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove section of existing plasterboard ceiling', N'm2', 136.0000, 16.0000, 2176.0000, N'', 16),
    (N'cl-cw-017', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing copping from parapet walls', N'm', 11.0000, 25.0000, 275.0000, N'', 17),
    (N'cl-cw-018', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove external doors', N'nr', 3.0000, 75.0000, 225.0000, N'', 18),
    (N'cl-cw-019', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove window', N'nr', 1.0000, 45.0000, 45.0000, N'Omit item V41', 19),
    (N'cl-cw-020', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'SUB-GWK', N'Break up external steps & retaining walls', N'item', 1.0000, 800.0000, 800.0000, N'', 20),
    (N'cl-cw-021', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing paving, shrubs, etc to areas of new work', N'm2', 108.0000, 12.0000, 1296.0000, N'', 21),
    (N'cl-cw-022', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 2.0000, 130.0000, 260.0000, N'', 22),
    (N'cl-cw-023', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 450.0000, 450.0000, N'', 23),
    (N'cl-cw-024', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Wastes connections', N'nr', 8.0000, 88.0000, 704.0000, N'', 24),
    (N'cl-cw-025', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 1000.0000, 1000.0000, N'', 25),
    (N'cl-cw-026', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'New manhole', N'nr', 1.0000, 725.0000, 725.0000, N'', 26),
    (N'cl-cw-027', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 750.0000, 750.0000, N'', 27),
    (N'cl-cw-028', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'Drainage remedial works & surveys', N'item', 1.0000, 700.0000, 700.0000, N'', 28),
    (N'cl-cw-029', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'Make connection into existing runs', N'item', 1.0000, 250.0000, 250.0000, N'', 29),
    (N'cl-cw-030', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'215 mm blockwork wall to first floor', N'm2', 2.0000, 122.0000, 244.0000, N'', 30),
    (N'cl-cw-031', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'F30', N'Accessories for brick/block walling', N'', N'', 0, N'MASON-BRK', N'Stone copping to parapet wall', N'm', 11.0000, 155.0000, 1705.0000, N'Omit item V27', 31),
    (N'cl-cw-032', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'B50', N'General structure', N'', N'', 0, N'STR-STL', N'Labour only to structural work', N'item', 1.0000, 3000.0000, 3000.0000, N'', 32),
    (N'cl-cw-033', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings first floor areas', N'm2', 136.0000, 20.0000, 2720.0000, N'', 33),
    (N'cl-cw-034', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'38 x 89 mm timber internal stud walls', N'm2', 32.0000, 58.0000, 1856.0000, N'', 34),
    (N'cl-cw-035', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'50 mm rockwool insulation between stud walls', N'm2', 32.0000, 16.0000, 512.0000, N'', 35),
    (N'cl-cw-036', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'18 mm marine plywood to stud walls', N'm2', 64.0000, 38.0000, 2432.0000, N'', 36),
    (N'cl-cw-037', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork & studs', N'm2', 68.0000, 20.0000, 1360.0000, N'', 37),
    (N'cl-cw-038', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-RDR', N'Make good areas of existing walls as required', N'item', 1.0000, 750.0000, 750.0000, N'', 38),
    (N'cl-cw-039', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-RDR', N'3 mm skim to ceilings', N'm2', 136.0000, 20.0000, 2720.0000, N'', 39),
    (N'cl-cw-040', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-RDR', N'3 mm skim to new walls', N'm2', 68.0000, 20.0000, 1360.0000, N'', 40),
    (N'cl-cw-041', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'SUP-SAN', N'Replacement window to Luna''s ensuite with blind', N'nr', 1.0000, 1950.0000, 1950.0000, N'Omit item V41', 41),
    (N'cl-cw-042', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'3500 x 2100 mm timber sliding doors', N'nr', 1.0000, 7255.0000, 7255.0000, N'Omit Item V19', 42),
    (N'cl-cw-043', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'2935 x 2100 mm timber sliding doors', N'nr', 2.0000, 6200.0000, 12400.0000, N'Omit Item V19', 43),
    (N'cl-cw-044', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L20', N'Doors', N'', N'', 0, N'SUP-DOR', N'926 mm Internal door lining & single door ( £120 supply )', N'nr', 1.0000, 345.0000, 345.0000, N'', 44),
    (N'cl-cw-045', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L20', N'Doors', N'', N'', 0, N'CARP-DOR', N'826 mm Internal door lining & single pocket door', N'nr', 2.0000, 1055.0000, 2110.0000, N'', 45),
    (N'cl-cw-046', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L20', N'Doors', N'', N'', 0, N'CARP-DOR', N'Internal door lining & single door with glazed screen', N'nr', 1.0000, 1980.0000, 1980.0000, N'Omit item V45', 46),
    (N'cl-cw-047', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L20', N'Doors', N'', N'', 0, N'WDR-TIM', N'Pool storage doors - fix only', N'nr', 3.0000, 175.0000, 525.0000, N'', 47),
    (N'cl-cw-048', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'L20', N'Doors', N'', N'', 0, N'SUP-IRO', N'Fix only - Door ironmongery', N'item', 1.0000, 450.0000, 450.0000, N'', 48),
    (N'cl-cw-049', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Hot & cold water supply', N'nr', 16.0000, 160.0000, 2560.0000, N'', 49),
    (N'cl-cw-050', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'SPEC-POO', N'Alterations to pool H&C water supply', N'item', 1.0000, 1500.0000, 1500.0000, N'', 50),
    (N'cl-cw-051', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Towel rails with TRVS - FF', N'nr', 1.0000, 500.0000, 500.0000, N'', 51),
    (N'cl-cw-052', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Alterations to existing system', N'item', 1.0000, 800.0000, 800.0000, N'', 52),
    (N'cl-cw-053', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'U90', N'General ventilation', N'', N'', 0, N'MEC-VNT', N'Extract fan', N'nr', 3.0000, 275.0000, 825.0000, N'', 53),
    (N'cl-cw-054', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 13.0000, 115.0000, 1495.0000, N'', 54),
    (N'cl-cw-055', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 1.0000, 95.0000, 95.0000, N'', 55),
    (N'cl-cw-056', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Recessed light fitting', N'nr', 63.0000, 108.0000, 6804.0000, N'', 56),
    (N'cl-cw-057', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 14.0000, 40.0000, 560.0000, N'', 57),
    (N'cl-cw-058', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 3.0000, 95.0000, 285.0000, N'', 58),
    (N'cl-cw-059', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Alterations to existing system', N'item', 1.0000, 800.0000, 800.0000, N'', 59),
    (N'cl-cw-060', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fix only - Tesla points', N'item', 1.0000, 950.0000, 950.0000, N'Omit Item V16', 60),
    (N'cl-cw-061', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 61),
    (N'cl-cw-062', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'B05', N'Fire saftey', N'', N'', 1, N'ELE-FIR', N'Fire saftey - Provisional sum', N'item', 1.0000, 1000.0000, 1000.0000, N'Omit Item V20', 62),
    (N'cl-cw-063', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'P10', N'Sundry insulation', N'', N'', 0, N'CARP-1FX', N'Plywood boxing & insulation to internal pipes', N'item', 1.0000, 450.0000, 450.0000, N'', 63),
    (N'cl-cw-064', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames', N'm', 45.0000, 12.0000, 540.0000, N'', 64),
    (N'cl-cw-065', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 46.0000, 24.0000, 1104.0000, N'', 65),
    (N'cl-cw-066', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 2.0000, 36.0000, 72.0000, N'', 66),
    (N'cl-cw-067', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-JNR', N'Fix only - Waredrobes / bespoke joinery', N'item', 1.0000, 1200.0000, 1200.0000, N'Omit item V37', 67),
    (N'cl-cw-068', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - Sanitry items (wc, basin, bath, hoist & shower FF)', N'item', 1.0000, 4255.0000, 4255.0000, N'', 68),
    (N'cl-cw-069', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M40', N'Tiling', N'', N'', 0, N'SUP-SAN', N'Fix only - Wall tiling Ensuite Luna', N'm2', 15.0000, 80.0000, 1200.0000, N'', 69),
    (N'cl-cw-070', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M40', N'Tiling', N'', N'', 0, N'TIL-STD', N'Fix only - Floor tiling & hallway', N'm2', 210.0000, 80.0000, 16800.0000, N'Omit Item V22', 70),
    (N'cl-cw-071', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 136.0000, 22.0000, 2992.0000, N'', 71),
    (N'cl-cw-072', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 248.0000, 20.0000, 4960.0000, N'', 72),
    (N'cl-cw-073', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M60', N'Painting', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm2', 12.0000, 36.0000, 432.0000, N'', 73),
    (N'cl-cw-074', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M60', N'Painting', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 220.0000, 9.0000, 1980.0000, N'', 74),
    (N'cl-cw-075', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M61', N'Intumescent coatings', N'', N'', 0, N'FIRE-PSV', N'Coating to basement wall plates', N'item', 1.0000, 400.0000, 400.0000, N'', 75),
    (N'cl-cw-076', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q10', N'Kerbs/ edgings/ channels/ paving accessories', N'', N'', 0, N'ENABLE-DEM', N'Remove existing slot drain', N'm', 17.0000, 34.0000, 578.0000, N'', 76),
    (N'cl-cw-077', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q10', N'Kerbs/ edgings/ channels/ paving accessories', N'', N'', 0, N'EXTW-PAV', N'New Aco slot drain', N'm', 17.0000, 142.0000, 2414.0000, N'', 77),
    (N'cl-cw-078', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'SUB-CON', N'Hardcore sub base to new patio area', N'm2', 100.0000, 36.0000, 3600.0000, N'Omit item V27', 78),
    (N'cl-cw-079', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'EXTW-PAV', N'Form new ramp', N'item', 1.0000, 2000.0000, 2000.0000, N'', 79),
    (N'cl-cw-080', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'SUB-GWK', N'Extend / alterations to retaining walls', N'item', 1.0000, 5000.0000, 5000.0000, N'Omit item V27', 80),
    (N'cl-cw-081', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q23', N'Front works', N'', N'', 0, N'ENABLE-DEM', N'Remove existing top surface - area tbc', N'm2', 100.0000, 30.0000, 3000.0000, N'Omit item V34', 81),
    (N'cl-cw-082', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q23', N'Front works', N'', N'', 0, N'EXTW-PAV', N'New resin bound finish - area tbc', N'm2', 100.0000, 145.0000, 14500.0000, N'Omit item V55', 82),
    (N'cl-cw-083', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q23', N'Front works', N'', N'', 0, N'SPEC-GAZ', N'Fix only - Carport', N'item', 1.0000, 2500.0000, 2500.0000, N'Omit item V27', 83),
    (N'cl-cw-084', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q23', N'Front works', N'', N'', 1, N'EXTW-FEN', N'Replacement sliding gate', N'item', 1.0000, 7500.0000, 7500.0000, N'Omit item V24', 84),
    (N'cl-cw-085', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'Q25', N'Slab pavings to patios', N'', N'', 0, N'EXTW-PAV', N'Fix only - Hard Landscaping', N'm2', 130.0000, 88.0000, 11440.0000, N'Omit item V27', 85),
    (N'cl-cw-086', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-CPT', N'Karndean vinyl flooring (£40 supply)', N'm2', 38.0000, 95.0000, 3610.0000, N'Omit Item V21', 86),
    (N'cl-cw-087', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-CPT', N'Altro vinyl flooring (£40 supply)', N'm2', 7.0000, 95.0000, 665.0000, N'', 87),
    (N'cl-cw-088', N'd64c8a72f4b14eebbabd251b54564ef5', 0, N'', N'Swimming Pool - Subcontractor', N'', N'', 0, N'SPEC-POO', N'H20 pools quote -  PCC-Bees-021023', N'item', 1.0000, 268692.6000, 268692.6000, N'Omit item V25', 88),
    (N'cl-ps-01', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'TIL-STD', N'Wall tiling (BASED  @ £50.00 / M2)', N'item', 1.0000, 21100.0000, 21100.0000, N'Omit item V25', 1),
    (N'cl-ps-02', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-IRO', N'Internal door ironmongery', N'item', 1.0000, 440.0000, 440.0000, N'Omit Item V43', 2),
    (N'cl-ps-03', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'HAND-SPE', N'Structural engineering items', N'item', 1.0000, 22000.0000, 22000.0000, N'Omit item V34', 3),
    (N'cl-ps-04', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'ENABLE-DEM', N'Decommission and remove ground source heat pump', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit item V27', 4),
    (N'cl-ps-05', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'MEC-DRN', N'Drainage remedial work', N'item', 1.0000, 2200.0000, 2200.0000, N'Omit item V32', 5),
    (N'cl-ps-06', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'EXTW-LND', N'Hard landscaping supply (Based on 130m^2@£50/m^2)', N'item', 1.0000, 7150.0000, 7150.0000, N'Omit item V27', 6),
    (N'cl-ps-07', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'MEC-DRN', N'Drainage survey pre and post completion', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit item V02', 7),
    (N'cl-ps-08', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'MEC-PLM', N'Existing mechanical system alterations', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit Item V14', 8),
    (N'cl-ps-09', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'ELE-STD', N'Existing electrical systems alterations', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit Item V14', 9),
    (N'cl-ps-10', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'CARP-WRD', N'Wardrobes and fitted furniture to bedroom', N'item', 1.0000, 3300.0000, 3300.0000, N'Omit item V31', 10),
    (N'cl-ps-11', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Luna''s ensuite storage/ fitted furniture', N'item', 1.0000, 2200.0000, 2200.0000, N'Omit item V31', 11),
    (N'cl-ps-12', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Closomat or similar WC', N'item', 1.0000, 6600.0000, 6600.0000, N'Omit Item V12', 12),
    (N'cl-ps-13', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'WH Vanity basin', N'item', 1.0000, 550.0000, 550.0000, N'Omit Item V12', 13),
    (N'cl-ps-14', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Shower tray, enclosure and shower', N'item', 1.0000, 3300.0000, 3300.0000, N'Omit Item V12', 14),
    (N'cl-ps-15', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Hi/lo bath', N'item', 1.0000, 19800.0000, 19800.0000, N'Omit Item V11', 15),
    (N'cl-ps-16', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Hoist installation - Luna''s Bedroom; Ensuite; Therapy Room; Snug', N'item', 1.0000, 19800.0000, 19800.0000, N'Omit Item V10', 16),
    (N'cl-ps-17', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SPEC-LFT', N'Pool hall alterations (including water supply, system, existing ductwork, existing heating & electrical systems, hoist, tiling)', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit item V35', 17),
    (N'cl-ps-18', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'TIL-STD', N'Internal floor tiling GF (BASED ON 50M2@£70/M^2)', N'item', 1.0000, 3850.0000, 3850.0000, N'Omit item V22', 18),
    (N'cl-ps-19', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SPEC-GAZ', N'Carport', N'item', 1.0000, 22000.0000, 22000.0000, N'Omit item V30', 19),
    (N'cl-ps-20', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'ENABLE-ASB', N'Asbestos survey', N'item', 1.0000, 2200.0000, 2200.0000, N'Omit item V07', 20),
    (N'cl-ps-21', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'ELE-STD', N'Light switches sockets to match existing system', N'item', 1.0000, 1650.0000, 1650.0000, N'Omit item V36', 21),
    (N'cl-ps-22', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Sanitary fittings to changing room and basement shower', N'item', 1.0000, 10450.0000, 10450.0000, N'Omit Item V12', 22),
    (N'cl-ps-23', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'SUP-TIL', N'Pool area additional floor tiling supply only', N'item', 1.0000, 12375.0000, 12375.0000, N'Omit item V25', 23),
    (N'cl-ps-24', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'WDR-TIM', N'Supply cost 3 no. pool storage doors', N'item', 1.0000, 2200.0000, 2200.0000, N'Omit item V33', 24),
    (N'cl-ps-25', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'ELE-EVC', N'Tesla charging point', N'item', 1.0000, 1650.0000, 1650.0000, N'Omit Item V08', 25),
    (N'cl-ps-26', N'd64c8a72f4b14eebbabd251b54564ef5', 1, N'', N'Provisional Sums', N'', N'', 1, N'MEC-SOL', N'Tesla Powerwall', N'item', 1.0000, 12650.0000, 12650.0000, N'Omit Item V08', 26),
    (N'cl-cont-01', N'd64c8a72f4b14eebbabd251b54564ef5', 2, N'', N'Contingency', N'', N'', 0, N'HAND-MSC', N'Contingency (NBS reference A54 / 590)', N'item', 1.0000, 20000.0000, 20000.0000, N'Omit item V56', 1)
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

-- Sanity check: the three seeded blocks should reconcile to the workbook.
SELECT
    SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 478174.60
    SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         -- 204965.00
    SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --  20000.00
    SUM(LineAmount) AS ContractSum                                               -- 703139.60
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'd64c8a72f4b14eebbabd251b54564ef5' AND ElementType IN (0, 1, 2)
  AND LineType NOT IN (3, 4);
GO
