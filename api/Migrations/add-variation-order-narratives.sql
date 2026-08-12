-- ============================================================================
-- AddVariationOrderNarratives  (2026-08-12)
-- ============================================================================
-- Variation orders gain the narrative sections of their official document:
-- CommercialBasis, ProgrammeImpact and Exclusions — free text, all optional,
-- 4000 characters each (the same allowance as the request document's narrative
-- fields). Additive only, so it is safe to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260812200000_AddVariationOrderNarratives.cs and
-- records itself in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812200000_AddVariationOrderNarratives'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [CommercialBasis] nvarchar(4000) NULL;
    ALTER TABLE [VariationOrderQuotes] ADD [ProgrammeImpact] nvarchar(4000) NULL;
    ALTER TABLE [VariationOrderQuotes] ADD [Exclusions] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812200000_AddVariationOrderNarratives'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812200000_AddVariationOrderNarratives', N'8.0.10');
END;
GO

COMMIT;
GO
