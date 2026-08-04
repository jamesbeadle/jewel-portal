BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804120000_AddXeroSitePnlMonths'
)
BEGIN
    CREATE TABLE [XeroSitePnlMonths] (
        [XeroSitePnlMonthId] nvarchar(80) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Month] datetime2 NOT NULL,
        [Income] decimal(18,4) NOT NULL,
        [CostOfSales] decimal(18,4) NOT NULL,
        [OperatingExpenses] decimal(18,4) NOT NULL,
        [LastSyncedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_XeroSitePnlMonths] PRIMARY KEY ([XeroSitePnlMonthId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804120000_AddXeroSitePnlMonths'
)
BEGIN
    CREATE INDEX [IX_XeroSitePnlMonths_ProjectId] ON [XeroSitePnlMonths] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804120000_AddXeroSitePnlMonths'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804120000_AddXeroSitePnlMonths', N'8.0.10');
END;
GO

COMMIT;
GO

