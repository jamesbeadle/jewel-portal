BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819100000_AddSubcontractorProspectFlag'
)
BEGIN
    ALTER TABLE [Subcontractors] ADD [IsProspect] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819100000_AddSubcontractorProspectFlag'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819100000_AddSubcontractorProspectFlag', N'8.0.10');
END;
GO

COMMIT;
GO

