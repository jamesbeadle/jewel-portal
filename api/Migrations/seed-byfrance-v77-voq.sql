-- ============================================================================
-- Seed: By France -- V77 Front Entrance Canopy (UNAPPROVED variation quote)
-- ----------------------------------------------------------------------------
-- Project : By France, Leas Green, Chislehurst, BR7 6HD  (JBB-2026-001)
-- ProjectId: 3490f944b29545c4b8d5a04130f42ab8
-- Source   : "By France - Valuation 18 - June 26 - New Build (V77 - Front
--            Entrance Canopy)" quote tab, dated 2026-07-01.
--
-- V77 is a quote submitted to the client and NOT yet approved, so this file
-- seeds ONLY a VariationOrderQuotes row (Status 2 = Tendering, matching the
-- pending-quote precedent of V19/V70/V71 in seed-byfrance-variations.sql).
-- Deliberately NOT seeded until approval:
--   * no VariationOrders row       (created on approval)
--   * no ValuationLineItems row    (V77 is not in the Val 18 register net of
--                                   GBP 215,737.58 -- adding nothing keeps
--                                   that file's reconciliation untouched)
--
-- EMAIL LINKING
-- Emails are associated with a VOQ by Outlook category tag, not by a DB row:
-- once this record exists it appears in the email-triage "Variation Order
-- Quote" picker, and linking an email tags its whole thread
-- "JPMS/VOQ-JBB-2026-001-0077" (see VariationOrderQuoteLinkProvider /
-- LinkMessageToRecordHandler). Nothing to seed for that here.
--
-- VOQ Status: 0=Draft 1=Inviting 2=Tendering 3=Selected 4=Approved 5=Rejected
-- EstimatedValue = quote total excl VAT: nett 4,868.00 + 10% OH&P 486.80
--                = GBP 5,354.80 (VAT 0%).
--
-- Idempotent: keyed on bf-voq-v77 via MERGE; a re-run refreshes every field.
-- Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[VariationOrderQuotes] AS target
USING (VALUES
    (N'bf-voq-v77', N'3490f944b29545c4b8d5a04130f42ab8', N'', 77, N'VOQ-0077',
     N'Front Entrance Canopy - PRO-064-(WD)-P-703',
     N'Supply and install new front entrance canopy per PRO-064-(WD)-P-703 (Rev - For Comment, June 2026). Out-of-sequence works, priced on day rates plus 10% OH&P in line with variation precedent on this project. Canopy projection 2150mm. Build-up: 50x150 C24 rafters on joist hangers, 150x75 C24 fascia infill, 18mm WBP ply deck, single-ply membrane to falls, 150mm min flashing upstand under cladding, black aluminium pressed fascia trim, ivory cement board soffit, pair of recessed downlights. NOTE: 150x75 PFC galvanised steel already installed under separate scope - EXCLUDED. '
     + N'LABOUR (day rates): 1.1 Carpenter - rafters, deck, fascia infill/trim, soffit - 2 day @ 320 = 640; 1.2 Labourer - assist/handling/tidy - 2 day @ 200 = 400; 1.3 Roofer - membrane + flashing upstand - 1 day @ 340 = 340; 1.4 Electrician - 2nd fix downlights, test & cert - 0.5 day @ 360 = 180. '
     + N'MATERIALS: 2.1 PFC steel - excluded - 0; 2.2 C24 timber pack 165; 2.3 Hangers/fixings 85; 2.4 18mm WBP ply 12m2 @ 22 = 264; 2.5 Single-ply membrane 12m2 @ 62 = 744 (matches Val Ref R135); 2.6 Flashing upstand 4.5m @ 38 = 171; 2.7 Black alu fascia trim 12m @ 48 = 576; 2.8 Ivory cement board soffit 9m2 @ 42 = 378; 2.9 Downlights (pair) 145; 2.10 Sealants/consumables 95. '
     + N'PLANT: 3.1 Alloy access tower 1 wk 210; 3.2 Muck away 1 load 475 (matches Val Ref R444). '
     + N'Nett cost 4,868.00 + 10% OH&P 486.80 = 5,354.80 excl VAT (VAT 0%).',
     2, NULL, NULL, 5354.8000, '2026-07-01', N'nigel.reilly@jewelgroup.co.uk', NULL, NULL)
) AS source (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
             Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
             CreatedAt, CreatedByEmail, ApprovedAt, ApprovedByEmail)
ON target.VariationOrderQuoteId = source.VariationOrderQuoteId
WHEN MATCHED THEN UPDATE SET
    ProjectId               = source.ProjectId,
    RequestId               = source.RequestId,
    Number                  = source.Number,
    Reference               = source.Reference,
    Title                   = source.Title,
    Description             = source.Description,
    Status                  = source.Status,
    SelectedBidPackageId    = source.SelectedBidPackageId,
    SelectedSubcontractorId = source.SelectedSubcontractorId,
    EstimatedValue          = source.EstimatedValue,
    CreatedAt               = source.CreatedAt,
    CreatedByEmail          = source.CreatedByEmail,
    ApprovedAt              = source.ApprovedAt,
    ApprovedByEmail         = source.ApprovedByEmail
WHEN NOT MATCHED BY TARGET THEN
    INSERT (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
            Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
            CreatedAt, CreatedByEmail, ApprovedAt, ApprovedByEmail)
    VALUES (source.VariationOrderQuoteId, source.ProjectId, source.RequestId, source.Number,
            source.Reference, source.Title, source.Description, source.Status,
            source.SelectedBidPackageId, source.SelectedSubcontractorId, source.EstimatedValue,
            source.CreatedAt, source.CreatedByEmail, source.ApprovedAt, source.ApprovedByEmail);
GO

-- Sanity check: V77 exists as a pending (Tendering) quote with no VO and no
-- valuation report line, and the Val 18 reconciliation is untouched.
SELECT
    (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes]
      WHERE VariationOrderQuoteId = N'bf-voq-v77' AND Status = 2)                              AS V77QuotePending,   -- 1
    (SELECT EstimatedValue FROM [dbo].[VariationOrderQuotes]
      WHERE VariationOrderQuoteId = N'bf-voq-v77')                                             AS V77Value,          -- 5354.80
    (SELECT COUNT(*) FROM [dbo].[VariationOrders]
      WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8' AND Number = 77)                   AS V77VoRows,          -- 0
    (SELECT COUNT(*) FROM [dbo].[ValuationLineItems]
      WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8' AND VariationRef = N'V77')         AS V77ReportLines,     -- 0
    (SELECT SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END)
       FROM [dbo].[ValuationLineItems]
      WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8' AND ElementType = 3)               AS NetVariations;      -- 215737.58
GO
