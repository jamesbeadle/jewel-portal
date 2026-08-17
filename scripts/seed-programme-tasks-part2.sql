/* ============================================================================
   Seed (part 2): By France + Ravenswood Ave programme tasks
   ----------------------------------------------------------------------------
   By France was skipped by the main seed because it already held 1 programme
   task (the test programme). This script DELETES all existing By France
   programme tasks and loads the 24 extracted from the REV8 export.
   Ravenswood loads once @RavenswoodWk1 is set to the Monday of its Week 1.
   ========================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ===== By France — replace test programme with 24 tasks from REV8 export ===== */
BEGIN TRAN;
DECLARE @P_byfrance NVARCHAR(64) = '3490f944b29545c4b8d5a04130f42ab8';
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @P_byfrance AND Name = N'By France')
    PRINT 'SKIP  By France — ProjectId does not match the By France project.';
ELSE
BEGIN
    DELETE FROM dbo.ProgrammeTasks WHERE ProjectId = @P_byfrance;
    PRINT 'INFO  By France — removed ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' existing programme task(s), including the test programme.';
    INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
    VALUES
    ('8fd723c86df34797b8ce0f792e66b030', @P_byfrance, N'Second Floor — Insulation/Dry Lining/Plaster', '2026-02-02T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
    ('99b1ef1d3de3474882421063ff513b4a', @P_byfrance, N'First Floor — Insulation/Dry Lining/Plaster', '2026-02-16T00:00:00+00:00', '2026-03-27T00:00:00+00:00', 100, NULL),
    ('10a84e37e02541669d8f68467427c21a', @P_byfrance, N'Second Floor — Window Installation TBC', '2026-02-23T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
    ('7c5ee89dcf4e4dc4afe981d920f20fcf', @P_byfrance, N'First Floor — Window Installation TBC', '2026-02-23T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
    ('46d2b64c9a2746819c22d301892dca89', @P_byfrance, N'Second Floor — FF/SF Staircase', '2026-02-23T00:00:00+00:00', '2026-04-10T00:00:00+00:00', 100, NULL),
    ('791ec6b033e74d8b81558f3d36eca11c', @P_byfrance, N'First Floor — FF/SF Staircase', '2026-03-02T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
    ('1d5a20b30e0b4c28b39268f774107c27', @P_byfrance, N'Second Floor — Tiling Installation', '2026-03-09T00:00:00+00:00', '2026-03-20T00:00:00+00:00', 100, NULL),
    ('e1459d2769164c3d9aa57466318038c9', @P_byfrance, N'Ground Floor — Insulation, UFH & Screed', '2026-03-09T00:00:00+00:00', '2026-04-03T00:00:00+00:00', 100, NULL),
    ('4254ce9d72b34fd78364caedbfd2ea5b', @P_byfrance, N'Ground Floor — Entrance Door & Window Installation TBC', '2026-03-16T00:00:00+00:00', '2026-03-27T00:00:00+00:00', 100, NULL),
    ('802943384f204a09a42c1fb5519570ca', @P_byfrance, N'Second Floor — Plumbing 2nd Fix', '2026-03-16T00:00:00+00:00', '2026-04-10T00:00:00+00:00', 100, NULL),
    ('7c4046d853e245bfa7f6df800e510075', @P_byfrance, N'First Floor — Tiling Installation', '2026-03-16T00:00:00+00:00', '2026-04-10T00:00:00+00:00', 100, NULL),
    ('56c8d65dad734717bdccdc2de7c96028', @P_byfrance, N'External Works — Tree & Fence Removal Boundaries TBC', '2026-03-16T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
    ('884beb137b80428dabe2ad2f881db6d0', @P_byfrance, N'External Works — Fencing to Boundary TBC', '2026-03-23T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
    ('b8f1404e99df4a3ba6caca7ef6d36845', @P_byfrance, N'Ground Floor — Insulation/Dry Lining/Plaster', '2026-03-23T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('b7ec7fa6599949028dfeee54e4aa875d', @P_byfrance, N'External Works — Excavation & masonry - Entrance TBC', '2026-03-30T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
    ('5c86972a33b94d74ad6c63f61d5ff2a8', @P_byfrance, N'Second Floor — Electrics 2nd Fix', '2026-03-30T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('17d8683833a14d4fb3ff3fa5a5fb2511', @P_byfrance, N'Second Floor — Carpentry 2nd Fix', '2026-03-30T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('83f43d90dc104aa081b2faf3810286e9', @P_byfrance, N'External Works — Render & Cladding', '2026-03-30T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('78521dc04a5b4fde8baef90e886ac845', @P_byfrance, N'First Floor — Plumbing 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('1c0d1e40077541f68900eb2fbb162800', @P_byfrance, N'First Floor — Electrics 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('fc84a85f896d4f46906b0cfa5f8daa95', @P_byfrance, N'First Floor — Carpentry 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('42ca9a2311054df4b27f87d9c5eef125', @P_byfrance, N'First Floor — Air Conditioning 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('01dfec7db7d04e94a9dc19bc689ac720', @P_byfrance, N'Second Floor — Decoration', '2026-04-20T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('35a3e2ff624540e78ed449da7dee50d9', @P_byfrance, N'External Works — Entrance Gate Survey - Install TBC', '2026-04-20T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL);
    PRINT 'OK    By France — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
END
COMMIT;
GO

/* ===== Ravenswood Ave — 17 tasks from REV1 draft ===== */
SET XACT_ABORT ON;
BEGIN TRAN;
/* REV1 is a draft with week numbers only. Set the Monday of Week 1: */
DECLARE @RavenswoodWk1 DATE = NULL;  -- e.g. '2026-09-07'
DECLARE @P_rav NVARCHAR(64) = '3bf6dcfa81764a248138fb5fd357aa84';
IF @RavenswoodWk1 IS NULL
    PRINT 'SKIP  Ravenswood Ave — @RavenswoodWk1 not set.';
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @P_rav AND Name = N'Ravenswood Ave')
    PRINT 'SKIP  Ravenswood Ave — ProjectId does not match the Ravenswood Ave project.';
ELSE IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_rav)
    PRINT 'SKIP  Ravenswood Ave — project already has programme tasks; nothing touched.';
ELSE
BEGIN
    INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
    VALUES
    ('ebbd70fa0f7145d3b7175bf420881632', @P_rav, N'Site set up & Demolition', DATEADD(DAY, 0, @RavenswoodWk1), DATEADD(DAY, 7, @RavenswoodWk1), 0, NULL),
    ('71ad7d9689ab49169b53567c06792189', @P_rav, N'Ground Works & Below Ground Drainage', DATEADD(DAY, 7, @RavenswoodWk1), DATEADD(DAY, 22, @RavenswoodWk1), 0, NULL),
    ('4e092a29dd734ba3a93fadf2f80eb808', @P_rav, N'Masonry Walls', DATEADD(DAY, 21, @RavenswoodWk1), DATEADD(DAY, 32, @RavenswoodWk1), 0, NULL),
    ('4ecb36a2ee6d4087a62c158851845509', @P_rav, N'Steelwork', DATEADD(DAY, 30, @RavenswoodWk1), DATEADD(DAY, 32, @RavenswoodWk1), 0, NULL),
    ('ad0ff7ef8fd54d478605cb3497fb9f0a', @P_rav, N'Timber Carcassing', DATEADD(DAY, 35, @RavenswoodWk1), DATEADD(DAY, 50, @RavenswoodWk1), 0, NULL),
    ('5250836e09974f789112c204ca2c05d1', @P_rav, N'Roofing inc. Leadwork', DATEADD(DAY, 49, @RavenswoodWk1), DATEADD(DAY, 60, @RavenswoodWk1), 0, NULL),
    ('3847ab9cfedf4de4bc42d99ec8e8dbe9', @P_rav, N'Glazing', DATEADD(DAY, 50, @RavenswoodWk1), DATEADD(DAY, 57, @RavenswoodWk1), 0, NULL),
    ('18a3a18eb5854d15bb1409c8dd2f862e', @P_rav, N'Power & Lighting 1st Fix', DATEADD(DAY, 63, @RavenswoodWk1), DATEADD(DAY, 77, @RavenswoodWk1), 0, NULL),
    ('193edd19b3ea4540b79a3dcefcbb9a25', @P_rav, N'Plumbing & Heating 1st Fix', DATEADD(DAY, 64, @RavenswoodWk1), DATEADD(DAY, 78, @RavenswoodWk1), 0, NULL),
    ('0e09353b78fc44a4b97597e138cc9361', @P_rav, N'Plaster & Drylinning', DATEADD(DAY, 73, @RavenswoodWk1), DATEADD(DAY, 94, @RavenswoodWk1), 0, NULL),
    ('3bdb66365df74da3b1255593e78ff157', @P_rav, N'Joinery', DATEADD(DAY, 84, @RavenswoodWk1), DATEADD(DAY, 99, @RavenswoodWk1), 0, NULL),
    ('c7215193e6394b41a7c492bc2ed9182a', @P_rav, N'Power & Lighting 2nd Fix', DATEADD(DAY, 91, @RavenswoodWk1), DATEADD(DAY, 101, @RavenswoodWk1), 0, NULL),
    ('2acd066e2b834875b5c46b4e57878cfd', @P_rav, N'Plumbing & Heating 2nd Fix', DATEADD(DAY, 91, @RavenswoodWk1), DATEADD(DAY, 102, @RavenswoodWk1), 0, NULL),
    ('6cf76eb749f7493693fb32ddde0d18f4', @P_rav, N'Decorations', DATEADD(DAY, 98, @RavenswoodWk1), DATEADD(DAY, 116, @RavenswoodWk1), 0, NULL),
    ('a51ecdff91514a108772d96a98c641b6', @P_rav, N'Floor Finishes', DATEADD(DAY, 105, @RavenswoodWk1), DATEADD(DAY, 116, @RavenswoodWk1), 0, NULL),
    ('399576d43de3483db2d1670369a53ae8', @P_rav, N'External Works & Final Drainage', DATEADD(DAY, 105, @RavenswoodWk1), DATEADD(DAY, 122, @RavenswoodWk1), 0, NULL),
    ('9bbbb160547349348e73dff40dcd59d4', @P_rav, N'Snagging & clear site', DATEADD(DAY, 119, @RavenswoodWk1), DATEADD(DAY, 122, @RavenswoodWk1), 0, NULL);
    PRINT 'OK    Ravenswood Ave — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
END
COMMIT;
GO
