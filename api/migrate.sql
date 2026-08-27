BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827090000_AddCalendarEvents'
)
BEGIN
    CREATE TABLE [CalendarEvents] (
        [CalendarEventId] nvarchar(64) NOT NULL,
        [ProjectId] nvarchar(64) NOT NULL,
        [Number] int NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Kind] int NOT NULL,
        [Date] datetimeoffset NOT NULL,
        [StartTime] nvarchar(5) NULL,
        [EndDate] datetimeoffset NULL,
        [Notes] nvarchar(max) NOT NULL,
        [ClientVisible] bit NOT NULL,
        [CreatedByEmail] nvarchar(256) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_CalendarEvents] PRIMARY KEY ([CalendarEventId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827090000_AddCalendarEvents'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_ProjectId_Date] ON [CalendarEvents] ([ProjectId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827090000_AddCalendarEvents'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_Number] ON [CalendarEvents] ([Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827090000_AddCalendarEvents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827090000_AddCalendarEvents', N'8.0.10');
END;
GO

COMMIT;
GO

