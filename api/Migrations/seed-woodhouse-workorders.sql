-- ============================================================================
-- Seed: Woodhouse -- work orders (Buildertrend purchase orders PO-01..PO-08)
-- ----------------------------------------------------------------------------
-- Project : Nichols Nymet, Woodhouse Lane  (MVL - Nichols Nymet Woodhouse Lane)
-- ProjectId: c16a737d8e1347f28917183b77360f1d
--
-- Seeds the eight Buildertrend purchase orders into WorkOrders/WorkOrderLines,
-- printed 8 Jul 2026. SourceReference holds the Buildertrend poid.
--
-- Cost-code mapping (Buildertrend -> JBB Cost Code Master v2.1), consistent
-- with the Abbot Road / By France seeds; originals kept in LegacyCostCode:
--
--     00006-01 Demolition          -> ENABLE-DEM
--     00006-02 Foundations         -> SUB-GWK   (Strip & Clear groundworks)
--     00006-04 Masonry             -> MASON-BRK
--     00006-05 Scaffolding         -> SCAFF-STD
--     00006-06 Carpentry 1st Fix   -> CARP-1FX
--     00006-07 Roofing & Guttering -> ROOF-FLT  (Dorking: EPDM flat membrane
--                                                roof, not general roofing)
--     00006-10 Windows & Doors     -> WDR-SPG   (Fluid Glass structural
--                                                glazing, as IQ Glass)
--     00006-13 Plumbing            -> MEC-PLM
--     00006-15 Electrics           -> ELE-STD
--     00006-27 Structural Steel PS -> STR-STL
--
-- Status: all eight orders are Released (1). PO-02 prints "Date Released N/A";
-- its approval date (23 Oct 2025) is used as AwardedAt. AwardedByEmail is the
-- internal approver on the prints (James Clark).
--
-- Notes preserved from the prints: PO-04 carries a negative "Client Deposit
-- Payment" omit line (-GBP 4,240.14); PO-05 spans four cost centres
-- (groundworks, steel, masonry, carpentry) on one order.
--
-- Subcontractors: matched by CompanyName, insert-if-missing (sub-wh-*).
-- "The Steel Team" resolves via prefix match as in the By France seed.
--
-- Idempotent: orders keyed on WorkOrderId (wh-wo-NN), lines on WorkOrderLineId
-- (wh-wo-NN-lN); lines no longer in this script are deleted for these orders
-- only. Safe to run repeatedly.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Subcontractors — insert-if-missing, matched by company name
-- ----------------------------------------------------------------------------
MERGE INTO [dbo].[Subcontractors] AS target
USING (VALUES
    (N'sub-wh-fluidglass', N'Fluid Glass'),
    (N'sub-wh-wilson',     N'Wilson Group'),
    (N'sub-wh-dorking',    N'Dorking Roofing Limited'),
    (N'sub-wh-anythingel', N'Anything Electrical (London) LTD'),
    (N'sub-wh-stripclear', N'Strip & Clear Services LTD'),
    (N'sub-wh-njwscaff',   N'NJW Scaffolding')
) AS source ([SubcontractorId], [CompanyName])
ON LOWER(target.[CompanyName]) = LOWER(source.[CompanyName])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([SubcontractorId], [CompanyName], [ContactName], [ContactEmail], [ContactPhone],
            [CisStatus], [OnboardedAt], [Category], [MobileNumber], [Town], [County],
            [Website], [Pli], [PliExpiry])
    VALUES (source.[SubcontractorId], source.[CompanyName], N'', N'', N'',
            N'', SYSDATETIMEOFFSET(), 0, N'', N'', N'',
            N'', N'', N'');
GO

-- ----------------------------------------------------------------------------
-- 2. Work orders
-- ----------------------------------------------------------------------------
DECLARE @proj nvarchar(64) = N'c16a737d8e1347f28917183b77360f1d';
DECLARE @approver nvarchar(256) = N'james.clark@jewelbb.co.uk';

DECLARE @subAnythingEl nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Anything Electrical (London) LTD'));
DECLARE @subStripClear nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Strip & Clear Services LTD'));
DECLARE @subSteelTeam  nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) LIKE N'the steel team%' ORDER BY [SubcontractorId]);
DECLARE @subFluidGlass nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Fluid Glass'));
DECLARE @subNjwScaff   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'NJW Scaffolding'));
DECLARE @subWilson     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Wilson Group'));
DECLARE @subDorking    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Dorking Roofing Limited'));

MERGE INTO [dbo].[WorkOrders] AS target
USING (VALUES
    -- (WorkOrderId, SubcontractorId, Number, Title, Value, Scope, CreatedAt, AwardedAt, ScheduledCompletion, SourceReference)
    (N'wh-wo-01', @subAnythingEl, 1, N'Anything Electrical - Contract REV B',   43755.00, N'As per quote and scope 22/10/2025 - REV B.',
        '2025-10-23T00:00:00+00:00', '2026-05-21T00:00:00+00:00', '2026-08-28T00:00:00+00:00', N'buildertrend:71043711'),
    (N'wh-wo-02', @subStripClear, 2, N'Strip & Clear - Phase 1',                22619.53, N'Phase 1 demolition as per quote specification 22/10/2025.',
        '2025-10-23T00:00:00+00:00', '2025-10-23T00:00:00+00:00', '2025-11-07T00:00:00+00:00', N'buildertrend:71043841'),
    (N'wh-wo-03', @subSteelTeam,  3, N'The Steel Team - Supply & Installation', 27485.00, N'',
        '2025-10-24T00:00:00+00:00', '2025-10-24T00:00:00+00:00', '2026-03-20T00:00:00+00:00', N'buildertrend:71070311'),
    (N'wh-wo-04', @subFluidGlass, 4, N'Fluid Glass - FGQ1024 CSA004',           79423.81, N'As per quote Fluid Glass - FGQ1024 CSA004 - 10/01/2025 & drawing pack FGP382_Rev.B.',
        '2025-10-31T00:00:00+00:00', '2025-10-31T00:00:00+00:00', '2026-05-29T00:00:00+00:00', N'buildertrend:71199691'),
    (N'wh-wo-05', @subStripClear, 5, N'Strip & Clear - Ground Works & Steel',  101380.19, N'',
        '2025-12-09T00:00:00+00:00', '2026-02-16T00:00:00+00:00', '2026-01-30T00:00:00+00:00', N'buildertrend:71877834'),
    (N'wh-wo-06', @subNjwScaff,   6, N'NJW Scaffolding',                         1700.00, N'As per quote provided 23rd Feb 26.',
        '2026-02-23T00:00:00+00:00', '2026-02-23T00:00:00+00:00', '2026-06-26T00:00:00+00:00', N'buildertrend:73177145'),
    (N'wh-wo-07', @subWilson,     7, N'Plumbing & Heating',                     35000.00, N'As per revised email quote on 22nd May 2026.',
        '2026-05-22T00:00:00+00:00', '2026-05-22T00:00:00+00:00', '2026-08-28T00:00:00+00:00', N'buildertrend:74971152'),
    (N'wh-wo-08', @subDorking,    8, N'Dorking Roofing Woodhouse Lane',         12462.38, N'New EPDM roof to rear single-storey extension; Code 4 lead cover flashing to the wall; return visits to dress the EPDM into the gutter. Based on warm-roof insulation with the whole roof fitted with OSB.',
        '2026-06-16T00:00:00+00:00', '2026-06-16T00:00:00+00:00', '2026-07-07T00:00:00+00:00', N'buildertrend:75396955')
) AS source ([WorkOrderId], [SubcontractorId], [Number], [Title], [Value], [Scope], [CreatedAt], [AwardedAt], [ScheduledCompletion], [SourceReference])
ON target.[WorkOrderId] = source.[WorkOrderId]
WHEN MATCHED THEN
    UPDATE SET
        target.[ProjectId]           = @proj,
        target.[BidPackageId]        = NULL,
        target.[SubcontractorId]     = source.[SubcontractorId],
        target.[Value]               = source.[Value],
        target.[Scope]               = source.[Scope],
        target.[AwardedAt]           = source.[AwardedAt],
        target.[AwardedByEmail]      = @approver,
        target.[Number]              = source.[Number],
        target.[Title]               = source.[Title],
        target.[Status]              = 1,  -- Released
        target.[CreatedAt]           = source.[CreatedAt],
        target.[ScheduledCompletion] = source.[ScheduledCompletion],
        target.[SourceReference]     = source.[SourceReference]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([WorkOrderId], [ProjectId], [BidPackageId], [SubcontractorId], [Value], [Scope],
            [AwardedAt], [AwardedByEmail], [Number], [Title], [Status], [CreatedAt],
            [ScheduledCompletion], [SourceReference])
    VALUES (source.[WorkOrderId], @proj, NULL, source.[SubcontractorId], source.[Value], source.[Scope],
            source.[AwardedAt], @approver, source.[Number], source.[Title], 1, source.[CreatedAt],
            source.[ScheduledCompletion], source.[SourceReference]);

-- ----------------------------------------------------------------------------
-- 3. Work order lines
-- ----------------------------------------------------------------------------
MERGE INTO [dbo].[WorkOrderLines] AS target
USING (VALUES
    -- (WorkOrderLineId, WorkOrderId, Title, Description, CostCode, LegacyCostCode, UnitCost, LineTotal, PaidToDate, SortOrder)
    (N'wh-wo-01-l1', N'wh-wo-01', N'Main Contract - REV B', N'As per quote and scope 22/10/2025', N'ELE-STD', N'00006-15 Electrics', 43375.00, 43375.00, 0, 1),
    (N'wh-wo-01-l2', N'wh-wo-01', N'Site Temp & Cabin Connection', N'', N'ELE-STD', N'00006-15 Electrics', 380.00, 380.00, 380.00, 2),
    (N'wh-wo-02-l1', N'wh-wo-02', N'Strip & Clear Phase 1 - Demolition', N'As per quote specification 22/10/2025', N'ENABLE-DEM', N'00006-01 Demolition', 16050.00, 16050.00, 16050.00, 1),
    (N'wh-wo-02-l2', N'wh-wo-02', N'Strip & Clear Phase 1 - Demolition V01', N'Removal of hedge & crazy paving; removal of fireplace in second lounge; form opening from hallway into kitchen & lintel.', N'ENABLE-DEM', N'00006-01 Demolition', 6569.53, 6569.53, 6569.53, 2),
    (N'wh-wo-03-l1', N'wh-wo-03', N'The Steel Team - Goal Post INV-69745', N'', N'STR-STL', N'00006-27 Structural Steel PS', 4280.00, 4280.00, 4280.00, 1),
    (N'wh-wo-03-l2', N'wh-wo-03', N'The Steel Team - Supply INV-74799', N'As per quote 9th March - INV-74799', N'STR-STL', N'00006-27 Structural Steel PS', 23205.00, 23205.00, 23205.00, 2),
    (N'wh-wo-04-l1', N'wh-wo-04', N'Fluid Glass - FGQ1024 CSA004', N'As per quote Fluid Glass - FGQ1024 CSA004 - 10/01/2025 & drawing pack FGP382_Rev.B', N'WDR-SPG', N'00006-10 Windows & Doors', 81913.95, 81913.95, 44908.23, 1),
    (N'wh-wo-04-l2', N'wh-wo-04', N'Fluid Glass - Client Deposit Payment', N'Omit item', N'WDR-SPG', N'00006-10 Windows & Doors', -4240.14, -4240.14, 0, 2),
    (N'wh-wo-04-l3', N'wh-wo-04', N'Fluid Glass - Variation 01 Lifting Equipment', N'3.00B - Lifting equipment required due to weight of pane (approx 350kg). Approx cost based off assumed suitable access, i.e. no road closure required (glazing with spider crane full contract lift with HIAB delivery).', N'WDR-SPG', N'00006-10 Windows & Doors', 1750.00, 1750.00, 0, 3),
    (N'wh-wo-05-l1', N'wh-wo-05', N'Strip & Clear - Groundworks', N'', N'SUB-GWK', N'00006-02 Foundations', 88162.73, 88162.73, 88162.73, 1),
    (N'wh-wo-05-l2', N'wh-wo-05', N'Strip & Clear - Steel Installation', N'Installation of picture frame steel, ground beam concrete and bricking up.', N'STR-STL', N'00006-27 Structural Steel PS', 8387.46, 8387.46, 8387.46, 2),
    (N'wh-wo-05-l3', N'wh-wo-05', N'V01 - Raised Patio & Tanking', N'', N'SUB-GWK', N'00006-02 Foundations', 2800.00, 2800.00, 2800.00, 3),
    (N'wh-wo-05-l4', N'wh-wo-05', N'V02 - Steel Base Plates', N'Additional base plate adjustment due to picture frame alignment', N'SUB-GWK', N'00006-02 Foundations', 300.00, 300.00, 300.00, 4),
    (N'wh-wo-05-l5', N'wh-wo-05', N'V03 - Steel Connection (Existing Kitchen)', N'', N'STR-STL', N'00006-27 Structural Steel PS', 480.00, 480.00, 480.00, 5),
    (N'wh-wo-05-l6', N'wh-wo-05', N'V04 - Brickwork Rear Elevation', N'', N'MASON-BRK', N'00006-04 Masonry', 870.00, 870.00, 870.00, 6),
    (N'wh-wo-05-l7', N'wh-wo-05', N'V05 - Timber Joist & Hangers (Existing Kitchen)', N'', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 380.00, 380.00, 380.00, 7),
    (N'wh-wo-06-l1', N'wh-wo-06', N'NJW Scaffolding', N'As per quote provided 23rd Feb 26', N'SCAFF-STD', N'00006-05 Scaffolding', 1700.00, 1700.00, 0, 1),
    (N'wh-wo-07-l1', N'wh-wo-07', N'Plumbing & Heating', N'As per revised email quote on 22nd May 2026', N'MEC-PLM', N'00006-13 Plumbing', 35000.00, 35000.00, 0, 1),
    (N'wh-wo-08-l1', N'wh-wo-08', N'Roofing works', N'New EPDM roof to rear single-storey extension; Code 4 lead cover flashing to the wall; return visits to dress the EPDM into the gutter. Warm-roof insulation, whole roof fitted with OSB.', N'ROOF-FLT', N'00006-07 Roofing & Guttering', 12462.38, 12462.38, 0, 1)
) AS source ([WorkOrderLineId], [WorkOrderId], [Title], [Description], [CostCode], [LegacyCostCode], [UnitCost], [LineTotal], [PaidToDate], [SortOrder])
ON target.[WorkOrderLineId] = source.[WorkOrderLineId]
WHEN MATCHED THEN
    UPDATE SET
        target.[WorkOrderId]    = source.[WorkOrderId],
        target.[Title]          = source.[Title],
        target.[Description]    = source.[Description],
        target.[CostType]       = N'Subcontractor',
        target.[CostCode]       = source.[CostCode],
        target.[LegacyCostCode] = source.[LegacyCostCode],
        target.[Quantity]       = 1.00,
        target.[Unit]           = N'item',
        target.[UnitCost]       = source.[UnitCost],
        target.[LineTotal]      = source.[LineTotal],
        target.[PaidToDate]     = source.[PaidToDate],
        target.[SortOrder]      = source.[SortOrder]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([WorkOrderLineId], [WorkOrderId], [Title], [Description], [CostType], [CostCode],
            [LegacyCostCode], [Quantity], [Unit], [UnitCost], [LineTotal], [PaidToDate], [SortOrder])
    VALUES (source.[WorkOrderLineId], source.[WorkOrderId], source.[Title], source.[Description],
            N'Subcontractor', source.[CostCode], source.[LegacyCostCode], 1.00, N'item',
            source.[UnitCost], source.[LineTotal], source.[PaidToDate], source.[SortOrder])
WHEN NOT MATCHED BY SOURCE AND target.[WorkOrderLineId] LIKE N'wh-wo-%' THEN
    DELETE;
GO

-- ----------------------------------------------------------------------------
-- 4. Reconciliation — totals should match the printed orders
-- ----------------------------------------------------------------------------
-- Expected: 8 orders, GBP 323,825.91 committed, GBP 196,772.95 paid, 19 lines.
-- Every row's Check column should be blank (no MISMATCH).
SELECT w.[Number], w.[Title], w.[Value] AS [OrderValue],
       lines.[LineTotal], lines.[Paid],
       CASE WHEN w.[Value] <> lines.[LineTotal] THEN N'MISMATCH' ELSE N'' END AS [Check]
FROM [dbo].[WorkOrders] w
CROSS APPLY (
    SELECT SUM(l.[LineTotal]) AS [LineTotal], SUM(l.[PaidToDate]) AS [Paid]
    FROM [dbo].[WorkOrderLines] l
    WHERE l.[WorkOrderId] = w.[WorkOrderId]
) lines
WHERE w.[ProjectId] = N'c16a737d8e1347f28917183b77360f1d'
ORDER BY w.[Number];
