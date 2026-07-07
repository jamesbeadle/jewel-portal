-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql. Remapped from the original JBB-* buckets on
-- 2026-07-07 -- audit trail: scripts/cost-code-remap-review.csv.
-- Seed: Albany Mews -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : 2 Albany Mews  (JBB-2026-006)
-- ProjectId: 6642a865d657422fa51c8bf4c800e973
--
-- Seeds the ORIGINAL contract scope only, taken from the Valuation 11 workbook
-- (April 26). Three blocks make up the Contract Sum, as per the By France
-- pattern:
--
--     Contract works    GBP 231,664.00
--     Provisional Sums  GBP 152,800.00
--     Contingency        GBP 30,000.00
--     ----------------------------------
--     Contract Sum      GBP 414,464.00
--
-- Variations (V01..V33, net GBP 40,823.67) are NOT seeded here -- they belong
-- in a separate variations seed. Per-valuation claim history (Valuation 01..11,
-- retention) is claim data (ValuationClaims/ClaimLines), not bill structure.
--
-- SectionCode/SectionName retain the workbook's NRM2 references; PS lines
-- retain their PC codes. CostCode maps each NRM2 section to the Jewel
-- cost-centre master (JBB-*, from seed-cost-centers.sql), consistent with the
-- By France seed; the workbook's own numeric codes (0001..0044) are dropped.
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (am-cw-NNN / am-ps-NN
-- / am-cont-NN). A re-run refreshes every field via MERGE. Variation lines for
-- this project are left untouched. Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'am-cw-001', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site Supervision', N'week', 30.0000, 1250.0000, 37500.0000, N'', 1),
    (N'am-cw-002', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'SCAFF-STD', N'Scaffolding', N'm2', 80.0000, 46.0000, 3680.0000, N'', 2),
    (N'am-cw-003', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 16.0000, 380.0000, 6080.0000, N'', 3),
    (N'am-cw-004', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection', N'item', 1.0000, 600.0000, 600.0000, N'', 4),
    (N'am-cw-005', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 30.0000, 90.0000, 2700.0000, N'', 5),
    (N'am-cw-006', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 6),
    (N'am-cw-007', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 30.0000, 125.0000, 3750.0000, N'', 7),
    (N'am-cw-008', N'6642a865d657422fa51c8bf4c800e973', 0, N'A10', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 1000.0000, 1000.0000, N'', 8),
    (N'am-cw-009', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 500.0000, 500.0000, N'', 9),
    (N'am-cw-010', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 600.0000, 600.0000, N'', 10),
    (N'am-cw-011', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 450.0000, 450.0000, N'', 11),
    (N'am-cw-012', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove kitchen & utility units, worktops & appliances', N'item', 1.0000, 240.0000, 240.0000, N'', 12),
    (N'am-cw-013', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items from WCs & bathrooms', N'item', 1.0000, 440.0000, 440.0000, N'', 13),
    (N'am-cw-014', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 6.0000, 20.0000, 120.0000, N'', 14),
    (N'am-cw-015', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal single skin walls', N'm2', 32.0000, 42.0000, 1344.0000, N'', 15),
    (N'am-cw-016', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing plasterboard ceiling - as required', N'm2', 32.0000, 12.0000, 384.0000, N'', 16),
    (N'am-cw-017', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove external doors & windows - to new areas', N'item', 1.0000, 250.0000, 250.0000, N'', 17),
    (N'am-cw-018', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-STS', N'Temporary propping & needling', N'item', 1.0000, 1000.0000, 1000.0000, N'Omit item V02', 18),
    (N'am-cw-019', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls - single skin', N'm2', 24.0000, 42.0000, 1008.0000, N'', 19),
    (N'am-cw-020', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls - cavity wall', N'm2', 3.0000, 80.0000, 240.0000, N'', 20),
    (N'am-cw-021', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Break up existing garage floor slab', N'm2', 30.0000, 32.0000, 960.0000, N'', 21),
    (N'am-cw-022', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove section of roof construction & covering', N'm2', 36.0000, 24.0000, 864.0000, N'', 22),
    (N'am-cw-023', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'MEC-DRN', N'Grub out redundnant drainage', N'item', 1.0000, 350.0000, 350.0000, N'', 23),
    (N'am-cw-024', N'6642a865d657422fa51c8bf4c800e973', 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing paving, shrubs, etc to areas of new work', N'm2', 20.0000, 12.0000, 240.0000, N'', 24),
    (N'am-cw-025', N'6642a865d657422fa51c8bf4c800e973', 0, N'D20', N'Excavation & filling', N'', N'', 0, N'SUB-EXC', N'Excavate to reduce levels & remove spoil : to new areas', N'm3', 12.0000, 110.0000, 1320.0000, N'', 25),
    (N'am-cw-026', N'6642a865d657422fa51c8bf4c800e973', 0, N'D20', N'Excavation & filling', N'', N'', 0, N'SUB-EXC', N'Excavate foundations 600 x 1000 mm & remove spoil', N'm3', 14.0000, 155.0000, 2170.0000, N'', 26),
    (N'am-cw-027', N'6642a865d657422fa51c8bf4c800e973', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 1850.0000, 1850.0000, N'', 27),
    (N'am-cw-028', N'6642a865d657422fa51c8bf4c800e973', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'New soakaway', N'item', 1.0000, 1150.0000, 1150.0000, N'Omit item V03', 28),
    (N'am-cw-029', N'6642a865d657422fa51c8bf4c800e973', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'Make connection into existing runs', N'item', 1.0000, 500.0000, 500.0000, N'', 29),
    (N'am-cw-030', N'6642a865d657422fa51c8bf4c800e973', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 3.0000, 130.0000, 390.0000, N'', 30),
    (N'am-cw-031', N'6642a865d657422fa51c8bf4c800e973', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 900.0000, 900.0000, N'', 31),
    (N'am-cw-032', N'6642a865d657422fa51c8bf4c800e973', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Wastes connections', N'nr', 14.0000, 88.0000, 1232.0000, N'', 32),
    (N'am-cw-033', N'6642a865d657422fa51c8bf4c800e973', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 1800.0000, 1800.0000, N'', 33),
    (N'am-cw-034', N'6642a865d657422fa51c8bf4c800e973', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'New manhole', N'nr', 1.0000, 725.0000, 725.0000, N'', 34),
    (N'am-cw-035', N'6642a865d657422fa51c8bf4c800e973', 0, N'E10', N'In situ concrete', N'', N'', 0, N'SUB-CON', N'Concrete in strip & pad foundations', N'm3', 10.0000, 180.0000, 1800.0000, N'', 35),
    (N'am-cw-036', N'6642a865d657422fa51c8bf4c800e973', 0, N'E10', N'In situ concrete', N'', N'', 0, N'SUB-CON', N'150 mm hardcore blinded with sand', N'm2', 38.0000, 42.0000, 1596.0000, N'Omit item V03', 36),
    (N'am-cw-037', N'6642a865d657422fa51c8bf4c800e973', 0, N'E10', N'In situ concrete', N'', N'', 0, N'SUB-CON', N'150 mm bed of concrete', N'm3', 6.0000, 295.0000, 1770.0000, N'Omit item V03', 37),
    (N'am-cw-038', N'6642a865d657422fa51c8bf4c800e973', 0, N'F1', N'Masonry walling', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in two skins of engineering brickwork & lean mix cavity fill', N'm2', 8.0000, 238.0000, 1904.0000, N'', 38),
    (N'am-cw-039', N'6642a865d657422fa51c8bf4c800e973', 0, N'F1', N'Masonry walling', N'', N'', 0, N'WPF-DMP', N'Damp proof course', N'm', 24.0000, 16.0000, 384.0000, N'', 39),
    (N'am-cw-040', N'6642a865d657422fa51c8bf4c800e973', 0, N'F1', N'Masonry walling', N'', N'', 0, N'INT-INW', N'Cavity wall in facing brickwork, 100 mm blockwork with 150 mm Celotex insulation to cavity', N'm2', 40.0000, 206.0000, 8240.0000, N'', 40),
    (N'am-cw-041', N'6642a865d657422fa51c8bf4c800e973', 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Single skin of facing brickwork to store', N'm2', 14.0000, 112.0000, 1568.0000, N'', 41),
    (N'am-cw-042', N'6642a865d657422fa51c8bf4c800e973', 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Supply of facing brickwork (£1.50 per brick)', N'm2', 54.0000, 90.0000, 4860.0000, N'', 42),
    (N'am-cw-043', N'6642a865d657422fa51c8bf4c800e973', 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Brick on edge over openings', N'm', 6.0000, 45.0000, 270.0000, N'', 43),
    (N'am-cw-044', N'6642a865d657422fa51c8bf4c800e973', 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'100 mm blockwork LB internal walls', N'm2', 26.0000, 86.0000, 2236.0000, N'', 44),
    (N'am-cw-045', N'6642a865d657422fa51c8bf4c800e973', 0, N'F30', N'Accessories', N'', N'', 0, N'MASON-BRK', N'Alterations to existing openings & making good reveals', N'item', 1.0000, 250.0000, 250.0000, N'', 45),
    (N'am-cw-046', N'6642a865d657422fa51c8bf4c800e973', 0, N'F30', N'Accessories', N'', N'', 0, N'MASON-BRK', N'Wall extension profiles', N'm', 12.0000, 32.0000, 384.0000, N'', 46),
    (N'am-cw-047', N'6642a865d657422fa51c8bf4c800e973', 0, N'F30', N'Accessories', N'', N'', 0, N'MASON-BRK', N'Thermabate cavity closers', N'm', 34.0000, 22.0000, 748.0000, N'', 47),
    (N'am-cw-048', N'6642a865d657422fa51c8bf4c800e973', 0, N'F30', N'Accessories', N'', N'', 0, N'MASON-BRK', N'Catnic CG90/100 lintel & tray over new openings', N'm', 8.0000, 122.0000, 976.0000, N'', 48),
    (N'am-cw-049', N'6642a865d657422fa51c8bf4c800e973', 0, N'F30', N'Accessories', N'', N'', 0, N'SUB-CON', N'Precast concrete lintels over internal openings', N'nr', 4.0000, 115.0000, 460.0000, N'Omit item V10 (No.2)', 49),
    (N'am-cw-050', N'6642a865d657422fa51c8bf4c800e973', 0, N'G10', N'Structural steel framing', N'', N'', 1, N'STR-STL', N'Structural steels & associated work - Provisional sum', N'item', 1.0000, 5000.0000, 5000.0000, N'Omit item V02', 50),
    (N'am-cw-051', N'6642a865d657422fa51c8bf4c800e973', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-CUT', N'50 x 150 mm timber roof rafters (to new pitched roof)', N'm2', 52.0000, 98.0000, 5096.0000, N'', 51),
    (N'am-cw-052', N'6642a865d657422fa51c8bf4c800e973', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-1FX', N'50 x 150 mm timber ceiling joists', N'm2', 26.0000, 98.0000, 2548.0000, N'Omit item V13 (15 sqm)', 52),
    (N'am-cw-053', N'6642a865d657422fa51c8bf4c800e973', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'ROOF-FLT', N'50 x 150 mm timber roof joists & plywood (to flat roof)', N'm2', 5.0000, 125.0000, 625.0000, N'', 53),
    (N'am-cw-054', N'6642a865d657422fa51c8bf4c800e973', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-1FX', N'Joist hangers', N'nr', 62.0000, 7.0000, 434.0000, N'', 54),
    (N'am-cw-055', N'6642a865d657422fa51c8bf4c800e973', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-1FX', N'Galvanised restaint straps', N'nr', 18.0000, 15.0000, 270.0000, N'', 55),
    (N'am-cw-056', N'6642a865d657422fa51c8bf4c800e973', 0, N'H1', N'Roofing', N'', N'', 0, N'SUB-CON', N'Breatherable membrane, battens & concrete tiles', N'm2', 52.0000, 88.0000, 4576.0000, N'', 56),
    (N'am-cw-057', N'6642a865d657422fa51c8bf4c800e973', 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'300 mm mineral insulation over ceiling joists', N'm2', 26.0000, 44.0000, 1144.0000, N'', 57),
    (N'am-cw-058', N'6642a865d657422fa51c8bf4c800e973', 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Ridge / hip tiles', N'm', 8.0000, 55.0000, 440.0000, N'', 58),
    (N'am-cw-059', N'6642a865d657422fa51c8bf4c800e973', 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Separate cost for supply of tiles (£0.60 each)', N'm2', 52.0000, 36.0000, 1872.0000, N'', 59),
    (N'am-cw-060', N'6642a865d657422fa51c8bf4c800e973', 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Roof ventilators', N'nr', 4.0000, 85.0000, 340.0000, N'', 60),
    (N'am-cw-061', N'6642a865d657422fa51c8bf4c800e973', 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-FSU', N'Fascia / soffit to new areas', N'm', 34.0000, 46.0000, 1564.0000, N'', 61),
    (N'am-cw-062', N'6642a865d657422fa51c8bf4c800e973', 0, N'H70', N'Leadwork', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining / soakers / aprons', N'item', 1.0000, 1000.0000, 1000.0000, N'', 62),
    (N'am-cw-063', N'6642a865d657422fa51c8bf4c800e973', 0, N'J40', N'Flexible sheet waterproofing', N'', N'', 0, N'WPF-DMP', N'Damp proof membranes 1200 g', N'm2', 38.0000, 16.0000, 608.0000, N'', 63),
    (N'am-cw-064', N'6642a865d657422fa51c8bf4c800e973', 0, N'J42', N'Single layer polymeric sheet roof coverings', N'', N'', 0, N'ROOF-FLT', N'150 mm Celotex insulation to flat roof deck', N'm2', 12.0000, 48.0000, 576.0000, N'', 64),
    (N'am-cw-065', N'6642a865d657422fa51c8bf4c800e973', 0, N'J42', N'Single layer polymeric sheet roof coverings', N'', N'', 0, N'ROOF-FLT', N'Plywood deck', N'm2', 12.0000, 42.0000, 504.0000, N'', 65),
    (N'am-cw-066', N'6642a865d657422fa51c8bf4c800e973', 0, N'J42', N'Single layer polymeric sheet roof coverings', N'', N'', 0, N'ROOF-FLT', N'Sarnifli single ply membrane', N'm2', 12.0000, 165.0000, 1980.0000, N'', 66),
    (N'am-cw-067', N'6642a865d657422fa51c8bf4c800e973', 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'MEC-DRN', N'New upvc guttering & rainwater pipework', N'm', 28.0000, 34.0000, 952.0000, N'', 67),
    (N'am-cw-068', N'6642a865d657422fa51c8bf4c800e973', 0, N'K1', N'Floors', N'', N'', 0, N'INT-INF', N'100 mm Celotex insulation to new floor slab', N'm2', 34.0000, 42.0000, 1428.0000, N'', 68),
    (N'am-cw-069', N'6642a865d657422fa51c8bf4c800e973', 0, N'M10', N'Cement based levelling screeds', N'', N'', 0, N'FLR-SCR', N'75 mm sand / cement floor screed', N'm2', 38.0000, 68.0000, 2584.0000, N'', 69),
    (N'am-cw-070', N'6642a865d657422fa51c8bf4c800e973', 0, N'P10', N'Sundry insulation', N'', N'', 0, N'CARP-1FX', N'Plywood boxing & insulation to internal pipes', N'item', 1.0000, 685.0000, 685.0000, N'', 70),
    (N'am-cw-071', N'6642a865d657422fa51c8bf4c800e973', 0, N'L10', N'Windows/ rooflights/ screens/ louvres - Provisional', N'', N'', 1, N'WDR-SPG', N'650 x 1050 mm upvc window - W01', N'nr', 1.0000, 725.0000, 725.0000, N'Omit item V07', 71),
    (N'am-cw-072', N'6642a865d657422fa51c8bf4c800e973', 0, N'L10', N'Windows/ rooflights/ screens/ louvres - Provisional', N'', N'', 1, N'WDR-SPG', N'Reglazed with obscure glazing', N'nr', 2.0000, 550.0000, 1100.0000, N'Omit item V07', 72),
    (N'am-cw-073', N'6642a865d657422fa51c8bf4c800e973', 0, N'L10', N'Windows/ rooflights/ screens/ louvres - Provisional', N'', N'', 1, N'ROOF-FLT', N'750 x 750 mm flat roof lights', N'nr', 2.0000, 1350.0000, 2700.0000, N'Omit item V18', 73),
    (N'am-cw-074', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 1, N'WDR-ALU', N'1585 x 2100 mm aluminium entrance door', N'nr', 1.0000, 1000.0000, 1000.0000, N'Omit item V07', 74),
    (N'am-cw-075', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 1, N'WDR-ALU', N'1585 x 2100 mm aluminium French doors', N'nr', 1.0000, 1400.0000, 1400.0000, N'Omit item V07', 75),
    (N'am-cw-076', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 1, N'WDR-ALU', N'2009 x 2100 mm aluminium external door with window', N'nr', 1.0000, 700.0000, 700.0000, N'Omit item V07', 76),
    (N'am-cw-077', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 1, N'WDR-ALU', N'2148 x 2100 mm aluminium bifolding doors', N'nr', 1.0000, 1500.0000, 1500.0000, N'Omit item V07', 77),
    (N'am-cw-078', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 1, N'WDR-TIM', N'2100 x 2260 mm composite store room door', N'nr', 1.0000, 600.0000, 600.0000, N'Omit item V07', 78),
    (N'am-cw-079', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 1, N'WDR-TIM', N'Timber garden gate', N'nr', 1.0000, 300.0000, 300.0000, N'Omit item V27', 79),
    (N'am-cw-080', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 0, N'SUP-DOR', N'910 mm Internal door lining & single door ( £200 supply )', N'nr', 1.0000, 1050.0000, 1050.0000, N'', 80),
    (N'am-cw-081', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 0, N'SUP-DOR', N'1030 mm Internal door lining & single door ( £250 supply )', N'nr', 6.0000, 505.0000, 3030.0000, N'', 81),
    (N'am-cw-082', N'6642a865d657422fa51c8bf4c800e973', 0, N'L20', N'Doors - Provisional for Externals', N'', N'', 0, N'SUP-DOR', N'1350 mm Internal door lining & double door ( £300 supply )', N'nr', 1.0000, 555.0000, 555.0000, N'', 82),
    (N'am-cw-083', N'6642a865d657422fa51c8bf4c800e973', 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Hot & cold water supply', N'nr', 19.0000, 180.0000, 3420.0000, N'', 83),
    (N'am-cw-084', N'6642a865d657422fa51c8bf4c800e973', 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-BLR', N'New central heating boiler & associated pipework', N'nr', 1.0000, 4250.0000, 4250.0000, N'', 84),
    (N'am-cw-085', N'6642a865d657422fa51c8bf4c800e973', 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Megaflo hot water cylinder', N'nr', 1.0000, 2780.0000, 2780.0000, N'', 85),
    (N'am-cw-086', N'6642a865d657422fa51c8bf4c800e973', 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Radiators with TRVs', N'nr', 3.0000, 590.0000, 1770.0000, N'', 86),
    (N'am-cw-087', N'6642a865d657422fa51c8bf4c800e973', 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Towel rails with TRVS', N'nr', 3.0000, 650.0000, 1950.0000, N'', 87),
    (N'am-cw-088', N'6642a865d657422fa51c8bf4c800e973', 0, N'U90', N'General ventilation', N'', N'', 0, N'MEC-VNT', N'Extract fan', N'nr', 5.0000, 325.0000, 1625.0000, N'', 88),
    (N'am-cw-089', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Consumer unit', N'nr', 1.0000, 1550.0000, 1550.0000, N'', 89),
    (N'am-cw-090', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 8.0000, 125.0000, 1000.0000, N'', 90),
    (N'am-cw-091', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 10.0000, 100.0000, 1000.0000, N'', 91),
    (N'am-cw-092', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Cooker switch', N'nr', 1.0000, 120.0000, 120.0000, N'', 92),
    (N'am-cw-093', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 2.0000, 95.0000, 190.0000, N'', 93),
    (N'am-cw-094', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Recessed light fitting', N'nr', 70.0000, 108.0000, 7560.0000, N'', 94),
    (N'am-cw-095', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Pendant lights', N'nr', 16.0000, 78.0000, 1248.0000, N'', 95),
    (N'am-cw-096', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'External lights', N'nr', 1.0000, 175.0000, 175.0000, N'', 96),
    (N'am-cw-097', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 16.0000, 40.0000, 640.0000, N'', 97),
    (N'am-cw-098', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 5.0000, 150.0000, 750.0000, N'', 98),
    (N'am-cw-099', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Carbon monoxide detector', N'nr', 1.0000, 118.0000, 118.0000, N'', 99),
    (N'am-cw-100', N'6642a865d657422fa51c8bf4c800e973', 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 100),
    (N'am-cw-101', N'6642a865d657422fa51c8bf4c800e973', 0, N'W90', N'Communications & security systems', N'', N'', 0, N'PRELIMS-SEC', N'All assoisated works', N'nr', 1.0000, 1000.0000, 1000.0000, N'', 101),
    (N'am-cw-102', N'6642a865d657422fa51c8bf4c800e973', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings', N'm2', 54.0000, 20.0000, 1080.0000, N'', 102),
    (N'am-cw-103', N'6642a865d657422fa51c8bf4c800e973', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber internal stud walls', N'm2', 24.0000, 62.0000, 1488.0000, N'', 103),
    (N'am-cw-104', N'6642a865d657422fa51c8bf4c800e973', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'50 mm rockwool insulation between stud walls', N'm2', 24.0000, 16.0000, 384.0000, N'', 104),
    (N'am-cw-105', N'6642a865d657422fa51c8bf4c800e973', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'11 mm plywood to stud walls', N'm2', 20.0000, 18.0000, 360.0000, N'', 105),
    (N'am-cw-106', N'6642a865d657422fa51c8bf4c800e973', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork & studs', N'm2', 154.0000, 20.0000, 3080.0000, N'', 106),
    (N'am-cw-107', N'6642a865d657422fa51c8bf4c800e973', 0, N'N13', N'Sanitry appliances', N'', N'', 0, N'SUP-APP', N'Fix only - WC to ground floor WC', N'nr', 1.0000, 340.0000, 340.0000, N'', 107),
    (N'am-cw-108', N'6642a865d657422fa51c8bf4c800e973', 0, N'N13', N'Sanitry appliances', N'', N'', 0, N'SUP-APP', N'Fix only - Wash hand basin to ground floor WC', N'nr', 1.0000, 320.0000, 320.0000, N'', 108),
    (N'am-cw-109', N'6642a865d657422fa51c8bf4c800e973', 0, N'N13', N'Sanitry appliances', N'', N'', 0, N'SUP-APP', N'Fix only - Sanitry items to wetroom', N'nr', 1.0000, 1500.0000, 1500.0000, N'', 109),
    (N'am-cw-110', N'6642a865d657422fa51c8bf4c800e973', 0, N'N13', N'Sanitry appliances', N'', N'', 0, N'SUP-APP', N'Fix only - WC to second floor', N'nr', 1.0000, 340.0000, 340.0000, N'', 110),
    (N'am-cw-111', N'6642a865d657422fa51c8bf4c800e973', 0, N'N13', N'Sanitry appliances', N'', N'', 0, N'SUP-APP', N'Fix only - Wash hand basin to second floor', N'nr', 1.0000, 320.0000, 320.0000, N'', 111),
    (N'am-cw-112', N'6642a865d657422fa51c8bf4c800e973', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-RDR', N'3 mm skim to ceilings', N'm2', 54.0000, 20.0000, 1080.0000, N'', 112),
    (N'am-cw-113', N'6642a865d657422fa51c8bf4c800e973', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-RDR', N'Make good existing areas as required', N'item', 1.0000, 1000.0000, 1000.0000, N'', 113),
    (N'am-cw-114', N'6642a865d657422fa51c8bf4c800e973', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-RDR', N'3 mm skim to new walls', N'm2', 154.0000, 20.0000, 3080.0000, N'', 114),
    (N'am-cw-115', N'6642a865d657422fa51c8bf4c800e973', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames (£4/m supply)', N'm', 80.0000, 14.0000, 1120.0000, N'', 115),
    (N'am-cw-116', N'6642a865d657422fa51c8bf4c800e973', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 82.0000, 28.0000, 2296.0000, N'', 116),
    (N'am-cw-117', N'6642a865d657422fa51c8bf4c800e973', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 4.0000, 44.0000, 176.0000, N'', 117),
    (N'am-cw-118', N'6642a865d657422fa51c8bf4c800e973', 0, N'P21', N'Door ironmongery', N'', N'', 0, N'SUP-IRO', N'Fix only - Door ironmongery', N'nr', 8.0000, 175.0000, 1400.0000, N'', 118),
    (N'am-cw-119', N'6642a865d657422fa51c8bf4c800e973', 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 134.0000, 18.0000, 2412.0000, N'', 119),
    (N'am-cw-120', N'6642a865d657422fa51c8bf4c800e973', 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 276.0000, 16.0000, 4416.0000, N'', 120),
    (N'am-cw-121', N'6642a865d657422fa51c8bf4c800e973', 0, N'M60', N'Painting', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm2', 32.0000, 36.0000, 1152.0000, N'', 121),
    (N'am-cw-122', N'6642a865d657422fa51c8bf4c800e973', 0, N'M60', N'Painting', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 245.0000, 9.0000, 2205.0000, N'', 122),
    (N'am-cw-123', N'6642a865d657422fa51c8bf4c800e973', 0, N'Z22', N'Sealants', N'', N'', 0, N'HAND-MSC', N'Sealants', N'item', 1.0000, 500.0000, 500.0000, N'', 123),
    (N'am-cw-124', N'6642a865d657422fa51c8bf4c800e973', 0, N'M50', N'Rubber / cork / etc', N'', N'', 0, N'FLR-LVT', N'Karndean flooring (£50/m supply)', N'm2', 112.0000, 105.0000, 11760.0000, N'Omit Item V25', 124),
    (N'am-cw-125', N'6642a865d657422fa51c8bf4c800e973', 0, N'P30', N'Trenches, pipeways for engineering services', N'', N'', 1, N'UTIL-TRN', N'All assosiated work - Provisional sum', N'item', 1.0000, 1500.0000, 1500.0000, N'Omit item V03', 125),
    (N'am-cw-126', N'6642a865d657422fa51c8bf4c800e973', 0, N'Q10', N'Kerbs/ edgings/ channels/ paving accessories', N'', N'', 0, N'EXTW-PAV', N'Aco threshold drain', N'm', 12.0000, 145.0000, 1740.0000, N'', 126),
    (N'am-cw-127', N'6642a865d657422fa51c8bf4c800e973', 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'SUB-CON', N'Hardcore sub base to new patio area', N'm2', 25.0000, 44.0000, 1100.0000, N'', 127),
    (N'am-ps-01', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC08', N'Provisional Sums', N'', N'', 1, N'ENABLE-ASB', N'Asbestos', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit Item V01', 1),
    (N'am-ps-02', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC09', N'Provisional Sums', N'', N'', 1, N'MEC-DRN', N'Existing drainage CCTV and remedial works', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit item V03', 2),
    (N'am-ps-03', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC21', N'Provisional Sums', N'', N'', 1, N'HAND-SPE', N'Structural Engineers details', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit item V03', 3),
    (N'am-ps-04', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC14', N'Provisional Sums', N'', N'', 1, N'UTIL-STD', N'Moving existing meter', N'item', 1.0000, 1600.0000, 1600.0000, N'Omit item V04', 4),
    (N'am-ps-05', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC11', N'Provisional Sums', N'', N'', 1, N'ELE-ALM', N'Intruder Alarm', N'item', 1.0000, 4300.0000, 4300.0000, N'Omit item V05', 5),
    (N'am-ps-06', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC12', N'Provisional Sums', N'', N'', 1, N'ELE-FIR', N'Fire and smoke alarm adaptation', N'item', 1.0000, 3500.0000, 3500.0000, N'Omit item V05', 6),
    (N'am-ps-07', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC18', N'Provisional Sums', N'', N'', 1, N'MEC-AC', N'Air conditioning unit to SW therapy (supply)', N'item', 1.0000, 9900.0000, 9900.0000, N'Omit item V10', 7),
    (N'am-ps-08', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC19', N'Provisional Sums', N'', N'', 1, N'MEC-BLR', N'Boiler Adaptation works unknown', N'item', 1.0000, 3800.0000, 3800.0000, N'Omit Item V12', 8),
    (N'am-ps-09', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC05', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'Main kitchen units, appliances and worktop', N'item', 1.0000, 36800.0000, 36800.0000, N'Omit item V09', 9),
    (N'am-ps-10', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC01', N'Provisional Sums', N'', N'', 1, N'SUP-TIL', N'Wall tiling supply only (BASED ON 75M2 @ £70.00 / M2)', N'item', 1.0000, 7525.0000, 7525.0000, N'Omit item V24', 10),
    (N'am-ps-11', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC02', N'Provisional Sums', N'', N'', 1, N'SUP-TIL', N'Floor tiling supply only(BASED ON 12M2 @ £100.00 / M2)', N'item', 1.0000, 2250.0000, 2250.0000, N'Omit item V24', 11),
    (N'am-ps-12', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC16', N'Provisional Sums', N'', N'', 1, N'SPEC-LFT', N'Lift', N'item', 1.0000, 22000.0000, 22000.0000, N'Omit item V08', 12),
    (N'am-ps-13', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC04', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Specialist rise and fall washbasin', N'item', 1.0000, 7125.0000, 7125.0000, N'Omit item V11', 13),
    (N'am-ps-14', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC07', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Shower tray and enclosure in seondfloor bathroom supply only. Fitting price to be included elsewhere.', N'item', 1.0000, 2700.0000, 2700.0000, N'Omit item V11', 14),
    (N'am-ps-15', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC15', N'Provisional Sums', N'', N'', 1, N'PRELIMS-WC', N'Gerebit toilet or similar and support rails', N'item', 1.0000, 8100.0000, 8100.0000, N'Omit item V11', 15),
    (N'am-ps-16', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC17', N'Provisional Sums', N'', N'', 1, N'SUP-SAN', N'Specialist bath', N'item', 1.0000, 11000.0000, 11000.0000, N'Omit item V11', 16),
    (N'am-ps-17', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC03', N'Provisional Sums', N'', N'', 1, N'SUP-IRO', N'Internal Door ironmongery (£125 per door)', N'item', 1.0000, 1100.0000, 1100.0000, N'Omit item V31', 17),
    (N'am-ps-18', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC10', N'Provisional Sums', N'', N'', 1, N'WIN-BLD', N'Blinds and curtains', N'item', 1.0000, 7000.0000, 7000.0000, N'Omit item V32', 18),
    (N'am-ps-19', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC13', N'Provisional Sums', N'', N'', 1, N'EXTW-LND', N'Soft landscaping', N'item', 1.0000, 3200.0000, 3200.0000, N'Omit item V30', 19),
    (N'am-ps-20', N'6642a865d657422fa51c8bf4c800e973', 1, N'PC20', N'Provisional Sums', N'', N'', 1, N'EXTW-PAV', N'Supply for paving slab Type TBC', N'item', 1.0000, 4400.0000, 4400.0000, N'Omit item V30', 20),
    (N'am-cont-01', N'6642a865d657422fa51c8bf4c800e973', 2, N'', N'Contingency', N'', N'', 0, N'HAND-MSC', N'Contingency Budget', N'item', 1.0000, 30000.0000, 30000.0000, N'Omit item V33', 1)
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
    SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 231664.00
    SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         -- 152800.00
    SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --  30000.00
    SUM(LineAmount) AS ContractSum                                               -- 414464.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'6642a865d657422fa51c8bf4c800e973' AND ElementType IN (0, 1, 2)
  AND LineType NOT IN (3, 4);
GO
