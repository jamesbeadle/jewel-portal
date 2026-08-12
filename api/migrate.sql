BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812170000_AddValuationReportSnapshotNumber'
)
BEGIN
    ALTER TABLE [ValuationReportSnapshots] ADD [Number] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812170000_AddValuationReportSnapshotNumber'
)
BEGIN

    EXEC sp_executesql N'
    WITH numbered AS (
        SELECT ValuationReportSnapshotId,
               ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY TakenAt, ValuationReportSnapshotId) AS rn
        FROM ValuationReportSnapshots
    )
    UPDATE s
    SET s.Number = n.rn
    FROM ValuationReportSnapshots s
    INNER JOIN numbered n ON n.ValuationReportSnapshotId = s.ValuationReportSnapshotId
    WHERE s.Number = 0;';

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812170000_AddValuationReportSnapshotNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812170000_AddValuationReportSnapshotNumber', N'8.0.10');
END;
GO

COMMIT;
GO

