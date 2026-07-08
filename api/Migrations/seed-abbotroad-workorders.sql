-- ============================================================================
-- Seed: Abbot Road -- work orders (Buildertrend purchase orders PO-01..PO-09)
-- ----------------------------------------------------------------------------
-- Project : 17a Abbot Road, Guildford, GU1 3TA  (LA - Abbot Road)
-- ProjectId: 4ec1ad1ca3a440c69f32f46f73aea005
--
-- Seeds the nine Buildertrend purchase orders into WorkOrders/WorkOrderLines,
-- printed 8 Jul 2026. Buildertrend PO numbers are kept (PO-01 -> WO-0001) --
-- numbering is per project, matching Buildertrend's per-job sequence.
-- SourceReference holds the Buildertrend poid from the print URL, so the
-- source record is always traceable and the seed is idempotent.
--
-- Cost-code mapping (Buildertrend 00006-xx -> JBB Cost Code Master v2.1,
-- confirmed by Nigel Reilly 8 Jul 2026). Each line keeps the original code in
-- LegacyCostCode for the audit trail:
--
--     00006-01 Demolition          -> ENABLE-DEM
--     00006-02 Foundations         -> SUB-PIL   (Capital Piling order)
--                                  -> ENABLE-COR (Robore core drilling)
--     00006-05 Scaffolding         -> SCAFF-STD
--     00006-10 Windows & Doors     -> WDR-ALU   (Cedar Views / Velfac)
--                                  -> WDR-SPG   (IQ Glass specialist glazing)
--     00006-15 Electrics           -> ELE-STD
--     00006-25 Asbestos PS         -> ENABLE-ASB
--     00006-27 Structural Steel PS -> STR-STL
--
-- Status: all nine orders are Released (1). Two prints show "Date Released
-- N/A" (PO-02, PO-07); their approval dates are used as AwardedAt instead.
-- AwardedByEmail is the internal approver on every print (James Clark).
--
-- Subcontractors: matched to the company directory by CompanyName
-- (case-insensitive); a minimal directory record is inserted only when no
-- match exists (stable ids sub-ar-*). Existing records are never modified.
--
-- Idempotent: subcontractors keyed on CompanyName; orders on WorkOrderId
-- (ar-wo-NN); lines on WorkOrderLineId (ar-wo-NN-lN). Lines no longer in this
-- script are deleted for these orders only. Safe to run repeatedly.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Subcontractors — insert-if-missing, matched by company name
-- ----------------------------------------------------------------------------
MERGE INTO [dbo].[Subcontractors] AS target
USING (VALUES
    (N'sub-ar-steelteam',  N'The Steel Team',                   N'020 3162 7777',  N'',              N''),
    (N'sub-ar-stripclear', N'Strip & Clear Services LTD',       N'',               N'Bexhill-On-Sea', N''),
    (N'sub-ar-rjswaste',   N'RJS Waste Management UK Limited',  N'01243213273',    N'Chichester',    N''),
    (N'sub-ar-cedarviews', N'Cedar Views Windows Ltd',          N'',               N'Chobham',       N'Surrey'),
    (N'sub-ar-capitalpil', N'Capital Piling Ltd',               N'',               N'London',        N''),
    (N'sub-ar-iqglass',    N'IQ Glass UK',                      N'',               N'Amersham',      N'Bucks'),
    (N'sub-ar-robore',     N'Robore - Concrete Cutting',        N'',               N'',              N''),
    (N'sub-ar-njwscaff',   N'NJW Scaffolding',                  N'020 8226 4285',  N'Beckenham',     N''),
    (N'sub-ar-anythingel', N'Anything Electrical (London) LTD', N'07794 676 569',  N'Epsom',         N'Surrey')
) AS source ([SubcontractorId], [CompanyName], [ContactPhone], [Town], [County])
ON LOWER(target.[CompanyName]) = LOWER(source.[CompanyName])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([SubcontractorId], [CompanyName], [ContactName], [ContactEmail], [ContactPhone],
            [CisStatus], [OnboardedAt], [Category], [MobileNumber], [Town], [County],
            [Website], [Pli], [PliExpiry])
    VALUES (source.[SubcontractorId], source.[CompanyName], N'', N'', source.[ContactPhone],
            N'', SYSDATETIMEOFFSET(), 0, N'', source.[Town], source.[County],
            N'', N'', N'');
GO

-- ----------------------------------------------------------------------------
-- 2. Work orders — resolve subcontractor ids by name, then MERGE the headers
-- ----------------------------------------------------------------------------
DECLARE @proj nvarchar(64) = N'4ec1ad1ca3a440c69f32f46f73aea005';
DECLARE @approver nvarchar(256) = N'james.clark@jewelbb.co.uk';

DECLARE @subSteelTeam  nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'The Steel Team'));
DECLARE @subStripClear nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Strip & Clear Services LTD'));
DECLARE @subRjsWaste   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'RJS Waste Management UK Limited'));
DECLARE @subCedarViews nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Cedar Views Windows Ltd'));
DECLARE @subCapitalPil nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Capital Piling Ltd'));
DECLARE @subIqGlass    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'IQ Glass UK'));
DECLARE @subRobore     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Robore - Concrete Cutting'));
DECLARE @subNjwScaff   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'NJW Scaffolding'));
DECLARE @subAnythingEl nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Anything Electrical (London) LTD'));

MERGE INTO [dbo].[WorkOrders] AS target
USING (VALUES
    -- (WorkOrderId, SubcontractorId, Number, Title, Value, Scope, CreatedAt, AwardedAt, ScheduledCompletion, SourceReference)
    (N'ar-wo-01', @subSteelTeam,  1, N'The Steel Team',              12580.00, N'Supply as per breakdown 28th Jan and drawing REV F (Gal columns included). Fabricated steel members B1-B26 and columns C1-C11 (CHS galvanised), loose plates, priming (zinc phosphate) and delivery.',
        '2025-06-09T00:00:00+00:00', '2026-01-22T00:00:00+00:00', '2026-02-13T00:00:00+00:00', N'buildertrend:68423965'),
    (N'ar-wo-02', @subStripClear, 2, N'Strip & Clear - Demolition',   3800.00, N'Demolition & rubbish removal as per demo plans.',
        '2025-06-23T00:00:00+00:00', '2025-06-26T00:00:00+00:00', '2025-06-20T00:00:00+00:00', N'buildertrend:68690460'),
    (N'ar-wo-03', @subRjsWaste,   3, N'Asbestos Survey',               165.00, N'As per quote ENQ7931-V1. Supply all labour and materials to attend site and collect ground samples; samples analysed by UKAS-accredited analytical company using polarised microscopy.',
        '2025-06-24T00:00:00+00:00', '2025-06-24T00:00:00+00:00', '2025-06-25T00:00:00+00:00', N'buildertrend:68721807'),
    (N'ar-wo-04', @subCedarViews, 4, N'Velfac Windows',               3777.60, N'Velfac window & door as per Dwg A1986_130B - Proposed Sections & Window-Door Schedule. Supply and installation, subject to survey (survey included for ordering purposes).',
        '2025-07-16T00:00:00+00:00', '2025-07-16T00:00:00+00:00', '2025-09-26T00:00:00+00:00', N'buildertrend:69122857'),
    (N'ar-wo-05', @subCapitalPil, 5, N'Capital Piling - 6539 REV 03', 27448.00, N'As per quote #6539_REV.03. SFA piling rig setup and per-location mobilisation, 300mm dia SFA open bore piles (avg 6m), engineer''s pile design, pile integrity testing, RC ground beams (finished below ground, beam form and standard reinforcement per client BBS), 85mm Cellcore anti-heave to beam undersides.',
        '2025-08-11T00:00:00+00:00', '2025-09-26T00:00:00+00:00', '2025-10-31T00:00:00+00:00', N'buildertrend:69600731'),
    (N'ar-wo-06', @subIqGlass,    6, N'IQ Glass - Q55861 - CSA005',  40949.03, N'As per Q55861 - CSA005 - 12th September 2025 (include extra over costs). Staged payments: 20% on order, 40% on design approval, 40% seven days prior to installation.',
        '2025-09-16T00:00:00+00:00', '2026-01-05T00:00:00+00:00', '2026-01-30T00:00:00+00:00', N'buildertrend:70292053'),
    (N'ar-wo-07', @subRobore,     7, N'Core Holes',                   1425.00, N'2x 500dia core holes at depth of 400mm.',
        '2025-11-03T00:00:00+00:00', '2025-11-03T00:00:00+00:00', '2025-10-31T00:00:00+00:00', N'buildertrend:71227083'),
    (N'ar-wo-08', @subNjwScaff,   8, N'Scaffolding',                  2100.00, N'Scaffolding to the front elevation to provide safe access to the balcony: 10 metres to the front elevation, 1x 4.5 metre return, 1x 2.5 metre return, 2 lifts at 2.5 metres high. Two phases, March-May 26.',
        '2026-02-23T00:00:00+00:00', '2026-02-23T00:00:00+00:00', '2026-05-29T00:00:00+00:00', N'buildertrend:73176995'),
    (N'ar-wo-09', @subAnythingEl, 9, N'Anything Electrical',            80.00, N'Temp works - adapting wiring for steel installation. Main package TBC. Two phases (1st & 2nd fix).',
        '2026-03-05T00:00:00+00:00', '2026-03-05T00:00:00+00:00', '2026-07-31T00:00:00+00:00', N'buildertrend:73399733')
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
-- 3. Work order lines — one row per printed item, cost code mapped per header
-- ----------------------------------------------------------------------------
MERGE INTO [dbo].[WorkOrderLines] AS target
USING (VALUES
    -- (WorkOrderLineId, WorkOrderId, Title, Description, CostCode, LegacyCostCode, UnitCost, LineTotal, PaidToDate, SortOrder)
    -- PO-01 The Steel Team (supply line priced GBP 0.00 on the order: GBP 9,495 paid by Chandlers directly)
    (N'ar-wo-01-l1', N'ar-wo-01', N'The Steel Team - Supply',              N'As per Invoice INV-72170 issued 28th January 26. Paid by Chandlers directly - GBP 9,495.', N'STR-STL', N'00006-27 Structural Steel PS',     0.00,     0.00,     0.00, 1),
    (N'ar-wo-01-l2', N'ar-wo-01', N'The Steel Team - Installation',        N'As per invoice INV-72274 issued 28th January 26',                                          N'STR-STL', N'00006-27 Structural Steel PS', 10900.00, 10900.00, 10900.00, 2),
    (N'ar-wo-01-l3', N'ar-wo-01', N'The Steel Team - V01',                 N'As per invoice INV-74305',                                                                 N'STR-STL', N'00006-27 Structural Steel PS',   130.00,   130.00,   130.00, 3),
    (N'ar-wo-01-l4', N'ar-wo-01', N'The Steel Team - V02',                 N'As per INV-74632 - Variation Works Agreed With Nigel Reilly 04/03/2026',                   N'STR-STL', N'00006-27 Structural Steel PS',  1175.00,  1175.00,  1175.00, 4),
    (N'ar-wo-01-l5', N'ar-wo-01', N'The Steel Team - V03 - Site Welder',   N'',                                                                                         N'STR-STL', N'00006-27 Structural Steel PS',   375.00,   375.00,   375.00, 5),
    -- PO-02 Strip & Clear
    (N'ar-wo-02-l1', N'ar-wo-02', N'Strip & Clear - Demolition',           N'As per quote provided includes waste removal',                                             N'ENABLE-DEM', N'00006-01 Demolition',        3800.00,  3800.00,  3800.00, 1),
    -- PO-03 RJS Waste
    (N'ar-wo-03-l1', N'ar-wo-03', N'Asbestos Survey',                      N'As per quote ENQ7931-V1',                                                                  N'ENABLE-ASB', N'00006-25 Asbestos PS',        165.00,   165.00,   165.00, 1),
    -- PO-04 Cedar Views (nothing paid yet)
    (N'ar-wo-04-l1', N'ar-wo-04', N'Velfac Window & Doors - Supply',       N'',                                                                                         N'WDR-ALU', N'00006-10 Windows & Doors',     3027.60,  3027.60,     0.00, 1),
    (N'ar-wo-04-l2', N'ar-wo-04', N'Installation Only',                    N'',                                                                                         N'WDR-ALU', N'00006-10 Windows & Doors',      750.00,   750.00,     0.00, 2),
    -- PO-05 Capital Piling (fully paid)
    (N'ar-wo-05-l1', N'ar-wo-05', N'SFA Open Bore Piles including design', N'',                                                                                         N'SUB-PIL', N'00006-02 Foundations',         8135.00,  8135.00,  8135.00, 1),
    (N'ar-wo-05-l2', N'ar-wo-05', N'RC Ground Beam as per client design',  N'',                                                                                         N'SUB-PIL', N'00006-02 Foundations',        13048.00, 13048.00, 13048.00, 2),
    (N'ar-wo-05-l3', N'ar-wo-05', N'Additional Pile - P7',                 N'Additional pile in location P7 to suit SE design',                                         N'SUB-PIL', N'00006-02 Foundations',          670.00,   670.00,   670.00, 3),
    (N'ar-wo-05-l4', N'ar-wo-05', N'Standing Charge',                      N'',                                                                                         N'SUB-PIL', N'00006-02 Foundations',         5595.00,  5595.00,  5595.00, 4),
    -- PO-06 IQ Glass (GBP 24,569.42 paid, GBP 16,379.61 remaining)
    (N'ar-wo-06-l1', N'ar-wo-06', N'IQ Glass - Q55861 - CSA005',           N'As per IQ Glass - Q55861 - CSA005 issued 12th September 2025',                             N'WDR-SPG', N'00006-10 Windows & Doors PS', 40949.03, 40949.03, 24569.42, 1),
    -- PO-07 Robore
    (N'ar-wo-07-l1', N'ar-wo-07', N'Core Drilling - Robore',               N'As per invoice rate.',                                                                     N'ENABLE-COR', N'00006-02 Foundations',       1425.00,  1425.00,  1425.00, 1),
    -- PO-08 NJW Scaffolding (nothing paid yet)
    (N'ar-wo-08-l1', N'ar-wo-08', N'Scaffolding',                          N'As per quote provided 23rd Feb 2026',                                                      N'SCAFF-STD', N'00006-05 Scaffolding',        2100.00,  2100.00,     0.00, 1),
    -- PO-09 Anything Electrical (nothing paid yet)
    (N'ar-wo-09-l1', N'ar-wo-09', N'Temp Works - Adapt Wiring',            N'Temp Works',                                                                               N'ELE-STD', N'00006-15 Electrics',             80.00,    80.00,     0.00, 1)
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
WHEN NOT MATCHED BY SOURCE AND target.[WorkOrderLineId] LIKE N'ar-wo-%' THEN
    DELETE;
GO

-- ----------------------------------------------------------------------------
-- 4. Reconciliation — totals should match the printed orders
-- ----------------------------------------------------------------------------
-- Expected: 9 orders, GBP 92,324.63 committed, GBP 69,987.42 paid.
-- Per print: PO-01 12,580.00/12,580.00 · PO-02 3,800.00/3,800.00
--            PO-03 165.00/165.00       · PO-04 3,777.60/0.00
--            PO-05 27,448.00/27,448.00 · PO-06 40,949.03/24,569.42
--            PO-07 1,425.00/1,425.00   · PO-08 2,100.00/0.00
--            PO-09 80.00/0.00
SELECT w.[Number], w.[Title], w.[Value] AS [OrderValue],
       lines.[LineTotal], lines.[Paid],
       CASE WHEN w.[Value] <> lines.[LineTotal] THEN N'MISMATCH' ELSE N'' END AS [Check]
FROM [dbo].[WorkOrders] w
CROSS APPLY (
    SELECT SUM(l.[LineTotal]) AS [LineTotal], SUM(l.[PaidToDate]) AS [Paid]
    FROM [dbo].[WorkOrderLines] l
    WHERE l.[WorkOrderId] = w.[WorkOrderId]
) lines
WHERE w.[ProjectId] = N'4ec1ad1ca3a440c69f32f46f73aea005'
ORDER BY w.[Number];
