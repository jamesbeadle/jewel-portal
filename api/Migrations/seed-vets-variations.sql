-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: Vets (School House, Slough) -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : School House Vets, Elmshott Lane, Slough, SL1 5RB
--           Construction Works Phase 3 - Proposed extensions & alterations
-- ProjectId: resolved at run time by site-name matcher 'vets'
--
-- Companion to seed-vets-valuation.sql, which seeds ONLY the original contract
-- scope (Contract Sum GBP 91,640.75). This file adds the post-contract
-- VARIATION ORDERS from the "Vets Valuation 6 - 3rd Phase" workbook,
-- reconciling to its variations register:
--
--     Contract Sum            GBP  91,640.75
--     Net Variations          GBP  15,956.95
--     ----------------------------------------
--     Revised Contract Sum    GBP 107,597.70
--
-- MODEL NOTE (unified variation orders, post-20260723120000_UnifyVariationOrders)
-- Each variation order is ONE row in [VariationOrderQuotes]; the dropped
-- [VariationOrders] table is NOT written. On the valuation report a VO shows
-- as a SINGLE summary line, so we seed ONE ValuationLineItem per VO whose
-- LineAmount is the NET of that VO's workbook rows (Quantity 1 x Rate = net).
-- V03 nets an omit (-1,300.00 railings decoration) against a new landscaping
-- item (+3,199.95); V08 is a pure omit (-6,000.00 prelims deduction) and is
-- seeded LineType 2 with a negative amount. All eight VOs are claimed 100% in
-- the register, so all are Approved (Status 2); none are Declined.
--
-- The workbook gives no VO dates, so CreatedAt/IssuedAt/ApprovedAt are
-- plausible seed dates spaced to sit just before the valuation month each VO
-- first appears claimed in (V01-V05 by Valuation 4, V06-V08 by Valuation 5),
-- per the Albany template's monthly-spacing approach.
--
-- Judgement calls: the workbook spells V01's register row 'Lanscaping -
-- Additional Items'; the headline corrects it to 'Landscaping'. No workbook
-- rows were skipped; the register's own addition matches the stated Net
-- Variations exactly, so no penny adjustment was needed.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--
-- Idempotent: keyed on stable ids (vt-voq-vNN / vt-vo-vNN). A re-run refreshes
-- every field via MERGE. The contract lines seeded by seed-vets-valuation.sql
-- are left untouched. Safe to run repeatedly.
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
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'vt-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Landscaping - Additional Items', N'Landscaping - Additional Items', 2, NULL, NULL, 3690.0000, N'V01', 3690.0000, N'EXTW-LND', '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', '2026-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'vt-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Joinery Boxing MHVR', N'Joinery Boxing MHVR', 2, NULL, NULL, 312.0000, N'V02', 312.0000, N'CARP-2FX', '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', '2026-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'vt-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Landscaping additional items & omit railings decoration', N'Landscaping additional items & omit railings decoration', 2, NULL, NULL, 1899.9500, N'V03', 1899.9500, N'EXTW-LND', '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', '2026-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'vt-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'UFH Boxing & Covering', N'UFH Boxing & Covering', 2, NULL, NULL, 585.0000, N'V04', 585.0000, N'CARP-2FX', '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', '2026-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'vt-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'Extended prelims - Project & Site Manager, rubbish removal (5 weeks)', N'Extended prelims - Project & Site Manager, rubbish removal (5 weeks)', 2, NULL, NULL, 10960.0000, N'V05', 10960.0000, N'PRELIMS-SMG', '2026-03-25', N'seed@jewelgroup.co.uk', '2026-04-01', '2026-04-15', N'seed@jewelgroup.co.uk', NULL),
        (N'vt-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Mains Water - Temp Works carried out by Jewel', N'Mains Water - Temp Works carried out by Jewel', 2, NULL, NULL, 2110.0000, N'V06', 2110.0000, N'UTIL-STD', '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', '2026-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'vt-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Additional electrical works for the laundry room', N'Additional electrical works for the laundry room', 2, NULL, NULL, 2400.0000, N'V07', 2400.0000, N'ELE-STD', '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', '2026-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'vt-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Prelims deduction - Project & Site Manager (3 weeks)', N'Prelims deduction - Project & Site Manager (3 weeks)', 2, NULL, NULL, -6000.0000, N'V08', -6000.0000, N'PRELIMS-SMG', '2026-04-20', N'seed@jewelgroup.co.uk', '2026-04-27', '2026-05-11', N'seed@jewelgroup.co.uk', NULL)
    ) AS source (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
                 Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
                 VariationRef, Value, CostCode, CreatedAt, CreatedByEmail, IssuedAt,
                 ApprovedAt, ApprovedByEmail, RejectedAt)
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
        VariationRef            = source.VariationRef,
        Value                   = source.Value,
        CostCode                = source.CostCode,
        CreatedAt               = source.CreatedAt,
        CreatedByEmail          = source.CreatedByEmail,
        IssuedAt                = source.IssuedAt,
        ApprovedAt              = source.ApprovedAt,
        ApprovedByEmail         = source.ApprovedByEmail,
        RejectedAt              = source.RejectedAt
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
                Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
                VariationRef, Value, CostCode, CreatedAt, CreatedByEmail, IssuedAt,
                ApprovedAt, ApprovedByEmail, RejectedAt)
        VALUES (source.VariationOrderQuoteId, source.ProjectId, source.RequestId, source.Number,
                source.Reference, source.Title, source.Description, source.Status,
                source.SelectedBidPackageId, source.SelectedSubcontractorId, source.EstimatedValue,
                source.VariationRef, source.Value, source.CostCode, source.CreatedAt,
                source.CreatedByEmail, source.IssuedAt, source.ApprovedAt,
                source.ApprovedByEmail, source.RejectedAt);

    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'vt-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Landscaping - Additional Items', 0, N'EXTW-LND', N'', N'item', 1.0000, 3690.0000, 3690.0000, N'', 1),
        (N'vt-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Joinery Boxing MHVR', 0, N'CARP-2FX', N'', N'item', 1.0000, 312.0000, 312.0000, N'', 2),
        (N'vt-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Landscaping additional items & omit railings decoration', 0, N'EXTW-LND', N'', N'item', 1.0000, 1899.9500, 1899.9500, N'', 3),
        (N'vt-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'UFH Boxing & Covering', 0, N'CARP-2FX', N'', N'item', 1.0000, 585.0000, 585.0000, N'', 4),
        (N'vt-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'Extended prelims - Project & Site Manager, rubbish removal (5 weeks)', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 10960.0000, 10960.0000, N'', 5),
        (N'vt-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Mains Water - Temp Works carried out by Jewel', 0, N'UTIL-STD', N'', N'item', 1.0000, 2110.0000, 2110.0000, N'', 6),
        (N'vt-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Additional electrical works for the laundry room', 0, N'ELE-STD', N'', N'item', 1.0000, 2400.0000, 2400.0000, N'', 7),
        (N'vt-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Prelims deduction - Project & Site Manager (3 weeks)', 2, N'PRELIMS-SMG', N'', N'item', 1.0000, -6000.0000, -6000.0000, N'', 8)
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

    PRINT 'Vets (School House, Slough): variation orders & variation report lines merged.';

    -- Sanity check: variation lines should reconcile to the workbook register.
    SELECT
        COUNT(*) AS VariationLines,                                                       --        8
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, -- 15956.95
        SUM(LineAmount) AS GrossOfAllVoLines                                              -- 15956.95
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType = 3;

    -- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
    SELECT
        SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    --  91640.75
        SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  15956.95
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 107597.70
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId;

    COMMIT TRAN;
END
GO
