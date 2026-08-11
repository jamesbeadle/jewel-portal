BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_AddCashUpFrontDeposit'
)
BEGIN
    ALTER TABLE [ProjectRetentions] ADD [DepositPercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_AddCashUpFrontDeposit'
)
BEGIN
    ALTER TABLE [ValuationClaims] ADD [DepositPercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_AddCashUpFrontDeposit'
)
BEGIN
    ALTER TABLE [ValuationClaims] ADD [DepositReleased] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_AddCashUpFrontDeposit'
)
BEGIN
    ALTER TABLE [ValuationReportSnapshots] ADD [DepositPercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_AddCashUpFrontDeposit'
)
BEGIN
    ALTER TABLE [ValuationReportSnapshots] ADD [DepositReleased] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_AddCashUpFrontDeposit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811120000_AddCashUpFrontDeposit', N'8.0.10');
END;
GO

COMMIT;
GO

