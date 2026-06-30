-- ============================================================================
-- AddBidPackageInvites  (Phase 1b — Bid Package Invites data layer)
-- ============================================================================
-- Two new tables backing BidPackageRecipientEntity and BidPackageLineItemEntity.
--
-- ⚠️ THIS APP AUTO-APPLIES MIGRATIONS ON STARTUP (Program.cs: context.Database.MigrateAsync()).
-- So you must add a REAL EF migration, not run this SQL by hand — otherwise the next deploy's
-- auto-migrate will try to CreateTable on tables that already exist and fail on startup.
--
-- REQUIRED FIX (in your dev environment, which has the .NET SDK):
--
--     dotnet ef migrations add AddBidPackageInvites --project api
--     # review the generated migration creates exactly BidPackageRecipients + BidPackageLineItems
--     # then deploy — startup MigrateAsync() applies it automatically
--     # (or apply now without deploy:)  dotnet ef database update --project api
--
-- The SQL below is REFERENCE ONLY (the schema the migration should produce). Do not run it against
-- an environment that auto-migrates. It matches the columns EF infers from the entities' data
-- annotations (SQL Server provider). It does NOT create indexes,
-- mirroring exactly what the scaffolded migration would produce; adding
-- IX_BidPackageRecipients_BidPackageId / IX_BidPackageLineItems_BidPackageId is
-- a recommended follow-up for query performance.
-- ============================================================================

CREATE TABLE [BidPackageRecipients] (
    [RecipientId]     nvarchar(64)   NOT NULL,
    [BidPackageId]    nvarchar(64)   NOT NULL,
    [SubcontractorId] nvarchar(64)   NOT NULL,
    [Status]          int            NOT NULL,
    [InvitedAt]       datetimeoffset NOT NULL,
    [RespondedAt]     datetimeoffset NULL,
    CONSTRAINT [PK_BidPackageRecipients] PRIMARY KEY ([RecipientId])
);

CREATE TABLE [BidPackageLineItems] (
    [LineItemId]   nvarchar(64)    NOT NULL,
    [BidPackageId] nvarchar(64)    NOT NULL,
    [Description]  nvarchar(512)   NOT NULL,
    [Unit]         nvarchar(32)    NOT NULL,
    [Quantity]     decimal(18,2)   NOT NULL,
    [Trade]        nvarchar(64)    NOT NULL,
    [SortOrder]    int             NOT NULL,
    CONSTRAINT [PK_BidPackageLineItems] PRIMARY KEY ([LineItemId])
);
