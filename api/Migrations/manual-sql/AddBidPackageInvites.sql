-- ============================================================================
-- AddBidPackageInvites  (Phase 1b — Bid Package Invites data layer)
-- ============================================================================
-- Two new tables backing BidPackageRecipientEntity and BidPackageLineItemEntity.
--
-- PREFERRED: regenerate a proper EF migration in your environment (it also
-- updates the .Designer.cs and JpmsContextModelSnapshot.cs, keeping the
-- migration chain consistent):
--
--     dotnet ef migrations add AddBidPackageInvites --project api
--     dotnet ef database update --project api
--
-- This .sql is provided so the schema can be reviewed and, if you apply
-- migrations manually, run directly. It matches the columns EF infers from the
-- entities' data annotations (SQL Server provider). It does NOT create indexes,
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
