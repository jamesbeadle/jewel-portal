BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902120000_SplitLabourWeekSignOffAtMonthEnd'
)
BEGIN
    DROP INDEX [IX_LabourWeekSignOffs_WorkerId_WeekStart] ON [LabourWeekSignOffs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902120000_SplitLabourWeekSignOffAtMonthEnd'
)
BEGIN
    ALTER TABLE [LabourWeekSignOffs] ADD [MonthStart] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902120000_SplitLabourWeekSignOffAtMonthEnd'
)
BEGIN

    EXEC sp_executesql N'
    UPDATE [LabourWeekSignOffs]
    SET [MonthStart] = DATETIMEOFFSETFROMPARTS(YEAR([WeekStart]), MONTH([WeekStart]), 1, 0, 0, 0, 0, 0, 0, 7)
    WHERE [MonthStart] = DATETIMEOFFSETFROMPARTS(1, 1, 1, 0, 0, 0, 0, 0, 0, 7);

    INSERT INTO [LabourWeekSignOffs] ([LabourWeekSignOffId], [WorkerId], [WeekStart], [MonthStart], [SignedOffByEmail], [SignedOffAt])
    SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), N''-'', N'''')),
           [WorkerId], [WeekStart],
           DATEADD(month, 1, [MonthStart]),
           [SignedOffByEmail], [SignedOffAt]
    FROM [LabourWeekSignOffs] AS existing
    WHERE MONTH(DATEADD(day, 6, [WeekStart])) <> MONTH([WeekStart])
      AND NOT EXISTS (
          SELECT 1 FROM [LabourWeekSignOffs] AS twin
          WHERE twin.[WorkerId] = existing.[WorkerId]
            AND twin.[WeekStart] = existing.[WeekStart]
            AND twin.[MonthStart] = DATEADD(month, 1, existing.[MonthStart]));
    ';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902120000_SplitLabourWeekSignOffAtMonthEnd'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LabourWeekSignOffs_WorkerId_WeekStart_MonthStart] ON [LabourWeekSignOffs] ([WorkerId], [WeekStart], [MonthStart]) WHERE [WorkerId] IS NOT NULL AND [WeekStart] IS NOT NULL AND [MonthStart] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902120000_SplitLabourWeekSignOffAtMonthEnd'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902120000_SplitLabourWeekSignOffAtMonthEnd', N'8.0.10');
END;
GO

COMMIT;
GO

