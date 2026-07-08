-- ============================================================================
-- Seed: Coombe Lane -- work orders (Buildertrend purchase orders PO-11..PO-63)
-- ----------------------------------------------------------------------------
-- Project : Coombe Lane  (PLG - Coombe Lane W)
-- ProjectId: d64c8a72f4b14eebbabd251b54564ef5
--
-- Seeds the 50 Buildertrend purchase orders provided (PO-11..PO-63; numbers
-- 20, 31 and 48 were never raised / not provided, and PO-01..PO-10 predate the
-- Buildertrend record). SourceReference holds the Buildertrend poid.
--
-- Cost-code mapping (Buildertrend -> JBB Cost Code Master v2.1), per the
-- "map by nature of work" rule agreed with Nigel Reilly 8 Jul 2026 — the
-- printed legacy code always survives in LegacyCostCode:
--
--     00006-01 Demolition          -> ENABLE-COR (both orders are Robore
--                                                 concrete cutting)
--     00003-1  Health & Safety     -> PRELIMS-HSO
--     00003-2  Rubbish Removal     -> ENABLE-SKP
--     00003-3  Temp Toilet         -> PRELIMS-WC
--     00006-04 Masonry             -> MASON-BRK
--     00006-05 Scaffolding         -> SCAFF-STD
--     00006-06 Carpentry 1st Fix   -> CARP-1FX
--     00006-07 Roofing & Guttering -> ROOF-RFR
--                                  -> ROOF-FSU  (PO-59 UPVC fascia/soffit line)
--     00006-09 Gypsum Board        -> INT-MGW   (both orders are metal track)
--     00006-10 Windows & Doors     -> WDR-ALU   (Generation Windows)
--                                  -> WDR-SPG   (PO-45 Hamilton Glass)
--                                  -> WDR-INT   (PO-56 Generation internal doors)
--     00006-11 Screed              -> FLR-SCR
--     00006-12 Plastering          -> INT-PLS
--                                  -> INT-COV   (PO-28 CM Cornice mouldings)
--     00006-13 Plumbing            -> MEC-PLM
--     00006-14 UFH System          -> MEC-UFH
--                                  -> MEC-AC    (PO-13 AC relocation)
--                                  -> SPEC-POO  (PO-55 pool heating system —
--                                                grouped with the Aquasure pool)
--     00006-15 Electrics           -> ELE-STD
--     00006-19 Tiling              -> TIL-STD
--     00006-20 Painting            -> DEC-STD
--     00006-21 Floor Finishes      -> FLR-WD    (Farrant's: timber flooring)
--     00006-22 Hard Landscaping    -> EXTW-LND  (JW Plant / SJF landscape)
--                                  -> EXTW-PAV  (PO-39 ramp & entrance,
--                                                PO-58 Oltco resin driveway)
--                                  -> INT-RDR   (PO-57 Dunkley external render)
--                                  -> EXTW-FEN  (PO-61 Hudson entrance gates)
--     00006-28 Temporary Works     -> PRELIMS-TMP
--     00006-30 Tile Supply PS      -> SUP-TIL
--     00006-36 Wardrobes & Storage -> CARP-WRD
--     00006-48 Carport PS          -> SPEC-GAZ
--     00006-50 Swimming Pool PS    -> SPEC-POO
--     10003    Labour - Jewel PS   -> PRELIMS-LAB
--
-- Cost types follow the prints: mostly Subcontractor, with Material/Other on
-- the aggregate cost orders (PO-17 "January 2024 Costs", PO-21 "Various Costs
-- February") and Other on PO-62. Real quantities are kept where printed
-- (PO-34: 5 days at GBP 280; PO-36: 2 men x 1 day at GBP 220).
--
-- "Multiple Suppliers" (PO-17) and "Various Costs" (PO-21) are Buildertrend
-- pseudo-vendors for aggregated material/other costs; they are seeded as
-- directory records so the orders keep their printed supplier.
--
-- Status: all 50 orders are Released (1). Where the print shows "Date Released
-- N/A" the approval date is used as AwardedAt (PO-11, 14, 15, 27, 56).
-- AwardedByEmail is the internal approver on the prints (James Clark).
--
-- Idempotent: orders keyed on WorkOrderId (cl-wo-NN), lines on WorkOrderLineId
-- (cl-wo-NN-lN); lines no longer in this script are deleted for these orders
-- only. Safe to run repeatedly.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Subcontractors — insert-if-missing, matched by company name
-- ----------------------------------------------------------------------------
MERGE INTO [dbo].[Subcontractors] AS target
USING (VALUES
    (N'sub-cl-matrimmings', N'M.A. Trimmings Plastering & Screeding Ltd'),
    (N'sub-cl-generation',  N'Generation Windows'),
    (N'sub-cl-asset',       N'Asset Integrated Services Ltd'),
    (N'sub-cl-farrants',    N'Farrant''s Flooring Ltd'),
    (N'sub-cl-robore',      N'Robore - Concrete Cutting'),
    (N'sub-cl-anythingel',  N'Anything Electrical (London) LTD'),
    (N'sub-cl-multiple',    N'Multiple Suppliers'),
    (N'sub-cl-dunkley',     N'Dunkley Plastering'),
    (N'sub-cl-aquasure',    N'Aquasure UK'),
    (N'sub-cl-various',     N'Various Costs'),
    (N'sub-cl-chart',       N'Chart Timber Buildings'),
    (N'sub-cl-jwplant',     N'JW Plant'),
    (N'sub-cl-smiths',      N'Smiths Fitted Wardrobes & Kitchens limited'),
    (N'sub-cl-steelo',      N'Steelo'),
    (N'sub-cl-cmcornice',   N'CM Cornice LTD'),
    (N'sub-cl-jewelps',     N'Jewel Property Serve Ltd'),
    (N'sub-cl-cplastics',   N'Complete Plastic Solutions'),
    (N'sub-cl-burns',       N'Burns Scaffolding Ltd'),
    (N'sub-cl-hamilton',    N'Hamilton Glass'),
    (N'sub-cl-sjf',         N'SJF Landscape'),
    (N'sub-cl-oltco',       N'Oltco Resin'),
    (N'sub-cl-hudson',      N'Hudson Security')
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
DECLARE @proj nvarchar(64) = N'd64c8a72f4b14eebbabd251b54564ef5';
DECLARE @approver nvarchar(256) = N'james.clark@jewelbb.co.uk';

DECLARE @subMaTrim     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'M.A. Trimmings Plastering & Screeding Ltd'));
DECLARE @subGeneration nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Generation Windows'));
DECLARE @subAsset      nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Asset Integrated Services Ltd'));
DECLARE @subFarrants   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Farrant''s Flooring Ltd'));
DECLARE @subRobore     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Robore - Concrete Cutting'));
DECLARE @subAnythingEl nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Anything Electrical (London) LTD'));
DECLARE @subMultiple   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Multiple Suppliers'));
DECLARE @subDunkley    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Dunkley Plastering'));
DECLARE @subAquasure   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Aquasure UK'));
DECLARE @subVarious    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Various Costs'));
DECLARE @subChart      nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Chart Timber Buildings'));
DECLARE @subJwPlant    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'JW Plant'));
DECLARE @subSmiths     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Smiths Fitted Wardrobes & Kitchens limited'));
DECLARE @subSteelo     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Steelo'));
DECLARE @subCmCornice  nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'CM Cornice LTD'));
DECLARE @subJewelPs    nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Jewel Property Serve Ltd'));
DECLARE @subCPlastics  nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Complete Plastic Solutions'));
DECLARE @subBurns      nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Burns Scaffolding Ltd'));
DECLARE @subHamilton   nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Hamilton Glass'));
DECLARE @subSjf        nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'SJF Landscape'));
DECLARE @subOltco      nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Oltco Resin'));
DECLARE @subHudson     nvarchar(64) = (SELECT TOP 1 [SubcontractorId] FROM [dbo].[Subcontractors] WHERE LOWER([CompanyName]) = LOWER(N'Hudson Security'));

MERGE INTO [dbo].[WorkOrders] AS target
USING (VALUES
    -- (WorkOrderId, SubcontractorId, Number, Title, Value, Scope, CreatedAt, AwardedAt, ScheduledCompletion, SourceReference)
    (N'cl-wo-11', @subMaTrim,     11, N'Screed',                                   1325.00, N'', '2023-12-20T00:00:00+00:00', '2023-12-20T00:00:00+00:00', '2023-12-15T00:00:00+00:00', N'buildertrend:58623399'),
    (N'cl-wo-12', @subGeneration, 12, N'Generation Windows',                      15130.00, N'', '2024-01-19T00:00:00+00:00', '2024-01-19T00:00:00+00:00', NULL,                        N'buildertrend:59074857'),
    (N'cl-wo-13', @subAsset,      13, N'Asset - AC Relocation',                    4302.54, N'', '2024-01-19T00:00:00+00:00', '2024-01-19T00:00:00+00:00', '2024-01-26T00:00:00+00:00', N'buildertrend:59075981'),
    (N'cl-wo-14', @subFarrants,   14, N'Farrant Flooring',                         3515.00, N'As per quote 17.01.24.', '2024-01-22T00:00:00+00:00', '2024-01-22T00:00:00+00:00', '2024-01-26T00:00:00+00:00', N'buildertrend:59094806'),
    (N'cl-wo-15', @subRobore,     15, N'Robore Cuts',                              3564.65, N'', '2024-01-26T00:00:00+00:00', '2024-01-26T00:00:00+00:00', '2024-02-09T00:00:00+00:00', N'buildertrend:59196039'),
    (N'cl-wo-16', @subAnythingEl, 16, N'Anything Electrical FF',                   7010.00, N'As per quote.', '2024-02-02T00:00:00+00:00', '2024-02-02T00:00:00+00:00', NULL,           N'buildertrend:59332173'),
    (N'cl-wo-17', @subMultiple,   17, N'January 2024 Costs',                       7443.74, N'Aggregated January 2024 material and site costs across suppliers.', '2024-02-07T00:00:00+00:00', '2024-02-07T00:00:00+00:00', '2024-01-31T00:00:00+00:00', N'buildertrend:59426249'),
    (N'cl-wo-18', @subDunkley,    18, N'Plastering FF',                            2250.00, N'As per quote provided.', '2024-02-26T00:00:00+00:00', '2024-02-26T00:00:00+00:00', '2024-02-29T00:00:00+00:00', N'buildertrend:59763764'),
    (N'cl-wo-19', @subAquasure,   19, N'Aquasure UK',                            218590.00, N'As per quote CLWPLG/1 - Chosen Extras.', '2024-02-27T00:00:00+00:00', '2024-02-27T00:00:00+00:00', '2024-06-28T00:00:00+00:00', N'buildertrend:59790397'),
    (N'cl-wo-21', @subVarious,    21, N'Various Costs February',                   1744.29, N'Aggregated February 2024 site costs across suppliers.', '2024-03-08T00:00:00+00:00', '2024-03-08T00:00:00+00:00', '2024-02-29T00:00:00+00:00', N'buildertrend:60005211'),
    (N'cl-wo-22', @subChart,      22, N'Carport',                                  9655.00, N'As per quote 22nd March 24.', '2024-03-25T00:00:00+00:00', '2024-03-25T00:00:00+00:00', '2024-05-31T00:00:00+00:00', N'buildertrend:60332354'),
    (N'cl-wo-23', @subJwPlant,    23, N'JW Plant - Landscape',                    62200.00, N'As per quote 26.03.24.', '2024-03-26T00:00:00+00:00', '2024-03-26T00:00:00+00:00', '2024-04-26T00:00:00+00:00', N'buildertrend:60375066'),
    (N'cl-wo-24', @subSmiths,     24, N'Smiths Fitted Wardrobes',                  3883.33, N'', '2024-03-28T00:00:00+00:00', '2024-03-28T00:00:00+00:00', '2024-04-12T00:00:00+00:00', N'buildertrend:60422882'),
    (N'cl-wo-25', @subSteelo,     25, N'Steelo Basement Steel',                     170.39, N'', '2024-04-02T00:00:00+00:00', '2024-04-02T00:00:00+00:00', '2024-04-05T00:00:00+00:00', N'buildertrend:60501762'),
    (N'cl-wo-26', @subAnythingEl, 26, N'Anything Electrical Basement',             6050.00, N'As per quote basement.', '2024-04-05T00:00:00+00:00', '2024-04-05T00:00:00+00:00', '2024-05-24T00:00:00+00:00', N'buildertrend:60579135'),
    (N'cl-wo-27', @subRobore,     27, N'Robore Cuts',                              3672.45, N'', '2024-04-05T00:00:00+00:00', '2024-04-05T00:00:00+00:00', '2024-03-01T00:00:00+00:00', N'buildertrend:60582372'),
    (N'cl-wo-28', @subCmCornice,  28, N'Cornice Mouldings',                        1380.00, N'As per quote provided.', '2024-04-10T00:00:00+00:00', '2024-04-10T00:00:00+00:00', '2024-04-19T00:00:00+00:00', N'buildertrend:60655094'),
    (N'cl-wo-29', @subJwPlant,    29, N'JW Plant - Landscape V01',                10880.00, N'', '2024-04-15T00:00:00+00:00', '2024-04-15T00:00:00+00:00', '2024-04-26T00:00:00+00:00', N'buildertrend:60739898'),
    (N'cl-wo-30', @subAnythingEl, 30, N'Anything Electrical Sensors',                90.00, N'', '2024-04-23T00:00:00+00:00', '2024-04-23T00:00:00+00:00', NULL,                        N'buildertrend:60910680'),
    (N'cl-wo-32', @subJewelPs,    32, N'Jewel PS - Painting',                       910.00, N'', '2024-05-14T00:00:00+00:00', '2024-05-14T00:00:00+00:00', '2024-05-10T00:00:00+00:00', N'buildertrend:61318752'),
    (N'cl-wo-33', @subJewelPs,    33, N'Jewel PS - Plumbing',                      1140.00, N'', '2024-05-14T00:00:00+00:00', '2024-05-14T00:00:00+00:00', '2024-05-10T00:00:00+00:00', N'buildertrend:61318754'),
    (N'cl-wo-34', @subJewelPs,    34, N'Jewel PS - Metal Track',                   1400.00, N'', '2024-05-15T00:00:00+00:00', '2024-05-15T00:00:00+00:00', '2024-05-24T00:00:00+00:00', N'buildertrend:61350842'),
    (N'cl-wo-35', @subJewelPs,    35, N'Jewel PS - Tiling',                         240.00, N'', '2024-05-15T00:00:00+00:00', '2024-05-15T00:00:00+00:00', '2024-05-24T00:00:00+00:00', N'buildertrend:61350925'),
    (N'cl-wo-36', @subJewelPs,    36, N'Jewel PS - Painting',                       476.99, N'', '2024-05-16T00:00:00+00:00', '2024-05-16T00:00:00+00:00', '2024-05-16T00:00:00+00:00', N'buildertrend:61377163'),
    (N'cl-wo-37', @subCPlastics,  37, N'Complete Plastics Roofing',                3970.00, N'As per quote.', '2024-05-20T00:00:00+00:00', '2024-05-20T00:00:00+00:00', '2024-06-28T00:00:00+00:00', N'buildertrend:61431773'),
    (N'cl-wo-38', @subAnythingEl, 38, N'Anything Electrical - External',            856.00, N'As per quote 21.05.24.', '2024-05-22T00:00:00+00:00', '2024-05-22T00:00:00+00:00', '2024-05-31T00:00:00+00:00', N'buildertrend:61483814'),
    (N'cl-wo-39', @subCPlastics,  39, N'Complete Plastics - Ramp & Entrance',      3050.00, N'', '2024-06-03T00:00:00+00:00', '2024-06-03T00:00:00+00:00', '2024-06-14T00:00:00+00:00', N'buildertrend:61674865'),
    (N'cl-wo-40', @subJewelPs,    40, N'Jewel PS - Plumbing',                       430.00, N'', '2024-06-04T00:00:00+00:00', '2024-06-04T00:00:00+00:00', '2024-05-31T00:00:00+00:00', N'buildertrend:61721778'),
    (N'cl-wo-41', @subJewelPs,    41, N'Jewel PS - Painting QU-1013',              1002.00, N'', '2024-06-17T00:00:00+00:00', '2024-06-17T00:00:00+00:00', '2024-06-21T00:00:00+00:00', N'buildertrend:61954543'),
    (N'cl-wo-42', @subBurns,      42, N'Scaffolding - Basement',                   3600.00, N'', '2024-06-26T00:00:00+00:00', '2024-06-26T00:00:00+00:00', '2024-07-05T00:00:00+00:00', N'buildertrend:62179963'),
    (N'cl-wo-43', @subAnythingEl, 43, N'Anything Electrical - Additions',          6992.00, N'', '2024-07-03T00:00:00+00:00', '2024-07-03T00:00:00+00:00', '2024-08-02T00:00:00+00:00', N'buildertrend:62308540'),
    (N'cl-wo-44', @subFarrants,   44, N'Farrant Flooring - Additional Bedroom',     800.00, N'', '2024-07-08T00:00:00+00:00', '2024-07-08T00:00:00+00:00', '2024-07-26T00:00:00+00:00', N'buildertrend:62355855'),
    (N'cl-wo-45', @subHamilton,   45, N'Hamilton Glass',                           5045.23, N'', '2024-07-16T00:00:00+00:00', '2024-07-16T00:00:00+00:00', '2024-08-02T00:00:00+00:00', N'buildertrend:62507637'),
    (N'cl-wo-46', @subAnythingEl, 46, N'Anything Electrical - Invoice 1479',        290.00, N'', '2024-07-22T00:00:00+00:00', '2024-07-22T00:00:00+00:00', '2024-07-22T00:00:00+00:00', N'buildertrend:62611248'),
    (N'cl-wo-47', @subJewelPs,    47, N'Jewel PS - Plumbing',                       280.00, N'', '2024-07-25T00:00:00+00:00', '2024-07-25T00:00:00+00:00', '2024-07-26T00:00:00+00:00', N'buildertrend:62688965'),
    (N'cl-wo-49', @subFarrants,   49, N'Farrant Flooring - Ground Floor',         26480.00, N'As per quote provided 29/07/24.', '2024-07-30T00:00:00+00:00', '2024-07-30T00:00:00+00:00', '2024-08-09T00:00:00+00:00', N'buildertrend:62771462'),
    (N'cl-wo-50', @subDunkley,    50, N'Dunkley Plaster - Basement',               5129.00, N'', '2024-07-30T00:00:00+00:00', '2024-07-30T00:00:00+00:00', '2024-08-09T00:00:00+00:00', N'buildertrend:62772812'),
    (N'cl-wo-51', @subSjf,        51, N'SJF Landscape',                            4000.00, N'', '2024-08-08T00:00:00+00:00', '2024-08-08T00:00:00+00:00', '2024-08-30T00:00:00+00:00', N'buildertrend:62944798'),
    (N'cl-wo-52', @subJewelPs,    52, N'Jewel PS - Metal Track QU0376',            3840.00, N'', '2024-08-12T00:00:00+00:00', '2024-08-12T00:00:00+00:00', '2024-08-09T00:00:00+00:00', N'buildertrend:62994068'),
    (N'cl-wo-53', @subAsset,      53, N'Asset UFH Basement - Additional',          2425.00, N'', '2024-08-12T00:00:00+00:00', '2024-08-12T00:00:00+00:00', '2024-08-23T00:00:00+00:00', N'buildertrend:62994073'),
    (N'cl-wo-54', @subJewelPs,    54, N'Jewel PS - Painting QU0408',                843.00, N'', '2024-08-19T00:00:00+00:00', '2024-08-19T00:00:00+00:00', '2024-08-16T00:00:00+00:00', N'buildertrend:63122324'),
    (N'cl-wo-55', @subAsset,      55, N'Asset - Pool Heating System',              5479.00, N'', '2024-09-11T00:00:00+00:00', '2024-09-11T00:00:00+00:00', '2024-09-20T00:00:00+00:00', N'buildertrend:63552853'),
    (N'cl-wo-56', @subGeneration, 56, N'Generation Windows - Internal Doors',     29711.00, N'', '2024-10-08T00:00:00+00:00', '2024-10-08T00:00:00+00:00', '2024-11-15T00:00:00+00:00', N'buildertrend:64044274'),
    (N'cl-wo-57', @subDunkley,    57, N'Dunkley Plastering - Render',              9000.00, N'', '2024-10-11T00:00:00+00:00', '2024-10-11T00:00:00+00:00', '2024-10-25T00:00:00+00:00', N'buildertrend:64110219'),
    (N'cl-wo-58', @subOltco,      58, N'Oltco Resin Driveway',                    18025.00, N'As per quote QU-1071.', '2024-10-16T00:00:00+00:00', '2024-10-16T00:00:00+00:00', '2024-11-08T00:00:00+00:00', N'buildertrend:64177872'),
    (N'cl-wo-59', @subCPlastics,  59, N'Complete Plastics - Fascia and Soffit',   10900.00, N'', '2024-11-20T00:00:00+00:00', '2024-11-20T00:00:00+00:00', '2025-02-14T00:00:00+00:00', N'buildertrend:64811637'),
    (N'cl-wo-60', @subJewelPs,    60, N'Jewel PS - Labour & Materials Nov 24',    16697.77, N'', '2024-11-21T00:00:00+00:00', '2024-11-21T00:00:00+00:00', NULL,                        N'buildertrend:64828058'),
    (N'cl-wo-61', @subHudson,     61, N'Hudson Entrance Gates',                   14867.00, N'', '2025-03-06T00:00:00+00:00', '2025-03-06T00:00:00+00:00', '2025-04-25T00:00:00+00:00', N'buildertrend:66554538'),
    (N'cl-wo-62', @subGeneration, 62, N'Generation Windows - Quote 9126',          3164.00, N'', '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', '2025-05-30T00:00:00+00:00', N'buildertrend:67664581'),
    (N'cl-wo-63', @subAnythingEl, 63, N'Anything Electrical - Invoice 1578',        385.00, N'', '2025-05-19T00:00:00+00:00', '2025-05-19T00:00:00+00:00', '2025-05-19T00:00:00+00:00', N'buildertrend:68038214')
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
-- 3. Work order lines (cost types and quantities as printed)
-- ----------------------------------------------------------------------------
MERGE INTO [dbo].[WorkOrderLines] AS target
USING (VALUES
    -- (WorkOrderLineId, WorkOrderId, Title, Description, CostType, CostCode, LegacyCostCode, Quantity, UnitCost, LineTotal, PaidToDate, SortOrder)
    (N'cl-wo-11-l1', N'cl-wo-11', N'Screed', N'', N'Subcontractor', N'FLR-SCR', N'00006-11 Screed', 1, 1325.00, 1325.00, 1325.00, 1),
    (N'cl-wo-12-l1', N'cl-wo-12', N'Generation Windows', N'', N'Subcontractor', N'WDR-ALU', N'00006-10 Windows & Doors', 1, 15130.00, 15130.00, 15130.00, 1),
    (N'cl-wo-13-l1', N'cl-wo-13', N'AC Relocation', N'', N'Subcontractor', N'MEC-AC', N'00006-14 UFH System', 1, 4302.54, 4302.54, 4302.54, 1),
    (N'cl-wo-14-l1', N'cl-wo-14', N'Farrants Flooring', N'As per quote 17.01.24', N'Subcontractor', N'FLR-WD', N'00006-21 Floor Finishes', 1, 3515.00, 3515.00, 3515.00, 1),
    (N'cl-wo-15-l1', N'cl-wo-15', N'Robore Concrete Cutting', N'', N'Subcontractor', N'ENABLE-COR', N'00006-01 Demolition', 1, 3564.65, 3564.65, 3564.65, 1),
    (N'cl-wo-16-l1', N'cl-wo-16', N'Anything Electrical FF', N'As per quote', N'Subcontractor', N'ELE-STD', N'00006-15 Electrics', 1, 7010.00, 7010.00, 7010.00, 1),
    (N'cl-wo-17-l1', N'cl-wo-17', N'Topps Tiles', N'', N'Material', N'SUP-TIL', N'00006-30 Tile Supply PS', 1, 28.63, 28.63, 28.63, 1),
    (N'cl-wo-17-l2', N'cl-wo-17', N'Harvey Fab Supplies & Sussex Plumbing Supplies', N'', N'Material', N'MEC-PLM', N'00006-13 Plumbing', 1, 3245.14, 3245.14, 3245.14, 2),
    (N'cl-wo-17-l3', N'cl-wo-17', N'Chandlers Building Supplies Ltd', N'', N'Material', N'INT-PLB', N'00006-09 Gypsum Board', 1, 801.49, 801.49, 801.49, 3),
    (N'cl-wo-17-l4', N'cl-wo-17', N'Chandlers Building Supplies Ltd & Screwfix', N'', N'Material', N'CARP-1FX', N'00006-06 - Carpentry 1st Fix', 1, 2600.39, 2600.39, 2600.39, 4),
    (N'cl-wo-17-l5', N'cl-wo-17', N'Chandlers Building Supplies Ltd', N'', N'Material', N'PRELIMS-HSO', N'00003-1 Health & Safety', 1, 5.06, 5.06, 5.06, 5),
    (N'cl-wo-17-l6', N'cl-wo-17', N'Skip It Waste Management Limited', N'', N'Other', N'ENABLE-SKP', N'00003-2 Rubbish Removal', 1, 270.00, 270.00, 270.00, 6),
    (N'cl-wo-17-l7', N'cl-wo-17', N'Euroloos limited', N'', N'Other', N'PRELIMS-WC', N'00003-3 Temp Toilet', 1, 265.71, 265.71, 265.71, 7),
    (N'cl-wo-17-l8', N'cl-wo-17', N'Chandlers Building Supplies Ltd', N'', N'Material', N'PRELIMS-HSO', N'00003-1 Health & Safety', 1, 36.06, 36.06, 36.06, 8),
    (N'cl-wo-17-l9', N'cl-wo-17', N'Chandlers Building Supplies Ltd', N'', N'Material', N'MASON-BRK', N'00006-04 Masonry', 1, 191.26, 191.26, 191.26, 9),
    (N'cl-wo-18-l1', N'cl-wo-18', N'Plastering FF', N'As per quote provided', N'Subcontractor', N'INT-PLS', N'00006-12 Plastering', 1, 2250.00, 2250.00, 2250.00, 1),
    (N'cl-wo-19-l1', N'cl-wo-19', N'Aquasure UK', N'As per quote CLWPLG/1 - Chosen Extras', N'Subcontractor', N'SPEC-POO', N'00006-50 Swimming Pool PS', 1, 218590.00, 218590.00, 218590.00, 1),
    (N'cl-wo-21-l1', N'cl-wo-21', N'Euroloos limited', N'', N'Other', N'PRELIMS-WC', N'00003-3 Temp Toilet', 1, 124.29, 124.29, 124.29, 1),
    (N'cl-wo-21-l2', N'cl-wo-21', N'Jewel Property Serve Ltd - INV-0410', N'', N'Other', N'MEC-PLM', N'00006-13 Plumbing', 1, 1350.00, 1350.00, 1350.00, 2),
    (N'cl-wo-21-l3', N'cl-wo-21', N'Skip It Waste Management Limited', N'', N'Other', N'ENABLE-SKP', N'00003-2 Rubbish Removal', 1, 270.00, 270.00, 270.00, 3),
    (N'cl-wo-22-l1', N'cl-wo-22', N'Carport', N'As per quote 22nd March 24', N'Subcontractor', N'SPEC-GAZ', N'00006-48 Carport PS', 1, 9655.00, 9655.00, 9655.00, 1),
    (N'cl-wo-23-l1', N'cl-wo-23', N'JW Plant - Landscape', N'As per quote 26.03.24', N'Subcontractor', N'EXTW-LND', N'00006-22 Hard Landscaping', 1, 62200.00, 62200.00, 62200.00, 1),
    (N'cl-wo-24-l1', N'cl-wo-24', N'Smiths Fitted Wardrobes', N'', N'Subcontractor', N'CARP-WRD', N'00006-36 Wardrobes & Storage PS', 1, 4660.00, 4660.00, 4660.00, 1),
    (N'cl-wo-24-l2', N'cl-wo-24', N'Smiths Fitted Wardrobes to adjust to net cost', N'', N'Subcontractor', N'CARP-WRD', N'00006-36 Wardrobes & Storage PS', 1, -776.67, -776.67, -776.67, 2),
    (N'cl-wo-25-l1', N'cl-wo-25', N'Steelo Basement Steel', N'', N'Subcontractor', N'PRELIMS-TMP', N'00006-28 Temporary Works', 1, 170.39, 170.39, 170.39, 1),
    (N'cl-wo-26-l1', N'cl-wo-26', N'Anything Electrical - Basement', N'As per quote basement', N'Subcontractor', N'ELE-STD', N'00006-15 Electrics', 1, 6050.00, 6050.00, 6050.00, 1),
    (N'cl-wo-27-l1', N'cl-wo-27', N'Robore Cuts', N'', N'Subcontractor', N'ENABLE-COR', N'00006-01 Demolition', 1, 3672.45, 3672.45, 3672.45, 1),
    (N'cl-wo-28-l1', N'cl-wo-28', N'Cornice Mouldings', N'As per quote provided', N'Subcontractor', N'INT-COV', N'00006-12 Plastering', 1, 1380.00, 1380.00, 1380.00, 1),
    (N'cl-wo-29-l1', N'cl-wo-29', N'JW Plant - V01', N'', N'Subcontractor', N'EXTW-LND', N'00006-22 Hard Landscaping', 1, 10880.00, 10880.00, 10880.00, 1),
    (N'cl-wo-30-l1', N'cl-wo-30', N'Anything Electrical Sensors', N'', N'Subcontractor', N'ELE-STD', N'00006-15 Electrics', 1, 90.00, 90.00, 90.00, 1),
    (N'cl-wo-32-l1', N'cl-wo-32', N'Jewel PS - Painting', N'', N'Subcontractor', N'DEC-STD', N'00006-20 Painting', 1, 910.00, 910.00, 910.00, 1),
    (N'cl-wo-33-l1', N'cl-wo-33', N'Jewel PS - Plumbing', N'', N'Subcontractor', N'MEC-PLM', N'00006-13 Plumbing', 1, 1140.00, 1140.00, 1140.00, 1),
    (N'cl-wo-34-l1', N'cl-wo-34', N'Jewel PS - Metal Track', N'Day Rate GBP 280', N'Subcontractor', N'INT-MGW', N'00006-09 Gypsum Board', 5, 280.00, 1400.00, 1400.00, 1),
    (N'cl-wo-35-l1', N'cl-wo-35', N'Jewel PS - Tiling', N'1 day - GBP 240', N'Subcontractor', N'TIL-STD', N'00006-19 Tiling', 1, 240.00, 240.00, 240.00, 1),
    (N'cl-wo-36-l1', N'cl-wo-36', N'Jewel PS - Painting', N'2 Men - 1 day - FF bedroom', N'Subcontractor', N'DEC-STD', N'00006-20 Painting', 2, 220.00, 440.00, 440.00, 1),
    (N'cl-wo-36-l2', N'cl-wo-36', N'Jewel PS - Painting Materials', N'', N'Subcontractor', N'DEC-STD', N'00006-20 Painting', 1, 36.99, 36.99, 36.99, 2),
    (N'cl-wo-37-l1', N'cl-wo-37', N'Complete Plastics Roofing', N'As per quote', N'Subcontractor', N'ROOF-RFR', N'00006-07 Roofing & Guttering', 1, 3970.00, 3970.00, 3970.00, 1),
    (N'cl-wo-38-l1', N'cl-wo-38', N'Anything Electrical - External Works', N'As per quote 21.05.24', N'Subcontractor', N'ELE-STD', N'00006-15 Electrics', 1, 856.00, 856.00, 856.00, 1),
    (N'cl-wo-39-l1', N'cl-wo-39', N'Complete Plastics - Ramp & Entrance', N'', N'Subcontractor', N'EXTW-PAV', N'00006-22 Hard Landscaping', 1, 3050.00, 3050.00, 3050.00, 1),
    (N'cl-wo-40-l1', N'cl-wo-40', N'Jewel PS - Plumbing', N'', N'Subcontractor', N'MEC-PLM', N'00006-13 Plumbing', 1, 430.00, 430.00, 430.00, 1),
    (N'cl-wo-41-l1', N'cl-wo-41', N'Jewel PS - Painting QU-1013', N'', N'Subcontractor', N'DEC-STD', N'00006-20 Painting', 1, 1002.00, 1002.00, 1002.00, 1),
    (N'cl-wo-42-l1', N'cl-wo-42', N'Scaffolding - Basement', N'', N'Subcontractor', N'SCAFF-STD', N'00006-05 Scaffolding', 1, 3600.00, 3600.00, 3600.00, 1),
    (N'cl-wo-43-l1', N'cl-wo-43', N'Anything Electrical - Additional Items', N'', N'Subcontractor', N'ELE-STD', N'00006-15 Electrics', 1, 6992.00, 6992.00, 6992.00, 1),
    (N'cl-wo-44-l1', N'cl-wo-44', N'Farrant Flooring - Additional Bedroom', N'', N'Subcontractor', N'FLR-WD', N'00006-21 Floor Finishes', 1, 800.00, 800.00, 800.00, 1),
    (N'cl-wo-45-l1', N'cl-wo-45', N'Hamilton Glass', N'', N'Subcontractor', N'WDR-SPG', N'00006-10 Windows & Doors', 1, 5045.23, 5045.23, 5045.23, 1),
    (N'cl-wo-46-l1', N'cl-wo-46', N'Anything Electrical - Invoice 1479', N'', N'Subcontractor', N'ELE-STD', N'00006-15 Electrics', 1, 290.00, 290.00, 290.00, 1),
    (N'cl-wo-47-l1', N'cl-wo-47', N'Jewel PS - Plumbing', N'', N'Subcontractor', N'MEC-PLM', N'00006-13 Plumbing', 1, 280.00, 280.00, 280.00, 1),
    (N'cl-wo-49-l1', N'cl-wo-49', N'Farrant Flooring - Ground Floor', N'As per quote provided 29/07/24', N'Subcontractor', N'FLR-WD', N'00006-21 Floor Finishes', 1, 25120.00, 25120.00, 25120.00, 1),
    (N'cl-wo-49-l2', N'cl-wo-49', N'Farrant Flooring - Ply', N'', N'Subcontractor', N'FLR-WD', N'00006-21 Floor Finishes', 1, 680.00, 680.00, 680.00, 2),
    (N'cl-wo-49-l3', N'cl-wo-49', N'Additional Works to the Staircase & Repairs', N'', N'Subcontractor', N'FLR-WD', N'00006-21 Floor Finishes', 1, 680.00, 680.00, 680.00, 3),
    (N'cl-wo-50-l1', N'cl-wo-50', N'Dunkley Plaster - Basement', N'', N'Subcontractor', N'INT-PLS', N'00006-12 Plastering', 1, 5129.00, 5129.00, 5129.00, 1),
    (N'cl-wo-51-l1', N'cl-wo-51', N'SJF Landscape', N'', N'Subcontractor', N'EXTW-LND', N'00006-22 Hard Landscaping', 1, 4000.00, 4000.00, 4000.00, 1),
    (N'cl-wo-52-l1', N'cl-wo-52', N'Jewel PS - Metal Track QU0376', N'', N'Subcontractor', N'INT-MGW', N'00006-09 Gypsum Board', 1, 3840.00, 3840.00, 3840.00, 1),
    (N'cl-wo-53-l1', N'cl-wo-53', N'Asset UFH Basement - Additional', N'', N'Subcontractor', N'MEC-UFH', N'00006-14 UFH System', 1, 2425.00, 2425.00, 2425.00, 1),
    (N'cl-wo-54-l1', N'cl-wo-54', N'Jewel PS - Painting QU0408', N'', N'Subcontractor', N'DEC-STD', N'00006-20 Painting', 1, 843.00, 843.00, 843.00, 1),
    (N'cl-wo-55-l1', N'cl-wo-55', N'Asset - Pool Heating System', N'', N'Subcontractor', N'SPEC-POO', N'00006-14 UFH System', 1, 5479.00, 5479.00, 5479.00, 1),
    (N'cl-wo-56-l1', N'cl-wo-56', N'Generation Windows Internal Doors', N'', N'Subcontractor', N'WDR-INT', N'00006-10 Windows & Doors', 1, 29711.00, 29711.00, 29711.00, 1),
    (N'cl-wo-57-l1', N'cl-wo-57', N'Dunkley Plaster - External Render', N'', N'Subcontractor', N'INT-RDR', N'00006-22 Hard Landscaping', 1, 9000.00, 9000.00, 9000.00, 1),
    (N'cl-wo-58-l1', N'cl-wo-58', N'Oltco Resin Driveway', N'As per quote QU-1071', N'Subcontractor', N'EXTW-PAV', N'00006-22 Hard Landscaping', 1, 18025.00, 18025.00, 18025.00, 1),
    (N'cl-wo-59-l1', N'cl-wo-59', N'Fascia and Soffit', N'Install new white Woodgrain UPVC fascia and soffits; new smooth white UPVC Deepflow/Highcap guttering; replace existing downpipes with new white round 68mm pipes; roofline remedial works as required; slight overhaul on existing garage FSG.', N'Subcontractor', N'ROOF-FSU', N'00006-07 Roofing & Guttering', 1, 6700.00, 6700.00, 6700.00, 1),
    (N'cl-wo-59-l2', N'cl-wo-59', N'Gable Roofing Works', N'Adapt scaffolding; remove roof verge and gable ladder; rebuild gable ladder; rebed verge.', N'Subcontractor', N'ROOF-RFR', N'00006-07 Roofing & Guttering', 1, 4200.00, 4200.00, 4200.00, 2),
    (N'cl-wo-60-l1', N'cl-wo-60', N'Jewel PS - Labour & Materials Nov 24', N'', N'Subcontractor', N'PRELIMS-LAB', N'10003 Labour - Jewel PS Materials', 1, 16697.77, 16697.77, 16697.77, 1),
    (N'cl-wo-61-l1', N'cl-wo-61', N'Hudson Entrance Gates', N'', N'Subcontractor', N'EXTW-FEN', N'00006-22 Hard Landscaping', 1, 14867.00, 14867.00, 14867.00, 1),
    (N'cl-wo-62-l1', N'cl-wo-62', N'Generation Windows - Quote 9126', N'', N'Other', N'WDR-ALU', N'00006-10 Windows & Doors', 1, 3164.00, 3164.00, 1582.00, 1),
    (N'cl-wo-63-l1', N'cl-wo-63', N'Anything Electrical - Invoice 1578', N'', N'Subcontractor', N'ELE-STD', N'00006-15 Electrics', 1, 385.00, 385.00, 385.00, 1)
) AS source ([WorkOrderLineId], [WorkOrderId], [Title], [Description], [CostType], [CostCode], [LegacyCostCode], [Quantity], [UnitCost], [LineTotal], [PaidToDate], [SortOrder])
ON target.[WorkOrderLineId] = source.[WorkOrderLineId]
WHEN MATCHED THEN
    UPDATE SET
        target.[WorkOrderId]    = source.[WorkOrderId],
        target.[Title]          = source.[Title],
        target.[Description]    = source.[Description],
        target.[CostType]       = source.[CostType],
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
            source.[CostType], source.[CostCode], source.[LegacyCostCode], source.[Quantity], N'item',
            source.[UnitCost], source.[LineTotal], source.[PaidToDate], source.[SortOrder])
WHEN NOT MATCHED BY SOURCE AND target.[WorkOrderLineId] LIKE N'cl-wo-%' THEN
    DELETE;
GO

-- ----------------------------------------------------------------------------
-- 4. Reconciliation — totals should match the printed orders
-- ----------------------------------------------------------------------------
-- Expected: 50 orders, GBP 544,284.38 committed, GBP 542,702.38 paid, 65 lines
-- (only PO-62 has an outstanding balance: GBP 1,582.00).
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
WHERE w.[ProjectId] = N'd64c8a72f4b14eebbabd251b54564ef5'
ORDER BY w.[Number];
