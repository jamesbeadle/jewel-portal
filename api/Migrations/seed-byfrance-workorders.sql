-- ============================================================================
-- Seed: By France -- work orders (Buildertrend purchase orders PO-01..PO-37)
-- ----------------------------------------------------------------------------
-- Project : By France, Leas Green, Chislehurst, BR7 6HD  (PLG - By France)
-- ProjectId: 3490f944b29545c4b8d5a04130f42ab8
--
-- Seeds the 36 Buildertrend purchase orders (PO-21 was never raised; the
-- number is skipped here too so paperwork cross-references hold) into
-- WorkOrders/WorkOrderLines, printed 8 Jul 2026. SourceReference holds the
-- Buildertrend poid from the print URL.
--
-- Cost-code mapping (Buildertrend -> JBB Cost Code Master v2.1). Each line
-- keeps the original code in LegacyCostCode for the audit trail:
--
--     00006-01 Demolition          -> ENABLE-DEM
--                                  -> SCAFF-STD  (PO-04 Burns: scaffold for the
--                                                 demolition phase, coded to
--                                                 Demolition in Buildertrend)
--     00003-2  Rubbish Removal     -> ENABLE-SKP
--     00006-02 Foundations         -> SUB-GWK    (BVMH is a groundworks package,
--                                                 unlike Abbot Road's piling)
--     00006-03 Drainage & Civil    -> SUB-DRN
--                                  -> EXTW-PAV   (PO-12 "Paved Area - Driveway"
--                                                 line only: paving, not drainage)
--     00006-04 Masonry             -> MASON-BRK
--     00006-05 Scaffolding         -> SCAFF-STD
--     00006-06 Carpentry 1st Fix   -> CARP-1FX
--     00006-07 Roofing & Guttering -> ROOF-RFR
--     00006-08 Staircase           -> STAIR-TIM
--     00006-09 Gypsum Board        -> INT-PLB
--     00006-10 Windows & Doors     -> WDR-ALU    (Generation aluminium casements)
--     00006-11 Screed              -> FLR-SCR
--     00006-12 Plastering          -> INT-PLS
--     00006-12 Render              -> INT-RDR
--     00006-13 Plumbing            -> MEC-PLM
--     00006-15 Electrics           -> ELE-STD
--     00006-22 Hard Landscaping    -> EXTW-PAV   (PO-15 tarmac driveway base)
--                                  -> EXTW-LND   (PO-34 J&L: ground clearance and
--                                                 preparation for fencing)
--     00006-27 Structural Steel PS -> STR-STL
--     00006-43 Trenching & Services-> UTIL-TRN
--     00006-53 Air Con PS          -> MEC-AC
--
-- Status: all 36 orders are Released (1). Where the print shows "Date Released
-- N/A" the approval date is used as AwardedAt (PO-03, 06, 17, 19, 26, 29, 30,
-- 35, 36). AwardedByEmail is the internal approver on the prints (James Clark).
--
-- Subcontractors: matched by CompanyName, insert-if-missing (stable ids
-- sub-bf-*). "The Steel Team Limited" resolves to the existing "The Steel
-- Team" record from the Abbot Road seed via prefix match — same firm, no
-- duplicate created.
--
-- Idempotent: orders keyed on WorkOrderId (bf-wo-NN), lines on WorkOrderLineId
-- (bf-wo-NN-lN); lines no longer in this script are deleted for these orders
-- only. Safe to run repeatedly.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Subcontractors — insert-if-missing, matched by company name
--    (The Steel Team Limited deliberately absent: see prefix match below.)
-- ----------------------------------------------------------------------------
MERGE INTO [dbo].[Subcontractors] AS target
USING (VALUES
    (N'sub-bf-ukpower',    N'UK Power Network'),
    (N'sub-bf-sgn',        N'SGN Connections Ltd'),
    (N'sub-bf-jwplant',    N'JW Plant'),
    (N'sub-bf-burns',      N'Burns Scaffolding Ltd'),
    (N'sub-bf-thameswtr',  N'Thames Water'),
    (N'sub-bf-stripclear', N'Strip & Clear Services LTD'),
    (N'sub-bf-bvmh',       N'BVMH Groundworks Ltd'),
    (N'sub-bf-btm',        N'BTM Southern Brickwork Contractors'),
    (N'sub-bf-njwscaff',   N'NJW Scaffolding'),
    (N'sub-bf-dtcarp',     N'D&T Carpentry LTD'),
    (N'sub-bf-swilliams',  N'S Williams Plumbing & Heating'),
    (N'sub-bf-generation', N'Generation Windows'),
    (N'sub-bf-anythingel', N'Anything Electrical (London) LTD'),
    (N'sub-bf-jpaircon',   N'JP Air conditioning'),
    (N'sub-bf-mgn',        N'MGN Drywall'),
    (N'sub-bf-maroofing',  N'M.A Roofing'),
    (N'sub-bf-jlbrick',    N'J & L Brickwork & Landscaping'),
    (N'sub-bf-stairban',   N'Stair and Banister')
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

-- Steel Team: reuse whichever record exists ("The Steel Team" from the Abbot
-- Road seed, or "The Steel Team Limited"); insert only if neither is present.
IF NOT EXISTS (SELECT 1 FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) LIKE N'the steel team%')
    INSERT INTO [dbo].[Subcontractors]
        ([SubcontractorId], [CompanyName], [ContactName], [ContactEmail], [ContactPhone],
         [CisStatus], [OnboardedAt], [Category], [MobileNumber], [Town], [County],
         [Website], [Pli], [PliExpiry])
    VALUES (N'sub-bf-steelteam', N'The Steel Team Limited', N'', N'', N'',
            N'', SYSDATETIMEOFFSET(), 0, N'', N'', N'', N'', N'', N'');
GO

-- ----------------------------------------------------------------------------
-- 2. Work orders
-- ----------------------------------------------------------------------------
DECLARE @proj nvarchar(64) = N'3490f944b29545c4b8d5a04130f42ab8';
DECLARE @approver nvarchar(256) = N'james.clark@jewelbb.co.uk';

DECLARE @subUkPower    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'UK Power Network'));
DECLARE @subSgn        nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'SGN Connections Ltd'));
DECLARE @subJwPlant    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'JW Plant'));
DECLARE @subBurns      nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Burns Scaffolding Ltd'));
DECLARE @subThames     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Thames Water'));
DECLARE @subStripClear nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Strip & Clear Services LTD'));
DECLARE @subBvmh       nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'BVMH Groundworks Ltd'));
DECLARE @subSteelTeam  nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) LIKE N'the steel team%' ORDER BY [SubcontractorId]);
DECLARE @subBtm        nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'BTM Southern Brickwork Contractors'));
DECLARE @subNjwScaff   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'NJW Scaffolding'));
DECLARE @subDtCarp     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'D&T Carpentry LTD'));
DECLARE @subSWilliams  nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'S Williams Plumbing & Heating'));
DECLARE @subGeneration nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Generation Windows'));
DECLARE @subAnythingEl nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Anything Electrical (London) LTD'));
DECLARE @subJpAirCon   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'JP Air conditioning'));
DECLARE @subMgn        nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'MGN Drywall'));
DECLARE @subMaRoofing  nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'M.A Roofing'));
DECLARE @subJlBrick    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'J & L Brickwork & Landscaping'));
DECLARE @subStairBan   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Stair and Banister'));

MERGE INTO [dbo].[WorkOrders] AS target
USING (VALUES
    -- (WorkOrderId, SubcontractorId, Number, Title, Value, Scope, CreatedAt, AwardedAt, ScheduledCompletion, SourceReference)
    (N'bf-wo-01', @subUkPower,    1,  N'UK Power Temp Supply',                          2817.00, N'',
        '2024-05-16T00:00:00+00:00', '2024-05-16T00:00:00+00:00', '2024-05-24T00:00:00+00:00', N'buildertrend:61380800'),
    (N'bf-wo-02', @subSgn,        2,  N'SGN Gas - New Connection',                      1667.00, N'',
        '2024-05-28T00:00:00+00:00', '2025-08-29T00:00:00+00:00', '2024-05-31T00:00:00+00:00', N'buildertrend:61564194'),
    (N'bf-wo-03', @subJwPlant,    3,  N'Demolition - JW Plant',                        16000.00, N'Demolish & remove one house leaving walls as marked on drawings WD-P-006/WD-E-002; remaining walls stripped back to masonry by hand; chimneys reduced in height by hand from inside the roof area; remove all foundations; excavator supplied for loading & stockpiling crushed concrete. Scaffold by M/C.',
        '2024-05-30T00:00:00+00:00', '2024-08-08T00:00:00+00:00', '2024-06-28T00:00:00+00:00', N'buildertrend:61616772'),
    (N'bf-wo-04', @subBurns,      4,  N'Burns Scaffold - Demolition Phase',             5660.00, N'',
        '2024-06-05T00:00:00+00:00', '2024-06-05T00:00:00+00:00', '2024-06-07T00:00:00+00:00', N'buildertrend:61743290'),
    (N'bf-wo-05', @subSgn,        5,  N'SGN - Disconnection',                           1299.00, N'',
        '2024-06-14T00:00:00+00:00', '2024-06-14T00:00:00+00:00', '2024-06-21T00:00:00+00:00', N'buildertrend:61928634'),
    (N'bf-wo-06', @subUkPower,    6,  N'UK Power - 3 Phase',                           11296.00, N'',
        '2024-06-14T00:00:00+00:00', '2025-09-04T00:00:00+00:00', '2024-07-12T00:00:00+00:00', N'buildertrend:61928643'),
    (N'bf-wo-07', @subAnythingEl, 7,  N'Anything Electrical',                           5280.00, N'',
        '2024-06-14T00:00:00+00:00', '2025-01-17T00:00:00+00:00', '2024-06-28T00:00:00+00:00', N'buildertrend:61928651'),
    (N'bf-wo-08', @subThames,     8,  N'Thames Water Upgrade',                         10980.00, N'',
        '2024-07-12T00:00:00+00:00', '2024-07-12T00:00:00+00:00', '2024-09-27T00:00:00+00:00', N'buildertrend:62458135'),
    (N'bf-wo-09', @subJwPlant,    9,  N'JW Plant - 12472',                               300.00, N'',
        '2024-07-22T00:00:00+00:00', '2024-07-22T00:00:00+00:00', '2024-07-19T00:00:00+00:00', N'buildertrend:62606828'),
    (N'bf-wo-10', @subStripClear, 10, N'Demolition - Phase 1',                         11200.00, N'',
        '2024-08-08T00:00:00+00:00', '2024-08-08T00:00:00+00:00', '2024-07-26T00:00:00+00:00', N'buildertrend:62944777'),
    (N'bf-wo-11', @subStripClear, 11, N'Demolition Phase 2',                           22800.00, N'',
        '2024-12-02T00:00:00+00:00', '2024-12-05T00:00:00+00:00', '2024-12-27T00:00:00+00:00', N'buildertrend:64962150'),
    (N'bf-wo-12', @subBvmh,       12, N'BVMH Groundworks',                            231198.54, N'As per quotes: Receipt_2024-12-19_115827; Foul Drainage outside the boundary 23.01.25; Driveway Drainage MC0476 CIV01 P1 14.02.25.',
        '2024-12-13T00:00:00+00:00', '2024-12-19T00:00:00+00:00', '2025-03-14T00:00:00+00:00', N'buildertrend:65186123'),
    (N'bf-wo-13', @subSteelTeam,  13, N'The Steel Team',                               68935.00, N'As per quote provided 18th December 2024; connection cost 7th January 2025; site survey 14th January 2025; installation 20th January 2025; Variation 01 Phase 1 28th February 2025.',
        '2025-01-06T00:00:00+00:00', '2025-01-06T00:00:00+00:00', '2025-03-28T00:00:00+00:00', N'buildertrend:65444345'),
    (N'bf-wo-14', @subBtm,        14, N'BTM Southern Brickwork',                       71467.50, N'',
        '2025-02-21T00:00:00+00:00', '2025-08-29T00:00:00+00:00', '2025-04-25T00:00:00+00:00', N'buildertrend:66310135'),
    (N'bf-wo-15', @subBvmh,       15, N'BVMH Groundworks Driveway Works',               8000.00, N'',
        '2025-03-12T00:00:00+00:00', '2025-03-12T00:00:00+00:00', '2025-03-18T00:00:00+00:00', N'buildertrend:66662593'),
    (N'bf-wo-16', @subBvmh,       16, N'BVMH Groundworks VO1 Ducting',                  2100.00, N'',
        '2025-03-12T00:00:00+00:00', '2025-03-12T00:00:00+00:00', '2025-03-17T00:00:00+00:00', N'buildertrend:66662908'),
    (N'bf-wo-17', @subNjwScaff,   17, N'NJW Scaffolding',                              37320.00, N'',
        '2025-03-25T00:00:00+00:00', '2025-10-24T00:00:00+00:00', '2025-05-30T00:00:00+00:00', N'buildertrend:66913912'),
    (N'bf-wo-18', @subSteelTeam,  18, N'The Steel Team - V02 - Day Work 55489',          375.00, N'',
        '2025-03-26T00:00:00+00:00', '2025-03-26T00:00:00+00:00', '2025-03-27T00:00:00+00:00', N'buildertrend:66940844'),
    (N'bf-wo-19', @subDtCarp,     19, N'D&T Carp - Roof Structure',                    46262.00, N'',
        '2025-03-31T00:00:00+00:00', '2026-02-02T00:00:00+00:00', '2025-05-30T00:00:00+00:00', N'buildertrend:67035279'),
    (N'bf-wo-20', @subBtm,        20, N'BTM Southern Brickwork - V01 - Day Work',        180.00, N'',
        '2025-04-01T00:00:00+00:00', '2025-04-01T00:00:00+00:00', '2025-03-31T00:00:00+00:00', N'buildertrend:67070861'),
    -- PO-21 was never raised in Buildertrend; the gap is intentional.
    (N'bf-wo-22', @subSteelTeam,  22, N'The Steel Team - V03 - G11 - SITE WORKS - Rev B', 3298.00, N'',
        '2025-05-01T00:00:00+00:00', '2025-05-01T00:00:00+00:00', '2025-05-23T00:00:00+00:00', N'buildertrend:67695997'),
    (N'bf-wo-23', @subBtm,        23, N'BTM Southern Brickwork - V02 - Day Work',        720.00, N'',
        '2025-05-02T00:00:00+00:00', '2025-05-02T00:00:00+00:00', '2025-05-02T00:00:00+00:00', N'buildertrend:67723436'),
    (N'bf-wo-24', @subSWilliams,  24, N'S Williams Plumbing & Heating',                71342.76, N'As per quote provided 19th September 2025 - REV03.',
        '2025-05-20T00:00:00+00:00', '2025-09-22T00:00:00+00:00', '2026-07-31T00:00:00+00:00', N'buildertrend:68064661'),
    (N'bf-wo-25', @subGeneration, 25, N'Generation Windows',                           76151.00, N'As per quote 9125 REV5 and Shop Dwgs REV4 23.05.2025. Thirty-two aluminium casement windows, one aluminium screen.',
        '2025-06-10T00:00:00+00:00', '2026-06-24T00:00:00+00:00', '2025-08-29T00:00:00+00:00', N'buildertrend:68454766'),
    (N'bf-wo-26', @subAnythingEl, 26, N'Anything Electrical',                          97810.00, N'As per the quote provided 12th August 2025 based on drawing issue PRO-064-(WD)-P-150 Rev L GF, P-151 Rev K FF, P-152 SF.',
        '2025-06-10T00:00:00+00:00', '2025-10-21T00:00:00+00:00', '2026-02-27T00:00:00+00:00', N'buildertrend:68455068'),
    (N'bf-wo-27', @subSteelTeam,  27, N'The Steel Team - Variation SF Installation',    1950.00, N'',
        '2025-08-01T00:00:00+00:00', '2025-08-01T00:00:00+00:00', '2025-08-08T00:00:00+00:00', N'buildertrend:69445842'),
    (N'bf-wo-28', @subDtCarp,     28, N'D&T Carp - 1st Fix & Cladding',                10230.00, N'',
        '2025-09-01T00:00:00+00:00', '2025-09-04T00:00:00+00:00', '2025-10-31T00:00:00+00:00', N'buildertrend:70006476'),
    (N'bf-wo-29', @subSteelTeam,  29, N'The Steel Team - Day Work INV-65175',            375.00, N'',
        '2025-09-16T00:00:00+00:00', '2025-09-16T00:00:00+00:00', '2025-09-05T00:00:00+00:00', N'buildertrend:70292166'),
    (N'bf-wo-30', @subSteelTeam,  30, N'The Steel Team - INV-66835',                     635.00, N'',
        '2025-09-16T00:00:00+00:00', '2025-10-20T00:00:00+00:00', '2025-09-19T00:00:00+00:00', N'buildertrend:70292169'),
    (N'bf-wo-31', @subJpAirCon,   31, N'JP Air Conditioning',                          26650.58, N'As per quote issued 12th September 2025 - QU-17567.',
        '2025-09-16T00:00:00+00:00', '2025-10-02T00:00:00+00:00', '2026-01-30T00:00:00+00:00', N'buildertrend:70294474'),
    (N'bf-wo-32', @subMgn,        32, N'MGN Drywall',                                 149617.60, N'As per quotes REV C Drylining & Plastering, REV A Render Works, REV A Screed Works (26th September 2025), plus variations V01-V10.',
        '2025-09-26T00:00:00+00:00', '2026-01-15T00:00:00+00:00', '2026-05-22T00:00:00+00:00', N'buildertrend:70513223'),
    (N'bf-wo-33', @subMaRoofing,  33, N'M.A Roofing',                                  59350.00, N'Materials GBP 21,570: breather membrane, counter batten, batten and fixings; Sandtoft 20/20 tile in Flanders; dry valley, dry ridge and dry verge systems; lead; inline vent tiles.',
        '2025-10-02T00:00:00+00:00', '2026-01-08T00:00:00+00:00', '2025-11-28T00:00:00+00:00', N'buildertrend:70627653'),
    (N'bf-wo-34', @subJlBrick,    34, N'J & L Landscaping',                            42000.00, N'Remove existing fencing and surrounding bushes/shrubs (excluding the Leas Green boundary); chip spoil on site; level and prepare ground for fencing.',
        '2025-10-08T00:00:00+00:00', '2025-10-08T00:00:00+00:00', '2025-10-31T00:00:00+00:00', N'buildertrend:70744991'),
    (N'bf-wo-35', @subSteelTeam,  35, N'The Steel Team - Invoice 67198',                 245.00, N'',
        '2025-10-20T00:00:00+00:00', '2025-10-20T00:00:00+00:00', '2025-10-24T00:00:00+00:00', N'buildertrend:70960365'),
    (N'bf-wo-36', @subSteelTeam,  36, N'The Steel Team - INV-68410',                     350.00, N'',
        '2025-10-30T00:00:00+00:00', '2025-10-30T00:00:00+00:00', '2025-11-07T00:00:00+00:00', N'buildertrend:71173458'),
    (N'bf-wo-37', @subStairBan,   37, N'Stair & Banister',                             33125.00, N'As per quote estimates 1001 & 1011, scope of works attached to order. American white oak staircases, supply and installation.',
        '2025-12-18T00:00:00+00:00', '2025-12-19T00:00:00+00:00', '2026-02-27T00:00:00+00:00', N'buildertrend:72063156')
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
    -- (WorkOrderLineId, WorkOrderId, Title, Description, CostCode, LegacyCostCode, Quantity, UnitCost, LineTotal, PaidToDate, SortOrder)
    (N'bf-wo-01-l1', N'bf-wo-01', N'UK Power Temp Supply', N'', N'ELE-STD', N'00006-15 Electrics', 1, 2817.00, 2817.00, 2817.00, 1),
    (N'bf-wo-02-l1', N'bf-wo-02', N'SGN Gas - New Connection', N'', N'MEC-PLM', N'00006-13 Plumbing', 1, 1667.00, 1667.00, 0, 1),
    (N'bf-wo-03-l1', N'bf-wo-03', N'Demolition', N'Demolish & remove 1 house leaving walls as marked on drawings WD-P-006/WD-E-002; remaining walls stripped back to masonry by hand; chimneys reduced by hand; remove all foundations; excavator for loading & stockpiling crushed concrete.', N'ENABLE-DEM', N'00006-01 Demolition', 1, 16000.00, 16000.00, 16000.00, 1),
    (N'bf-wo-03-l2', N'bf-wo-03', N'Removal of Waste', N'Removal of all demolition waste', N'ENABLE-SKP', N'00003-2 Rubbish Removal', 1, 0, 0, 0, 2),
    (N'bf-wo-04-l1', N'bf-wo-04', N'Burns Scaffold - Demolition Phase', N'', N'SCAFF-STD', N'00006-01 Demolition', 1, 5660.00, 5660.00, 5660.00, 1),
    (N'bf-wo-05-l1', N'bf-wo-05', N'SGN - Disconnection', N'Disconnection only', N'UTIL-TRN', N'00006-43 Trenching & Services', 1, 1299.00, 1299.00, 1299.00, 1),
    (N'bf-wo-06-l1', N'bf-wo-06', N'UK Power - 3 Phase', N'As per quote issued 3rd September 2025', N'UTIL-TRN', N'00006-43 Trenching & Services', 1, 11296.00, 11296.00, 0, 1),
    (N'bf-wo-07-l1', N'bf-wo-07', N'Temp Works', N'UK Power Temp board and connections', N'ELE-STD', N'00006-15 Electrics', 1, 3890.00, 3890.00, 3890.00, 1),
    (N'bf-wo-07-l2', N'bf-wo-07', N'Site Security', N'Supply and installation of 6 x 50W floodlights with PIR', N'ELE-STD', N'00006-15 Electrics', 1, 1390.00, 1390.00, 1390.00, 2),
    (N'bf-wo-08-l1', N'bf-wo-08', N'Thames Water Upgrade', N'', N'UTIL-TRN', N'00006-43 Trenching & Services', 1, 10980.00, 10980.00, 0, 1),
    (N'bf-wo-09-l1', N'bf-wo-09', N'JW Plant - 12472', N'', N'UTIL-TRN', N'00006-43 Trenching & Services', 1, 300.00, 300.00, 300.00, 1),
    (N'bf-wo-10-l1', N'bf-wo-10', N'Demolition - Strip & Clear', N'', N'ENABLE-DEM', N'00006-01 Demolition', 1, 11200.00, 11200.00, 11200.00, 1),
    (N'bf-wo-11-l1', N'bf-wo-11', N'Demolition Phase 2', N'', N'ENABLE-DEM', N'00006-01 Demolition', 1, 19200.00, 19200.00, 19200.00, 1),
    (N'bf-wo-11-l2', N'bf-wo-11', N'Demolition - Variation 1 Clearance', N'', N'ENABLE-DEM', N'00006-01 Demolition', 1, 3600.00, 3600.00, 3600.00, 2),
    (N'bf-wo-12-l1',  N'bf-wo-12', N'Engineer/Prelims', N'', N'SUB-GWK', N'00006-02 Foundations', 1, 8556.00, 8556.00, 8556.00, 1),
    (N'bf-wo-12-l2',  N'bf-wo-12', N'Foundations', N'', N'SUB-GWK', N'00006-02 Foundations', 1, 69999.02, 69999.02, 69999.02, 2),
    (N'bf-wo-12-l3',  N'bf-wo-12', N'Substructure Blockwork', N'', N'SUB-GWK', N'00006-02 Foundations', 1, 28063.80, 28063.80, 28063.80, 3),
    (N'bf-wo-12-l4',  N'bf-wo-12', N'Floor Slab', N'', N'SUB-GWK', N'00006-02 Foundations', 1, 28166.80, 28166.80, 28166.80, 4),
    (N'bf-wo-12-l5',  N'bf-wo-12', N'Scaffold Hard Standing', N'', N'SUB-GWK', N'00006-02 Foundations', 1, 5923.20, 5923.20, 5923.20, 5),
    (N'bf-wo-12-l6',  N'bf-wo-12', N'Service Trenching', N'', N'UTIL-TRN', N'00006-43 Trenching & Services', 1, 3600.00, 3600.00, 3600.00, 6),
    (N'bf-wo-12-l7',  N'bf-wo-12', N'Foul Drainage', N'Foul Drainage as per MC0476 Model1-CIV10', N'SUB-DRN', N'00006-03 Drainage & Civil', 1, 20800.00, 20800.00, 20800.00, 7),
    (N'bf-wo-12-l8',  N'bf-wo-12', N'Surface Water Drainage', N'As per MC0476 Model1-CIV10', N'SUB-DRN', N'00006-03 Drainage & Civil', 1, 35045.00, 35045.00, 35045.00, 8),
    (N'bf-wo-12-l9',  N'bf-wo-12', N'Foul Drainage - Outside of the Boundary', N'Foul connection into Leas Green', N'SUB-DRN', N'00006-03 Drainage & Civil', 1, 5250.00, 5250.00, 5250.00, 9),
    (N'bf-wo-12-l10', N'bf-wo-12', N'Attenuation Tank - MC0476 CIV01 P1', N'MC0476 CIV01 P1', N'SUB-DRN', N'00006-03 Drainage & Civil', 1, 11500.00, 11500.00, 11500.00, 10),
    (N'bf-wo-12-l11', N'bf-wo-12', N'Paved Area - Driveway - MC0476 CIV01 P1', N'MC0476 CIV01 P1', N'EXTW-PAV', N'00006-03 Drainage & Civil', 1, 14294.72, 14294.72, 14294.72, 11),
    (N'bf-wo-13-l1', N'bf-wo-13', N'Steel - Supply', N'Fabrication Drawings, Supply, Fabrication, Steel-to-Steel Fixings, Priming/Galvanising and Delivery.', N'STR-STL', N'00006-27 Structural Steel PS', 1, 43835.00, 43835.00, 43835.00, 1),
    (N'bf-wo-13-l2', N'bf-wo-13', N'Connection Details including Calcs', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 3150.00, 3150.00, 3150.00, 2),
    (N'bf-wo-13-l3', N'bf-wo-13', N'Steel Site Survey', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 400.00, 400.00, 400.00, 3),
    (N'bf-wo-13-l4', N'bf-wo-13', N'Steel - Installation', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 20300.00, 20300.00, 20300.00, 4),
    (N'bf-wo-13-l5', N'bf-wo-13', N'Steel - Variation 01 - Phase 1', N'Variations - Phase 1 installation', N'STR-STL', N'00006-27 Structural Steel PS', 1, 1250.00, 1250.00, 1250.00, 5),
    (N'bf-wo-14-l1', N'bf-wo-14', N'BTM Southern Brickwork', N'', N'MASON-BRK', N'00006-04 Masonry', 1, 59062.50, 59062.50, 55851.00, 1),
    (N'bf-wo-14-l2', N'bf-wo-14', N'BTM Southern Variation Day Works', N'', N'MASON-BRK', N'00006-04 Masonry', 1, 12405.00, 12405.00, 0, 2),
    (N'bf-wo-15-l1', N'bf-wo-15', N'Tarmac base works', N'Supply and install 80mm of tarmac base to the driveway area 160m2', N'EXTW-PAV', N'00006-22 Hard Landscaping', 1, 7000.00, 7000.00, 7000.00, 1),
    (N'bf-wo-15-l2', N'bf-wo-15', N'Crushed concrete works', N'Supply and install crushed concrete to entrance of site.', N'EXTW-PAV', N'00006-22 Hard Landscaping', 1, 1000.00, 1000.00, 1000.00, 2),
    (N'bf-wo-16-l1', N'bf-wo-16', N'Ducting to the Gate area', N'Supply and install x 2 100mm ducting to the gate area.', N'SUB-DRN', N'00006-03 Drainage & Civil', 1, 950.00, 950.00, 950.00, 1),
    (N'bf-wo-16-l2', N'bf-wo-16', N'Ducting to the Pond area', N'Supply and install x2 100mm ducting to pond area.', N'SUB-DRN', N'00006-03 Drainage & Civil', 1, 750.00, 750.00, 750.00, 2),
    (N'bf-wo-16-l3', N'bf-wo-16', N'MDPE water pipe to Pond area', N'Supply x 1 32mm mdpe water pipe to pond area.', N'SUB-DRN', N'00006-03 Drainage & Civil', 1, 400.00, 400.00, 400.00, 3),
    (N'bf-wo-17-l1', N'bf-wo-17', N'NJW Scaffolding', N'External Scaffolding', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 9550.00, 9550.00, 9550.00, 1),
    (N'bf-wo-17-l2', N'bf-wo-17', N'NJW Scaffolding - GF Bird Cages', N'Birdcages as per quote 09.04.2025', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 10750.00, 10750.00, 10750.00, 2),
    (N'bf-wo-17-l3', N'bf-wo-17', N'V01 - NJW Scaffolding - SF Bird Cages', N'Variation - Bird Cages for SF and Roof', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 7500.00, 7500.00, 7500.00, 3),
    (N'bf-wo-17-l4', N'bf-wo-17', N'V02 - NJW Scaffolding - Bridges to GF', N'', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 1200.00, 1200.00, 1200.00, 4),
    (N'bf-wo-17-l5', N'bf-wo-17', N'V03 - NJW Scaffold - Staircase & Crash', N'', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 1020.00, 1020.00, 1020.00, 5),
    (N'bf-wo-17-l6', N'bf-wo-17', N'V04 - NJW Scaffold - Rear Gable Lift', N'', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 500.00, 500.00, 500.00, 6),
    (N'bf-wo-17-l7', N'bf-wo-17', N'V05 - LGF Roofs and crash', N'As per V05', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 6300.00, 6300.00, 6300.00, 7),
    (N'bf-wo-17-l8', N'bf-wo-17', N'V06 - Staircase Adaptions', N'Birdcage and lift for middle and top floor access', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 500.00, 500.00, 0, 8),
    (N'bf-wo-18-l1', N'bf-wo-18', N'V01 - The Steel Team - Day Work', N'Day Work', N'STR-STL', N'00006-27 Structural Steel PS', 1, 375.00, 375.00, 375.00, 1),
    (N'bf-wo-19-l1', N'bf-wo-19', N'Carpentry - Structure', N'Phase 1 of the carpentry package - structure', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 43000.00, 43000.00, 36076.52, 1),
    (N'bf-wo-19-l2', N'bf-wo-19', N'V01 - FF Stud Walls', N'', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 225.00, 225.00, 0, 2),
    (N'bf-wo-19-l3', N'bf-wo-19', N'V02 - Roof Steels', N'', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 720.00, 720.00, 0, 3),
    (N'bf-wo-19-l4', N'bf-wo-19', N'V03 - SF Loft Eves', N'Included on invoice ref: 00936', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 406.00, 406.00, 0, 4),
    (N'bf-wo-19-l5', N'bf-wo-19', N'V04 - Hangers into Blockwork', N'', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 180.00, 180.00, 0, 5),
    (N'bf-wo-19-l6', N'bf-wo-19', N'V05 - Vapor barrier, insulation, osb and fixings', N'', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 516.00, 516.00, 0, 6),
    (N'bf-wo-19-l7', N'bf-wo-19', N'V06 - Spandrel panel with fireboard', N'', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 360.00, 360.00, 0, 7),
    (N'bf-wo-19-l8', N'bf-wo-19', N'V07 - Parapet Wall onto of Blockwork', N'', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 135.00, 135.00, 0, 8),
    (N'bf-wo-19-l9', N'bf-wo-19', N'V08 - Additional Works Throughout', N'Build wall on 2nd floor to block off vaulted section; cut out joist and trim for SVP; SVP boxing for kitchen layout; pack out door opening for kitchen layout change; cladding on dormers.', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 720.00, 720.00, 0, 9),
    (N'bf-wo-20-l1', N'bf-wo-20', N'BTM Southern Brickwork - V01 - Day Work', N'Day Work as per Invoice - BTMCHISLEX01', N'MASON-BRK', N'00006-04 Masonry', 1, 180.00, 180.00, 180.00, 1),
    (N'bf-wo-22-l1', N'bf-wo-22', N'G11 - SITE WORKS - Rev B', N'Site works and installation as per G11 - SITE WORKS - Rev B', N'STR-STL', N'00006-27 Structural Steel PS', 1, 2610.00, 2610.00, 2610.00, 1),
    (N'bf-wo-22-l2', N'bf-wo-22', N'Supply Variations - HSFG Bolts', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 400.00, 400.00, 400.00, 2),
    (N'bf-wo-22-l3', N'bf-wo-22', N'Supply Variations - Stiffeners', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 288.00, 288.00, 288.00, 3),
    (N'bf-wo-23-l1', N'bf-wo-23', N'BTM Southern Brickwork - V02 - Day Work', N'Day works agreed for changes to window openings', N'MASON-BRK', N'00006-04 Masonry', 24, 30.00, 720.00, 720.00, 1),
    (N'bf-wo-24-l1', N'bf-wo-24', N'S Williams Plumbing & Heating', N'', N'MEC-PLM', N'00006-13 Plumbing', 1, 67268.56, 67268.56, 52460.27, 1),
    (N'bf-wo-24-l2', N'bf-wo-24', N'Variation 01 - UFH Carers', N'Supply and install a new 3-zone Nexxa underfloor heating system in the carer''s living area; manifold mounted in the boiler location in the kitchen area; controlled via separate 2-port zone valve; room stats in kitchen/living area and hall.', N'MEC-PLM', N'00006-13 Plumbing', 1, 4074.20, 4074.20, 0, 2),
    (N'bf-wo-25-l1', N'bf-wo-25', N'Generation Windows - Ref 9125', N'', N'WDR-ALU', N'00006-10 Windows & Doors', 1, 80000.00, 80000.00, 72000.00, 1),
    (N'bf-wo-25-l2', N'bf-wo-25', N'Omit Entrance Door Cost', N'', N'WDR-ALU', N'00006-10 Windows & Doors', 1, -3849.00, -3849.00, 0, 2),
    (N'bf-wo-26-l1', N'bf-wo-26', N'Anything Electrical - Main Contract', N'As per quote provided 9th June. PRO-064-(WD)-P-150 Rev I ELECTRICAL PLAN GF; P-151 Rev H FF; P-152 Rev F SF.', N'ELE-STD', N'00006-15 Electrics', 1, 88435.00, 88435.00, 51370.00, 1),
    (N'bf-wo-26-l2', N'bf-wo-26', N'V01 - Invoice 1621', N'Supply & install 32a and commando socket to cabin', N'ELE-STD', N'00006-15 Electrics', 1, 390.00, 390.00, 0, 2),
    (N'bf-wo-26-l3', N'bf-wo-26', N'V02 - Invoice 1640', N'Supply & installation of site lighting', N'ELE-STD', N'00006-15 Electrics', 1, 980.00, 980.00, 0, 3),
    (N'bf-wo-26-l4', N'bf-wo-26', N'V03 - FF Changes', N'First floor adaptations to existing wiring in line with updates to M&E; supply and installation of 2 x wall lights on second floor staircase.', N'ELE-STD', N'00006-15 Electrics', 1, 490.00, 490.00, 0, 4),
    (N'bf-wo-26-l5', N'bf-wo-26', N'V04 - SF REV H', N'PRO-064-(WD)-P-152 Rev H ELECTRICAL PLAN SF', N'ELE-STD', N'00006-15 Electrics', 1, 300.00, 300.00, 0, 5),
    (N'bf-wo-26-l6', N'bf-wo-26', N'V05 - SF REV J', N'PRO-064-(WD)-P-152 Rev J ELECTRICAL PLAN SF', N'ELE-STD', N'00006-15 Electrics', 1, 225.00, 225.00, 0, 6),
    (N'bf-wo-26-l7', N'bf-wo-26', N'V06 - GF Rev Q', N'LED strip approx 82m (GBP 4,710); additional CCTV (GBP 900); gazebo feed (GBP 380); garden lighting provision (GBP 140); boundary fence provision (GBP 220); gate feed (GBP 320); pond feed (GBP 320).', N'ELE-STD', N'00006-15 Electrics', 1, 6990.00, 6990.00, 0, 7),
    (N'bf-wo-27-l1', N'bf-wo-27', N'The Steel Team - Variation SF Installation', N'As per invoice 63108', N'STR-STL', N'00006-27 Structural Steel PS', 1, 1950.00, 1950.00, 1950.00, 1),
    (N'bf-wo-28-l1', N'bf-wo-28', N'Carpentry 1st fix & Cladding', N'As per itemised quote', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 10230.00, 10230.00, 5944.54, 1),
    (N'bf-wo-29-l1', N'bf-wo-29', N'Day work as per INV-65175', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 375.00, 375.00, 375.00, 1),
    (N'bf-wo-30-l1', N'bf-wo-30', N'The Steel Team - INV-66835', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 635.00, 635.00, 635.00, 1),
    (N'bf-wo-31-l1', N'bf-wo-31', N'JP Air Conditioning - QU-17567', N'As per QU-17567', N'MEC-AC', N'00006-53 Air Con PS', 1, 26650.58, 26650.58, 13148.26, 1),
    (N'bf-wo-32-l1',  N'bf-wo-32', N'REV C - Dryling & Plastering', N'As per quote 26th September 2025', N'INT-PLS', N'00006-12 Plastering', 1, 108111.60, 108111.60, 68637.40, 1),
    (N'bf-wo-32-l2',  N'bf-wo-32', N'REV A - Render Works', N'As per quote 26th September 2025', N'INT-RDR', N'00006-12 Render', 1, 15480.00, 15480.00, 0, 2),
    (N'bf-wo-32-l3',  N'bf-wo-32', N'REV A - Screed Works', N'As per quote 26th September 2025', N'FLR-SCR', N'00006-11 Screed', 1, 12935.00, 12935.00, 0, 3),
    (N'bf-wo-32-l4',  N'bf-wo-32', N'V01 - Insulation & board to Eaves', N'Variation 01 - 12th January 2026 revised 15th Jan', N'INT-PLB', N'00006-09 Gypsum Board', 1, 5208.10, 5208.10, 0, 4),
    (N'bf-wo-32-l5',  N'bf-wo-32', N'V02 - Solid Dab Fixings', N'As per quote and mark up drawing provided 14th Jan 2026', N'INT-PLB', N'00006-09 Gypsum Board', 1, 2519.93, 2519.93, 0, 5),
    (N'bf-wo-32-l6',  N'bf-wo-32', N'V03 - Plasterboard to D-02-01 due to steel beam above wall', N'', N'INT-PLB', N'00006-09 Gypsum Board', 1, 242.45, 242.45, 0, 6),
    (N'bf-wo-32-l7',  N'bf-wo-32', N'V04 - Tilebacker to bathrooms, gypline and hardwall', N'', N'INT-PLB', N'00006-09 Gypsum Board', 1, 3584.52, 3584.52, 0, 7),
    (N'bf-wo-32-l8',  N'bf-wo-32', N'V05 - GF Insulation - 50mm Optim R TBC', N'To be confirmed', N'FLR-SCR', N'00006-11 Screed', 1, 0, 0, 0, 8),
    (N'bf-wo-32-l9',  N'bf-wo-32', N'V06 - Flow Screed TBC', N'To be confirmed', N'FLR-SCR', N'00006-11 Screed', 1, 0, 0, 0, 9),
    (N'bf-wo-32-l10', N'bf-wo-32', N'V07 - Plasterboard to High Level Sloping Ceilings in Main Entrance', N'', N'INT-PLB', N'00006-09 Gypsum Board', 1, 329.01, 329.01, 0, 10),
    (N'bf-wo-32-l11', N'bf-wo-32', N'V08 - Install plasterboard edge bead to reveals to allow a fixing point', N'', N'INT-PLB', N'00006-09 Gypsum Board', 1, 596.70, 596.70, 0, 11),
    (N'bf-wo-32-l12', N'bf-wo-32', N'V09 - Cut Steels in loft - requested on site', N'Issued 24.02.26', N'INT-PLB', N'00006-09 Gypsum Board', 1, 150.00, 150.00, 0, 12),
    (N'bf-wo-32-l13', N'bf-wo-32', N'V10 - Additional Gypline to Bedroom 3 and above Main Entrance', N'Issued 24.02.26', N'INT-PLB', N'00006-09 Gypsum Board', 1, 460.29, 460.29, 0, 13),
    (N'bf-wo-33-l1', N'bf-wo-33', N'Supply Materials', N'', N'ROOF-RFR', N'00006-07 Roofing & Guttering', 1, 21570.00, 21570.00, 21570.00, 1),
    (N'bf-wo-33-l2', N'bf-wo-33', N'Installation Main Roof', N'', N'ROOF-RFR', N'00006-07 Roofing & Guttering', 1, 18480.00, 18480.00, 18480.00, 2),
    (N'bf-wo-33-l3', N'bf-wo-33', N'LGF - Supply & Installation', N'', N'ROOF-RFR', N'00006-07 Roofing & Guttering', 1, 18700.00, 18700.00, 18300.00, 3),
    (N'bf-wo-33-l4', N'bf-wo-33', N'V01 - Rainwater Main Roof', N'', N'ROOF-RFR', N'00006-07 Roofing & Guttering', 1, 600.00, 600.00, 0, 4),
    (N'bf-wo-34-l1', N'bf-wo-34', N'J & L Brickwork & Landscaping', N'As per quote provided 7th October 2025', N'EXTW-LND', N'00006-22 Hard Landscaping', 1, 42000.00, 42000.00, 42000.00, 1),
    (N'bf-wo-35-l1', N'bf-wo-35', N'The Steel Team - Invoice 67198', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 245.00, 245.00, 245.00, 1),
    (N'bf-wo-36-l1', N'bf-wo-36', N'The Steel Team - INV-68410', N'', N'STR-STL', N'00006-27 Structural Steel PS', 1, 350.00, 350.00, 350.00, 1),
    (N'bf-wo-37-l1', N'bf-wo-37', N'Stair & Banister 1001', N'Main staircase: 1no single winder cut string staircase in American white oak (treads, risers, strings, newels, craftsman handrail and base rail, barley twist spindles, curtail feature treads). Supply and installation as quote 1001.', N'STAIR-TIM', N'00006-08 Staircase', 1, 17680.00, 17680.00, 0, 1),
    (N'bf-wo-37-l2', N'bf-wo-37', N'Stair & Banister 1011 Oak FF/SF', N'1no double winder shaped cut string staircase FF/SF in American white oak, matching specification. Supply and installation as quote 1011.', N'STAIR-TIM', N'00006-08 Staircase', 1, 15445.00, 15445.00, 0, 2)
) AS source ([WorkOrderLineId], [WorkOrderId], [Title], [Description], [CostCode], [LegacyCostCode], [Quantity], [UnitCost], [LineTotal], [PaidToDate], [SortOrder])
ON target.[WorkOrderLineId] = source.[WorkOrderLineId]
WHEN MATCHED THEN
    UPDATE SET
        target.[WorkOrderId]    = source.[WorkOrderId],
        target.[Title]          = source.[Title],
        target.[Description]    = source.[Description],
        target.[CostType]       = N'Subcontractor',
        target.[CostCode]       = source.[CostCode],
        target.[LegacyCostCode] = source.[LegacyCostCode],
        target.[Quantity]       = source.[Quantity],
        target.[Unit]           = N'item',
        target.[UnitCost]       = source.[UnitCost],
        target.[LineTotal]      = source.[LineTotal],
        target.[PaidToDate]     = source.[PaidToDate],
        target.[SortOrder]      = source.[SortOrder]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([WorkOrderLineId], [WorkOrderId], [Title], [Description], [CostType], [CostCode],
            [LegacyCostCode], [Quantity], [Unit], [UnitCost], [LineTotal], [PaidToDate], [SortOrder])
    VALUES (source.[WorkOrderLineId], source.[WorkOrderId], source.[Title], source.[Description],
            N'Subcontractor', source.[CostCode], source.[LegacyCostCode], source.[Quantity], N'item',
            source.[UnitCost], source.[LineTotal], source.[PaidToDate], source.[SortOrder])
WHEN NOT MATCHED BY SOURCE AND target.[WorkOrderLineId] LIKE N'bf-wo-%' THEN
    DELETE;
GO

-- ----------------------------------------------------------------------------
-- 4. Reconciliation — totals should match the printed orders
-- ----------------------------------------------------------------------------
-- Expected: 36 orders, GBP 1,128,986.98 committed, GBP 876,375.53 paid,
-- 98 lines. Every row's Check column should be blank (no MISMATCH).
SELECT w.[Number], w.[Title], w.[Value] AS [OrderValue],
       lines.[LineTotal], lines.[Paid],
       CASE WHEN w.[Value] <> lines.[LineTotal] THEN N'MISMATCH' ELSE N'' END AS [Check]
FROM [dbo].[WorkOrders] w
CROSS APPLY (
    SELECT SUM(l.[LineTotal]) AS [LineTotal], SUM(l.[PaidToDate]) AS [Paid]
    FROM [dbo].[WorkOrderLines] l
    WHERE l.[WorkOrderId] = w.[WorkOrderId]
) lines
WHERE w.[ProjectId] = N'3490f944b29545c4b8d5a04130f42ab8'
ORDER BY w.[Number];
