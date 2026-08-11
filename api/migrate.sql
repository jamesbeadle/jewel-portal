BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_AddTodoItemLinks'
)
BEGIN
    CREATE TABLE [TodoItemLinks] (
        [TodoItemLinkId] nvarchar(64) NOT NULL,
        [TodoItemAId] nvarchar(64) NOT NULL,
        [TodoItemBId] nvarchar(64) NOT NULL,
        [LinkedAt] datetimeoffset NOT NULL,
        [LinkedByEmail] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_TodoItemLinks] PRIMARY KEY ([TodoItemLinkId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_AddTodoItemLinks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TodoItemLinks_TodoItemAId_TodoItemBId] ON [TodoItemLinks] ([TodoItemAId], [TodoItemBId]) WHERE [TodoItemAId] IS NOT NULL AND [TodoItemBId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_AddTodoItemLinks'
)
BEGIN
    CREATE INDEX [IX_TodoItemLinks_TodoItemBId] ON [TodoItemLinks] ([TodoItemBId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_AddTodoItemLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811100000_AddTodoItemLinks', N'8.0.10');
END;
GO

COMMIT;
GO

