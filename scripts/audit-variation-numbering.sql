-- ============================================================================
-- Audit: VOQ / VO numbering integrity (2026-07-22)
-- ----------------------------------------------------------------------------
-- Context: until 2026-07-22, CreateVoqFromRfq and ApproveVariationOrderQuote
-- computed the next Number as MAX(Number)+1 across ALL projects, so a record
-- created through the app on one project could continue another project's
-- sequence (e.g. a first VOQ on a new project minted as VOQ-0077 because
-- By France's seeded register runs to 76). Both handlers are now per-project,
-- and an approved VO reuses its VOQ's number when free (VOQ-0072 -> V72).
--
-- Run this READ-ONLY audit before adding the unique indexes below. Expected
-- result on a healthy database: sections 1 and 2 return no rows; section 3
-- lists app-created records for eyeball review (seeded rows all have
-- CreatedByEmail = 'seed@jewelgroup.co.uk').
--
-- FOLLOW-UP once sections 1 & 2 are clean (in the dev environment, which has
-- the .NET SDK — the app auto-applies EF migrations on startup, so this must
-- be a REAL EF migration, not hand-run SQL):
--
--     dotnet ef migrations add AddVariationNumberUniqueIndexes --project api
--
--     // in the migration's Up():
--     //   CreateIndex UX_VariationOrderQuotes_Project_Number
--     //     on VariationOrderQuotes (ProjectId, Number) UNIQUE
--     //   CreateIndex UX_VariationOrders_Project_Number
--     //     on VariationOrders (ProjectId, Number) UNIQUE
--     // (mirror UX_Requests_Project_Reference in
--     //  20260702120000_AddRequestReferenceUniqueIndex.cs for style)
-- ============================================================================

-- 1) Duplicate VOQ numbers within a project (must be empty before indexing).
SELECT q.ProjectId, p.Reference AS ProjectRef, q.Number, COUNT(*) AS Rows,
       STRING_AGG(q.VariationOrderQuoteId, ', ') AS VoqIds
FROM   [dbo].[VariationOrderQuotes] q
LEFT   JOIN [dbo].[Projects] p ON p.ProjectId = q.ProjectId
GROUP  BY q.ProjectId, p.Reference, q.Number
HAVING COUNT(*) > 1
ORDER  BY p.Reference, q.Number;

-- 2) Duplicate VO numbers within a project (must be empty before indexing).
SELECT v.ProjectId, p.Reference AS ProjectRef, v.Number, COUNT(*) AS Rows,
       STRING_AGG(v.VariationOrderId, ', ') AS VoIds
FROM   [dbo].[VariationOrders] v
LEFT   JOIN [dbo].[Projects] p ON p.ProjectId = v.ProjectId
GROUP  BY v.ProjectId, p.Reference, v.Number
HAVING COUNT(*) > 1
ORDER  BY p.Reference, v.Number;

-- 3) App-created records (non-seed) with their numbers, for eyeball review:
--    any VOQ/VO here whose Number looks like it continued ANOTHER project's
--    sequence (a big jump past its own project's seeded max) was minted by the
--    old global MAX+1 bug. Renumbering is a judgement call — the reference may
--    already be quoted in correspondence — so this audit only surfaces them.
SELECT 'VOQ' AS Kind, p.Reference AS ProjectRef, q.Number, q.Reference,
       q.Title, q.CreatedAt, q.CreatedByEmail
FROM   [dbo].[VariationOrderQuotes] q
LEFT   JOIN [dbo].[Projects] p ON p.ProjectId = q.ProjectId
WHERE  q.CreatedByEmail <> N'seed@jewelgroup.co.uk'
UNION ALL
SELECT 'VO', p.Reference, v.Number, v.VariationRef, v.Title,
       v.ApprovedAt, v.ApprovedByEmail
FROM   [dbo].[VariationOrders] v
LEFT   JOIN [dbo].[Projects] p ON p.ProjectId = v.ProjectId
WHERE  v.ApprovedByEmail <> N'seed@jewelgroup.co.uk'
ORDER  BY ProjectRef, Kind, Number;
