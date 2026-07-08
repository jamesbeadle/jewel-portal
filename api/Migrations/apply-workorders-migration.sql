-- ============================================================================
-- Manually applies migration 20260708130000_ExtendWorkOrdersForCostCenters
-- ----------------------------------------------------------------------------
-- The app auto-applies EF migrations on startup (Program.cs MigrateAsync), but
-- the work-order seeds are needed BEFORE the updated API is deployed. This
-- script therefore applies the migration's exact schema changes by hand AND
-- records the migration in [__EFMigrationsHistory], so the next deploy's
-- auto-migrate sees it as already applied and skips it (per the warning in
-- manual-sql/AddBidPackageInvites.sql, hand-applied schema without the history
-- row would crash the next startup).
--
-- Idempotent: the whole script is a no-op when the history row already exists
-- (i.e. after the new API has been deployed, or on a re-run).
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory]
               WHERE [MigrationId] = N'20260708130000_ExtendWorkOrdersForCostCenters')
BEGIN
    PRINT 'Applying 20260708130000_ExtendWorkOrdersForCostCenters...';

    -- WorkOrders: BidPackageId becomes optional, Scope widens to 4000.
    ALTER TABLE [dbo].[WorkOrders] ALTER COLUMN [BidPackageId] nvarchar(64) NULL;
    ALTER TABLE [dbo].[WorkOrders] ALTER COLUMN [Scope] nvarchar(4000) NOT NULL;

    -- WorkOrders: new header columns (mirrors the EF migration's AddColumn calls).
    ALTER TABLE [dbo].[WorkOrders] ADD [Number] int NOT NULL DEFAULT 0;
    ALTER TABLE [dbo].[WorkOrders] ADD [Title] nvarchar(256) NOT NULL DEFAULT N'';
    ALTER TABLE [dbo].[WorkOrders] ADD [Status] int NOT NULL DEFAULT 0;
    ALTER TABLE [dbo].[WorkOrders] ADD [CreatedAt] datetimeoffset NOT NULL DEFAULT '2026-01-01T00:00:00+00:00';
    ALTER TABLE [dbo].[WorkOrders] ADD [ScheduledCompletion] datetimeoffset NULL;
    ALTER TABLE [dbo].[WorkOrders] ADD [SourceReference] nvarchar(64) NULL;

    -- Backfill existing awarded orders: Released, created at award, numbered by
    -- award order. Dynamic SQL because the columns were added in this batch.
    EXEC(N'
        UPDATE [dbo].[WorkOrders] SET [CreatedAt] = [AwardedAt], [Status] = 1;
        WITH Numbered AS (
            SELECT [WorkOrderId], ROW_NUMBER() OVER (ORDER BY [AwardedAt], [WorkOrderId]) AS Rn
            FROM [dbo].[WorkOrders]
        )
        UPDATE w SET w.[Number] = n.Rn
        FROM [dbo].[WorkOrders] w
        JOIN Numbered n ON n.[WorkOrderId] = w.[WorkOrderId];
    ');

    CREATE TABLE [dbo].[WorkOrderLines] (
        [WorkOrderLineId] nvarchar(64)   NOT NULL,
        [WorkOrderId]     nvarchar(64)   NOT NULL,
        [Title]           nvarchar(256)  NOT NULL,
        [Description]     nvarchar(1024) NOT NULL,
        [CostType]        nvarchar(64)   NOT NULL,
        [CostCode]        nvarchar(32)   NOT NULL,
        [LegacyCostCode]  nvarchar(128)  NOT NULL,
        [Quantity]        decimal(18,4)  NOT NULL,
        [Unit]            nvarchar(32)   NOT NULL,
        [UnitCost]        decimal(18,4)  NOT NULL,
        [LineTotal]       decimal(18,4)  NOT NULL,
        [PaidToDate]      decimal(18,4)  NOT NULL,
        [SortOrder]       int            NOT NULL,
        CONSTRAINT [PK_WorkOrderLines] PRIMARY KEY ([WorkOrderLineId])
    );

    -- Record the migration so startup MigrateAsync() skips it. ProductVersion
    -- copied from the latest applied migration (same EF Core version).
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    SELECT N'20260708130000_ExtendWorkOrdersForCostCenters',
           (SELECT TOP 1 [ProductVersion] FROM [dbo].[__EFMigrationsHistory] ORDER BY [MigrationId] DESC);

    PRINT 'Applied and recorded in __EFMigrationsHistory.';
END
ELSE
    PRINT 'Migration 20260708130000_ExtendWorkOrdersForCostCenters already applied - skipping.';
GO
