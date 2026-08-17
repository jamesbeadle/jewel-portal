/* ============================================================================
   Seed (part 3): Ravenswood Ave programme — 17 tasks from the REV1 draft,
   Week 1 = Mon 02 Feb 2026 (per Nigel, 2026-08-17). Programme runs to early
   June 2026, so progress is 100 throughout as at today.
   Safe to re-run: skipped if Ravenswood already has programme tasks.
   ========================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @P_rav NVARCHAR(64) = '3bf6dcfa81764a248138fb5fd357aa84';
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @P_rav AND Name = N'Ravenswood Ave')
    PRINT 'SKIP  Ravenswood Ave — ProjectId does not match the Ravenswood Ave project.';
ELSE IF EXISTS (SELECT 1 FROM dbo.ProgrammeTasks WHERE ProjectId = @P_rav)
    PRINT 'SKIP  Ravenswood Ave — project already has programme tasks; nothing touched.';
ELSE
BEGIN
    INSERT INTO dbo.ProgrammeTasks (ProgrammeTaskId, ProjectId, Title, PlannedStart, PlannedEnd, ProgressPercent, BoqLineItemId)
    VALUES
    ('b7ed9f4437d84716888049f4b9930b46', @P_rav, N'Site set up & Demolition', '2026-02-02T00:00:00+00:00', '2026-02-09T00:00:00+00:00', 100, NULL),
    ('17a02eeeb4bc4047a9fb869b8311bd43', @P_rav, N'Ground Works & Below Ground Drainage', '2026-02-09T00:00:00+00:00', '2026-02-24T00:00:00+00:00', 100, NULL),
    ('8dd06a16c3b44a2fb6ffbb0c35a10772', @P_rav, N'Masonry Walls', '2026-02-23T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
    ('a7e3b64ef80d489bb7c405b8b75a39e2', @P_rav, N'Steelwork', '2026-03-04T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 100, NULL),
    ('87f2eec99a8241838e6f0cf867795ecf', @P_rav, N'Timber Carcassing', '2026-03-09T00:00:00+00:00', '2026-03-24T00:00:00+00:00', 100, NULL),
    ('16b057c229d64cd5b57a69a892494bcb', @P_rav, N'Roofing inc. Leadwork', '2026-03-23T00:00:00+00:00', '2026-04-03T00:00:00+00:00', 100, NULL),
    ('a040e51f15dc42e7bac922721b6cf0d4', @P_rav, N'Glazing', '2026-03-24T00:00:00+00:00', '2026-03-31T00:00:00+00:00', 100, NULL),
    ('d80ffc24ef6047c5a65409a26b083b2a', @P_rav, N'Power & Lighting 1st Fix', '2026-04-06T00:00:00+00:00', '2026-04-20T00:00:00+00:00', 100, NULL),
    ('9f374999028e4aabbfccca8126a5dfe6', @P_rav, N'Plumbing & Heating 1st Fix', '2026-04-07T00:00:00+00:00', '2026-04-21T00:00:00+00:00', 100, NULL),
    ('d3b3aeebaed54a29ae7f2f20383931f3', @P_rav, N'Plaster & Drylinning', '2026-04-16T00:00:00+00:00', '2026-05-07T00:00:00+00:00', 100, NULL),
    ('47f0ce03ba11479ba25dd5aab039fcc2', @P_rav, N'Joinery', '2026-04-27T00:00:00+00:00', '2026-05-12T00:00:00+00:00', 100, NULL),
    ('54d8b50954d241b797803e8abcd9a9fa', @P_rav, N'Power & Lighting 2nd Fix', '2026-05-04T00:00:00+00:00', '2026-05-14T00:00:00+00:00', 100, NULL),
    ('441cb5ff6573404d8d28527e1a464e7d', @P_rav, N'Plumbing & Heating 2nd Fix', '2026-05-04T00:00:00+00:00', '2026-05-15T00:00:00+00:00', 100, NULL),
    ('fde47e2d3dd249c2a8fe8104e2f9a0e6', @P_rav, N'Decorations', '2026-05-11T00:00:00+00:00', '2026-05-29T00:00:00+00:00', 100, NULL),
    ('9facfe1efdd84fb98cee7a51b457ab7e', @P_rav, N'Floor Finishes', '2026-05-18T00:00:00+00:00', '2026-05-29T00:00:00+00:00', 100, NULL),
    ('eec1f217aaa74d70bf04ff65fd9ac47f', @P_rav, N'External Works & Final Drainage', '2026-05-18T00:00:00+00:00', '2026-06-04T00:00:00+00:00', 100, NULL),
    ('7a2588ad50f44b748902453b9f0c123d', @P_rav, N'Snagging & clear site', '2026-06-01T00:00:00+00:00', '2026-06-04T00:00:00+00:00', 100, NULL);
    PRINT 'OK    Ravenswood Ave — ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' programme tasks inserted.';
END
COMMIT;
GO
