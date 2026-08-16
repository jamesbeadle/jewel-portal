-- ============================================================================
-- AddBidPackageInviteDraft  (2026-08-16)
-- ============================================================================
-- The invite composer's persisted draft: six nullable columns on BidPackages
-- (InviteDraftSubject/Body/To/Cc/Bcc/SavedAt). Additive only, so it is safe to
-- apply BEFORE the deploy — and it must be applied NOW: the deployed API
-- already maps these columns, so until they exist every BidPackages query
-- 500s (JPMS-225597, ListLinkableRecords type=BidPackageInvite, 16 Aug).
--
-- Mirrors the HAND-CORRECTED api/Migrations/20260816113113_AddBidPackageInviteDraft.cs.
-- The originally scaffolded migration of that name was generated against a
-- stale snapshot and tried to drop live tables — NEVER run a script generated
-- from the pre-correction file. Each column is guarded individually
-- (belt-and-braces on top of the history guard) and the migration id is
-- recorded in __EFMigrationsHistory so EF never re-applies it.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-bid-package-invite-draft.sql -b -o add-bid-package-invite-draft.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    IF COL_LENGTH('BidPackages', 'InviteDraftBcc') IS NULL
        ALTER TABLE [BidPackages] ADD [InviteDraftBcc] nvarchar(max) NULL;
    IF COL_LENGTH('BidPackages', 'InviteDraftBody') IS NULL
        ALTER TABLE [BidPackages] ADD [InviteDraftBody] nvarchar(max) NULL;
    IF COL_LENGTH('BidPackages', 'InviteDraftCc') IS NULL
        ALTER TABLE [BidPackages] ADD [InviteDraftCc] nvarchar(2000) NULL;
    IF COL_LENGTH('BidPackages', 'InviteDraftSavedAt') IS NULL
        ALTER TABLE [BidPackages] ADD [InviteDraftSavedAt] datetimeoffset NULL;
    IF COL_LENGTH('BidPackages', 'InviteDraftSubject') IS NULL
        ALTER TABLE [BidPackages] ADD [InviteDraftSubject] nvarchar(512) NULL;
    IF COL_LENGTH('BidPackages', 'InviteDraftTo') IS NULL
        ALTER TABLE [BidPackages] ADD [InviteDraftTo] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816113113_AddBidPackageInviteDraft'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260816113113_AddBidPackageInviteDraft', N'8.0.10');
END;
GO

COMMIT;
GO
