-- ============================================================================
-- AddProjectExpectedMonthlyValuation  (2026-08-13)
-- ============================================================================
-- Projects gain ExpectedMonthlyValuation (decimal, nullable): the FD's forecast
-- assumption of how much the architect is expected to certify per valuation
-- month. Null keeps the Cash Forecast's even spread to practical completion;
-- set, the forecast claims at this rate until left-to-claim runs out. Edited
-- inline on the Cash Forecast page; forecasting only — never touches
-- valuations or invoices. Additive only, so it is safe to apply BEFORE the
-- deploy. Mirrors api/Migrations/20260813150000_AddProjectExpectedMonthly-
-- Valuation.cs and records itself in __EFMigrationsHistory so EF never
-- re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813150000_AddProjectExpectedMonthlyValuation'
)
BEGIN
    ALTER TABLE [Projects] ADD [ExpectedMonthlyValuation] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813150000_AddProjectExpectedMonthlyValuation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813150000_AddProjectExpectedMonthlyValuation', N'8.0.10');
END;
GO

COMMIT;
GO
