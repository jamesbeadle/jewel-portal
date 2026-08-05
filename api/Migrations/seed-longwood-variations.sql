-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed,
-- per JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: Longwood (Horsham Road, Cranleigh) -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : Longwood, 133 Horsham Road, Cranleigh
-- ProjectId: resolved at run time by site-name matcher 'horshamroadlongwoodcranleigh'
--
-- Companion to seed-longwood-valuation.sql, which seeds ONLY the original
-- contract scope (Contract works / PC Sums / Contingency = Contract Sum
-- GBP 765,664.00). This file adds the post-contract VARIATION ORDERS from
-- "Longwood Valuation 1 - Sept 25", reconciling to the workbook's
-- variations register:
--
--     Net Variations          GBP  -1,630.00
--     Contract Sum            GBP 765,664.00
--     ------------------------------------------
--     Revised Contract Sum    GBP 764,034.00
--
-- MODEL NOTE (unified variation orders, post-20260723120000_UnifyVariationOrders)
-- Each workbook VO is one row in [VariationOrderQuotes] (the unified variation
-- order record) plus ONE summary line in [ValuationLineItems] whose LineAmount
-- is the NET of that VO's workbook rows (Quantity 1 x Rate = net):
--
--     V01  Scaffolding: omit GBP 10,000.00 provisional sum,
--          add quoted scaffolding GBP 9,870.00          -> net    -130.00
--     V02  Trenches/pipeways (P30) provisional sum omit -> net  -1,500.00
--     --------------------------------------------------------------------
--     Net Variations                                            -1,630.00
--
-- Both VOs are approved and fully claimed in Claim 1 (Sept 25); the workbook
-- gives no VO dates, so plausible dates just ahead of that first valuation
-- month are used. There are no declined VOs in this workbook.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--
-- Idempotent: keyed on stable ids (lw-voq-vNN / lw-vo-vNN) via MERGE. The
-- contract/PC/contingency lines seeded by seed-longwood-valuation.sql are
-- left untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'horshamroadlongwoodcranleigh'
       OR LOWER(REPLACE(Name, ' ', '')) = 'horshamroadlongwoodcranleigh'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'horshamroadlongwoodcranleigh' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Longwood (Horsham Road, Cranleigh) — no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'lw-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Scaffolding - omit provisional sum, add quoted scaffolding', N'Scaffolding - omit provisional sum, add quoted scaffolding', 2, NULL, NULL, -130.0000, N'V01', -130.0000, N'SCAFF-STD', '2025-08-04', N'seed@jewelgroup.co.uk', '2025-08-11', '2025-08-25', N'seed@jewelgroup.co.uk', NULL),
        (N'lw-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Trenches & pipeways for engineering services - omit provisional sum', N'Trenches & pipeways for engineering services - omit provisional sum', 2, NULL, NULL, -1500.0000, N'V02', -1500.0000, N'UTIL-TRN', '2025-08-11', N'seed@jewelgroup.co.uk', '2025-08-18', '2025-09-01', N'seed@jewelgroup.co.uk', NULL)
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
        (N'lw-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Scaffolding - omit provisional sum, add quoted scaffolding', 2, N'SCAFF-STD', N'', N'item', 1.0000, -130.0000, -130.0000, N'', 1),
        (N'lw-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Trenches & pipeways for engineering services - omit provisional sum', 2, N'UTIL-TRN', N'', N'item', 1.0000, -1500.0000, -1500.0000, N'', 2)
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

    PRINT 'Longwood (Horsham Road, Cranleigh): variation orders & variation lines merged.';

    -- Sanity check: variation lines should reconcile to the workbook register.
    SELECT
        COUNT(*) AS VariationLines,                                                       -- 2
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations  -- -1630.00
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType = 3;

    -- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
    SELECT
        SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 765664.00
        SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  -1630.00
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 764034.00
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId;

    COMMIT TRAN;
END
GO
