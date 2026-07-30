-- Idempotent migration script: 20260729100000_TodoItemsAssigneePerson
-- Adds the nullable AssigneePersonEmail column to TodoItems (the optional pin of a
-- to-do to one named holder of its assigned role). Purely additive — no drops, no
-- type changes, no data movement — so it is safe on the runbook's own terms
-- (docs/09-operations/applying-migrations.md) and safe to run twice.

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260729100000_TodoItemsAssigneePerson')
BEGIN
    ALTER TABLE [TodoItems] ADD [AssigneePersonEmail] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260729100000_TodoItemsAssigneePerson')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729100000_TodoItemsAssigneePerson', N'8.0.10');
END;
GO
