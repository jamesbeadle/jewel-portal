-- ============================================================================
-- AddWorkerSettlementIdentityAndChaseDismissals  (2026-08-31)
-- ============================================================================
-- The accountant's month-end unblockers: Workers gain IsSoleTrader (the worker
-- is their own settlement counterparty — no invented directory company) and an
-- engagement window (EngagedFrom/EngagedTo — bounds what the chase list
-- EXPECTS, never what counts); LabourChaseDismissals records reviewed
-- chase-days dismissed with a reason, so the derived chase list and the
-- unconfirmed-cost accrual can be cleared without inventing a timesheet.
-- Additive only, so it is safe to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260831220000_AddWorkerSettlementIdentityAndChaseDismissals.cs
-- and records itself in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831220000_AddWorkerSettlementIdentityAndChaseDismissals'
)
BEGIN
    ALTER TABLE [Workers] ADD [IsSoleTrader] bit NOT NULL CONSTRAINT [DF_Workers_IsSoleTrader] DEFAULT 0;
    ALTER TABLE [Workers] ADD [EngagedFrom] datetimeoffset NULL;
    ALTER TABLE [Workers] ADD [EngagedTo] datetimeoffset NULL;

    CREATE TABLE [LabourChaseDismissals] (
        [LabourChaseDismissalId] nvarchar(64)   NOT NULL,
        [WorkerId]               nvarchar(64)   NOT NULL,
        [Date]                   datetimeoffset NOT NULL,
        [Reason]                 nvarchar(512)  NOT NULL,
        [DismissedByEmail]       nvarchar(256)  NOT NULL,
        [DismissedAt]            datetimeoffset NOT NULL,
        CONSTRAINT [PK_LabourChaseDismissals] PRIMARY KEY ([LabourChaseDismissalId])
    );

    CREATE UNIQUE INDEX [IX_LabourChaseDismissals_WorkerId_Date]
        ON [LabourChaseDismissals] ([WorkerId], [Date]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831220000_AddWorkerSettlementIdentityAndChaseDismissals', N'8.0.10');

    PRINT 'AddWorkerSettlementIdentityAndChaseDismissals: applied.';
END
ELSE
    PRINT 'AddWorkerSettlementIdentityAndChaseDismissals: already applied — nothing to do.';
GO

COMMIT;
GO
