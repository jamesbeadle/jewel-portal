-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: Windy Ridge Godalming -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : Windy Ridge, Primrose Ridge, Godalming, GU7 2ND
-- ProjectId: resolved at run time by site-name matcher 'windyridgegodalming'
--
-- Companion to seed-windyridge-valuation.sql, which seeds ONLY the original
-- contract scope (Contract Sum GBP 176,784.55). This file adds the
-- post-contract VARIATION ORDERS from the "Valuation 10 - Retention"
-- workbook, reconciling to the workbook's variations register:
--
--     Net Variations          GBP  36,748.59
--     Contract Sum            GBP 176,784.55
--     ----------------------------------------
--     Revised Contract Sum    GBP 213,533.14
--
-- MODEL NOTE (unified variation orders, post-20260723 UnifyVariationOrders)
-- Each workbook VO is split into multiple priced lines (omits of contract
-- scope as negatives, new items as positives). On the JPMS valuation report a
-- VO shows as a SINGLE summary line, so we seed ONE ValuationLineItem per VO
-- whose LineAmount is the NET of that VO's workbook lines (Quantity 1 x Rate
-- = net), plus ONE row per VO in VariationOrderQuotes (the unified variation
-- record) with Status Approved and VariationRef = the same V-number.
--
-- All 21 VOs are APPROVED (every one carries claimed lines in the register).
-- Skipped row: V16 "Rear Step Paving" (rate 345.00, blank amount, comment
-- "Omit item") -- the workbook itself excludes it from V16's net and from the
-- register total, so V16 nets to 1,135.00 and the file reconciles to
-- GBP 36,748.59 exactly. No penny adjustment was needed.
-- V05's "Alum Windows Prov Sum" (rate 'PS', amount 850.00) is included at the
-- workbook amount; V07 omits the same 850.00 back out.
--
-- Dates: the workbook gives no VO dates; CreatedAt is placed just before each
-- VO's first claimed valuation month (Valuation 01 taken as Jun 2025 ..
-- Valuation 09 as Feb 2026), IssuedAt ~ +7 days, ApprovedAt ~ +21 days.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--
-- Idempotent: keyed on stable ids (wr-voq-vNN / wr-vo-vNN) via MERGE. The
-- contract lines seeded by seed-windyridge-valuation.sql are left untouched.
-- Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'windyridgegodalming'
       OR LOWER(REPLACE(Name, ' ', '')) = 'windyridgegodalming'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'windyridgegodalming' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Windy Ridge Godalming -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
    (N'wr-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Unique Steel Windows - omit Crittal doors, windows & pocket doors', N'Unique Steel Windows - omit Crittal doors, windows & pocket doors', 2, NULL, NULL, 10602.0000, N'V01', 10602.0000, N'WDR-ALU', '2025-05-20', N'seed@jewelgroup.co.uk', '2025-05-27', '2025-06-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Omit Velux GGL FC08 - RL01 (No.1)', N'Omit Velux GGL FC08 - RL01 (No.1)', 2, NULL, NULL, -2880.0000, N'V02', -2880.0000, N'WDR-SPG', '2025-09-20', N'seed@jewelgroup.co.uk', '2025-09-27', '2025-10-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Omit Unique Steel Windows ID02; Eclisse Oak pocket door', N'Omit Unique Steel Windows ID02; Eclisse Oak pocket door', 2, NULL, NULL, -2648.7600, N'V03', -2648.7600, N'WDR-INT', '2025-09-20', N'seed@jewelgroup.co.uk', '2025-09-27', '2025-10-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Omit timber cladding extra-over options (Cedar, Douglas Fir, Accoya, Larch)', N'Omit timber cladding extra-over options (Cedar, Douglas Fir, Accoya, Larch)', 2, NULL, NULL, -4488.0000, N'V04', -4488.0000, N'CARP-1FX', '2025-06-20', N'seed@jewelgroup.co.uk', '2025-06-27', '2025-07-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'Additional building works - demolition, foundations, masonry, steel frame, roof & finishes', N'Additional building works - demolition, foundations, masonry, steel frame, roof & finishes', 2, NULL, NULL, 6320.0000, N'V05', 6320.0000, N'ROOF-FLT', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Existing roofing works & Velux GGL FC08 - RL01', N'Existing roofing works & Velux GGL FC08 - RL01', 2, NULL, NULL, 8315.0000, N'V06', 8315.0000, N'ROOF-TLO', '2025-07-20', N'seed@jewelgroup.co.uk', '2025-07-27', '2025-08-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Generation Windows - Elevation L-1838-4-405 (less Alum Windows PS)', N'Generation Windows - Elevation L-1838-4-405 (less Alum Windows PS)', 2, NULL, NULL, 6915.0000, N'V07', 6915.0000, N'WDR-ALU', '2025-08-20', N'seed@jewelgroup.co.uk', '2025-08-27', '2025-09-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Electrical additions - sockets, lights, PIR & kick heater', N'Electrical additions - sockets, lights, PIR & kick heater', 2, NULL, NULL, 2916.0000, N'V08', 2916.0000, N'ELE-STD', '2025-08-20', N'seed@jewelgroup.co.uk', '2025-08-27', '2025-09-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Rear entrance door installation', N'Rear entrance door installation', 2, NULL, NULL, 560.0000, N'V09', 560.0000, N'WDR-TIM', '2025-09-20', N'seed@jewelgroup.co.uk', '2025-09-27', '2025-10-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Inground mains gas services', N'Inground mains gas services', 2, NULL, NULL, 3725.0000, N'V10', 3725.0000, N'UTIL-STD', '2025-09-20', N'seed@jewelgroup.co.uk', '2025-09-27', '2025-10-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'Structural wall alterations', N'Structural wall alterations', 2, NULL, NULL, 750.0000, N'V11', 750.0000, N'MASON-BRK', '2025-09-20', N'seed@jewelgroup.co.uk', '2025-09-27', '2025-10-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Glazing rear door', N'Glazing rear door', 2, NULL, NULL, 296.0000, N'V12', 296.0000, N'WDR-SPG', '2025-10-20', N'seed@jewelgroup.co.uk', '2025-10-27', '2025-11-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Sanitary ware revisions - shower tray, basin mixers & accessories', N'Sanitary ware revisions - shower tray, basin mixers & accessories', 2, NULL, NULL, -758.6500, N'V13', -758.6500, N'SUP-SAN', '2025-10-20', N'seed@jewelgroup.co.uk', '2025-10-27', '2025-11-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'Hive control, external lighting, bay cabling, shower pump & LED mirror', N'Hive control, external lighting, bay cabling, shower pump & LED mirror', 2, NULL, NULL, 1960.0000, N'V14', 1960.0000, N'ELE-STD', '2025-11-20', N'seed@jewelgroup.co.uk', '2025-11-27', '2025-12-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Oak sandblasting, varnishing & decoration works', N'Oak sandblasting, varnishing & decoration works', 2, NULL, NULL, 5320.0000, N'V15', 5320.0000, N'DEC-STD', '2025-11-20', N'seed@jewelgroup.co.uk', '2025-11-27', '2025-12-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'Timber sleepers & pocket door hardware', N'Timber sleepers & pocket door hardware', 2, NULL, NULL, 1135.0000, N'V16', 1135.0000, N'EXTW-LND', '2025-11-20', N'seed@jewelgroup.co.uk', '2025-11-27', '2025-12-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'Omit external windows & doors allowance', N'Omit external windows & doors allowance', 2, NULL, NULL, -1500.0000, N'V17', -1500.0000, N'WDR-ALU', '2025-11-20', N'seed@jewelgroup.co.uk', '2025-11-27', '2025-12-11', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'Electrician attendance, external socket, junction box & door stops', N'Electrician attendance, external socket, junction box & door stops', 2, NULL, NULL, 710.0000, N'V18', 710.0000, N'ELE-STD', '2025-12-20', N'seed@jewelgroup.co.uk', '2025-12-27', '2026-01-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v19', @ProjectId, N'', 19, N'VOQ-0019', N'Basin alterations - carpentry, tiling, decoration & materials', N'Basin alterations - carpentry, tiling, decoration & materials', 2, NULL, NULL, 1090.0000, N'V19', 1090.0000, N'TIL-STD', '2025-12-20', N'seed@jewelgroup.co.uk', '2025-12-27', '2026-01-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v20', @ProjectId, N'', 20, N'VOQ-0020', N'Velux blinds - omit standard blinds, add pleated blackout', N'Velux blinds - omit standard blinds, add pleated blackout', 2, NULL, NULL, -235.0000, N'V20', -235.0000, N'WIN-BLD', '2025-12-20', N'seed@jewelgroup.co.uk', '2025-12-27', '2026-01-10', N'seed@jewelgroup.co.uk', NULL),
    (N'wr-voq-v21', @ProjectId, N'', 21, N'VOQ-0021', N'Omit Velux pleated blackout blinds & stove pipework installation', N'Omit Velux pleated blackout blinds & stove pipework installation', 2, NULL, NULL, -1355.0000, N'V21', -1355.0000, N'WIN-BLD', '2026-01-20', N'seed@jewelgroup.co.uk', '2026-01-27', '2026-02-10', N'seed@jewelgroup.co.uk', NULL)
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
    (N'wr-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Unique Steel Windows - omit Crittal doors, windows & pocket doors', 0, N'WDR-ALU', N'', N'item', 1.0000, 10602.0000, 10602.0000, N'', 1),
    (N'wr-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Omit Velux GGL FC08 - RL01 (No.1)', 2, N'WDR-SPG', N'', N'item', 1.0000, -2880.0000, -2880.0000, N'', 2),
    (N'wr-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Omit Unique Steel Windows ID02; Eclisse Oak pocket door', 2, N'WDR-INT', N'', N'item', 1.0000, -2648.7600, -2648.7600, N'', 3),
    (N'wr-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Omit timber cladding extra-over options (Cedar, Douglas Fir, Accoya, Larch)', 2, N'CARP-1FX', N'', N'item', 1.0000, -4488.0000, -4488.0000, N'', 4),
    (N'wr-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'Additional building works - demolition, foundations, masonry, steel frame, roof & finishes', 0, N'ROOF-FLT', N'', N'item', 1.0000, 6320.0000, 6320.0000, N'', 5),
    (N'wr-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Existing roofing works & Velux GGL FC08 - RL01', 0, N'ROOF-TLO', N'', N'item', 1.0000, 8315.0000, 8315.0000, N'', 6),
    (N'wr-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Generation Windows - Elevation L-1838-4-405 (less Alum Windows PS)', 0, N'WDR-ALU', N'', N'item', 1.0000, 6915.0000, 6915.0000, N'', 7),
    (N'wr-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Electrical additions - sockets, lights, PIR & kick heater', 0, N'ELE-STD', N'', N'item', 1.0000, 2916.0000, 2916.0000, N'', 8),
    (N'wr-vo-v09', @ProjectId, 3, N'', N'', N'V09', N'Rear entrance door installation', 0, N'WDR-TIM', N'', N'item', 1.0000, 560.0000, 560.0000, N'', 9),
    (N'wr-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Inground mains gas services', 0, N'UTIL-STD', N'', N'item', 1.0000, 3725.0000, 3725.0000, N'', 10),
    (N'wr-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'Structural wall alterations', 0, N'MASON-BRK', N'', N'item', 1.0000, 750.0000, 750.0000, N'', 11),
    (N'wr-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Glazing rear door', 0, N'WDR-SPG', N'', N'item', 1.0000, 296.0000, 296.0000, N'', 12),
    (N'wr-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Sanitary ware revisions - shower tray, basin mixers & accessories', 2, N'SUP-SAN', N'', N'item', 1.0000, -758.6500, -758.6500, N'', 13),
    (N'wr-vo-v14', @ProjectId, 3, N'', N'', N'V14', N'Hive control, external lighting, bay cabling, shower pump & LED mirror', 0, N'ELE-STD', N'', N'item', 1.0000, 1960.0000, 1960.0000, N'', 14),
    (N'wr-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Oak sandblasting, varnishing & decoration works', 0, N'DEC-STD', N'', N'item', 1.0000, 5320.0000, 5320.0000, N'', 15),
    (N'wr-vo-v16', @ProjectId, 3, N'', N'', N'V16', N'Timber sleepers & pocket door hardware', 0, N'EXTW-LND', N'', N'item', 1.0000, 1135.0000, 1135.0000, N'', 16),
    (N'wr-vo-v17', @ProjectId, 3, N'', N'', N'V17', N'Omit external windows & doors allowance', 2, N'WDR-ALU', N'', N'item', 1.0000, -1500.0000, -1500.0000, N'', 17),
    (N'wr-vo-v18', @ProjectId, 3, N'', N'', N'V18', N'Electrician attendance, external socket, junction box & door stops', 0, N'ELE-STD', N'', N'item', 1.0000, 710.0000, 710.0000, N'', 18),
    (N'wr-vo-v19', @ProjectId, 3, N'', N'', N'V19', N'Basin alterations - carpentry, tiling, decoration & materials', 0, N'TIL-STD', N'', N'item', 1.0000, 1090.0000, 1090.0000, N'', 19),
    (N'wr-vo-v20', @ProjectId, 3, N'', N'', N'V20', N'Velux blinds - omit standard blinds, add pleated blackout', 2, N'WIN-BLD', N'', N'item', 1.0000, -235.0000, -235.0000, N'', 20),
    (N'wr-vo-v21', @ProjectId, 3, N'', N'', N'V21', N'Omit Velux pleated blackout blinds & stove pipework installation', 2, N'WIN-BLD', N'', N'item', 1.0000, -1355.0000, -1355.0000, N'', 21)
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

    PRINT 'Windy Ridge Godalming: variation orders & variation lines merged.';
    COMMIT TRAN;

    -- Sanity check: variation lines should reconcile to the workbook register.
    SELECT
        COUNT(*) AS VariationLines,                                                       -- 21
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, -- 36748.59
        SUM(LineAmount) AS GrossOfAllVoLines                                              -- 36748.59
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType = 3;

    -- Combined check: Contract Sum + Net Variations = Revised Contract Sum.
    SELECT
        SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,   -- 176784.55
        SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, --  36748.59
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                        -- 213533.14
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId;

    -- VOQ records should mirror the report's variation lines exactly.
    SELECT
        (SELECT COUNT(*)   FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId) AS VariationOrderQuotes, -- 21
        (SELECT SUM(Value) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId AND Status = 2) AS NetVoValue, -- 36748.59
        (SELECT COUNT(*)
           FROM [dbo].[VariationOrderQuotes] voq
           LEFT JOIN [dbo].[ValuationLineItems] li
             ON li.ProjectId = voq.ProjectId AND li.ElementType = 3 AND li.VariationRef = voq.VariationRef
          WHERE voq.ProjectId = @ProjectId AND voq.Status = 2 AND li.ValuationLineItemId IS NULL) AS VosMissingReportLine; -- 0
END
GO
