-- 2026-09-03 — the project register "Drawings" became "Documents" (it holds party-wall awards,
-- building-control letters and reports as well as drawings). No schema change: tables, columns
-- and API paths still say Drawing. This script brings the DATA the connector reads by name into
-- line with the renamed tools and actions. Safe to re-run; every statement is idempotent.
--
-- The API also resolves the OLD names (AiLegacyNames) so nothing breaks before this runs — it just
-- keeps the admin page and the saved skills honest about the current names.

SET NOCOUNT ON;

-- 1. Doctrine attached on Admin → AI Actions: action names and the area key.
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'list_documents'                    WHERE [TargetKey] = N'list_drawings';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'register_document'                 WHERE [TargetKey] = N'register_drawing';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'update_document_metadata'          WHERE [TargetKey] = N'update_drawing_metadata';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'approve_document_revision'         WHERE [TargetKey] = N'approve_drawing_revision';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'set_document_revision_label'       WHERE [TargetKey] = N'set_drawing_revision_label';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'delete_document_revision'          WHERE [TargetKey] = N'delete_drawing_revision';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'delete_document'                   WHERE [TargetKey] = N'delete_drawing';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'create_document_folder'            WHERE [TargetKey] = N'create_drawing_folder';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'rename_document_folder'            WHERE [TargetKey] = N'rename_drawing_folder';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'delete_document_folder'            WHERE [TargetKey] = N'delete_drawing_folder';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'move_document_to_folder'           WHERE [TargetKey] = N'move_drawing_to_folder';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'file_document_to_project_documents' WHERE [TargetKey] = N'file_document_as_drawing';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'set_bid_package_documents'         WHERE [TargetKey] = N'set_bid_package_drawings';
UPDATE [dbo].[AiActionSkills] SET [TargetKey] = N'Documents'                         WHERE [TargetKind] = N'area' AND [TargetKey] = N'Drawings';

-- A row that already existed under the new key would now be a duplicate — keep one.
;WITH dupes AS (
    SELECT [ActionSkillId],
           ROW_NUMBER() OVER (PARTITION BY [TargetKind], [TargetKey], [SkillKey] ORDER BY [AttachedAt]) AS rn
    FROM [dbo].[AiActionSkills]
)
DELETE FROM dupes WHERE rn > 1;

-- 2. Saved skill bodies that name the old tools/actions (the seeded jpms-document-filing among
--    them). Plain token swaps; the prose around them is left to the team to reword in the portal.
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'file_document_as_drawing', N'file_document_to_project_documents') WHERE [Body] LIKE N'%file_document_as_drawing%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'set_bid_package_drawings', N'set_bid_package_documents')          WHERE [Body] LIKE N'%set_bid_package_drawings%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'list_drawings', N'list_documents')                                WHERE [Body] LIKE N'%list_drawings%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'register_drawing', N'register_document')                          WHERE [Body] LIKE N'%register_drawing%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'update_drawing_metadata', N'update_document_metadata')            WHERE [Body] LIKE N'%update_drawing_metadata%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'approve_drawing_revision', N'approve_document_revision')          WHERE [Body] LIKE N'%approve_drawing_revision%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'set_drawing_revision_label', N'set_document_revision_label')      WHERE [Body] LIKE N'%set_drawing_revision_label%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'delete_drawing_revision', N'delete_document_revision')            WHERE [Body] LIKE N'%delete_drawing_revision%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'delete_drawing_folder', N'delete_document_folder')                WHERE [Body] LIKE N'%delete_drawing_folder%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'create_drawing_folder', N'create_document_folder')                WHERE [Body] LIKE N'%create_drawing_folder%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'rename_drawing_folder', N'rename_document_folder')                WHERE [Body] LIKE N'%rename_drawing_folder%';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'move_drawing_to_folder', N'move_document_to_folder')              WHERE [Body] LIKE N'%move_drawing_to_folder%';
-- delete_drawing LAST and word-bounded by the space/newline that follows it in prose, so the
-- longer names above are never clipped.
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'delete_drawing ', N'delete_document ')                            WHERE [Body] LIKE N'%delete_drawing %';
UPDATE [dbo].[Skills] SET [Body] = REPLACE([Body], N'delete_drawing)', N'delete_document)')                            WHERE [Body] LIKE N'%delete_drawing)%';

SELECT [TargetKind], [TargetKey], [SkillKey] FROM [dbo].[AiActionSkills] WHERE [TargetKey] LIKE N'%document%' ORDER BY 1, 2, 3;
