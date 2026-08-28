-- One-off backfill: give every existing project the standard drawing-folder set.
-- (New projects get these automatically at creation — api/Features/Drawings/StandardDrawingFolders.cs;
-- keep that file's name list and this one in step.)
--
-- Idempotent: a project only receives the folders it is missing, matched case-insensitively
-- against its TOP-LEVEL folders, so re-running adds nothing and existing folders (and their
-- drawings and sub-folders) are never touched. Data only — no schema. Run with:
--
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--     -i scripts/seed-standard-drawing-folders.sql -b
--
SET NOCOUNT ON;

DECLARE @names TABLE (Name nvarchar(128) NOT NULL);
INSERT INTO @names (Name) VALUES
    (N'Architect'),
    (N'As Built'),
    (N'Drainage'),
    (N'Finishes'),
    (N'Reports'),
    (N'Specification'),
    (N'Structural'),
    (N'Sub-Contractor');

-- Ids match DrawingIdentifierFactory's compact-guid format (32 lowercase hex chars).
INSERT INTO DrawingFolders (DrawingFolderId, ProjectId, Name, CreatedAt, ParentDrawingFolderId)
SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), N'-', N'')),
       p.ProjectId,
       n.Name,
       TODATETIMEOFFSET(SYSUTCDATETIME(), 0),
       NULL
FROM Projects p
CROSS JOIN @names n
WHERE NOT EXISTS (
    SELECT 1
    FROM DrawingFolders f
    WHERE f.ProjectId = p.ProjectId
      AND f.ParentDrawingFolderId IS NULL
      AND LOWER(f.Name) = LOWER(n.Name)
);

DECLARE @added int = @@ROWCOUNT;
PRINT CONCAT('Standard drawing folders added: ', @added);
