/* ============================================================================
   Seed: Programme tasks for the six live/active projects
   ----------------------------------------------------------------------------
   Source: Programme of Works exports (PDF), bar positions extracted per week
   column and anchored to each programme's own date header:

     17a Abbot Road        Wk1 = Mon 05 Jan 2026  (REV6, Phase 2, EOT-01)
     64 Ravenswood Avenue  Wk1 = @RavenswoodWk1   (REV1 draft carries no dates)
     Woodhouse Lane        Wk1 = Mon 13 Oct 2025  (REV5; export starts at Wk25)
     2 Albany Mews         Wk1 = Mon 09 Jun 2025  (REV3, EOT-02)
     149a Coombe Lane West Wk1 = Mon 18 Sep 2023  (REV4)
     By France             Wk1 = Mon 06 Jan 2025  (REV8 draft; export starts
                                                   at Wk57 = 02 Feb 2026)

   Where a programme shows a task twice because an EOT moved it (original bar
   left of the revision, shifted bar right of it), only the shifted/current bar
   is seeded. Genuine return visits are seeded as separate tasks marked "(2)".
   ProgressPercent is derived from elapsed working time as at 17 Aug 2026.

   Safe to re-run: each project is skipped if it already has any programme
   tasks, so nothing entered through the app is ever touched.
   ========================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;



/* ===== 17a Abbot Road — 27 tasks — REV6 Phase 2 EOT-01 (current/EOT bar positions; superseded pre-EOT bars omitted) ===== */
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @P_abbot NVARCHAR(64);
IF (SELECT COUNT(*) FROM dbo.Projects WHERE Name LIKE N'%Abbot Road%') <> 1
    PRINT 'SKIP  17a Abbot Road — expected exactly one project matching %Abbot Road%.';
ELSE
BEGIN
    SELECT @P_abbot = ProjectId FROM dbo.Projects WHERE Name LIKE N'%Abbot Road%';
    IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_abbot)
        PRINT 'SKIP  17a Abbot Road — project already has programme tasks; nothing touched.';
    ELSE
    BEGIN
        INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
        VALUES
        ('eb916ec4e2b24d3fb983fa71b5ea1832', @P_abbot, N'Ground beam & pad foundation', '2026-01-05T00:00:00+00:00', '2026-01-16T00:00:00+00:00', 100, NULL),
        ('01c22f82e4b0418480e8d483fc1397e6', @P_abbot, N'Scaffolding', '2026-01-05T00:00:00+00:00', '2026-01-16T00:00:00+00:00', 100, NULL),
        ('5da2af05518b482583c36f7fabc51951', @P_abbot, N'Steel Survey & Fabrication', '2026-01-12T00:00:00+00:00', '2026-01-21T00:00:00+00:00', 100, NULL),
        ('c9431aa4fed14f17a20c269ff8b9f300', @P_abbot, N'Steel delivery & Installation', '2026-01-22T00:00:00+00:00', '2026-01-30T00:00:00+00:00', 100, NULL),
        ('42570e0a1fc245cfa25ec1adf7309c23', @P_abbot, N'IQ Glass - Site Survey & Drawings', '2026-01-26T00:00:00+00:00', '2026-02-13T00:00:00+00:00', 100, NULL),
        ('2a47b5c0e95749c9962e515a14eefae6', @P_abbot, N'IQ Glass - Installation', '2026-02-09T00:00:00+00:00', '2026-08-21T00:00:00+00:00', 96, NULL),
        ('040b2da368e440f0b92e81850da5401d', @P_abbot, N'Scaffolding (2)', '2026-04-27T00:00:00+00:00', '2026-08-28T00:00:00+00:00', 89, NULL),
        ('8aa5353721b64f8ba9a2bf3988b661d0', @P_abbot, N'Timber - Joists/Rafters/Studwork', '2026-05-18T00:00:00+00:00', '2026-06-19T00:00:00+00:00', 100, NULL),
        ('9a559a7c1e264a39a0e33d619d7ed206', @P_abbot, N'Demolition & Opening Kitchen', '2026-06-08T00:00:00+00:00', '2026-06-19T00:00:00+00:00', 100, NULL),
        ('000b89fe8186403d9bf6cf9750ba93a4', @P_abbot, N'Roofing & Rainwater', '2026-07-06T00:00:00+00:00', '2026-07-31T00:00:00+00:00', 100, NULL),
        ('e71a33d630f14f97a29f3cf930031938', @P_abbot, N'Velfac Windows - Survey to install', '2026-07-06T00:00:00+00:00', '2026-08-21T00:00:00+00:00', 86, NULL),
        ('5b99c5fe58a143e483eab4a97b09adbf', @P_abbot, N'Partions & Studs', '2026-07-13T00:00:00+00:00', '2026-07-24T00:00:00+00:00', 100, NULL),
        ('3e842bc971294fd4a9721b3b0a2fe606', @P_abbot, N'Electrical 1st Fix', '2026-07-20T00:00:00+00:00', '2026-07-31T00:00:00+00:00', 100, NULL),
        ('0cdd2a536fec4872883d8db34c74c3e8', @P_abbot, N'Plumbing 1st Fix (include UFH)', '2026-07-27T00:00:00+00:00', '2026-08-07T00:00:00+00:00', 100, NULL),
        ('59c3e99c56b245cbb693dfaf8c76936d', @P_abbot, N'Subfloor construction following first fix', '2026-08-03T00:00:00+00:00', '2026-08-14T00:00:00+00:00', 100, NULL),
        ('6c5f29abbd304e49841becb2c71c4915', @P_abbot, N'Insulation & Gyprock', '2026-08-03T00:00:00+00:00', '2026-08-28T00:00:00+00:00', 50, NULL),
        ('b2e2a6b4a13948018409851a87e9e392', @P_abbot, N'External Cladding & Render', '2026-08-10T00:00:00+00:00', '2026-09-04T00:00:00+00:00', 25, NULL),
        ('5baebca8fdbb4442a6feb3966dc97efc', @P_abbot, N'Staircase - Survey to installation', '2026-08-10T00:00:00+00:00', '2026-09-18T00:00:00+00:00', 17, NULL),
        ('5683b56117c742a99672d490c19b774f', @P_abbot, N'Plasting internal', '2026-08-17T00:00:00+00:00', '2026-09-11T00:00:00+00:00', 0, NULL),
        ('bf29f9bc6c554e81b90a722c01ec7fe6', @P_abbot, N'Drainage', '2026-08-31T00:00:00+00:00', '2026-09-25T00:00:00+00:00', 0, NULL),
        ('3fa3cddfc6ee42909a6fddf652a0e3d0', @P_abbot, N'Carpentry - 2nd fix', '2026-09-07T00:00:00+00:00', '2026-09-25T00:00:00+00:00', 0, NULL),
        ('d0b48ea063154b699251f0c9c67deae5', @P_abbot, N'Electrical 2nd Fix', '2026-09-14T00:00:00+00:00', '2026-09-25T00:00:00+00:00', 0, NULL),
        ('f339bd548c7d4dd1ad15596b5a199efb', @P_abbot, N'Plumbing 2nd Fix', '2026-09-14T00:00:00+00:00', '2026-09-25T00:00:00+00:00', 0, NULL),
        ('4deb7aeaca3d4b83913830248636e954', @P_abbot, N'External Works', '2026-09-14T00:00:00+00:00', '2026-10-02T00:00:00+00:00', 0, NULL),
        ('c65a57439fa44ea3a321d0c5001111f5', @P_abbot, N'Decorations', '2026-09-21T00:00:00+00:00', '2026-10-09T00:00:00+00:00', 0, NULL),
        ('864bac12201b4b70bc855066e7025070', @P_abbot, N'Snagging & Testing', '2026-10-05T00:00:00+00:00', '2026-10-09T00:00:00+00:00', 0, NULL),
        ('fdb3c83d8ad741f294c37695bc12ee63', @P_abbot, N'Builders Clean', '2026-10-09T00:00:00+00:00', '2026-10-09T00:00:00+00:00', 0, NULL);
        PRINT 'OK    17a Abbot Road — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
    END
END
COMMIT;
GO

/* ===== 64 Ravenswood Avenue — 17 tasks — REV1 draft, 18 weeks (no dates on the programme; set @RavenswoodWk1 below) ===== */
/* Ravenswood REV1 is a draft with week numbers only - no calendar dates.
   Set the Monday of its Week 1 here; leave NULL to skip Ravenswood. */
DECLARE @RavenswoodWk1 DATE = NULL;  -- e.g. '2026-09-07'
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @P_ravenswood NVARCHAR(64);
IF (SELECT COUNT(*) FROM dbo.Projects WHERE Name LIKE N'%Ravenswood%') <> 1
    PRINT 'SKIP  64 Ravenswood Avenue — expected exactly one project matching %Ravenswood%.';
ELSE IF @RavenswoodWk1 IS NULL
    PRINT 'SKIP  64 Ravenswood Avenue — @RavenswoodWk1 not set (draft programme has no dates).';
ELSE
BEGIN
    SELECT @P_ravenswood = ProjectId FROM dbo.Projects WHERE Name LIKE N'%Ravenswood%';
    IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_ravenswood)
        PRINT 'SKIP  64 Ravenswood Avenue — project already has programme tasks; nothing touched.';
    ELSE
    BEGIN
        INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
        VALUES
        ('5a6e169f8a83416781d666d67d1feab1', @P_ravenswood, N'Site set up & Demolition', DATEADD(DAY, 0, @RavenswoodWk1), DATEADD(DAY, 7, @RavenswoodWk1), 0, NULL),
        ('a043a306cce948e9851168616f01f40d', @P_ravenswood, N'Ground Works & Below Ground Drainage', DATEADD(DAY, 7, @RavenswoodWk1), DATEADD(DAY, 22, @RavenswoodWk1), 0, NULL),
        ('28d5f9a739b941e69062b84755ade937', @P_ravenswood, N'Masonry Walls', DATEADD(DAY, 21, @RavenswoodWk1), DATEADD(DAY, 32, @RavenswoodWk1), 0, NULL),
        ('adb8ae2234e544f086e2eb4b41e1fd1e', @P_ravenswood, N'Steelwork', DATEADD(DAY, 30, @RavenswoodWk1), DATEADD(DAY, 32, @RavenswoodWk1), 0, NULL),
        ('3bccc5e4bbd944788bf56bb44b1207df', @P_ravenswood, N'Timber Carcassing', DATEADD(DAY, 35, @RavenswoodWk1), DATEADD(DAY, 50, @RavenswoodWk1), 0, NULL),
        ('f5e23021faef4162ac0c2ccfa50e41a8', @P_ravenswood, N'Roofing inc. Leadwork', DATEADD(DAY, 49, @RavenswoodWk1), DATEADD(DAY, 60, @RavenswoodWk1), 0, NULL),
        ('dfea8f20bbe742e6b6ded0217dee1523', @P_ravenswood, N'Glazing', DATEADD(DAY, 50, @RavenswoodWk1), DATEADD(DAY, 57, @RavenswoodWk1), 0, NULL),
        ('fedc7c88a30a4197a66a9059493a67e8', @P_ravenswood, N'Power & Lighting 1st Fix', DATEADD(DAY, 63, @RavenswoodWk1), DATEADD(DAY, 77, @RavenswoodWk1), 0, NULL),
        ('6a57f027c3544603b9d4fdd5857fa3cf', @P_ravenswood, N'Plumbing & Heating 1st Fix', DATEADD(DAY, 64, @RavenswoodWk1), DATEADD(DAY, 78, @RavenswoodWk1), 0, NULL),
        ('0ab97b1bdf014a4f86eb42fb790df37c', @P_ravenswood, N'Plaster & Drylinning', DATEADD(DAY, 73, @RavenswoodWk1), DATEADD(DAY, 94, @RavenswoodWk1), 0, NULL),
        ('a0d4f339132044d8853cdbe1379d1ab2', @P_ravenswood, N'Joinery', DATEADD(DAY, 84, @RavenswoodWk1), DATEADD(DAY, 99, @RavenswoodWk1), 0, NULL),
        ('985b976bf3aa438295c712126bbfc362', @P_ravenswood, N'Power & Lighting 2nd Fix', DATEADD(DAY, 91, @RavenswoodWk1), DATEADD(DAY, 101, @RavenswoodWk1), 0, NULL),
        ('6a65f6c5683445fabb92091ed3052f57', @P_ravenswood, N'Plumbing & Heating 2nd Fix', DATEADD(DAY, 91, @RavenswoodWk1), DATEADD(DAY, 102, @RavenswoodWk1), 0, NULL),
        ('120e31c19257452f8dc5b7eb196aa1c6', @P_ravenswood, N'Decorations', DATEADD(DAY, 98, @RavenswoodWk1), DATEADD(DAY, 116, @RavenswoodWk1), 0, NULL),
        ('62fd35382791422aad7f5f1103fd9600', @P_ravenswood, N'Floor Finishes', DATEADD(DAY, 105, @RavenswoodWk1), DATEADD(DAY, 116, @RavenswoodWk1), 0, NULL),
        ('d8acdb00acab4816a67ef87f743eceea', @P_ravenswood, N'External Works & Final Drainage', DATEADD(DAY, 105, @RavenswoodWk1), DATEADD(DAY, 122, @RavenswoodWk1), 0, NULL),
        ('142f2f4cd44b493d8c00d8ee04a79f7e', @P_ravenswood, N'Snagging & clear site', DATEADD(DAY, 119, @RavenswoodWk1), DATEADD(DAY, 122, @RavenswoodWk1), 0, NULL);
        PRINT 'OK    64 Ravenswood Avenue — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
    END
END
COMMIT;
GO

/* ===== Woodhouse Lane — 37 tasks — REV5 (export shows Wk25 onward only; tasks completed before 30 Mar 2026 are not included) ===== */
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @P_woodhouse NVARCHAR(64);
IF (SELECT COUNT(*) FROM dbo.Projects WHERE Name LIKE N'%Woodhouse%') <> 1
    PRINT 'SKIP  Woodhouse Lane — expected exactly one project matching %Woodhouse%.';
ELSE
BEGIN
    SELECT @P_woodhouse = ProjectId FROM dbo.Projects WHERE Name LIKE N'%Woodhouse%';
    IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_woodhouse)
        PRINT 'SKIP  Woodhouse Lane — project already has programme tasks; nothing touched.';
    ELSE
    BEGIN
        INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
        VALUES
        ('09e3522308b040848a02b3905da4f3f5', @P_woodhouse, N'Timber - Joists / Rafters / Studwork', '2026-04-10T00:00:00+00:00', '2026-04-30T00:00:00+00:00', 100, NULL),
        ('0217ab4f7535495ba18329b398dea8a1', @P_woodhouse, N'Scaffolding', '2026-04-10T00:00:00+00:00', '2026-06-04T00:00:00+00:00', 100, NULL),
        ('9a7159241df2420aa9c922587c2888dc', @P_woodhouse, N'Fluid Glass Shop Dwgs Approved - 17 Apr', '2026-04-16T00:00:00+00:00', '2026-04-16T00:00:00+00:00', 100, NULL),
        ('82f3b2ab01834fdaacbc5b9d750b47f0', @P_woodhouse, N'Partitions & Studs GF & FF', '2026-04-17T00:00:00+00:00', '2026-04-30T00:00:00+00:00', 100, NULL),
        ('19cc8f84cc0b4f7bbfa3d721810c2816', @P_woodhouse, N'MF Ceilings GF - Tom Howley Survey', '2026-04-27T00:00:00+00:00', '2026-05-06T00:00:00+00:00', 100, NULL),
        ('b0f9af504fac4fd3981ffdf1699cc3ad', @P_woodhouse, N'Insulation, UFH, Screed & Levelling', '2026-05-01T00:00:00+00:00', '2026-05-14T00:00:00+00:00', 100, NULL),
        ('9fa2ecebae2d43458599faa5fec2ad0b', @P_woodhouse, N'Roofing & Rainwater', '2026-05-01T00:00:00+00:00', '2026-05-14T00:00:00+00:00', 100, NULL),
        ('8bb5692383ba4362b7ecfb3b2bd682e9', @P_woodhouse, N'Insulation & Wall Lining', '2026-05-01T00:00:00+00:00', '2026-05-21T00:00:00+00:00', 100, NULL),
        ('4c00ff2a2d5741d89d2f671ac2b43637', @P_woodhouse, N'Service Trenching', '2026-05-01T00:00:00+00:00', '2026-05-21T00:00:00+00:00', 100, NULL),
        ('b0626d91c406417f9092ad2d30ea5fb7', @P_woodhouse, N'High Level Rainwater', '2026-05-08T00:00:00+00:00', '2026-05-21T00:00:00+00:00', 100, NULL),
        ('ccef88904007437c8a2fe0a73903c990', @P_woodhouse, N'Tom Howley Kitchen Survey', '2026-05-15T00:00:00+00:00', '2026-05-15T00:00:00+00:00', 100, NULL),
        ('c7a2632b5a3d44a1a71c17259da54ae6', @P_woodhouse, N'Electrical 1st Fix - Existing Str.', '2026-05-15T00:00:00+00:00', '2026-05-28T00:00:00+00:00', 100, NULL),
        ('98817f6cfb2c427d860f4a0e0c2a96b3', @P_woodhouse, N'Plumbing 1st Fix - Existing Str.', '2026-05-15T00:00:00+00:00', '2026-05-28T00:00:00+00:00', 100, NULL),
        ('806970501831445e9c2bfe780b1e0ecf', @P_woodhouse, N'Tile Setout Confirmed - Int/Ext', '2026-05-22T00:00:00+00:00', '2026-05-28T00:00:00+00:00', 100, NULL),
        ('edaf046590a64bbdb7d3043edfc3cd8b', @P_woodhouse, N'Electrical 1st Fix - New Structure', '2026-05-22T00:00:00+00:00', '2026-06-03T00:00:00+00:00', 100, NULL),
        ('1be032e4627740edbde368902f06566b', @P_woodhouse, N'Plumbing 1st Fix - New Structure', '2026-05-22T00:00:00+00:00', '2026-06-03T00:00:00+00:00', 100, NULL),
        ('c3919eebe0344a1eb1c243dfaaf5ff18', @P_woodhouse, N'Rendering & Cladding', '2026-05-22T00:00:00+00:00', '2026-06-04T00:00:00+00:00', 100, NULL),
        ('ef4d1a5b64b64ca2b91e688aa57aa53e', @P_woodhouse, N'Roofing & Rainwater (2)', '2026-05-29T00:00:00+00:00', '2026-06-04T00:00:00+00:00', 100, NULL),
        ('93f12defdfd0469bb2480638516ca079', @P_woodhouse, N'Plaster & Drylining', '2026-06-04T00:00:00+00:00', '2026-07-02T00:00:00+00:00', 100, NULL),
        ('02058013d6c4488ea2a55452ce27b145', @P_woodhouse, N'High Level Rainwater (2)', '2026-06-05T00:00:00+00:00', '2026-06-11T00:00:00+00:00', 100, NULL),
        ('4de42216df0e4264a96406f6de70ac16', @P_woodhouse, N'Services Reconnection & LPG Tank', '2026-06-05T00:00:00+00:00', '2026-06-11T00:00:00+00:00', 100, NULL),
        ('cd7bd214e4e0407ba88e12f9d7f66093', @P_woodhouse, N'CP Hart 1st Fix - Delivery', '2026-06-05T00:00:00+00:00', '2026-06-11T00:00:00+00:00', 100, NULL),
        ('a00e38f9f3e649d1a6fdf234016540f4', @P_woodhouse, N'Fluid Glass Installation Commences', '2026-06-19T00:00:00+00:00', '2026-06-25T00:00:00+00:00', 100, NULL),
        ('e1e700b82bbc488f82cd77f27f150862', @P_woodhouse, N'Timber - Glass Balustrade', '2026-06-19T00:00:00+00:00', '2026-07-23T00:00:00+00:00', 100, NULL),
        ('5a474a116de14c6fa34a7128eab7e88a', @P_woodhouse, N'Tiling - Internal Areas', '2026-07-03T00:00:00+00:00', '2026-07-23T00:00:00+00:00', 100, NULL),
        ('b716acf8b09c48328a54b8ed8c983e66', @P_woodhouse, N'CP Hart 2nd Fix Sanitary Ware', '2026-07-10T00:00:00+00:00', '2026-07-16T00:00:00+00:00', 100, NULL),
        ('be0e76c50ce645e2ac4a54360727b0ef', @P_woodhouse, N'Decorations', '2026-07-17T00:00:00+00:00', '2026-07-24T00:00:00+00:00', 100, NULL),
        ('98d57f7ea3394300a174b37558f77bff', @P_woodhouse, N'Carpentry - 2nd Fix', '2026-07-17T00:00:00+00:00', '2026-08-13T00:00:00+00:00', 100, NULL),
        ('789fe4e1a6264d0889325ec34f5cfef7', @P_woodhouse, N'Tiling & Paving', '2026-07-24T00:00:00+00:00', '2026-08-13T00:00:00+00:00', 100, NULL),
        ('647ba4c6bd374a549f1edbf18fadea14', @P_woodhouse, N'Electrical 2nd Fix', '2026-07-29T00:00:00+00:00', '2026-08-20T00:00:00+00:00', 76, NULL),
        ('91632b077793457690f879a5f8dc5cb9', @P_woodhouse, N'Tom Howley Installation Commences', '2026-07-31T00:00:00+00:00', '2026-08-13T00:00:00+00:00', 100, NULL),
        ('2951a1d764154a918a27587349f62761', @P_woodhouse, N'Plumbing 2nd Fix', '2026-08-06T00:00:00+00:00', '2026-08-27T00:00:00+00:00', 44, NULL),
        ('49260e99e7a14854800c572e3ca1d704', @P_woodhouse, N'Stone Cladding & Copings', '2026-08-07T00:00:00+00:00', '2026-08-20T00:00:00+00:00', 60, NULL),
        ('5842f5d8ab744a6497516663f380c217', @P_woodhouse, N'Decorations (2)', '2026-08-11T00:00:00+00:00', '2026-09-03T00:00:00+00:00', 22, NULL),
        ('93aedb792c61411299c8893ec66a7746', @P_woodhouse, N'Floor Finishes', '2026-08-28T00:00:00+00:00', '2026-09-03T00:00:00+00:00', 0, NULL),
        ('f15b2e24c72c4c17a3bcee9ae1e3a339', @P_woodhouse, N'Snagging & Testing', '2026-08-28T00:00:00+00:00', '2026-09-10T00:00:00+00:00', 0, NULL),
        ('b5e1001940944ce688f05d97e4d14f9f', @P_woodhouse, N'Builders Clean', '2026-09-04T00:00:00+00:00', '2026-09-10T00:00:00+00:00', 0, NULL);
        PRINT 'OK    Woodhouse Lane — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
    END
END
COMMIT;
GO

/* ===== 2 Albany Mews — 26 tasks — REV3 EOT-02 ===== */
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @P_albany NVARCHAR(64);
IF (SELECT COUNT(*) FROM dbo.Projects WHERE Name LIKE N'%Albany Mews%') <> 1
    PRINT 'SKIP  2 Albany Mews — expected exactly one project matching %Albany Mews%.';
ELSE
BEGIN
    SELECT @P_albany = ProjectId FROM dbo.Projects WHERE Name LIKE N'%Albany Mews%';
    IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_albany)
        PRINT 'SKIP  2 Albany Mews — project already has programme tasks; nothing touched.';
    ELSE
    BEGIN
        INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
        VALUES
        ('53a60c115a0b4be6af055b1afd99e850', @P_albany, N'Site Setup & Demolition', '2025-06-09T00:00:00+00:00', '2025-06-20T00:00:00+00:00', 100, NULL),
        ('531ac61ca4694eafa0918f396b4bb2ad', @P_albany, N'Excavation Drainage & Concrete', '2025-06-23T00:00:00+00:00', '2025-07-11T00:00:00+00:00', 100, NULL),
        ('831bd7172d574315b7c1676ea5204af6', @P_albany, N'Masonry below DPC', '2025-07-09T00:00:00+00:00', '2025-07-18T00:00:00+00:00', 100, NULL),
        ('0a0515668be2407a81c343ffa5effc07', @P_albany, N'Insulation and block & beam', '2025-07-16T00:00:00+00:00', '2025-07-18T00:00:00+00:00', 100, NULL),
        ('f2eda276439a49b38b59a5559a47902d', @P_albany, N'Internal Excavation & Pads', '2025-07-21T00:00:00+00:00', '2025-08-08T00:00:00+00:00', 100, NULL),
        ('6816d6f073d1491d8d045e3bf177a475', @P_albany, N'Temp Propping', '2025-08-04T00:00:00+00:00', '2025-08-15T00:00:00+00:00', 100, NULL),
        ('f971dc18d08c47e1acd08fb8aa0962d6', @P_albany, N'Masonry & Insulation', '2025-08-04T00:00:00+00:00', '2025-09-05T00:00:00+00:00', 100, NULL),
        ('303c4a79a7024cda97b9651b729dbac0', @P_albany, N'Steelwork & Lintels', '2025-08-26T00:00:00+00:00', '2025-09-12T00:00:00+00:00', 100, NULL),
        ('0929085d25024016841753a3fd261e06', @P_albany, N'Timber - Joists/Rafters/Studwork', '2025-09-08T00:00:00+00:00', '2025-10-17T00:00:00+00:00', 100, NULL),
        ('695daea289d14113b8db88249b37ab25', @P_albany, N'Roofing & Rainwater', '2025-09-29T00:00:00+00:00', '2025-10-24T00:00:00+00:00', 100, NULL),
        ('2fb8b7d371604683844d0a404e1ff903', @P_albany, N'Windows & Doors', '2025-09-29T00:00:00+00:00', '2025-11-21T00:00:00+00:00', 100, NULL),
        ('3f669b70cba544de90fd050420764939', @P_albany, N'Lift Installation', '2025-10-06T00:00:00+00:00', '2026-02-06T00:00:00+00:00', 100, NULL),
        ('0df22108fbb745a3ab454b01231da89e', @P_albany, N'Partions & Studs', '2025-10-27T00:00:00+00:00', '2025-11-14T00:00:00+00:00', 100, NULL),
        ('124fc4dbf8494ca2bc13dc7f24aed1e2', @P_albany, N'Electrical 1st Fix', '2025-10-27T00:00:00+00:00', '2025-11-21T00:00:00+00:00', 100, NULL),
        ('04429236857f467c896585a11120a4b1', @P_albany, N'Plumbing 1st Fix', '2025-10-27T00:00:00+00:00', '2025-11-21T00:00:00+00:00', 100, NULL),
        ('072336591cbe4571a3611ee0d7bb466d', @P_albany, N'Plaster & Drylinning', '2025-10-27T00:00:00+00:00', '2025-12-12T00:00:00+00:00', 100, NULL),
        ('d73d2c4ac3f64578a5d22ca2afdc9f1e', @P_albany, N'Kitchen Installation', '2025-11-03T00:00:00+00:00', '2026-01-23T00:00:00+00:00', 100, NULL),
        ('2edd378bed6c4e9985f4be33592ce8c2', @P_albany, N'Tiling', '2025-11-24T00:00:00+00:00', '2026-01-16T00:00:00+00:00', 100, NULL),
        ('9cd70c20c49e4b4190f8643bebbbfe35', @P_albany, N'Carpentry - 2nd fix', '2025-12-08T00:00:00+00:00', '2026-01-30T00:00:00+00:00', 100, NULL),
        ('8725bf2aca9e44a787f55f27fb62d2c7', @P_albany, N'Electrical 2nd Fix', '2025-12-08T00:00:00+00:00', '2026-01-30T00:00:00+00:00', 100, NULL),
        ('79534f0167fa41f0a8bb98ef144ca40c', @P_albany, N'Plumbing 2nd Fix', '2025-12-08T00:00:00+00:00', '2026-01-30T00:00:00+00:00', 100, NULL),
        ('db7bff4743fd4ab7be716d5fb5333cd7', @P_albany, N'External Works', '2026-01-30T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
        ('336c79fbd52641418d105342f9874492', @P_albany, N'Decorations', '2026-02-02T00:00:00+00:00', '2026-02-27T00:00:00+00:00', 100, NULL),
        ('109e54c52abe44d591bc5ec47ac3e2cf', @P_albany, N'Floor Finishes', '2026-02-09T00:00:00+00:00', '2026-02-27T00:00:00+00:00', 100, NULL),
        ('5a7eb6d812ac43cdb7377f0c9195730f', @P_albany, N'Snagging & Testing', '2026-02-23T00:00:00+00:00', '2026-02-27T00:00:00+00:00', 100, NULL),
        ('f42064e2b0344036b5b16be8302d9ef7', @P_albany, N'Builders Clean', '2026-03-04T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL);
        PRINT 'OK    2 Albany Mews — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
    END
END
COMMIT;
GO

/* ===== 149a Coombe Lane West — 47 tasks — REV4 (final positions incl. EOT-01..03; superseded bars omitted) ===== */
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @P_coombe NVARCHAR(64);
IF (SELECT COUNT(*) FROM dbo.Projects WHERE Name LIKE N'%Coombe Lane%') <> 1
    PRINT 'SKIP  149a Coombe Lane West — expected exactly one project matching %Coombe Lane%.';
ELSE
BEGIN
    SELECT @P_coombe = ProjectId FROM dbo.Projects WHERE Name LIKE N'%Coombe Lane%';
    IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_coombe)
        PRINT 'SKIP  149a Coombe Lane West — project already has programme tasks; nothing touched.';
    ELSE
    BEGIN
        INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
        VALUES
        ('83310ae34bf8423b9a98be02d9c2bb00', @P_coombe, N'Site Setup & Demolition', '2023-09-18T00:00:00+00:00', '2023-10-06T00:00:00+00:00', 100, NULL),
        ('7504d8cea62a496b8afc7e0e2d99c506', @P_coombe, N'First Floor — Structural works', '2023-10-02T00:00:00+00:00', '2023-10-27T00:00:00+00:00', 100, NULL),
        ('304f0b8506f6433394ef56e185f2da2f', @P_coombe, N'First Floor — Roofing Works - Dormer', '2023-10-23T00:00:00+00:00', '2023-11-03T00:00:00+00:00', 100, NULL),
        ('6bbcbf1943e64c7e9a1f718ab989fc26', @P_coombe, N'First Floor — Studwork Bathroom Layouts', '2023-10-23T00:00:00+00:00', '2023-11-17T00:00:00+00:00', 100, NULL),
        ('ef5846829d774fdd812a4a66d5e27981', @P_coombe, N'First Floor — Electrical 1st Fix', '2023-11-20T00:00:00+00:00', '2023-11-30T00:00:00+00:00', 100, NULL),
        ('a2b58a6cb1454c8781ffdb55c17a049f', @P_coombe, N'First Floor — Plumbing 1st Fix', '2023-11-20T00:00:00+00:00', '2023-11-30T00:00:00+00:00', 100, NULL),
        ('b793edb586ff4801b180cf53eb8bab15', @P_coombe, N'First Floor — UFH & Screed', '2024-01-02T00:00:00+00:00', '2024-01-12T00:00:00+00:00', 100, NULL),
        ('d45459594eb84bab8439d490d7040fa1', @P_coombe, N'First Floor — Electrical 1st Fix (2)', '2024-01-29T00:00:00+00:00', '2024-02-15T00:00:00+00:00', 100, NULL),
        ('2f1aea0e0f784470b9b50390b1f7c6b9', @P_coombe, N'First Floor — Plumbing 1st Fix (2)', '2024-01-29T00:00:00+00:00', '2024-02-15T00:00:00+00:00', 100, NULL),
        ('44ac7c70ed604b658898de52d21076e3', @P_coombe, N'Basement Pool — Demolition & Concrete Cutting', '2024-01-29T00:00:00+00:00', '2024-02-23T00:00:00+00:00', 100, NULL),
        ('afc40776fff54016a78e5e28f6966a86', @P_coombe, N'First Floor — Plaster & Drylinning', '2024-02-19T00:00:00+00:00', '2024-03-08T00:00:00+00:00', 100, NULL),
        ('dc97a3492e6d4626bd25dce6e1fd46f7', @P_coombe, N'Ground Floor — Window Survey & Install', '2024-02-26T00:00:00+00:00', '2024-03-22T00:00:00+00:00', 100, NULL),
        ('7d869821da6041e0ba63bf4424ab6145', @P_coombe, N'First Floor — Tiling', '2024-03-04T00:00:00+00:00', '2024-03-15T00:00:00+00:00', 100, NULL),
        ('eb600ae15307421e8e1a3a2c554b7a1e', @P_coombe, N'Basement Pool — Pool Contractor - 15weeks', '2024-03-11T00:00:00+00:00', '2024-07-05T00:00:00+00:00', 100, NULL),
        ('a776587cb43847e399d1e318be295dcc', @P_coombe, N'First Floor — Carpentry - 2nd fix', '2024-03-18T00:00:00+00:00', '2024-04-04T00:00:00+00:00', 100, NULL),
        ('62728f6e89404b64b93e2529a890d4ca', @P_coombe, N'First Floor — Electrical 2nd Fix', '2024-03-22T00:00:00+00:00', '2024-04-12T00:00:00+00:00', 100, NULL),
        ('cd21cb08a5244b8ab0f6347fd02d4c66', @P_coombe, N'First Floor — Plumbing 2nd Fix', '2024-03-22T00:00:00+00:00', '2024-04-12T00:00:00+00:00', 100, NULL),
        ('e2ffdcf28462425aad28e85e49caa816', @P_coombe, N'External Works — Excavate base levels & Remove existing', '2024-04-02T00:00:00+00:00', '2024-05-03T00:00:00+00:00', 100, NULL),
        ('25cd30efda7845ecbbeb6d454a2db0e7', @P_coombe, N'First Floor — Decorations', '2024-04-15T00:00:00+00:00', '2024-04-26T00:00:00+00:00', 100, NULL),
        ('f41b4823282c4125a1e98aa11b210302', @P_coombe, N'First Floor — Floor Finishes', '2024-04-22T00:00:00+00:00', '2024-05-03T00:00:00+00:00', 100, NULL),
        ('4819b4df398e4d76b83e89e2b43b1426', @P_coombe, N'External Works — Paving & Block Rear', '2024-04-22T00:00:00+00:00', '2024-05-24T00:00:00+00:00', 100, NULL),
        ('1006ba457e00497a8790c558beecee68', @P_coombe, N'Basement Pool — Forming Walls & Ceiling', '2024-07-08T00:00:00+00:00', '2024-07-26T00:00:00+00:00', 100, NULL),
        ('6454bde15c454429b45fabd2eae78650', @P_coombe, N'Ground Floor — Strip Existing Flooring', '2024-07-15T00:00:00+00:00', '2024-08-02T00:00:00+00:00', 100, NULL),
        ('336e6a9fd3d64662955d520a81e9e6d0', @P_coombe, N'Basement Pool — UFH Screed & Leveling', '2024-07-22T00:00:00+00:00', '2024-08-02T00:00:00+00:00', 100, NULL),
        ('17092e3523e14d63ba486effd9e2f67e', @P_coombe, N'External Works — Resin Driveway', '2024-07-22T00:00:00+00:00', '2024-08-02T00:00:00+00:00', 100, NULL),
        ('5b67e934a764438dabb4be10bb424105', @P_coombe, N'External Works — Structural Works front', '2024-07-22T00:00:00+00:00', '2024-09-13T00:00:00+00:00', 100, NULL),
        ('adf2d25da29b43f9a63e1916bd38b834', @P_coombe, N'Basement Pool — Pool Storage Doors', '2024-07-29T00:00:00+00:00', '2024-09-27T00:00:00+00:00', 100, NULL),
        ('3c0b9a244fd14e5fa47fb5225f17f4d9', @P_coombe, N'Basement Pool — Crittal Doors', '2024-07-29T00:00:00+00:00', '2024-10-04T00:00:00+00:00', 100, NULL),
        ('8ad74a57cbb5418287133100cc865fd7', @P_coombe, N'Ground Floor — Crittall Doors', '2024-07-29T00:00:00+00:00', '2024-10-04T00:00:00+00:00', 100, NULL),
        ('4ac2046ecdfb41e088230548c6bb210a', @P_coombe, N'Basement Pool — 1st fix plumbing', '2024-08-05T00:00:00+00:00', '2024-08-15T00:00:00+00:00', 100, NULL),
        ('9ff16c88f62e4ee68f463c921c52cc0f', @P_coombe, N'Basement Pool — 1st fix electrics', '2024-08-05T00:00:00+00:00', '2024-08-15T00:00:00+00:00', 100, NULL),
        ('eb29293e3937460bba47b71269de9b01', @P_coombe, N'Ground Floor — Screed & Leveling', '2024-08-05T00:00:00+00:00', '2024-08-15T00:00:00+00:00', 100, NULL),
        ('49c18699fb1040fea940f3267d85bfdc', @P_coombe, N'External Works — B&Q area', '2024-08-05T00:00:00+00:00', '2024-09-13T00:00:00+00:00', 100, NULL),
        ('ffc6202cff3d4df3ba1fed72b6da5ec6', @P_coombe, N'Builder Clean', '2024-08-12T00:00:00+00:00', '2024-08-15T00:00:00+00:00', 100, NULL),
        ('e35119e9789d47b7bb8fe9b596e07aed', @P_coombe, N'Ground Floor — Floor finishes', '2024-08-12T00:00:00+00:00', '2024-09-06T00:00:00+00:00', 100, NULL),
        ('39501c0caf21442fafa73bc86a5cc859', @P_coombe, N'Basement Pool — Plaster & Drylinning', '2024-08-19T00:00:00+00:00', '2024-08-30T00:00:00+00:00', 100, NULL),
        ('1367aa2fedcc4320bd44eb1ac7a76298', @P_coombe, N'Ground Floor — Walk on Glass', '2024-09-02T00:00:00+00:00', '2024-09-06T00:00:00+00:00', 100, NULL),
        ('8fb453276c474c16b32bb1202e78d88e', @P_coombe, N'Basement Pool — Floor & Wall Tiling', '2024-09-02T00:00:00+00:00', '2024-09-20T00:00:00+00:00', 100, NULL),
        ('c0abf53761c04968a27129ef785fc30c', @P_coombe, N'Basement Pool — Plumbing & Electrical Fit off', '2024-09-16T00:00:00+00:00', '2024-09-27T00:00:00+00:00', 100, NULL),
        ('e22e2c572ab7456284c7a0ed83b47ca5', @P_coombe, N'Basement Pool — Decoration', '2024-09-23T00:00:00+00:00', '2024-10-04T00:00:00+00:00', 100, NULL),
        ('aa49a60ba65f4543aa405e0e038a9135', @P_coombe, N'External Works — Resin Driveway (2)', '2024-09-23T00:00:00+00:00', '2024-10-11T00:00:00+00:00', 100, NULL),
        ('04d2cf6e61054318b500929553eba43b', @P_coombe, N'External Works — Soft Landscape', '2024-09-23T00:00:00+00:00', '2024-10-25T00:00:00+00:00', 100, NULL),
        ('35a6a824029149f09b4674510fa81232', @P_coombe, N'Basement Pool — Pool Contractor - 15weeks (2)', '2024-09-30T00:00:00+00:00', '2024-10-18T00:00:00+00:00', 100, NULL),
        ('989671d1aa1343669afabc0963ff658a', @P_coombe, N'External Works — Carport', '2024-09-30T00:00:00+00:00', '2024-10-18T00:00:00+00:00', 100, NULL),
        ('6034a56cb621476e803e46eb47073deb', @P_coombe, N'Ground Floor — Decoration', '2024-10-07T00:00:00+00:00', '2024-10-18T00:00:00+00:00', 100, NULL),
        ('11398a9f39514d9380dac5763ac508d1', @P_coombe, N'External Works — Auto Gates', '2024-10-14T00:00:00+00:00', '2024-10-25T00:00:00+00:00', 100, NULL),
        ('1a0924ed61b44547bd3dfa418af06e4e', @P_coombe, N'Basement Pool — Snagging & Testing', '2024-10-21T00:00:00+00:00', '2024-10-25T00:00:00+00:00', 100, NULL);
        PRINT 'OK    149a Coombe Lane West — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
    END
END
COMMIT;
GO

/* ===== By France — 24 tasks — REV8 draft (export shows 2 Feb 2026 onward only; earlier completed tasks are not included) ===== */
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @P_byfrance NVARCHAR(64);
IF (SELECT COUNT(*) FROM dbo.Projects WHERE Name LIKE N'%By France%') <> 1
    PRINT 'SKIP  By France — expected exactly one project matching %By France%.';
ELSE
BEGIN
    SELECT @P_byfrance = ProjectId FROM dbo.Projects WHERE Name LIKE N'%By France%';
    IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_byfrance)
        PRINT 'SKIP  By France — project already has programme tasks; nothing touched.';
    ELSE
    BEGIN
        INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
        VALUES
        ('5c0a76c3791743019ab1ff4e3ff715dc', @P_byfrance, N'Second Floor — Insulation/Dry Lining/Plaster', '2026-02-02T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
        ('6686f0e58470470f9e15f524cf2f57c5', @P_byfrance, N'First Floor — Insulation/Dry Lining/Plaster', '2026-02-16T00:00:00+00:00', '2026-03-27T00:00:00+00:00', 100, NULL),
        ('d6453563762b4322acc082fd2be1051f', @P_byfrance, N'Second Floor — Window Installation TBC', '2026-02-23T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
        ('d52d62bd36c94e11a16fee5af8806a81', @P_byfrance, N'First Floor — Window Installation TBC', '2026-02-23T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
        ('2fca8135ee734463b985d63f960dbec9', @P_byfrance, N'Second Floor — FF/SF Staircase', '2026-02-23T00:00:00+00:00', '2026-04-10T00:00:00+00:00', 100, NULL),
        ('b80b4a69477549579705af391af46da7', @P_byfrance, N'First Floor — FF/SF Staircase', '2026-03-02T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
        ('3b027b61dff1402bb0767c2f2d61668b', @P_byfrance, N'Second Floor — Tiling Installation', '2026-03-09T00:00:00+00:00', '2026-03-20T00:00:00+00:00', 100, NULL),
        ('a1c4e5dcc0644eb2ae4532677dfa6fff', @P_byfrance, N'Ground Floor — Insulation, UFH & Screed', '2026-03-09T00:00:00+00:00', '2026-04-03T00:00:00+00:00', 100, NULL),
        ('363b6655d63b470a8b6cdd75561a4985', @P_byfrance, N'Ground Floor — Entrance Door & Window Installation TBC', '2026-03-16T00:00:00+00:00', '2026-03-27T00:00:00+00:00', 100, NULL),
        ('afed642c02a3412db57637b49bb77fa7', @P_byfrance, N'Second Floor — Plumbing 2nd Fix', '2026-03-16T00:00:00+00:00', '2026-04-10T00:00:00+00:00', 100, NULL),
        ('77b39fb10dfa401fab01a13a0d4a6f51', @P_byfrance, N'First Floor — Tiling Installation', '2026-03-16T00:00:00+00:00', '2026-04-10T00:00:00+00:00', 100, NULL),
        ('4a974e6299e9444a8626376c5b6d4255', @P_byfrance, N'External Works — Tree & Fence Removal Boundaries TBC', '2026-03-16T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
        ('80cf7be39c53494382f9d2c046b72112', @P_byfrance, N'External Works — Fencing to Boundary TBC', '2026-03-23T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
        ('7204923ef0cc4b3a9a6acf87248d5e18', @P_byfrance, N'Ground Floor — Insulation/Dry Lining/Plaster', '2026-03-23T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('a81934589fe1409d998ed2b3bc3289aa', @P_byfrance, N'External Works — Excavation & masonry - Entrance TBC', '2026-03-30T00:00:00+00:00', '2026-04-17T00:00:00+00:00', 100, NULL),
        ('aa7db9b8afd04e05b67ed9d35f4072f7', @P_byfrance, N'Second Floor — Electrics 2nd Fix', '2026-03-30T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('1494c4cf50cd4199a294defe99b43275', @P_byfrance, N'Second Floor — Carpentry 2nd Fix', '2026-03-30T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('c10093b2370041189359f6eaf678f24d', @P_byfrance, N'External Works — Render & Cladding', '2026-03-30T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('c5ed9900684545f58e49547e3d9f23a7', @P_byfrance, N'First Floor — Plumbing 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('710689f030ca4b03bcdcc253b9273eef', @P_byfrance, N'First Floor — Electrics 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('b58ba7aec68f48d0ad4ff9fc6f295054', @P_byfrance, N'First Floor — Carpentry 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('730f7d7856ad4b78bfbc3650aaa8fe9d', @P_byfrance, N'First Floor — Air Conditioning 2nd Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('e35c6b8509b640ff87cdabd9c6902b33', @P_byfrance, N'Second Floor — Decoration', '2026-04-20T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
        ('d6b97a7f35324273b659d3d1d3927ebb', @P_byfrance, N'External Works — Entrance Gate Survey - Install TBC', '2026-04-20T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL);
        PRINT 'OK    By France — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
    END
END
COMMIT;
GO
