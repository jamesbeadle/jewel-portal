BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811190000_AddInvoiceDepositCredited'
)
BEGIN
    ALTER TABLE [ValuationInvoices] ADD [DepositCredited] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811190000_AddInvoiceDepositCredited'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811190000_AddInvoiceDepositCredited', N'8.0.10');
END;
GO

COMMIT;
GO

