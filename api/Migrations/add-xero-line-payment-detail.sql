-- ============================================================================
-- AddXeroLinePaymentDetail  (2026-09-14)
-- ============================================================================
-- The bill's payment detail on every stored Xero purchase line, beside the
-- InvoiceTotal / AmountDue the paid maths already reads: AmountPaid (the cash
-- Xero recorded against the bill — under CIS, short of the total by the
-- deduction), CisDeduction (what Xero calculated and withheld for HMRC) and
-- FullyPaidOnDate (when nothing further was owed). The accountant's ask: the
-- supplier account must show the actual payment made and the CIS deducted.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260914210000_AddXeroLinePaymentDetail.cs.
-- Additive only — the money columns default to 0 and the date to NULL, so
-- every existing line reads "no detail yet" until the next ledger sync fills
-- it. Safe to apply BEFORE or WITH the deploy; must be applied before the
-- deployed api reads the columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-xero-line-payment-detail.sql -b \
--          -o add-xero-line-payment-detail.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260914210000_AddXeroLinePaymentDetail')
BEGIN
    IF COL_LENGTH('XeroLedgerLines', 'AmountPaid') IS NULL
        ALTER TABLE [XeroLedgerLines] ADD [AmountPaid] decimal(18,4) NOT NULL
            CONSTRAINT [DF_XeroLedgerLines_AmountPaid] DEFAULT (0);
    IF COL_LENGTH('XeroLedgerLines', 'CisDeduction') IS NULL
        ALTER TABLE [XeroLedgerLines] ADD [CisDeduction] decimal(18,4) NOT NULL
            CONSTRAINT [DF_XeroLedgerLines_CisDeduction] DEFAULT (0);
    IF COL_LENGTH('XeroLedgerLines', 'FullyPaidOnDate') IS NULL
        ALTER TABLE [XeroLedgerLines] ADD [FullyPaidOnDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260914210000_AddXeroLinePaymentDetail')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260914210000_AddXeroLinePaymentDetail', N'8.0.10');
END;
GO

COMMIT;
GO
