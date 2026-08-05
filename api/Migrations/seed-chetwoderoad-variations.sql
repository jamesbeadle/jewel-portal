-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: 21 Chetwode Road -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : 21 Chetwode Road SW17 7RF
-- ProjectId: resolved at run time by site-name matcher '21chetwoderoad'
--            (XeroSiteName first, then Name; nothing touched if no match).
--
-- Companion to seed-chetwoderoad-valuation.sql, which seeds ONLY the original
-- contract scope (Contract Sum GBP 826,141.23). This file adds the
-- post-contract VARIATION ORDERS from the "Silvercrow Chetwode Valuation 07 -
-- April 24 REVISED" workbook, reconciling to the workbook's variations
-- register:
--
--     Contract Sum            GBP 826,141.23
--     Net Variations          GBP   5,823.89
--     ----------------------------------------
--     Revised Contract Sum    GBP 831,965.12
--
-- MODEL NOTE (unified variation orders, post-20260723120000_UnifyVariationOrders)
-- Each workbook VO is split into multiple priced rows (omits of contract scope
-- as negatives, new items as positives). On the JPMS valuation report a VO
-- shows as a SINGLE summary line, so we seed ONE ValuationLineItem per
-- APPROVED VO whose LineAmount is the NET of that VO's workbook rows
-- (Quantity 1 x Rate = net), plus ONE VariationOrderQuotes row per VO -- the
-- single unified variation record (there is no separate VariationOrders table
-- any more).
--
-- V01..V10 and V12..V19 are approved (Status 2) and sum to the register's
-- stated Net Variations of GBP 5,823.89 EXACTLY. V11 ("ASHP - Pre Insulated
-- Pipework") is marked TBC in the workbook with no amount carried into the
-- register total: it is seeded as a QUOTING record (Status 0) with
-- EstimatedValue 2,318.45 and gets NO valuation line. Two V19 rows with blank
-- amounts ("Uplift - Caber deck instead of 18mm ply" / "Remove GBP 1,635" and
-- "Additional entrance gate to hoarding" / "Remove GBP 380") are excluded by
-- the register itself and are not part of V19's net. No VO is declined.
--
-- Approval dates are plausible seeds spaced to match the claim periods the
-- register shows each VO landing in (V01 in VAL.1 Oct-23; the rest around
-- VAL.6/VAL.7, Mar/Apr-24). The workbook gives no actual VO dates.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all lines here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net >= 0 -> Priced (addition)
--   * net <  0 -> Omit   (net reduction; stored as a negative LineAmount)
--
-- Idempotent: keyed on stable ids (cd-voq-vNN / cd-vo-vNN) via MERGE (no WHEN
-- NOT MATCHED BY SOURCE). The contract lines seeded by
-- seed-chetwoderoad-valuation.sql are left untouched. Safe to run repeatedly.
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
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'cd-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Structural steel supply & install - Quotation 97818/5, in lieu of steel PC sum', N'Structural steel supply & install - Quotation 97818/5, in lieu of steel PC sum', 2, NULL, NULL, 12260.7800, N'V01', 12260.7800, N'STR-STL', '2023-09-20', N'seed@jewelgroup.co.uk', '2023-09-27', '2023-10-11', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Baystar ASHP, electric boilers & Trealighter PV in lieu of gas boilers & gas supply', N'Baystar ASHP, electric boilers & Trealighter PV in lieu of gas boilers & gas supply', 2, NULL, NULL, 58035.8900, N'V02', 58035.8900, N'MEC-HTS', '2024-02-10', N'seed@jewelgroup.co.uk', '2024-02-17', '2024-03-02', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Omit window, external door & glass balustrade installation (client direct)', N'Omit window, external door & glass balustrade installation (client direct)', 2, NULL, NULL, -20370.0000, N'V03', -20370.0000, N'STR-GRL', '2024-02-12', N'seed@jewelgroup.co.uk', '2024-02-19', '2024-03-04', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Blockwork & lintel revisions - garden wall, padstones & Naylor ER2 lintels', N'Blockwork & lintel revisions - garden wall, padstones & Naylor ER2 lintels', 2, NULL, NULL, -5057.2000, N'V04', -5057.2000, N'MASON-BRK', '2024-02-14', N'seed@jewelgroup.co.uk', '2024-02-21', '2024-03-06', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'25mm barrier pipe water supply in lieu of MDPE', N'25mm barrier pipe water supply in lieu of MDPE', 2, NULL, NULL, 700.0000, N'V05', 700.0000, N'MEC-PLM', '2024-02-16', N'seed@jewelgroup.co.uk', '2024-02-23', '2024-03-08', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Painted timber hoarding & metal gate posts', N'Painted timber hoarding & metal gate posts', 2, NULL, NULL, 2500.0000, N'V06', 2500.0000, N'PRELIMS-HRD', '2024-02-20', N'seed@jewelgroup.co.uk', '2024-02-27', '2024-03-12', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Burns Scaffolding (invoices BS1616/BS1624) in lieu of scaffolding provisional sum', N'Burns Scaffolding (invoices BS1616/BS1624) in lieu of scaffolding provisional sum', 2, NULL, NULL, -9467.0000, N'V07', -9467.0000, N'SCAFF-STD', '2024-02-22', N'seed@jewelgroup.co.uk', '2024-02-29', '2024-03-14', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Upper floor redesign - joists at 400 centres, 18mm ply & rockwool insulation', N'Upper floor redesign - joists at 400 centres, 18mm ply & rockwool insulation', 2, NULL, NULL, 16658.2500, N'V08', 16658.2500, N'CARP-1FX', '2024-02-24', N'seed@jewelgroup.co.uk', '2024-03-02', '2024-03-16', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Temporary works - chimney breast removal', N'Temporary works - chimney breast removal', 2, NULL, NULL, 1750.0000, N'V09', 1750.0000, N'ENABLE-STS', '2024-03-01', N'seed@jewelgroup.co.uk', '2024-03-08', '2024-03-22', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Retaining wall - hollow blocks, H10 bars & concrete pour', N'Retaining wall - hollow blocks, H10 bars & concrete pour', 2, NULL, NULL, 1983.0000, N'V10', 1983.0000, N'SUB-CON', '2024-03-05', N'seed@jewelgroup.co.uk', '2024-03-12', '2024-03-26', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'ASHP - pre insulated pipework (TBC)', N'ASHP - pre insulated pipework (TBC)', 0, NULL, NULL, 2318.4500, NULL, 0.0000, NULL, '2024-03-10', N'seed@jewelgroup.co.uk', NULL, NULL, NULL, NULL),
        (N'cd-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Omit temporary works - chimney breast removal (reversal of V09 uplift)', N'Omit temporary works - chimney breast removal (reversal of V09 uplift)', 2, NULL, NULL, -612.5000, N'V12', -612.5000, N'ENABLE-STS', '2024-03-12', N'seed@jewelgroup.co.uk', '2024-03-19', '2024-04-02', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Omit Baystar deposit amount', N'Omit Baystar deposit amount', 2, NULL, NULL, -450.0000, N'V13', -450.0000, N'MEC-HTS', '2024-03-14', N'seed@jewelgroup.co.uk', '2024-03-21', '2024-04-04', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'Omit 140mm blockwork - rear garden wall', N'Omit 140mm blockwork - rear garden wall', 2, NULL, NULL, -264.0000, N'V14', -264.0000, N'MASON-BRK', '2024-03-16', N'seed@jewelgroup.co.uk', '2024-03-23', '2024-04-06', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Naylor ER2 lintels 1200mm', N'Naylor ER2 lintels 1200mm', 2, NULL, NULL, 84.0000, N'V15', 84.0000, N'MASON-BRK', '2024-03-18', N'seed@jewelgroup.co.uk', '2024-03-25', '2024-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'First floor joist adjustment - re-measure at 400 centres', N'First floor joist adjustment - re-measure at 400 centres', 2, NULL, NULL, 160.2900, N'V16', 160.2900, N'CARP-1FX', '2024-03-20', N'seed@jewelgroup.co.uk', '2024-03-27', '2024-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'Caber deck - 10% deduction of claimed amount', N'Caber deck - 10% deduction of claimed amount', 2, NULL, NULL, -347.9800, N'V17', -347.9800, N'CARP-1FX', '2024-03-22', N'seed@jewelgroup.co.uk', '2024-03-29', '2024-04-12', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'Masonry re-measure - omit brickwork/blockwork/padstones, add revised quantities', N'Masonry re-measure - omit brickwork/blockwork/padstones, add revised quantities', 2, NULL, NULL, -58665.6400, N'V18', -58665.6400, N'MASON-BRK', '2024-03-24', N'seed@jewelgroup.co.uk', '2024-03-31', '2024-04-14', N'seed@jewelgroup.co.uk', NULL),
        (N'cd-voq-v19', @ProjectId, N'', 19, N'VOQ-0019', N'Site sundries - floodlights, materials on site, temp fencing, pump hire & grout', N'Site sundries - floodlights, materials on site, temp fencing, pump hire & grout', 2, NULL, NULL, 6926.0000, N'V19', 6926.0000, N'HAND-MSC', '2024-03-26', N'seed@jewelgroup.co.uk', '2024-04-02', '2024-04-16', N'seed@jewelgroup.co.uk', NULL)
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
        (N'cd-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Structural steel supply & install - Quotation 97818/5, in lieu of steel PC sum', 0, N'STR-STL', N'', N'item', 1.0000, 12260.7800, 12260.7800, N'', 1),
        (N'cd-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Baystar ASHP, electric boilers & Trealighter PV in lieu of gas boilers & gas supply', 0, N'MEC-HTS', N'', N'item', 1.0000, 58035.8900, 58035.8900, N'', 2),
        (N'cd-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Omit window, external door & glass balustrade installation (client direct)', 2, N'STR-GRL', N'', N'item', 1.0000, -20370.0000, -20370.0000, N'', 3),
        (N'cd-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Blockwork & lintel revisions - garden wall, padstones & Naylor ER2 lintels', 2, N'MASON-BRK', N'', N'item', 1.0000, -5057.2000, -5057.2000, N'', 4),
        (N'cd-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'25mm barrier pipe water supply in lieu of MDPE', 0, N'MEC-PLM', N'', N'item', 1.0000, 700.0000, 700.0000, N'', 5),
        (N'cd-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Painted timber hoarding & metal gate posts', 0, N'PRELIMS-HRD', N'', N'item', 1.0000, 2500.0000, 2500.0000, N'', 6),
        (N'cd-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Burns Scaffolding (invoices BS1616/BS1624) in lieu of scaffolding provisional sum', 2, N'SCAFF-STD', N'', N'item', 1.0000, -9467.0000, -9467.0000, N'', 7),
        (N'cd-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Upper floor redesign - joists at 400 centres, 18mm ply & rockwool insulation', 0, N'CARP-1FX', N'', N'item', 1.0000, 16658.2500, 16658.2500, N'', 8),
        (N'cd-vo-v09', @ProjectId, 3, N'', N'', N'V09', N'Temporary works - chimney breast removal', 0, N'ENABLE-STS', N'', N'item', 1.0000, 1750.0000, 1750.0000, N'', 9),
        (N'cd-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Retaining wall - hollow blocks, H10 bars & concrete pour', 0, N'SUB-CON', N'', N'item', 1.0000, 1983.0000, 1983.0000, N'', 10),
        (N'cd-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Omit temporary works - chimney breast removal (reversal of V09 uplift)', 2, N'ENABLE-STS', N'', N'item', 1.0000, -612.5000, -612.5000, N'', 11),
        (N'cd-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Omit Baystar deposit amount', 2, N'MEC-HTS', N'', N'item', 1.0000, -450.0000, -450.0000, N'', 12),
        (N'cd-vo-v14', @ProjectId, 3, N'', N'', N'V14', N'Omit 140mm blockwork - rear garden wall', 2, N'MASON-BRK', N'', N'item', 1.0000, -264.0000, -264.0000, N'', 13),
        (N'cd-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Naylor ER2 lintels 1200mm', 0, N'MASON-BRK', N'', N'item', 1.0000, 84.0000, 84.0000, N'', 14),
        (N'cd-vo-v16', @ProjectId, 3, N'', N'', N'V16', N'First floor joist adjustment - re-measure at 400 centres', 0, N'CARP-1FX', N'', N'item', 1.0000, 160.2900, 160.2900, N'', 15),
        (N'cd-vo-v17', @ProjectId, 3, N'', N'', N'V17', N'Caber deck - 10% deduction of claimed amount', 2, N'CARP-1FX', N'', N'item', 1.0000, -347.9800, -347.9800, N'', 16),
        (N'cd-vo-v18', @ProjectId, 3, N'', N'', N'V18', N'Masonry re-measure - omit brickwork/blockwork/padstones, add revised quantities', 2, N'MASON-BRK', N'', N'item', 1.0000, -58665.6400, -58665.6400, N'', 17),
        (N'cd-vo-v19', @ProjectId, 3, N'', N'', N'V19', N'Site sundries - floodlights, materials on site, temp fencing, pump hire & grout', 0, N'HAND-MSC', N'', N'item', 1.0000, 6926.0000, 6926.0000, N'', 18)
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

    PRINT '21 Chetwode Road: 18 approved variation lines and 19 variation orders merged.';

    -- Sanity check: variation lines should reconcile to the workbook register.
    SELECT
        COUNT(*) AS VariationLines,                                                       -- 18
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, -- 5823.89
        SUM(LineAmount) AS GrossOfAllVoLines                                              -- 5823.89
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType = 3;

    -- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
    SELECT
        SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 826141.23
        SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --   5823.89
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 831965.12
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId;

    COMMIT TRAN;
END
GO
