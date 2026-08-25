-- ============================================================================
-- AddVariationOrderDraftLines  (2026-08-25)
-- ============================================================================
-- A variation gains a STAGED agreed build-up: DraftLinesJson nvarchar(max) NULL
-- on VariationOrderQuotes — the client-agreed priced lines captured before
-- approval so the approve modal opens pre-seeded and the estimate reads the
-- agreed figure (the assistant's variation_build_up dialog writes here).
-- Consumed by approval. Additive only, so it is safe to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260825130000_AddVariationOrderDraftLines.cs and
-- records itself in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825130000_AddVariationOrderDraftLines'
)
BEGIN
    ALTER TABLE [VariationOrderQuotes] ADD [DraftLinesJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825130000_AddVariationOrderDraftLines'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825130000_AddVariationOrderDraftLines', N'8.0.10');
END;
GO

COMMIT;
GO
