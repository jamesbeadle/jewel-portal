-- ============================================================================
-- Seed the GLOBAL cost-center master
-- ----------------------------------------------------------------------------
-- One shared cost-center hierarchy used by every project's Financials tab.
-- Curated flat list of trade cost centers (variation markers, tender-value
-- duplicates and individual estimate lines from the source register removed).
--
-- This script OWNS the [CostCenters] table: it creates the table if missing
-- and then seeds it. The table is intentionally NOT created by an EF migration
-- so the master list is managed here rather than per project. Run it against
-- the Azure SQL database once (and again whenever the list below changes).
--
-- Idempotent: safe to run repeatedly. Rows are matched on CostCenterId; the
-- Code / Name / SortOrder are refreshed and the row re-activated on a re-run.
-- To retire a cost center, set IsActive = 0 (the Financials view hides it)
-- rather than deleting the row.
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
    (N'cc-0010', N'00001',    N'Management & Labour Costs',     10),
    (N'cc-0020', N'00002',    N'Welfare & Hoarding',            20),
    (N'cc-0030', N'00003',    N'Plant & Equipment',             30),
    (N'cc-0040', N'00005',    N'Sundry Items',                  40),
    (N'cc-0050', N'00006-01', N'Demolition',                    50),
    (N'cc-0060', N'0003',     N'Excavation & Earthwork',        60),
    (N'cc-0070', N'0004',     N'Concrete Works',                70),
    (N'cc-0080', N'00002-02', N'Foundations',                   80),
    (N'cc-0090', N'00006-03', N'Drainage',                      90),
    (N'cc-0100', N'0009',     N'Brickwork & Blockwork',        100),
    (N'cc-0110', N'00006-27', N'Structural Steelwork',         110),
    (N'cc-0120', N'00006-06', N'Carpenter - 1st Fix',          120),
    (N'cc-0130', N'00006-07', N'Roofer',                       130),
    (N'cc-0140', N'00006-11', N'Insulation & Screed',          140),
    (N'cc-0150', N'00006-10', N'Glazier',                      150),
    (N'cc-0160', N'00006-08', N'Glazing',                      160),
    (N'cc-0170', N'00006-13', N'Plumbing & Heating',           170),
    (N'cc-0180', N'00006-15', N'Electrical',                   180),
    (N'cc-0190', N'00006-12', N'Plaster',                      190),
    (N'cc-0200', N'00006-12', N'Render',                       200),
    (N'cc-0210', N'00006-18', N'Carpenter - 2nd Fix',          210),
    (N'cc-0220', N'00006-19', N'Wall Tiling & Floor Finishes', 220),
    (N'cc-0230', N'00006-20', N'Painter & Decorator',          230),
    (N'cc-0240', N'00006-22', N'Landscape',                    240)
) AS source ([CostCenterId], [Code], [Name], [SortOrder])
ON target.[CostCenterId] = source.[CostCenterId]
WHEN MATCHED THEN
    UPDATE SET
        target.[Code]      = source.[Code],
        target.[Name]      = source.[Name],
        target.[SortOrder] = source.[SortOrder],
        target.[IsActive]  = 1
WHEN NOT MATCHED THEN
    INSERT ([CostCenterId], [Code], [Name], [SortOrder], [IsActive])
    VALUES (source.[CostCenterId], source.[Code], source.[Name], source.[SortOrder], 1);
GO

SELECT [SortOrder], [Code], [Name], [IsActive]
FROM [dbo].[CostCenters]
ORDER BY [SortOrder];
