-- ============================================================================
-- Seed: Abbot Road -- Variation Orders (VOQs, VOs and report variation lines)
-- ----------------------------------------------------------------------------
-- Project : 17a Abbot Road, Guildford, GU1 3TA  (JBB-2026-002)
-- ProjectId: 4ec1ad1ca3a440c69f32f46f73aea005
--
-- Companion to seed-abbotroad-valuation.sql (contract scope, Contract Sum
-- GBP 298,946.04). Adds the variations register from "Valuation No.12 -
-- 17a Abbot Road". Unlike the By France / Albany Mews registers, this one is
-- live and MIXED-STATUS, and is seeded per the VOQ/VO lifecycle rule (only
-- AGREED variations reach the valuation report):
--
--   * Agreed   (V01 V04 V05 V09 V23) -> VOQ Approved + VO Issued + report line
--   * TBR      (V02 V03 V07 V08 V10 V11) -> VOQ Tendering, estimated value,
--              NO VO and NO report line (not yet agreed)
--   * Declined (V06) -> VOQ Rejected, no VO
--   * RFI/review, unpriced (V12..V22) -> VOQ Draft, no value yet
--
-- DELIBERATE DIVERGENCE FROM THE WORKBOOK: its Net Variations of
-- GBP 37,556.00 includes the TBR items (GBP 20,105.50). Seeded this way the
-- report shows Net Variations GBP 17,450.50 (agreed only) and a Revised
-- Contract Sum of GBP 316,396.54; the TBR value sits on the Variations tab as
-- open quotes until approved. V03 is seeded at the register's carried amount
-- (440.00), not its quoted rate (770.00).
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
-- VOQ Status : 0=Draft 1=Inviting 2=Tendering 3=Selected 4=Approved 5=Rejected
-- VO  Status : 0=Approved 1=Issued 2=Cancelled
--
-- Idempotent: keyed on stable ids (ar-vo-vNN / ar-voq-vNN / ar-vord-vNN) via
-- MERGE. The contract/PS lines from seed-abbotroad-valuation.sql are left
-- untouched. Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'ar-vo-v01', N'4ec1ad1ca3a440c69f32f46f73aea005', 3, N'', N'', N'V01', N'Levelling Compound Removal', 0, N'', N'', N'item', 1.0000, 1050.0000, 1050.0000, N'', 1),
    (N'ar-vo-v04', N'4ec1ad1ca3a440c69f32f46f73aea005', 3, N'', N'', N'V04', N'Various', 0, N'', N'', N'item', 1.0000, 6410.0000, 6410.0000, N'', 2),
    (N'ar-vo-v05', N'4ec1ad1ca3a440c69f32f46f73aea005', 3, N'', N'', N'V05', N'Electrical', 0, N'', N'', N'item', 1.0000, 3900.0000, 3900.0000, N'', 3),
    (N'ar-vo-v09', N'4ec1ad1ca3a440c69f32f46f73aea005', 3, N'', N'', N'V09', N'External Tap', 0, N'', N'', N'item', 1.0000, 546.0000, 546.0000, N'', 4),
    (N'ar-vo-v23', N'4ec1ad1ca3a440c69f32f46f73aea005', 3, N'', N'', N'V23', N'Staircase', 0, N'', N'', N'item', 1.0000, 5544.5000, 5544.5000, N'', 5)
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

MERGE INTO [dbo].[VariationOrderQuotes] AS target
USING (VALUES
    (N'ar-voq-v01', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 1, N'VOQ-0001', N'Levelling Compound Removal', N'Levelling Compound Removal', 4, NULL, NULL, 1050.0000, '2026-01-10', N'seed@jewelgroup.co.uk', '2026-01-31', N'seed@jewelgroup.co.uk'),
    (N'ar-voq-v02', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 2, N'VOQ-0002', N'Additional Steel Works', N'Additional Steel Works', 2, NULL, NULL, 2826.0000, '2026-01-16', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v03', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 3, N'VOQ-0003', N'Install and sub base - Porcelain Tiling to LGF Stairwell & Store (client supply)', N'Install and sub base - Porcelain Tiling to LGF Stairwell & Store (client supply)', 2, NULL, NULL, 440.0000, '2026-01-22', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v04', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 4, N'VOQ-0004', N'Various', N'Various', 4, NULL, NULL, 6410.0000, '2026-01-28', N'seed@jewelgroup.co.uk', '2026-02-18', N'seed@jewelgroup.co.uk'),
    (N'ar-voq-v05', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 5, N'VOQ-0005', N'Electrical', N'Electrical', 4, NULL, NULL, 3900.0000, '2026-02-03', N'seed@jewelgroup.co.uk', '2026-02-24', N'seed@jewelgroup.co.uk'),
    (N'ar-voq-v06', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 6, N'VOQ-0006', N'Existing Ceiling Insulation', N'Existing Ceiling Insulation', 5, NULL, NULL, NULL, '2026-02-09', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v07', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 7, N'VOQ-0007', N'Planter New', N'Planter New', 2, NULL, NULL, 4951.7000, '2026-02-15', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v08', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 8, N'VOQ-0008', N'Additional Roof Works', N'Additional Roof Works', 2, NULL, NULL, 3821.8000, '2026-02-21', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v09', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 9, N'VOQ-0009', N'External Tap', N'External Tap', 4, NULL, NULL, 546.0000, '2026-02-27', N'seed@jewelgroup.co.uk', '2026-03-20', N'seed@jewelgroup.co.uk'),
    (N'ar-voq-v10', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 10, N'VOQ-0010', N'Additional Insualtion to Roof', N'Additional Insualtion to Roof', 2, NULL, NULL, NULL, '2026-03-05', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v11', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 11, N'VOQ-0011', N'Porcelean Tile and Sub Base Change', N'Porcelean Tile and Sub Base Change', 2, NULL, NULL, 8066.0000, '2026-03-11', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v12', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 12, N'VOQ-0012', N'Kitchen door removal and brick up to to window', N'Kitchen door removal and brick up to to window', 0, NULL, NULL, NULL, '2026-03-17', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v13', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 13, N'VOQ-0013', N'Existing Stone Cladding tie and rebuild', N'Existing Stone Cladding tie and rebuild', 0, NULL, NULL, NULL, '2026-03-23', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v14', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 14, N'VOQ-0014', N'Marmox block change to 215mm and cut down & additions to GF', N'Marmox block change to 215mm and cut down & additions to GF', 0, NULL, NULL, NULL, '2026-03-29', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v15', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 15, N'VOQ-0015', N'Store room sub floor removal and reinstatement', N'Store room sub floor removal and reinstatement', 0, NULL, NULL, NULL, '2026-04-04', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v16', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 16, N'VOQ-0016', N'En Suit roof vent internal works', N'En Suit roof vent internal works', 0, NULL, NULL, NULL, '2026-04-10', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v17', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 17, N'VOQ-0017', N'Replace/Repair Exisitng FF Rainwater Goods', N'Replace/Repair Exisitng FF Rainwater Goods', 0, NULL, NULL, NULL, '2026-04-16', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v18', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 18, N'VOQ-0018', N'Replace/repair exiting FF&SF roof covering and ridges', N'Replace/repair exiting FF&SF roof covering and ridges', 0, NULL, NULL, NULL, '2026-04-22', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v19', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 19, N'VOQ-0019', N'Beam 8 steel hole drill and connection', N'Beam 8 steel hole drill and connection', 0, NULL, NULL, NULL, '2026-04-28', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v20', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 20, N'VOQ-0020', N'GF Pad Foundation and Beam to Store Doorway & Existng Slab Underpin', N'GF Pad Foundation and Beam to Store Doorway & Existng Slab Underpin', 0, NULL, NULL, NULL, '2026-05-04', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v21', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 21, N'VOQ-0021', N'Aris Rail to roof works', N'Aris Rail to roof works', 0, NULL, NULL, NULL, '2026-05-10', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v22', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 22, N'VOQ-0022', N'12mm to 18mm Plywood change to cladding face', N'12mm to 18mm Plywood change to cladding face', 0, NULL, NULL, NULL, '2026-05-16', N'seed@jewelgroup.co.uk', NULL, NULL),
    (N'ar-voq-v23', N'4ec1ad1ca3a440c69f32f46f73aea005', N'', 23, N'VOQ-0023', N'Staircase', N'Staircase', 4, NULL, NULL, 5544.5000, '2026-05-22', N'seed@jewelgroup.co.uk', '2026-06-12', N'seed@jewelgroup.co.uk')
) AS source (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
             Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
             CreatedAt, CreatedByEmail, ApprovedAt, ApprovedByEmail)
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
    CreatedAt               = source.CreatedAt,
    CreatedByEmail          = source.CreatedByEmail,
    ApprovedAt              = source.ApprovedAt,
    ApprovedByEmail         = source.ApprovedByEmail
WHEN NOT MATCHED BY TARGET THEN
    INSERT (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
            Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
            CreatedAt, CreatedByEmail, ApprovedAt, ApprovedByEmail)
    VALUES (source.VariationOrderQuoteId, source.ProjectId, source.RequestId, source.Number,
            source.Reference, source.Title, source.Description, source.Status,
            source.SelectedBidPackageId, source.SelectedSubcontractorId, source.EstimatedValue,
            source.CreatedAt, source.CreatedByEmail, source.ApprovedAt, source.ApprovedByEmail);
GO

MERGE INTO [dbo].[VariationOrders] AS target
USING (VALUES
    (N'ar-vord-v01', N'4ec1ad1ca3a440c69f32f46f73aea005', N'ar-voq-v01', N'', 1, N'V01', N'Levelling Compound Removal', N'Levelling Compound Removal', 1, 1050.0000, NULL, N'', '2026-01-31', N'seed@jewelgroup.co.uk', '2026-02-07', NULL),
    (N'ar-vord-v04', N'4ec1ad1ca3a440c69f32f46f73aea005', N'ar-voq-v04', N'', 4, N'V04', N'Various', N'Various', 1, 6410.0000, NULL, N'', '2026-02-18', N'seed@jewelgroup.co.uk', '2026-02-25', NULL),
    (N'ar-vord-v05', N'4ec1ad1ca3a440c69f32f46f73aea005', N'ar-voq-v05', N'', 5, N'V05', N'Electrical', N'Electrical', 1, 3900.0000, NULL, N'', '2026-02-24', N'seed@jewelgroup.co.uk', '2026-03-03', NULL),
    (N'ar-vord-v09', N'4ec1ad1ca3a440c69f32f46f73aea005', N'ar-voq-v09', N'', 9, N'V09', N'External Tap', N'External Tap', 1, 546.0000, NULL, N'', '2026-03-20', N'seed@jewelgroup.co.uk', '2026-03-27', NULL),
    (N'ar-vord-v23', N'4ec1ad1ca3a440c69f32f46f73aea005', N'ar-voq-v23', N'', 23, N'V23', N'Staircase', N'Staircase', 1, 5544.5000, NULL, N'', '2026-06-12', N'seed@jewelgroup.co.uk', '2026-06-19', NULL)
) AS source (VariationOrderId, ProjectId, VariationOrderQuoteId, RequestId, Number, VariationRef,
             Title, Description, Status, Value, SubcontractorId, CostCode,
             ApprovedAt, ApprovedByEmail, IssuedAt, CancelledAt)
ON target.VariationOrderId = source.VariationOrderId
WHEN MATCHED THEN UPDATE SET
    ProjectId             = source.ProjectId,
    VariationOrderQuoteId = source.VariationOrderQuoteId,
    RequestId             = source.RequestId,
    Number                = source.Number,
    VariationRef          = source.VariationRef,
    Title                 = source.Title,
    Description           = source.Description,
    Status                = source.Status,
    Value                 = source.Value,
    SubcontractorId       = source.SubcontractorId,
    CostCode              = source.CostCode,
    ApprovedAt            = source.ApprovedAt,
    ApprovedByEmail       = source.ApprovedByEmail,
    IssuedAt              = source.IssuedAt,
    CancelledAt           = source.CancelledAt
WHEN NOT MATCHED BY TARGET THEN
    INSERT (VariationOrderId, ProjectId, VariationOrderQuoteId, RequestId, Number, VariationRef,
            Title, Description, Status, Value, SubcontractorId, CostCode,
            ApprovedAt, ApprovedByEmail, IssuedAt, CancelledAt)
    VALUES (source.VariationOrderId, source.ProjectId, source.VariationOrderQuoteId,
            source.RequestId, source.Number, source.VariationRef, source.Title,
            source.Description, source.Status, source.Value, source.SubcontractorId,
            source.CostCode, source.ApprovedAt, source.ApprovedByEmail,
            source.IssuedAt, source.CancelledAt);
GO

-- Sanity checks: agreed-only on the report; quotes carry the TBR value.
SELECT
    (SELECT COUNT(*)   FROM [dbo].[ValuationLineItems]    WHERE ProjectId = N'4ec1ad1ca3a440c69f32f46f73aea005' AND ElementType = 3) AS VariationLines,       -- 5
    (SELECT SUM(LineAmount) FROM [dbo].[ValuationLineItems] WHERE ProjectId = N'4ec1ad1ca3a440c69f32f46f73aea005' AND ElementType = 3 AND LineType NOT IN (3,4)) AS NetVariations, -- 17450.50
    (SELECT COUNT(*)   FROM [dbo].[VariationOrderQuotes]  WHERE ProjectId = N'4ec1ad1ca3a440c69f32f46f73aea005') AS VariationOrderQuotes, -- 23
    (SELECT COUNT(*)   FROM [dbo].[VariationOrders]       WHERE ProjectId = N'4ec1ad1ca3a440c69f32f46f73aea005') AS VariationOrders,      -- 5
    (SELECT SUM(EstimatedValue) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = N'4ec1ad1ca3a440c69f32f46f73aea005' AND Status = 2) AS TbrQuoteValue; -- 20105.50
GO

-- Combined check: Contract Sum + agreed Net Variations = Revised Contract Sum.
SELECT
    SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 298946.04
    SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  17450.50
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 316396.54
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'4ec1ad1ca3a440c69f32f46f73aea005';
GO
