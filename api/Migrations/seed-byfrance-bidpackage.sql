-- ============================================================================
-- Seed: By France -- Bid Package BPI-0001 "By France Landscaping"
-- ----------------------------------------------------------------------------
-- Project : By France, Leas Green, Chislehurst, BR7 6HD  (JBB-2026-001)
-- ProjectId: 3490f944b29545c4b8d5a04130f42ab8
-- Package  : 85c0c5093fbd49059a7fc0383aaf362d  (BPI-0001, created in-app)
--
-- SOURCE
-- Email thread "Fw: BY FRANCE - Landscaping Packages" (Nigel Reilly ->
-- projects@jewelbb.co.uk, 6 Jul 2026), which rolls up James Clark's breakdown
-- of tender allowances and provisionals for the landscaping area:
--
--   Tender allowances
--     Q25 Slab pavings to patios: 600x600 stone paving  228 m2 @ 145.00 = 33,060.00
--     Aco threshold drain                                24 m  @ 145.00 =  3,480.00
--     Concrete edging & widening to access                1 item        =  1,500.00
--     Hardcore sub-base to new patio area & front drive 448 m2 @  28.00 = 12,544.00
--     Tarmac to front drive                             220 m2 @  85.00 = 18,700.00
--   Provisionals
--     Topsoil for soft landscaping                        1 item        =  2,000.00
--     Lawn seeding                                        1 item        =  2,500.00
--     Electric entrance gates (PS)                        1 item        = 15,000.00
--
-- COMMERCIAL CONTEXT (for whoever compares the tenders)
-- The patio was a Provisional Sum; the client has now selected the actual
-- slabs and the area is revised 228 m2 -> 250 m2. Slabs are FREE-ISSUED by
-- Jewel: Marshalls Fairstone Antique Alverno 4-size project pack, silver
-- limestone -- Chandlers quotation 72870702 (22/06/2026), 13 packs @ 839.81
-- = 10,917.53 supply (~43.67/m2). Within the 145.00/m2 allowance that leaves
-- ~101.40/m2 for installation -- the number a subbie install rate has to
-- beat before the variation is raised to the architect. Sand & cement for
-- 50mm beds is priced as its own line so it can be dropped if Jewel supplies
-- via a revised Chandlers quote instead.
--
-- DELIBERATELY EXCLUDED: the V39 entrance items (fencing removal, brick
-- piers, face brickwork, 35 m2 external paving, 211 lm brick edging). V39 is
-- unapproved and awaiting Alison's revised landscaping plans (P-800 is at
-- Rev I); tendering it now would price against dead drawings. Add the lines
-- once the variation is reviewed against the updated plans.
--
-- Line-item Coverage is left Unassigned (0); the QS links lines to the
-- contract BoQ or a VOQ in-app once the variation position is settled.
--
-- Idempotent: keyed on stable ids (bf-bpi1-li-NN). A re-run refreshes every
-- field via MERGE and removes any other line items on THIS package (so the
-- package is exactly as constructed here). The package row itself is
-- refreshed if present, inserted if the in-app record was deleted.
-- Recipients are NOT seeded -- invites are issued in-app from the directory.
-- Safe to run repeatedly.
-- ============================================================================

-- ---- The package -----------------------------------------------------------
MERGE INTO [dbo].[BidPackages] AS target
USING (VALUES
    (N'85c0c5093fbd49059a7fc0383aaf362d', N'3490f944b29545c4b8d5a04130f42ab8',
     N'By France Landscaping', N'Landscaping', 1 /*Inviting*/, 1 /*BPI-0001*/)
) AS source (BidPackageId, ProjectId, Title, Trade, Status, Number)
ON target.BidPackageId = source.BidPackageId
WHEN MATCHED THEN UPDATE SET
    ProjectId = source.ProjectId,
    Title     = source.Title,
    Trade     = source.Trade,
    Status    = source.Status,
    Number    = source.Number
WHEN NOT MATCHED THEN INSERT
    (BidPackageId, ProjectId, Title, Trade, Status, CreatedAt, OwnerEmail, VariationOrderQuoteId, Number)
    VALUES (source.BidPackageId, source.ProjectId, source.Title, source.Trade,
            source.Status, SYSDATETIMEOFFSET(), N'nigel.reilly@jewelgroup.co.uk', NULL, source.Number);

-- ---- Line items -------------------------------------------------------------
-- Trade groups mirror how the detail page renders (grouped by Trade).
MERGE INTO [dbo].[BidPackageLineItems] AS target
USING (VALUES
    -- Rear patio (the package's headline scope)
    (N'bf-bpi1-li-01', N'Lay Marshalls Fairstone Antique Alverno 4-size project pack silver limestone paving to rear patio on 50mm sand:cement bed. Slabs free-issued by Jewel (Chandlers quotation 72870702). Area revised from 228m2 tender allowance.', N'm²', 250.00, N'Paving',       1),
    (N'bf-bpi1-li-02', N'Supply of sand & cement for 50mm mortar beds (subcontractor-supply option — omit price if Jewel supplies via revised Chandlers quote).',                                                                                     N'm²', 250.00, N'Paving',       2),
    (N'bf-bpi1-li-03', N'Aco threshold drain.',                                                                                                                                                                                                       N'm',   24.00, N'Paving',       3),
    (N'bf-bpi1-li-04', N'Concrete edging & widening to access.',                                                                                                                                                                                      N'item',  1.00, N'Paving',       4),
    -- Groundworks
    (N'bf-bpi1-li-05', N'Hardcore sub-base to new patio area & front drive.',                                                                                                                                                                         N'm²', 448.00, N'Groundworks',  5),
    -- Front drive
    (N'bf-bpi1-li-06', N'Tarmac to front drive.',                                                                                                                                                                                                     N'm²', 220.00, N'Surfacing',    6),
    -- Soft landscaping
    (N'bf-bpi1-li-07', N'Topsoil for soft landscaping.',                                                                                                                                                                                              N'item',  1.00, N'Soft landscaping', 7),
    (N'bf-bpi1-li-08', N'Lawn seeding.',                                                                                                                                                                                                              N'item',  1.00, N'Soft landscaping', 8),
    -- Provisional / specialist
    (N'bf-bpi1-li-09', N'Electric entrance gates (Provisional Sum £15,000).',                                                                                                                                                                         N'item',  1.00, N'Gates',        9)
) AS source (LineItemId, Description, Unit, Quantity, Trade, SortOrder)
ON target.LineItemId = source.LineItemId
WHEN MATCHED THEN UPDATE SET
    BidPackageId = N'85c0c5093fbd49059a7fc0383aaf362d',
    Description  = source.Description,
    Unit         = source.Unit,
    Quantity     = source.Quantity,
    Trade        = source.Trade,
    SortOrder    = source.SortOrder,
    Coverage     = 0,
    BoqLineItemId = NULL,
    VariationOrderQuoteId = NULL
WHEN NOT MATCHED THEN INSERT
    (LineItemId, BidPackageId, Description, Unit, Quantity, Trade, SortOrder, Coverage, BoqLineItemId, VariationOrderQuoteId)
    VALUES (source.LineItemId, N'85c0c5093fbd49059a7fc0383aaf362d', source.Description,
            source.Unit, source.Quantity, source.Trade, source.SortOrder, 0, NULL, NULL)
WHEN NOT MATCHED BY SOURCE AND target.BidPackageId = N'85c0c5093fbd49059a7fc0383aaf362d' THEN DELETE;
