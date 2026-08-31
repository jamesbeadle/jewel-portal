-- ============================================================================
-- seed-ai-action-skills.sql  (2026-08-31)
-- ============================================================================
-- The opening skill-to-action wiring for the connector (docs/ai/10-mcp-connector.md
-- §2d), attached at AREA level throughout so a new action in an area inherits its
-- doctrine automatically. The mapping:
--
--   jbb-second-brain (house knowledge, terminology, record lineage) — EVERY area,
--     the connector's replacement for "pinned on every turn".
--   commercial-director (the master commercial/QS manual) — every area where money,
--     entitlement or contractual position moves.
--   nigel-commercial-doctrine (dispute correspondence, clause verification, reserve
--     doctrine, no sub-cost disclosure) — the client/counter-party-facing areas:
--     claims, variations, notices, correspondence, contracts, retention, LADs,
--     final-account territory.
--   commercial-director-mistake-prevention (codified failure modes for workbooks,
--     rate build-ups, sub comparisons, notices) — the areas where figures and
--     external deliverables are authored.
--
-- Idempotent: inserts only rows not already present, and only for skills that
-- exist, so it is safe to re-run and safe after the team has edited attachments
-- on the AI Actions page (it never deletes or replaces). One-off data fix — not
-- an EF migration; run via sqlcmd.
-- ============================================================================

SET NOCOUNT ON;

DECLARE @attachedBy nvarchar(256) = N'automation@jewelbb.co.uk';

INSERT INTO [dbo].[AiActionSkills]
    ([ActionSkillId], [TargetKind], [TargetKey], [SkillKey], [AttachedByEmail], [AttachedAt])
SELECT
    LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), N'-', N'')),
    N'area',
    v.[TargetKey],
    v.[SkillKey],
    @attachedBy,
    SYSDATETIMEOFFSET()
FROM (VALUES
    -- jbb-second-brain: every area ------------------------------------------------
    (N'Access requests',      N'jbb-second-brain'),
    (N'BoQ',                  N'jbb-second-brain'),
    (N'Building control',     N'jbb-second-brain'),
    (N'CVR',                  N'jbb-second-brain'),
    (N'Calendar',             N'jbb-second-brain'),
    (N'Cashflow',             N'jbb-second-brain'),
    (N'Closeout & defects',   N'jbb-second-brain'),
    (N'Commercial',           N'jbb-second-brain'),
    (N'Contacts',             N'jbb-second-brain'),
    (N'Correspondence',       N'jbb-second-brain'),
    (N'Cost centres',         N'jbb-second-brain'),
    (N'Directory & users',    N'jbb-second-brain'),
    (N'Document control',     N'jbb-second-brain'),
    (N'Drawings',             N'jbb-second-brain'),
    (N'Health & safety',      N'jbb-second-brain'),
    (N'LADs',                 N'jbb-second-brain'),
    (N'Labour',               N'jbb-second-brain'),
    (N'Leads & CRM',          N'jbb-second-brain'),
    (N'Mobilisation',         N'jbb-second-brain'),
    (N'Platform',             N'jbb-second-brain'),
    (N'Procurement',          N'jbb-second-brain'),
    (N'Progress & programme', N'jbb-second-brain'),
    (N'Project contracts',    N'jbb-second-brain'),
    (N'Projects',             N'jbb-second-brain'),
    (N'Rates',                N'jbb-second-brain'),
    (N'Requests & RFIs',      N'jbb-second-brain'),
    (N'Retention',            N'jbb-second-brain'),
    (N'Site',                 N'jbb-second-brain'),
    (N'Subcontractors',       N'jbb-second-brain'),
    (N'Tender enquiries',     N'jbb-second-brain'),
    (N'To-dos',               N'jbb-second-brain'),
    (N'Useful information',   N'jbb-second-brain'),
    (N'Valuation invoices',   N'jbb-second-brain'),
    (N'Variations',           N'jbb-second-brain'),
    -- commercial-director: wherever money, entitlement or position moves -----------
    (N'BoQ',                  N'commercial-director'),
    (N'CVR',                  N'commercial-director'),
    (N'Cashflow',             N'commercial-director'),
    (N'Closeout & defects',   N'commercial-director'),
    (N'Commercial',           N'commercial-director'),
    (N'Correspondence',       N'commercial-director'),
    (N'Cost centres',         N'commercial-director'),
    (N'LADs',                 N'commercial-director'),
    (N'Procurement',          N'commercial-director'),
    (N'Project contracts',    N'commercial-director'),
    (N'Rates',                N'commercial-director'),
    (N'Requests & RFIs',      N'commercial-director'),
    (N'Retention',            N'commercial-director'),
    (N'Subcontractors',       N'commercial-director'),
    (N'Tender enquiries',     N'commercial-director'),
    (N'Valuation invoices',   N'commercial-director'),
    (N'Variations',           N'commercial-director'),
    -- nigel-commercial-doctrine: client/counter-party-facing position --------------
    (N'Closeout & defects',   N'nigel-commercial-doctrine'),
    (N'Commercial',           N'nigel-commercial-doctrine'),
    (N'Correspondence',       N'nigel-commercial-doctrine'),
    (N'LADs',                 N'nigel-commercial-doctrine'),
    (N'Project contracts',    N'nigel-commercial-doctrine'),
    (N'Requests & RFIs',      N'nigel-commercial-doctrine'),
    (N'Retention',            N'nigel-commercial-doctrine'),
    (N'Valuation invoices',   N'nigel-commercial-doctrine'),
    (N'Variations',           N'nigel-commercial-doctrine'),
    -- mistake-prevention: where figures and deliverables are authored --------------
    (N'BoQ',                  N'commercial-director-mistake-prevention'),
    (N'CVR',                  N'commercial-director-mistake-prevention'),
    (N'Cashflow',             N'commercial-director-mistake-prevention'),
    (N'Commercial',           N'commercial-director-mistake-prevention'),
    (N'Cost centres',         N'commercial-director-mistake-prevention'),
    (N'Procurement',          N'commercial-director-mistake-prevention'),
    (N'Rates',                N'commercial-director-mistake-prevention'),
    (N'Requests & RFIs',      N'commercial-director-mistake-prevention'),
    (N'Subcontractors',       N'commercial-director-mistake-prevention'),
    (N'Tender enquiries',     N'commercial-director-mistake-prevention'),
    (N'Valuation invoices',   N'commercial-director-mistake-prevention'),
    (N'Variations',           N'commercial-director-mistake-prevention')
) AS v ([TargetKey], [SkillKey])
INNER JOIN [dbo].[Skills] s
    ON s.[SkillKey] = v.[SkillKey]
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AiActionSkills] existing
    WHERE existing.[TargetKind] = N'area'
      AND existing.[TargetKey] = v.[TargetKey]
      AND existing.[SkillKey] = v.[SkillKey]
);

PRINT CONCAT('Attached ', @@ROWCOUNT, ' area-skill rows (72 in the full mapping; fewer means some already existed).');

SELECT [TargetKey], COUNT(*) AS [Skills]
FROM [dbo].[AiActionSkills]
WHERE [TargetKind] = N'area'
GROUP BY [TargetKey]
ORDER BY [TargetKey];
