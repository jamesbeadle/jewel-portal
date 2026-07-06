-- ============================================================================
-- Seed the GLOBAL cost-center master
-- ----------------------------------------------------------------------------
-- One shared cost-center hierarchy used by every project's Financials tab.
-- These are the high-level Jewel (JBB-*) buckets from the NRM2 <-> Jewel cost
-- code mapping: a single generic set that every NRM2 element rolls up into, so
-- the same cost centres apply across all projects rather than project-specific
-- trade lines. Codes are the Jewel codes; names are the Jewel internal element.
--
-- This script OWNS the [CostCenters] table: it creates the table if missing
-- and then seeds it. The table is intentionally NOT created by an EF migration
-- so the master list is managed here rather than per project. Run it against
-- the Azure SQL database once (and again whenever the list below changes).
--
-- Idempotent: safe to run repeatedly. Rows are matched on CostCenterId; the
-- Code / Name / SortOrder are refreshed and the row re-activated on a re-run.
-- Any cost centre NOT in the list below is automatically deactivated
-- (IsActive = 0) so superseded rows drop out of the Financials view without
-- being deleted. To retire a cost centre, remove it from the VALUES list.
--
-- ⚠ SUPERSEDED FOR DAY-TO-DAY CHANGES: cost codes are now managed in-app on
-- the Cost codes page (/cost-codes — AddCostCenter / ReviseCostCenter). This
-- script remains only for bootstrapping an empty database. Do NOT re-run it
-- against a live database: the NOT MATCHED BY SOURCE clause would retire
-- every code added or renamed through the app.
-- ============================================================================

IF OBJECT_ID(N'[dbo].[CostCenters]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CostCenters] (
        [CostCenterId] nvarchar(64)  NOT NULL,
        [Code]         nvarchar(32)  NOT NULL,
        [Name]         nvarchar(256) NOT NULL,
        [SortOrder]    int           NOT NULL,
        [IsActive]     bit           NOT NULL CONSTRAINT [DF_CostCenters_IsActive] DEFAULT (1),
        CONSTRAINT [PK_CostCenters] PRIMARY KEY ([CostCenterId])
    );
END;
GO

MERGE INTO [dbo].[CostCenters] AS target
USING (VALUES
    (N'cc-jbb-fac', N'JBB-FAC', N'Facilitating works',          10),
    (N'cc-jbb-dem', N'JBB-DEM', N'Demolition',                  20),
    (N'cc-jbb-sub', N'JBB-SUB', N'Substructure',                30),
    (N'cc-jbb-frm', N'JBB-FRM', N'Frame',                       40),
    (N'cc-jbb-upf', N'JBB-UPF', N'Upper floors',                50),
    (N'cc-jbb-rfs', N'JBB-RFS', N'Roof structure & coverings',  60),
    (N'cc-jbb-str', N'JBB-STR', N'Stairs & ramps',              70),
    (N'cc-jbb-exw', N'JBB-EXW', N'External walls',              80),
    (N'cc-jbb-wed', N'JBB-WED', N'Windows & external doors',    90),
    (N'cc-jbb-iwp', N'JBB-IWP', N'Internal walls',             100),
    (N'cc-jbb-ind', N'JBB-IND', N'Internal doors',             110),
    (N'cc-jbb-wlf', N'JBB-WLF', N'Wall finishes',              120),
    (N'cc-jbb-flf', N'JBB-FLF', N'Floor finishes',             130),
    (N'cc-jbb-clf', N'JBB-CLF', N'Ceiling finishes',           140),
    (N'cc-jbb-ffe', N'JBB-FFE', N'FF&E',                       150),
    (N'cc-jbb-san', N'JBB-SAN', N'Sanitaryware',               160),
    (N'cc-jbb-seq', N'JBB-SEQ', N'Services equipment',         170),
    (N'cc-jbb-drn', N'JBB-DRN', N'Drainage installation',      180),
    (N'cc-jbb-plb', N'JBB-PLB', N'Plumbing',                   190),
    (N'cc-jbb-hts', N'JBB-HTS', N'Heat source',                200),
    (N'cc-jbb-htg', N'JBB-HTG', N'Heating distribution',       210),
    (N'cc-jbb-ven', N'JBB-VEN', N'Ventilation',                220),
    (N'cc-jbb-ele', N'JBB-ELE', N'Electrical',                 230),
    (N'cc-jbb-bwc', N'JBB-BWC', N'Builders work',              240),
    (N'cc-jbb-ext', N'JBB-EXT', N'External works',             250),
    (N'cc-jbb-rpp', N'JBB-RPP', N'Paving & roads',             260),
    (N'cc-jbb-sfl', N'JBB-SFL', N'Soft landscape',             270),
    (N'cc-jbb-edr', N'JBB-EDR', N'External drainage',          280),
    (N'cc-jbb-prl', N'JBB-PRL', N'Preliminaries',              290),
    (N'cc-jbb-ohp', N'JBB-OHP', N'Overheads & profit',         300),
    (N'cc-jbb-fee', N'JBB-FEE', N'Professional fees',          310),
    (N'cc-jbb-rsk', N'JBB-RSK', N'Risk / contingency',         320)
) AS source ([CostCenterId], [Code], [Name], [SortOrder])
ON target.[CostCenterId] = source.[CostCenterId]
WHEN MATCHED THEN
    UPDATE SET
        target.[Code]      = source.[Code],
        target.[Name]      = source.[Name],
        target.[SortOrder] = source.[SortOrder],
        target.[IsActive]  = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([CostCenterId], [Code], [Name], [SortOrder], [IsActive])
    VALUES (source.[CostCenterId], source.[Code], source.[Name], source.[SortOrder], 1)
WHEN NOT MATCHED BY SOURCE THEN
    -- Any cost centre no longer in the master list above is retired, not deleted.
    UPDATE SET target.[IsActive] = 0;
GO

SELECT [SortOrder], [Code], [Name], [IsActive]
FROM [dbo].[CostCenters]
ORDER BY [SortOrder];
