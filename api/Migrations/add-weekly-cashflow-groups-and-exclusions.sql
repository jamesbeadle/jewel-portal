-- ============================================================================
-- AddWeeklyCashflowGroupsAndExclusions  (2026-08-28)
-- ============================================================================
-- Two accountant's requests on the Weekly Cashflow: supplier groups — sets of
-- Xero supplier names the Supplier bills band pulls together into one line
-- (the material suppliers: Grant & Stone, HSS Hire, Skip IT) — and exclusions
-- — "don't count this Xero entry, a direct-debit item already covers it" (the
-- Jaguar case: a monthly DD spread as a manual item, plus a one-off bill in
-- Xero that must not double-count). Additive only (two new tables), so it is
-- safe to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260828110000_AddWeeklyCashflowGroupsAndExclusions.cs
-- and records itself in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828110000_AddWeeklyCashflowGroupsAndExclusions'
)
BEGIN
    CREATE TABLE [WeeklyCashflowSupplierGroups] (
        [SupplierGroupId]  nvarchar(64)   NOT NULL,
        [Name]             nvarchar(200)  NOT NULL,
        [ContactNamesJson] nvarchar(4000) NOT NULL,
        [CreatedByEmail]   nvarchar(256)  NOT NULL,
        [CreatedAt]        datetimeoffset NOT NULL,
        CONSTRAINT [PK_WeeklyCashflowSupplierGroups] PRIMARY KEY ([SupplierGroupId])
    );

    CREATE TABLE [WeeklyCashflowExclusions] (
        [PlacementKey]    nvarchar(128)  NOT NULL,
        [ExcludedByEmail] nvarchar(256)  NOT NULL,
        [ExcludedAt]      datetimeoffset NOT NULL,
        CONSTRAINT [PK_WeeklyCashflowExclusions] PRIMARY KEY ([PlacementKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828110000_AddWeeklyCashflowGroupsAndExclusions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828110000_AddWeeklyCashflowGroupsAndExclusions', N'8.0.10');
END;
GO

COMMIT;
GO
