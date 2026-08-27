-- Applies 20260827090000_AddCalendarEvents directly. Identical to what the scoped idempotent EF
-- script would emit for this migration: guarded table + indexes, then the history row. Safe to
-- re-run. Until this runs, the Calendar tab and "Raise calendar event" in the Control Centre
-- 500 on the missing table ("Invalid object name 'CalendarEvents'").
--
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i apply-calendar-events.sql -b -o apply-calendar-events.log

IF OBJECT_ID(N'[dbo].[CalendarEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CalendarEvents] (
        [CalendarEventId]  nvarchar(64)   NOT NULL,
        [ProjectId]        nvarchar(64)   NOT NULL,
        -- Global sequence behind the CAL-#### reference (the mailbox tag stem).
        [Number]           int            NOT NULL,
        [Title]            nvarchar(256)  NOT NULL,
        -- CalendarEventKind: 0 SiteVisit, 1 Delivery, 2 Meeting, 3 SubcontractorAttendance, 4 Other.
        [Kind]             int            NOT NULL,
        -- UK-local calendar date at midnight UTC (the SiteClock rule).
        [Date]             datetimeoffset NOT NULL,
        -- "HH:mm" wall-clock text; NULL = all day.
        [StartTime]        nvarchar(5)    NULL,
        -- Inclusive last day of a multi-day event; NULL = single day.
        [EndDate]          datetimeoffset NULL,
        [Notes]            nvarchar(max)  NOT NULL,
        [ClientVisible]    bit            NOT NULL,
        [CreatedByEmail]   nvarchar(256)  NOT NULL,
        [CreatedAt]        datetimeoffset NOT NULL,
        CONSTRAINT [PK_CalendarEvents] PRIMARY KEY ([CalendarEventId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CalendarEvents_ProjectId_Date'
               AND object_id = OBJECT_ID(N'[dbo].[CalendarEvents]'))
    CREATE INDEX [IX_CalendarEvents_ProjectId_Date] ON [dbo].[CalendarEvents] ([ProjectId], [Date]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CalendarEvents_Number'
               AND object_id = OBJECT_ID(N'[dbo].[CalendarEvents]'))
    CREATE INDEX [IX_CalendarEvents_Number] ON [dbo].[CalendarEvents] ([Number]);
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260827090000_AddCalendarEvents')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827090000_AddCalendarEvents', N'8.0.10');
GO
