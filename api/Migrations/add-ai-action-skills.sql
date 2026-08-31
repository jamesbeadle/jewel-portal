-- ============================================================================
-- AddAiActionSkills  (2026-08-31)
-- ============================================================================
-- Skills attached to connector actions, or to whole action areas, so
-- describe_action inlines the attached doctrine next to the argument schema.
-- The actions are code (AiActionRegistry); the skills are rows (Skills); this
-- table is the edge between them, curated on the AI Actions admin page.
-- Additive only, so it is safe to apply BEFORE the deploy.
-- Mirrors api/Migrations/20260831090000_AddAiActionSkills.cs and records
-- itself in __EFMigrationsHistory so EF never re-applies it.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831090000_AddAiActionSkills'
)
BEGIN
    CREATE TABLE [AiActionSkills] (
        [ActionSkillId]   nvarchar(64)   NOT NULL,
        [TargetKind]      nvarchar(16)   NOT NULL,
        [TargetKey]       nvarchar(128)  NOT NULL,
        [SkillKey]        nvarchar(128)  NOT NULL,
        [AttachedByEmail] nvarchar(256)  NOT NULL,
        [AttachedAt]      datetimeoffset NOT NULL,
        CONSTRAINT [PK_AiActionSkills] PRIMARY KEY ([ActionSkillId])
    );

    CREATE UNIQUE INDEX [IX_AiActionSkills_Target_Skill]
        ON [AiActionSkills] ([TargetKind], [TargetKey], [SkillKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831090000_AddAiActionSkills'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831090000_AddAiActionSkills', N'8.0.10');
END;
GO

COMMIT;
GO
