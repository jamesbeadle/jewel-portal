-- ============================================================================
-- AddKeyedBankStatementBalances  (2026-10-03)
-- ============================================================================
-- The bank statement balances a director keys by hand, for the Cash in bank
-- figure: one row per Xero bank account — the balance the bank's statement
-- shows, its date, who keyed it and when. Additive only.
--
-- House-style scoped script: applies the migration directly and records its id
-- in __EFMigrationsHistory so EF never re-applies it. Mirrors
-- api/Migrations/20261003090000_AddKeyedBankStatementBalances.cs. Safe to apply
-- BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-keyed-bank-statement-balances.sql -b -o add-keyed-bank-statement-balances.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261003090000_AddKeyedBankStatementBalances')
BEGIN
    IF OBJECT_ID(N'[KeyedBankStatementBalances]', N'U') IS NULL
    BEGIN
        CREATE TABLE [KeyedBankStatementBalances] (
            [AccountId] nvarchar(128) NOT NULL,
            [AccountName] nvarchar(200) NOT NULL,
            [Balance] decimal(18,4) NOT NULL,
            [StatementDate] datetimeoffset NOT NULL,
            [KeyedByEmail] nvarchar(256) NOT NULL,
            [KeyedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_KeyedBankStatementBalances] PRIMARY KEY ([AccountId])
        );
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003090000_AddKeyedBankStatementBalances', N'8.0.10');
END;
GO

COMMIT;
GO
