-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: Vets (School House, Slough) -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : School House Vets, Elmshott Lane, Slough, SL1 5RB
--           Construction Works Phase 3 - Proposed extensions & alterations
-- ProjectId: resolved at run time by site-name matcher 'vets'
--
-- Seeds the CONTRACT SCOPE only, taken from the "Vets Valuation 6 - 3rd Phase"
-- workbook. A single Contract Works block makes up the Contract Sum; there is
-- no separate Provisional Sum or Contingency block:
--
--     Contract works        GBP  91,640.75
--     ----------------------------------
--     Contract Sum          GBP  91,640.75
--     Net Variations        GBP  15,956.95   (seeded by seed-vets-variations.sql)
--     ----------------------------------
--     Revised Contract Sum  GBP 107,597.70
--
-- The workbook has no NRM2 or section numbering; sections are its loose
-- headings (Prelims, Landscaping, Clean on Completion) plus three unheaded
-- groups named here from context (X-Ray Door, Internal Doors, Joinery &
-- Fittings). SectionCode is assigned sequentially (01..06) in workbook order.
--
-- Judgement calls:
--   * vt-cw-009 "OH&P on above" (X-ray door): the workbook's rate cell reads
--     the text 'item' with amount GBP 1,720.00, so Quantity=1, Rate=amount
--     (workbook amount kept as the truth).
--   * "Decorate existing front metal railings" carries the workbook comment
--     'Omit Item' -- it stays PRICED here; the omission is part of V03's net
--     in seed-vets-variations.sql.
--   * No workbook rows were skipped: every priced row is transcribed and the
--     block reconciles to the stated Contract Sum to the penny.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (vt-cw-NNN). A re-run
-- refreshes every field via MERGE. Variation lines for this project are left
-- untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'vets'
       OR LOWER(REPLACE(Name, ' ', '')) = 'vets'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'vets' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Vets (School House, Slough) — no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'vt-cw-001', @ProjectId, 0, N'01', N'Prelims', N'', N'', 0, N'PRELIMS-PMG', N'Project Manager', N'week', 6.0000, 750.0000, 4500.0000, N'', 1),
        (N'vt-cw-002', @ProjectId, 0, N'01', N'Prelims', N'', N'', 0, N'PRELIMS-SMG', N'Site Manager', N'week', 6.0000, 1250.0000, 7500.0000, N'', 2),
        (N'vt-cw-003', @ProjectId, 0, N'01', N'Prelims', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 6.0000, 90.0000, 540.0000, N'', 3),
        (N'vt-cw-004', @ProjectId, 0, N'01', N'Prelims', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 1.0000, 600.0000, 600.0000, N'', 4),
        (N'vt-cw-005', @ProjectId, 0, N'01', N'Prelims', N'', N'', 0, N'ENABLE-SKP', N'Rubbish Removal', N'item', 6.0000, 320.0000, 1920.0000, N'', 5),
        (N'vt-cw-006', @ProjectId, 0, N'01', N'Prelims', N'', N'', 0, N'PRELIMS-PRO', N'Floor Protection', N'item', 1.0000, 2000.0000, 2000.0000, N'', 6),
        (N'vt-cw-007', @ProjectId, 0, N'01', N'Prelims', N'', N'', 0, N'ELE-STD', N'Generator Hire - Temp Electrics', N'item', 1.0000, 2500.0000, 2500.0000, N'', 7),
        (N'vt-cw-008', @ProjectId, 0, N'02', N'X-Ray Door', N'', N'', 0, N'CARP-DOR', N'X-ray door Installation', N'nr', 1.0000, 1200.0000, 1200.0000, N'', 8),
        (N'vt-cw-009', @ProjectId, 0, N'02', N'X-Ray Door', N'', N'', 0, N'CARP-DOR', N'OH&P on above', N'nr', 1.0000, 1720.0000, 1720.0000, N'Workbook rate reads ''item''; workbook amount kept', 9),
        (N'vt-cw-010', @ProjectId, 0, N'03', N'Internal Doors', N'', N'', 0, N'CARP-DOR', N'Install Internal non vision doors', N'nr', 5.0000, 275.0000, 1375.0000, N'', 10),
        (N'vt-cw-011', @ProjectId, 0, N'03', N'Internal Doors', N'', N'', 0, N'SUP-DOR', N'Supply of the above', N'nr', 5.0000, 150.0000, 750.0000, N'', 11),
        (N'vt-cw-012', @ProjectId, 0, N'03', N'Internal Doors', N'', N'', 0, N'CARP-DOR', N'Install internal glazed paneled doors', N'nr', 11.0000, 350.0000, 3850.0000, N'', 12),
        (N'vt-cw-013', @ProjectId, 0, N'03', N'Internal Doors', N'', N'', 0, N'SUP-DOR', N'Supply of the above', N'nr', 11.0000, 210.0000, 2310.0000, N'', 13),
        (N'vt-cw-014', @ProjectId, 0, N'03', N'Internal Doors', N'', N'', 0, N'CARP-DOR', N'Remove installed frames for cages - include for frame install', N'nr', 3.0000, 320.0000, 960.0000, N'', 14),
        (N'vt-cw-015', @ProjectId, 0, N'03', N'Internal Doors', N'', N'', 0, N'CARP-DOR', N'Install Client supplied cage doors', N'nr', 3.0000, 500.0000, 1500.0000, N'', 15),
        (N'vt-cw-016', @ProjectId, 0, N'04', N'Joinery & Fittings', N'', N'', 0, N'ELE-STD', N'Joinery LED lighting', N'nr', 1.0000, 750.0000, 750.0000, N'', 16),
        (N'vt-cw-017', @ProjectId, 0, N'04', N'Joinery & Fittings', N'', N'', 0, N'ELE-STD', N'Under cabinet LED lighting', N'nr', 1.0000, 750.0000, 750.0000, N'', 17),
        (N'vt-cw-018', @ProjectId, 0, N'04', N'Joinery & Fittings', N'', N'', 0, N'CARP-KIT', N'Install Prep room units, worktops & appliances', N'item', 1.0000, 8000.0000, 8000.0000, N'', 18),
        (N'vt-cw-019', @ProjectId, 0, N'04', N'Joinery & Fittings', N'', N'', 0, N'CARP-KIT', N'OH&P on above', N'nr', 1.0000, 1900.0000, 1900.0000, N'', 19),
        (N'vt-cw-020', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'DEC-STD', N'Decorate existing front metal railings', N'm', 25.0000, 52.0000, 1300.0000, N'Omit Item', 20),
        (N'vt-cw-021', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'SUB-EXC', N'Excavation 350mm depth across site & waste removal', N'item', 1.0000, 7200.0000, 7200.0000, N'', 21),
        (N'vt-cw-022', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'SUB-DRN', N'Supply and Install Aco Channel', N'item', 1.0000, 2790.0000, 2790.0000, N'', 22),
        (N'vt-cw-023', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-PAV', N'Supply and Lay Geotextile Membrane', N'item', 1.0000, 542.3400, 542.3400, N'', 23),
        (N'vt-cw-024', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-PAV', N'Supply and Lay 150mm TypeOne', N'item', 1.0000, 877.1800, 877.1800, N'', 24),
        (N'vt-cw-025', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-PAV', N'Compaction', N'item', 1.0000, 94.3200, 94.3200, N'', 25),
        (N'vt-cw-026', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-PAV', N'50mm Sand Blinding', N'item', 1.0000, 330.1200, 330.1200, N'', 26),
        (N'vt-cw-027', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-PAV', N'Supply and Lay Standard 600 x 600mm Standard Concrete Slabs', N'item', 1.0000, 7074.0000, 7074.0000, N'', 27),
        (N'vt-cw-028', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-LND', N'Supply and Install 20mm Golden Gravel', N'item', 1.0000, 5067.3000, 5067.3000, N'', 28),
        (N'vt-cw-029', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-LND', N'Level and Compact Gravel', N'item', 1.0000, 723.9000, 723.9000, N'', 29),
        (N'vt-cw-030', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-PAV', N'250mm MOT Type1', N'item', 1.0000, 4488.1800, 4488.1800, N'', 30),
        (N'vt-cw-031', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-TRF', N'Supply and Lay 100mm Topsoil and Seed', N'item', 1.0000, 1521.4500, 1521.4500, N'', 31),
        (N'vt-cw-032', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-TRF', N'Astro Turf', N'item', 1.0000, 4293.0000, 4293.0000, N'', 32),
        (N'vt-cw-033', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-LND', N'Supply and Install Bike Hoops', N'item', 1.0000, 468.0000, 468.0000, N'', 33),
        (N'vt-cw-034', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-LND', N'Timber Edging strips', N'item', 1.0000, 1260.0000, 1260.0000, N'', 34),
        (N'vt-cw-035', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-SHD', N'Build client supplied timber shed', N'item', 1.0000, 950.0000, 950.0000, N'', 35),
        (N'vt-cw-036', @ProjectId, 0, N'05', N'Landscaping', N'', N'', 0, N'EXTW-LND', N'OH&P on above', N'item', 1.0000, 7535.9600, 7535.9600, N'', 36),
        (N'vt-cw-037', @ProjectId, 0, N'06', N'Clean on Completion', N'', N'', 0, N'HAND-CLI', N'Clean on Completion', N'item', 1.0000, 500.0000, 500.0000, N'', 37)
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

    PRINT 'Vets (School House, Slough): valuation lines merged.';

    -- Sanity check: the seeded block should reconcile to the workbook.
    SELECT
        SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 91640.75
        SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --     0.00
        SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --     0.00
        SUM(LineAmount) AS ContractSum                                               -- 91640.75
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
      AND LineType NOT IN (3, 4);

    COMMIT TRAN;
END
GO
