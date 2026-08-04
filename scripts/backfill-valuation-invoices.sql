-- ============================================================================
-- Historic valuation-invoice backfill — generated 2026-08-04 from the accountant's
-- Xero receivable-invoice export (Jewel Bespoke Build Ltd, 1 Aug 2022 - 31 Aug 2026),
-- grouped by the Xero "Sites" tracking option. All figures net of VAT.
--
-- Each site is one guarded, transactional batch that:
--   * resolves the project by XeroSiteName (or project name), case/space-insensitive —
--     the same match the Xero cost-allocation suggester uses;
--   * SKIPs (PRINT, no error) projects with ANY existing valuation invoice — idempotent,
--     and live projects that already invoice through JPMS are never touched;
--   * SKIPs projects holding a Preapproved claim (raw inserts cannot re-freeze their
--     totals — enter those through the app's manual-invoice flow instead);
--   * inserts each sale as a paid/issued MANUAL valuation invoice (IsManual = 1,
--     backdated, no report snapshot — mirroring CreateValuationInvoiceHandler), with a
--     ManualEntry audit event naming the Xero invoice;
--   * rolls the paid total into Projects.ValuationInvoicePaidTotal.
--
-- Paid invoices use the invoice date as the payment date (the export carries no
-- payment dates). 'Approved' (authorised, unpaid) invoices land as Issued, unpaid.
--
-- Apply:  sqlcmd -S <server> -d <db> -U <user> -i scripts/backfill-valuation-invoices.sql -b
-- ============================================================================
--
-- NOT INCLUDED — no Sites tracking in the export (place by hand if they belong to a project):
--   INV-0015     2023-03-24    10,967.19  79 KINGS DRIVE — Final Retention
--   INV-0019     2023-04-20       793.80  Clear-cut Carpentry Ltd — Jack Easty Hours
--   INV-0021     2023-05-09     1,587.60  Clear-cut Carpentry Ltd — Jack Easty April/May 23
--   INV-0023     2023-06-30     1,111.32  Clear-cut Carpentry Ltd — Jack Easty June 23
--   INV-0028     2023-08-30     1,428.84  Clear-cut Carpentry Ltd — Jack Easty July 23
--   INV-0156     2025-05-30     1,864.00  Jewel Property Serve Ltd — Works at 198 Chiltern Court
--   INV-0002     2023-01-06    41,503.34  School House Vets — Vets Valuation 1 - Phase 3
--   INV-0005     2023-02-06    12,725.33  School House Vets — Vets Valuation 3 - Phase 3
--   INV-0008     2023-02-17     5,495.75  School House Vets — Vets Valuation 4 - Phase 3
--   INV-0018     2023-10-21     2,652.70  School House Vets — Vets retention - Phase 3
--   CN-0080      2024-04-30    -2,652.70  School House Vets — Vets retention - Phase 3
--
-- NOT INCLUDED — wrong status or type:
--   P10104770    2026-05-14    20,000.00  1 Fairfield Avenue (status Draft)
--   None         2024-02-29        -9.00  67 Beresford Road (receivable overpayment — not a sales invoice)
--   INV-0085     2024-05-29    30,000.00  Cornerways (status Deleted)
--   INV-0188     2026-01-05    21,116.84  David Needham (status Deleted)
--   None         2023-09-13       -30.00  PLG Consultants LTD (receivable overpayment — not a sales invoice)
--   INV-0011     2023-02-28     2,110.00  School House Vets (status Deleted)
--   Total                           0.00  Windy Ridge (status None)

GO
-- ===== 149a Coombe Lane West — 19 invoices, net 834,212.33, of which paid 834,212.33 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '149acoombelanewest'
       OR LOWER(REPLACE(Name, ' ', '')) = '149acoombelanewest'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '149acoombelanewest' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  149a Coombe Lane West — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  149a Coombe Lane West — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  149a Coombe Lane West — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('e76d242ef4cb4323ac58343862bda0a6', @ProjectId, NULL, 1, 'VI-0001', '2023-10-01T00:00:00+00:00', 80306.35, 80306.35, 2, '2023-10-18T00:00:00+00:00', '2023-10-18T00:00:00+00:00', '2023-10-18T00:00:00+00:00', 0, 1),
        ('1979ef5840fe42bc8bf132ad3e2e1f33', @ProjectId, NULL, 2, 'VI-0002', '2023-11-01T00:00:00+00:00', 14305.09, 14305.09, 2, '2023-11-15T00:00:00+00:00', '2023-11-15T00:00:00+00:00', '2023-11-15T00:00:00+00:00', 0, 1),
        ('0121fcd8e1204566bcec967abd42698e', @ProjectId, NULL, 3, 'VI-0003', '2023-12-01T00:00:00+00:00', 28847.89, 28847.89, 2, '2023-12-13T00:00:00+00:00', '2023-12-13T00:00:00+00:00', '2023-12-13T00:00:00+00:00', 0, 1),
        ('e189f39f79b248b9a788dd2d1ccf824f', @ProjectId, NULL, 4, 'VI-0004', '2024-01-01T00:00:00+00:00', 17394.40, 17394.40, 2, '2024-01-10T00:00:00+00:00', '2024-01-10T00:00:00+00:00', '2024-01-10T00:00:00+00:00', 0, 1),
        ('e5fcb451674c4c72886bf60c9c29e4f2', @ProjectId, NULL, 5, 'VI-0005', '2024-02-01T00:00:00+00:00', 20589.11, 20589.11, 2, '2024-02-07T00:00:00+00:00', '2024-02-07T00:00:00+00:00', '2024-02-07T00:00:00+00:00', 0, 1),
        ('cbc478e6def242d5a3604d49d7c42aa2', @ProjectId, NULL, 6, 'VI-0006', '2024-03-01T00:00:00+00:00', 25722.44, 25722.44, 2, '2024-03-05T00:00:00+00:00', '2024-03-05T00:00:00+00:00', '2024-03-05T00:00:00+00:00', 0, 1),
        ('3c5f038961f24a1788df56ed5bf7f334', @ProjectId, NULL, 7, 'VI-0007', '2024-04-01T00:00:00+00:00', 40095.46, 40095.46, 2, '2024-04-03T00:00:00+00:00', '2024-04-03T00:00:00+00:00', '2024-04-03T00:00:00+00:00', 0, 1),
        ('427fb2bab3c942c1a95234e38d7b393d', @ProjectId, NULL, 8, 'VI-0008', '2024-05-01T00:00:00+00:00', 107302.40, 107302.40, 2, '2024-05-01T00:00:00+00:00', '2024-05-01T00:00:00+00:00', '2024-05-01T00:00:00+00:00', 0, 1),
        ('f26b1ebc156646d2af8b7c5c996abfe5', @ProjectId, NULL, 9, 'VI-0009', '2024-05-01T00:00:00+00:00', 89588.09, 89588.09, 2, '2024-05-30T00:00:00+00:00', '2024-05-30T00:00:00+00:00', '2024-05-30T00:00:00+00:00', 0, 1),
        ('dccd091f68394fa8a893ce9559536a3f', @ProjectId, NULL, 10, 'VI-0010', '2024-06-01T00:00:00+00:00', 51929.85, 51929.85, 2, '2024-06-26T00:00:00+00:00', '2024-06-26T00:00:00+00:00', '2024-06-26T00:00:00+00:00', 0, 1),
        ('41cb7657441b4d6d93d0db1521c99938', @ProjectId, NULL, 11, 'VI-0011', '2024-07-01T00:00:00+00:00', 82907.93, 82907.93, 2, '2024-07-17T00:00:00+00:00', '2024-07-17T00:00:00+00:00', '2024-07-17T00:00:00+00:00', 0, 1),
        ('7ba9bb16e9b2493c91e50f489d42dbc7', @ProjectId, NULL, 12, 'VI-0012', '2024-08-01T00:00:00+00:00', 51657.01, 51657.01, 2, '2024-08-21T00:00:00+00:00', '2024-08-21T00:00:00+00:00', '2024-08-21T00:00:00+00:00', 0, 1),
        ('be9d2e915d05476fac7a6f7bdb85aaab', @ProjectId, NULL, 13, 'VI-0013', '2024-09-01T00:00:00+00:00', 29155.81, 29155.81, 2, '2024-09-18T00:00:00+00:00', '2024-09-18T00:00:00+00:00', '2024-09-18T00:00:00+00:00', 0, 1),
        ('810647437e0345b5be2cfb07e399752e', @ProjectId, NULL, 14, 'VI-0014', '2024-10-01T00:00:00+00:00', 55974.19, 55974.19, 2, '2024-10-16T00:00:00+00:00', '2024-10-16T00:00:00+00:00', '2024-10-16T00:00:00+00:00', 0, 1),
        ('1861969543c848c4aecbe3bfbdd01274', @ProjectId, NULL, 15, 'VI-0015', '2024-11-01T00:00:00+00:00', 54918.07, 54918.07, 2, '2024-11-13T00:00:00+00:00', '2024-11-13T00:00:00+00:00', '2024-11-13T00:00:00+00:00', 0, 1),
        ('6f19880bc5be46d58579e9c97f2eae2a', @ProjectId, NULL, 16, 'VI-0016', '2024-12-01T00:00:00+00:00', 24739.90, 24739.90, 2, '2024-12-11T00:00:00+00:00', '2024-12-11T00:00:00+00:00', '2024-12-11T00:00:00+00:00', 0, 1),
        ('6574fdf469a644f1993d17e132fe211c', @ProjectId, NULL, 17, 'VI-0017', '2025-02-01T00:00:00+00:00', 31435.96, 31435.96, 2, '2025-02-27T00:00:00+00:00', '2025-02-27T00:00:00+00:00', '2025-02-27T00:00:00+00:00', 0, 1),
        ('1e190f185c544237a23d2f58c74e2a27', @ProjectId, NULL, 18, 'VI-0018', '2025-06-01T00:00:00+00:00', 21233.42, 21233.42, 2, '2025-06-25T00:00:00+00:00', '2025-06-25T00:00:00+00:00', '2025-06-25T00:00:00+00:00', 0, 1),
        ('033b4cdfb291484c9c9699f7ede9bc56', @ProjectId, NULL, 19, 'VI-0019', '2025-09-01T00:00:00+00:00', 6108.96, 6108.96, 2, '2025-09-15T00:00:00+00:00', '2025-09-15T00:00:00+00:00', '2025-09-15T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('d2d56b3e90874b15ba298f44e437fe48', 'e76d242ef4cb4323ac58343862bda0a6', 8, '2023-10-18T00:00:00+00:00', 'Backfilled from Xero invoice INV-0040 — Coombelane - Valuation 1 - INS 737. Historic completed works (accounts export, Aug 2026).', 80306.35),
        ('bf3e1a7c209643649551af83c8423ebb', '1979ef5840fe42bc8bf132ad3e2e1f33', 8, '2023-11-15T00:00:00+00:00', 'Backfilled from Xero invoice INV-0044 — Coombelane - Valuation 2 - INS 737. Historic completed works (accounts export, Aug 2026).', 14305.09),
        ('d5f9858bea5c4f63b3cfa3b96fc1f2bc', '0121fcd8e1204566bcec967abd42698e', 8, '2023-12-13T00:00:00+00:00', 'Backfilled from Xero invoice INV-0047 — Coombelane - Valuation 3 - INS 737. Historic completed works (accounts export, Aug 2026).', 28847.89),
        ('436d4db41b9a4293857186aa0df65f2c', 'e189f39f79b248b9a788dd2d1ccf824f', 8, '2024-01-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0054 — Coombelane - Valuation 4 - INS 737. Historic completed works (accounts export, Aug 2026).', 17394.40),
        ('0d7c6c28599b4d1b9dda1acefd45ea5b', 'e5fcb451674c4c72886bf60c9c29e4f2', 8, '2024-02-07T00:00:00+00:00', 'Backfilled from Xero invoice INV-0061 — Coombelane - Valuation 5 - INS 737. Historic completed works (accounts export, Aug 2026).', 20589.11),
        ('26468164fa144f1eb5250ed7d9b19d3f', 'cbc478e6def242d5a3604d49d7c42aa2', 8, '2024-03-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0069 — Coombelane - Valuation 6 - INS 737. Historic completed works (accounts export, Aug 2026).', 25722.44),
        ('c05d948ab90b4620bce74501fbd618f4', '3c5f038961f24a1788df56ed5bf7f334', 8, '2024-04-03T00:00:00+00:00', 'Backfilled from Xero invoice INV-0075 — Coombelane - Valuation 7 - INS 737. Historic completed works (accounts export, Aug 2026).', 40095.46),
        ('98a10337336f4ac1b017bb4451ea0266', '427fb2bab3c942c1a95234e38d7b393d', 8, '2024-05-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0081 — Coombelane - Valuation 8 - INS 737. Historic completed works (accounts export, Aug 2026).', 107302.40),
        ('9e80fb5b420241ff9b1699b220ad38a2', 'f26b1ebc156646d2af8b7c5c996abfe5', 8, '2024-05-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0088 — Coombelane - Valuation 9 - INS 737. Historic completed works (accounts export, Aug 2026).', 89588.09),
        ('c6e5915550a743f69bc44c7af8892621', 'dccd091f68394fa8a893ce9559536a3f', 8, '2024-06-26T00:00:00+00:00', 'Backfilled from Xero invoice INV-0095 — Coombelane - Valuation 10 - INS 737. Historic completed works (accounts export, Aug 2026).', 51929.85),
        ('a6256a711b8141afbba2a2e0590cb6bd', '41cb7657441b4d6d93d0db1521c99938', 8, '2024-07-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0099 — Coombelane - Valuation 11 - INS 737. Historic completed works (accounts export, Aug 2026).', 82907.93),
        ('f872a98ec5e749b98319adf8fd1f1926', '7ba9bb16e9b2493c91e50f489d42dbc7', 8, '2024-08-21T00:00:00+00:00', 'Backfilled from Xero invoice INV-0104 — Coombelane - Valuation 12 - INS 737. Historic completed works (accounts export, Aug 2026).', 51657.01),
        ('00aec7a4b4794e9d9774493461cb78c8', 'be9d2e915d05476fac7a6f7bdb85aaab', 8, '2024-09-18T00:00:00+00:00', 'Backfilled from Xero invoice INV-0111 — Coombelane - Valuation 13 - INS 737. Historic completed works (accounts export, Aug 2026).', 29155.81),
        ('63b2be2834a042f899d6684806c75670', '810647437e0345b5be2cfb07e399752e', 8, '2024-10-16T00:00:00+00:00', 'Backfilled from Xero invoice INV-0115 — Coombelane - Valuation 14 - INS 737. Historic completed works (accounts export, Aug 2026).', 55974.19),
        ('045c7ad01c8248f88feed32c375534f0', '1861969543c848c4aecbe3bfbdd01274', 8, '2024-11-13T00:00:00+00:00', 'Backfilled from Xero invoice INV-0120 — Coombelane - Valuation 15 - INS 737. Historic completed works (accounts export, Aug 2026).', 54918.07),
        ('7ad83e17675748f688312b86ee51e950', '6f19880bc5be46d58579e9c97f2eae2a', 8, '2024-12-11T00:00:00+00:00', 'Backfilled from Xero invoice INV-0127 — Coombelane - Valuation 16 - INS 737. Historic completed works (accounts export, Aug 2026).', 24739.90),
        ('77ff699376654ff5a24cacb1bae2da84', '6574fdf469a644f1993d17e132fe211c', 8, '2025-02-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0137 — Coombelane - Valuation 17 - INS 737. Historic completed works (accounts export, Aug 2026).', 31435.96),
        ('802f1ecdea8541f48170fba57a5b966a', '1e190f185c544237a23d2f58c74e2a27', 8, '2025-06-25T00:00:00+00:00', 'Backfilled from Xero invoice INV-0159 — Coombelane - Valuation 18 - INS 737. Historic completed works (accounts export, Aug 2026).', 21233.42),
        ('02f82754d641442fafe48d4319149222', '033b4cdfb291484c9c9699f7ede9bc56', 8, '2025-09-15T00:00:00+00:00', 'Backfilled from Xero invoice INV-0173 — Coombelane - Valuation 19 - INS 737. Historic completed works (accounts export, Aug 2026).', 6108.96);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 834212.33 WHERE ProjectId = @ProjectId;
    PRINT 'OK    149a Coombe Lane West — 19 invoices backfilled, net 834,212.33 (paid 834,212.33).';
END
COMMIT;

GO
-- ===== 17a Abbot Road — 14 invoices, net 207,235.08, of which paid 189,340.88 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '17aabbotroad'
       OR LOWER(REPLACE(Name, ' ', '')) = '17aabbotroad'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '17aabbotroad' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  17a Abbot Road — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  17a Abbot Road — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  17a Abbot Road — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('fa05a4539cc04694b3d17ed81cb2f296', @ProjectId, NULL, 1, 'VI-0001', '2025-06-01T00:00:00+00:00', 16296.82, 16296.82, 2, '2025-06-27T00:00:00+00:00', '2025-06-27T00:00:00+00:00', '2025-06-27T00:00:00+00:00', 0, 1),
        ('34a052d86cc9441e9a6cdaef883a32a3', @ProjectId, NULL, 2, 'VI-0002', '2025-07-01T00:00:00+00:00', 2199.25, 2199.25, 2, '2025-07-25T00:00:00+00:00', '2025-07-25T00:00:00+00:00', '2025-07-25T00:00:00+00:00', 0, 1),
        ('ae73e7d589964a2c8440362a1db7d499', @ProjectId, NULL, 3, 'VI-0003', '2025-08-01T00:00:00+00:00', 12838.64, 12838.64, 2, '2025-08-21T00:00:00+00:00', '2025-08-21T00:00:00+00:00', '2025-08-21T00:00:00+00:00', 0, 1),
        ('76bbba30d65c499a8eff77c97e68fa8d', @ProjectId, NULL, 4, 'VI-0004', '2025-09-01T00:00:00+00:00', 11322.10, 11322.10, 2, '2025-09-16T00:00:00+00:00', '2025-09-16T00:00:00+00:00', '2025-09-16T00:00:00+00:00', 0, 1),
        ('76d73e98863d481c809eb41d77ed764f', @ProjectId, NULL, 5, 'VI-0005', '2025-10-01T00:00:00+00:00', 13194.17, 13194.17, 2, '2025-10-22T00:00:00+00:00', '2025-10-22T00:00:00+00:00', '2025-10-22T00:00:00+00:00', 0, 1),
        ('15a32fb1219648a29f51220037d59f91', @ProjectId, NULL, 6, 'VI-0006', '2025-11-01T00:00:00+00:00', 25172.24, 25172.24, 2, '2025-11-19T00:00:00+00:00', '2025-11-19T00:00:00+00:00', '2025-11-19T00:00:00+00:00', 0, 1),
        ('14c8aca1095a4bb494de4e2704125ad9', @ProjectId, NULL, 7, 'VI-0007', '2025-11-01T00:00:00+00:00', -0.08, -0.08, 2, '2025-11-20T00:00:00+00:00', '2025-11-20T00:00:00+00:00', '2025-11-20T00:00:00+00:00', 0, 1),
        ('cc9148f80dcf4e00a68d59c6a5662f6e', @ProjectId, NULL, 8, 'VI-0008', '2026-02-01T00:00:00+00:00', 21043.07, 21043.07, 2, '2026-02-04T00:00:00+00:00', '2026-02-04T00:00:00+00:00', '2026-02-04T00:00:00+00:00', 0, 1),
        ('c96e471fa7a84c9ab0f1226fe3f0c1fd', @ProjectId, NULL, 9, 'VI-0009', '2026-03-01T00:00:00+00:00', 22712.52, 22712.52, 2, '2026-03-06T00:00:00+00:00', '2026-03-06T00:00:00+00:00', '2026-03-06T00:00:00+00:00', 0, 1),
        ('a9f23741761f426582e10d898268ca45', @ProjectId, NULL, 10, 'VI-0010', '2026-04-01T00:00:00+00:00', 13771.20, 13771.20, 2, '2026-04-15T00:00:00+00:00', '2026-04-15T00:00:00+00:00', '2026-04-15T00:00:00+00:00', 0, 1),
        ('42e25711a3ef43fa8cec42380aba965a', @ProjectId, NULL, 11, 'VI-0011', '2026-05-01T00:00:00+00:00', 18211.50, 18211.50, 2, '2026-05-15T00:00:00+00:00', '2026-05-15T00:00:00+00:00', '2026-05-15T00:00:00+00:00', 0, 1),
        ('5da1d1e53a9543f4a814f18ff29c1edf', @ProjectId, NULL, 12, 'VI-0012', '2026-05-01T00:00:00+00:00', 17894.20, 0.00, 1, '2026-05-22T00:00:00+00:00', '2026-05-22T00:00:00+00:00', NULL, 0, 1),
        ('74af07bf03624939b722e9ba9fed890e', @ProjectId, NULL, 13, 'VI-0013', '2026-07-01T00:00:00+00:00', 14685.25, 14685.25, 2, '2026-07-08T00:00:00+00:00', '2026-07-08T00:00:00+00:00', '2026-07-08T00:00:00+00:00', 0, 1),
        ('712984defa4b45b09f01dcd5f3ff506d', @ProjectId, NULL, 14, 'VI-0014', '2026-07-01T00:00:00+00:00', 17894.20, 17894.20, 2, '2026-07-30T00:00:00+00:00', '2026-07-30T00:00:00+00:00', '2026-07-30T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('cbc95f24a64047d3a5629a5fd711c1de', 'fa05a4539cc04694b3d17ed81cb2f296', 8, '2025-06-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0160 — Abbot Road - Valuation 01. Historic completed works (accounts export, Aug 2026).', 16296.82),
        ('2c0d18ac7d08459192cd821c089dcf27', '34a052d86cc9441e9a6cdaef883a32a3', 8, '2025-07-25T00:00:00+00:00', 'Backfilled from Xero invoice INV-0167 — Abbot Road - Valuation 02. Historic completed works (accounts export, Aug 2026).', 2199.25),
        ('e3f6e19dd155415b856eb4b400859518', 'ae73e7d589964a2c8440362a1db7d499', 8, '2025-08-21T00:00:00+00:00', 'Backfilled from Xero invoice INV-0170 — Abbot Road - Valuation 03. Historic completed works (accounts export, Aug 2026).', 12838.64),
        ('1b32b541cc3943a2abbcea664866054d', '76bbba30d65c499a8eff77c97e68fa8d', 8, '2025-09-16T00:00:00+00:00', 'Backfilled from Xero invoice INV-0174 — Abbot Road - Valuation 04. Historic completed works (accounts export, Aug 2026).', 11322.10),
        ('38733018f39d438f9691ba189e1ce62c', '76d73e98863d481c809eb41d77ed764f', 8, '2025-10-22T00:00:00+00:00', 'Backfilled from Xero invoice INV-0178 — Abbot Road - Valuation 05. Historic completed works (accounts export, Aug 2026).', 13194.17),
        ('ba2af2e629f84a73a0e1208560496558', '15a32fb1219648a29f51220037d59f91', 8, '2025-11-19T00:00:00+00:00', 'Backfilled from Xero invoice INV-0182 — Abbot Road - Valuation 05. Historic completed works (accounts export, Aug 2026).', 25172.24),
        ('56940b223e0a438ca26d5a67f0b3197f', '14c8aca1095a4bb494de4e2704125ad9', 8, '2025-11-20T00:00:00+00:00', 'Backfilled from Xero credit note CN-0181 — Abbot Road - Valuation 02. Historic completed works (accounts export, Aug 2026).', -0.08),
        ('6946db193d324822b4343373819987b0', 'cc9148f80dcf4e00a68d59c6a5662f6e', 8, '2026-02-04T00:00:00+00:00', 'Backfilled from Xero invoice INV-0196 — Abbot Road - Valuation 07. Historic completed works (accounts export, Aug 2026).', 21043.07),
        ('a66aff0eb77b455e954347a4888957f0', 'c96e471fa7a84c9ab0f1226fe3f0c1fd', 8, '2026-03-06T00:00:00+00:00', 'Backfilled from Xero invoice INV-0200 — Abbot Road - Valuation 07. Historic completed works (accounts export, Aug 2026).', 22712.52),
        ('ee07e45b7b804adbaf7a3b080bbaa3ff', 'a9f23741761f426582e10d898268ca45', 8, '2026-04-15T00:00:00+00:00', 'Backfilled from Xero invoice INV-0204 — Abbot Road - Valuation 09. Historic completed works (accounts export, Aug 2026).', 13771.20),
        ('6c30720b1a7a4e90a4f55a0f6ce6bfa7', '42e25711a3ef43fa8cec42380aba965a', 8, '2026-05-15T00:00:00+00:00', 'Backfilled from Xero invoice INV-0208 — Abbot Road - Valuation 10. Historic completed works (accounts export, Aug 2026).', 18211.50),
        ('5c9812baf75348f6826cbe719893d4c6', '5da1d1e53a9543f4a814f18ff29c1edf', 8, '2026-05-22T00:00:00+00:00', 'Backfilled from Xero invoice INV-0209 — Abbot Road - Valuation 11. Historic completed works (accounts export, Aug 2026).', 17894.20),
        ('1d76aa68d6014a1a8e297cf5b4a1cc16', '74af07bf03624939b722e9ba9fed890e', 8, '2026-07-08T00:00:00+00:00', 'Backfilled from Xero invoice INV-0216 — Abbot Road - Valuation 12. Historic completed works (accounts export, Aug 2026).', 14685.25),
        ('51bb1601e9df4b7eb5505b4e524cd4d3', '712984defa4b45b09f01dcd5f3ff506d', 8, '2026-07-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0217 — Abbot Road - Valuation 13. Historic completed works (accounts export, Aug 2026).', 17894.20);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 189340.88 WHERE ProjectId = @ProjectId;
    PRINT 'OK    17a Abbot Road — 14 invoices backfilled, net 207,235.08 (paid 189,340.88).';
END
COMMIT;

GO
-- ===== 2 Albany Mews — 10 invoices, net 426,592.96, of which paid 426,592.96 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '2albanymews'
       OR LOWER(REPLACE(Name, ' ', '')) = '2albanymews'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '2albanymews' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  2 Albany Mews — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  2 Albany Mews — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  2 Albany Mews — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('6a85b95a059d41748714b3d4dd7546ca', @ProjectId, NULL, 1, 'VI-0001', '2025-07-01T00:00:00+00:00', 32608.99, 32608.99, 2, '2025-07-07T00:00:00+00:00', '2025-07-07T00:00:00+00:00', '2025-07-07T00:00:00+00:00', 0, 1),
        ('f47ac1a20ffc46168f483c21a956b550', @ProjectId, NULL, 2, 'VI-0002', '2025-08-01T00:00:00+00:00', 46353.29, 46353.29, 2, '2025-08-04T00:00:00+00:00', '2025-08-04T00:00:00+00:00', '2025-08-04T00:00:00+00:00', 0, 1),
        ('bbdd73af7edd43ea938dbabd209fe103', @ProjectId, NULL, 3, 'VI-0003', '2025-09-01T00:00:00+00:00', 37880.82, 37880.82, 2, '2025-09-02T00:00:00+00:00', '2025-09-02T00:00:00+00:00', '2025-09-02T00:00:00+00:00', 0, 1),
        ('f12c6778e59c4baab03eba9e3bffbe88', @ProjectId, NULL, 4, 'VI-0004', '2025-09-01T00:00:00+00:00', 52733.32, 52733.32, 2, '2025-09-30T00:00:00+00:00', '2025-09-30T00:00:00+00:00', '2025-09-30T00:00:00+00:00', 0, 1),
        ('3729d85315374badaf02202e153eb490', @ProjectId, NULL, 5, 'VI-0005', '2025-10-01T00:00:00+00:00', 60825.88, 60825.88, 2, '2025-10-30T00:00:00+00:00', '2025-10-30T00:00:00+00:00', '2025-10-30T00:00:00+00:00', 0, 1),
        ('0caa457a4e6f4426a124379be92fae79', @ProjectId, NULL, 6, 'VI-0006', '2025-11-01T00:00:00+00:00', 37320.99, 37320.99, 2, '2025-11-24T00:00:00+00:00', '2025-11-24T00:00:00+00:00', '2025-11-24T00:00:00+00:00', 0, 1),
        ('ad5be0a7affc4b9dbab48a8b47eec502', @ProjectId, NULL, 7, 'VI-0007', '2025-12-01T00:00:00+00:00', 40453.61, 40453.61, 2, '2025-12-18T00:00:00+00:00', '2025-12-18T00:00:00+00:00', '2025-12-18T00:00:00+00:00', 0, 1),
        ('3496d55062ed42a7bc13714c02939dbd', @ProjectId, NULL, 8, 'VI-0008', '2026-01-01T00:00:00+00:00', 27179.50, 27179.50, 2, '2026-01-20T00:00:00+00:00', '2026-01-20T00:00:00+00:00', '2026-01-20T00:00:00+00:00', 0, 1),
        ('ef352d6e8d894d1f9cfd571dcbd4f1fd', @ProjectId, NULL, 9, 'VI-0009', '2026-02-01T00:00:00+00:00', 38618.93, 38618.93, 2, '2026-02-19T00:00:00+00:00', '2026-02-19T00:00:00+00:00', '2026-02-19T00:00:00+00:00', 0, 1),
        ('d1ea82806e424a729fe37350bab71bf3', @ProjectId, NULL, 10, 'VI-0010', '2026-03-01T00:00:00+00:00', 52617.63, 52617.63, 2, '2026-03-26T00:00:00+00:00', '2026-03-26T00:00:00+00:00', '2026-03-26T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('57a1696c3e3c4196a76255c8f6be6085', '6a85b95a059d41748714b3d4dd7546ca', 8, '2025-07-07T00:00:00+00:00', 'Backfilled from Xero invoice INV-0164 — Valuation 01 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 32608.99),
        ('d2ed72e8b2b34a4082c8aff25eee3a88', 'f47ac1a20ffc46168f483c21a956b550', 8, '2025-08-04T00:00:00+00:00', 'Backfilled from Xero invoice INV-0168 — Valuation 02 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 46353.29),
        ('75fe680ecd2f43dab6aa7c167834c1df', 'bbdd73af7edd43ea938dbabd209fe103', 8, '2025-09-02T00:00:00+00:00', 'Backfilled from Xero invoice INV-0171 — Valuation 03 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 37880.82),
        ('4ee342ca04aa4cf8a924a1b548b8f1fb', 'f12c6778e59c4baab03eba9e3bffbe88', 8, '2025-09-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0176 — Valuation 04 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 52733.32),
        ('f2aefbe229d44ea0aa4d03a1ee39fa53', '3729d85315374badaf02202e153eb490', 8, '2025-10-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0179 — Valuation 5 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 60825.88),
        ('fe5fb15e7b444bd99c8b3d5671c8652b', '0caa457a4e6f4426a124379be92fae79', 8, '2025-11-24T00:00:00+00:00', 'Backfilled from Xero invoice INV-0183 — Valuation 6 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 37320.99),
        ('7f5e3554e2034b29ae2175047ac94262', 'ad5be0a7affc4b9dbab48a8b47eec502', 8, '2025-12-18T00:00:00+00:00', 'Backfilled from Xero invoice INV-0187 — Valuation 7 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 40453.61),
        ('da3828bee80e4b9f84a4e6fcffcc1132', '3496d55062ed42a7bc13714c02939dbd', 8, '2026-01-20T00:00:00+00:00', 'Backfilled from Xero invoice INV-0191 — Valuation 8 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 27179.50),
        ('5d923462328f4704a1bbe57bfff79751', 'ef352d6e8d894d1f9cfd571dcbd4f1fd', 8, '2026-02-19T00:00:00+00:00', 'Backfilled from Xero invoice INV-0198 — Valuation 9 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 38618.93),
        ('5c822daabf604381966a581e46b5f051', 'd1ea82806e424a729fe37350bab71bf3', 8, '2026-03-26T00:00:00+00:00', 'Backfilled from Xero invoice INV-0201 — Valuation 10 - 2 Albany Mews - PRO 131. Historic completed works (accounts export, Aug 2026).', 52617.63);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 426592.96 WHERE ProjectId = @ProjectId;
    PRINT 'OK    2 Albany Mews — 10 invoices backfilled, net 426,592.96 (paid 426,592.96).';
END
COMMIT;

GO
-- ===== 21 Chetwode Road — 8 invoices, net 135,677.97, of which paid 135,677.97 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '21chetwoderoad'
       OR LOWER(REPLACE(Name, ' ', '')) = '21chetwoderoad'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '21chetwoderoad' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  21 Chetwode Road — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  21 Chetwode Road — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  21 Chetwode Road — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('46c3ed1e934c468c8fc3f74922a944e3', @ProjectId, NULL, 1, 'VI-0001', '2023-09-01T00:00:00+00:00', 23541.56, 23541.56, 2, '2023-09-25T00:00:00+00:00', '2023-09-25T00:00:00+00:00', '2023-09-25T00:00:00+00:00', 0, 1),
        ('f8377ced3d864cb1b53e15718add9271', @ProjectId, NULL, 2, 'VI-0002', '2023-11-01T00:00:00+00:00', 14330.93, 14330.93, 2, '2023-11-13T00:00:00+00:00', '2023-11-13T00:00:00+00:00', '2023-11-13T00:00:00+00:00', 0, 1),
        ('3b9e5ff36c1f4449b17c4a11f98db5c6', @ProjectId, NULL, 3, 'VI-0003', '2024-01-01T00:00:00+00:00', 12768.74, 12768.74, 2, '2024-01-09T00:00:00+00:00', '2024-01-09T00:00:00+00:00', '2024-01-09T00:00:00+00:00', 0, 1),
        ('41e895f615884ada99d3153b3eabe69f', @ProjectId, NULL, 4, 'VI-0004', '2024-01-01T00:00:00+00:00', 12528.73, 12528.73, 2, '2024-01-25T00:00:00+00:00', '2024-01-25T00:00:00+00:00', '2024-01-25T00:00:00+00:00', 0, 1),
        ('370f47f61f6345f682d20821445dd133', @ProjectId, NULL, 5, 'VI-0005', '2024-02-01T00:00:00+00:00', 26307.55, 26307.55, 2, '2024-02-29T00:00:00+00:00', '2024-02-29T00:00:00+00:00', '2024-02-29T00:00:00+00:00', 0, 1),
        ('cd5d6de10d5f41c5bc30e932c5555d4a', @ProjectId, NULL, 6, 'VI-0006', '2024-03-01T00:00:00+00:00', 40650.46, 40650.46, 2, '2024-03-26T00:00:00+00:00', '2024-03-26T00:00:00+00:00', '2024-03-26T00:00:00+00:00', 0, 1),
        ('5adafceb358f4441aad1e058791d986a', @ProjectId, NULL, 7, 'VI-0007', '2024-06-01T00:00:00+00:00', 5200.00, 5200.00, 2, '2024-06-21T00:00:00+00:00', '2024-06-21T00:00:00+00:00', '2024-06-21T00:00:00+00:00', 0, 1),
        ('a0cfcd6c2cba4be5a2b8a59d7c05699f', @ProjectId, NULL, 8, 'VI-0008', '2025-01-01T00:00:00+00:00', 350.00, 350.00, 2, '2025-01-14T00:00:00+00:00', '2025-01-14T00:00:00+00:00', '2025-01-14T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('3ed97e42f15f402aae957aa3dee7e756', '46c3ed1e934c468c8fc3f74922a944e3', 8, '2023-09-25T00:00:00+00:00', 'Backfilled from Xero invoice INV-0037 — PROJECT: 21 Chetwode Road SW17 7RF. Historic completed works (accounts export, Aug 2026).', 23541.56),
        ('6caf7edeb8ef4da097ed79bd7ca64173', 'f8377ced3d864cb1b53e15718add9271', 8, '2023-11-13T00:00:00+00:00', 'Backfilled from Xero invoice INV-0042 — PROJECT: 21 Chetwode Road SW17 7RF. Historic completed works (accounts export, Aug 2026).', 14330.93),
        ('2943f183067d4eb185b79cc1cbb5cb1d', '3b9e5ff36c1f4449b17c4a11f98db5c6', 8, '2024-01-09T00:00:00+00:00', 'Backfilled from Xero invoice INV-0052 — PROJECT: 21 Chetwode Road SW17 7RF. Historic completed works (accounts export, Aug 2026).', 12768.74),
        ('e4de1cdd81094f9ea237a56aa74217d2', '41e895f615884ada99d3153b3eabe69f', 8, '2024-01-25T00:00:00+00:00', 'Backfilled from Xero invoice INV-0059 — PROJECT: 21 Chetwode Road SW17 7RF. Historic completed works (accounts export, Aug 2026).', 12528.73),
        ('1629c6b082fc4f49b6de7d9d8ecc32d0', '370f47f61f6345f682d20821445dd133', 8, '2024-02-29T00:00:00+00:00', 'Backfilled from Xero invoice INV-0066 — PROJECT: 21 Chetwode Road SW17 7RF. Historic completed works (accounts export, Aug 2026).', 26307.55),
        ('b3f9cf1131204ec3893939274b6e1415', 'cd5d6de10d5f41c5bc30e932c5555d4a', 8, '2024-03-26T00:00:00+00:00', 'Backfilled from Xero invoice INV-0072 — PROJECT: 21 Chetwode Road SW17 7RF. Historic completed works (accounts export, Aug 2026).', 40650.46),
        ('aa7d6d06b80949de9928ef2f5086b663', '5adafceb358f4441aad1e058791d986a', 8, '2024-06-21T00:00:00+00:00', 'Backfilled from Xero invoice INV-0091 — PROJECT: 21 Chetwode Road SW17 7RF. Historic completed works (accounts export, Aug 2026).', 5200.00),
        ('da42e6119a784fd3a75b18d01eb5cb24', 'a0cfcd6c2cba4be5a2b8a59d7c05699f', 8, '2025-01-14T00:00:00+00:00', 'Backfilled from Xero invoice INV-0130 — Steelo - Jewel BB Payment. Historic completed works (accounts export, Aug 2026).', 350.00);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 135677.97 WHERE ProjectId = @ProjectId;
    PRINT 'OK    21 Chetwode Road — 8 invoices backfilled, net 135,677.97 (paid 135,677.97).';
END
COMMIT;

GO
-- ===== 24 Sherwood Park SM1 2SQ — 18 invoices, net 660,656.74, of which paid 660,656.74 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '24sherwoodparksm12sq'
       OR LOWER(REPLACE(Name, ' ', '')) = '24sherwoodparksm12sq'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '24sherwoodparksm12sq' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  24 Sherwood Park SM1 2SQ — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  24 Sherwood Park SM1 2SQ — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  24 Sherwood Park SM1 2SQ — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('3d8f434ff7ae4ba5990a47240343f8cb', @ProjectId, NULL, 1, 'VI-0001', '2022-12-01T00:00:00+00:00', 53921.05, 53921.05, 2, '2022-12-14T00:00:00+00:00', '2022-12-14T00:00:00+00:00', '2022-12-14T00:00:00+00:00', 0, 1),
        ('533770ebfa4c4d77b394f67f5e307d67', @ProjectId, NULL, 2, 'VI-0002', '2023-01-01T00:00:00+00:00', 18245.70, 18245.70, 2, '2023-01-17T00:00:00+00:00', '2023-01-17T00:00:00+00:00', '2023-01-17T00:00:00+00:00', 0, 1),
        ('d466dd0d961d40a5b221621dda0ae219', @ProjectId, NULL, 3, 'VI-0003', '2023-02-01T00:00:00+00:00', 40401.60, 40401.60, 2, '2023-02-14T00:00:00+00:00', '2023-02-14T00:00:00+00:00', '2023-02-14T00:00:00+00:00', 0, 1),
        ('ff6e7a8d0b5d4922b5595ad1a396a98e', @ProjectId, NULL, 4, 'VI-0004', '2023-03-01T00:00:00+00:00', 37403.40, 37403.40, 2, '2023-03-10T00:00:00+00:00', '2023-03-10T00:00:00+00:00', '2023-03-10T00:00:00+00:00', 0, 1),
        ('00944dbcba69405684e7da279d676c37', @ProjectId, NULL, 5, 'VI-0005', '2023-04-01T00:00:00+00:00', 66694.07, 66694.07, 2, '2023-04-12T00:00:00+00:00', '2023-04-12T00:00:00+00:00', '2023-04-12T00:00:00+00:00', 0, 1),
        ('df39d020bc714e26914bd68f014aa54e', @ProjectId, NULL, 6, 'VI-0006', '2023-05-01T00:00:00+00:00', 53447.47, 53447.47, 2, '2023-05-09T00:00:00+00:00', '2023-05-09T00:00:00+00:00', '2023-05-09T00:00:00+00:00', 0, 1),
        ('f88c74f3fb444dfeba4441fa003d62be', @ProjectId, NULL, 7, 'VI-0007', '2023-06-01T00:00:00+00:00', 46083.08, 46083.08, 2, '2023-06-08T00:00:00+00:00', '2023-06-08T00:00:00+00:00', '2023-06-08T00:00:00+00:00', 0, 1),
        ('e94c22bedf5c4440a9f935f39434cb45', @ProjectId, NULL, 8, 'VI-0008', '2023-06-01T00:00:00+00:00', 38225.15, 38225.15, 2, '2023-06-28T00:00:00+00:00', '2023-06-28T00:00:00+00:00', '2023-06-28T00:00:00+00:00', 0, 1),
        ('690fa40449c046e3814ca6d1a2b86b11', @ProjectId, NULL, 9, 'VI-0009', '2023-07-01T00:00:00+00:00', 60167.77, 60167.77, 2, '2023-07-20T00:00:00+00:00', '2023-07-20T00:00:00+00:00', '2023-07-20T00:00:00+00:00', 0, 1),
        ('866d3c6e79594322a99ef2f441a4f835', @ProjectId, NULL, 10, 'VI-0010', '2023-08-01T00:00:00+00:00', 40862.51, 40862.51, 2, '2023-08-09T00:00:00+00:00', '2023-08-09T00:00:00+00:00', '2023-08-09T00:00:00+00:00', 0, 1),
        ('c9792f85f5b148d0b980a6c65b95586c', @ProjectId, NULL, 11, 'VI-0011', '2023-08-01T00:00:00+00:00', 28613.53, 28613.53, 2, '2023-08-23T00:00:00+00:00', '2023-08-23T00:00:00+00:00', '2023-08-23T00:00:00+00:00', 0, 1),
        ('77510bcd4b96453d974c748d3d7b7782', @ProjectId, NULL, 12, 'VI-0012', '2023-09-01T00:00:00+00:00', 60290.02, 60290.02, 2, '2023-09-20T00:00:00+00:00', '2023-09-20T00:00:00+00:00', '2023-09-20T00:00:00+00:00', 0, 1),
        ('76aa86c754e844e880683e29801d6404', @ProjectId, NULL, 13, 'VI-0013', '2023-10-01T00:00:00+00:00', 43198.53, 43198.53, 2, '2023-10-18T00:00:00+00:00', '2023-10-18T00:00:00+00:00', '2023-10-18T00:00:00+00:00', 0, 1),
        ('6272f27b970e41239b0ba6b1451e0ea5', @ProjectId, NULL, 14, 'VI-0014', '2023-11-01T00:00:00+00:00', 20972.32, 20972.32, 2, '2023-11-15T00:00:00+00:00', '2023-11-15T00:00:00+00:00', '2023-11-15T00:00:00+00:00', 0, 1),
        ('e841a0226c4945e1bc58efb7b057c29e', @ProjectId, NULL, 15, 'VI-0015', '2023-12-01T00:00:00+00:00', 8181.87, 8181.87, 2, '2023-12-12T00:00:00+00:00', '2023-12-12T00:00:00+00:00', '2023-12-12T00:00:00+00:00', 0, 1),
        ('c6abe9d2a271470d8d345a2d4e7b71b4', @ProjectId, NULL, 16, 'VI-0016', '2024-01-01T00:00:00+00:00', 9025.33, 9025.33, 2, '2024-01-16T00:00:00+00:00', '2024-01-16T00:00:00+00:00', '2024-01-16T00:00:00+00:00', 0, 1),
        ('af3dfbb8f132453c86e5bb94c628651c', @ProjectId, NULL, 17, 'VI-0017', '2024-01-01T00:00:00+00:00', 16466.67, 16466.67, 2, '2024-01-16T00:00:00+00:00', '2024-01-16T00:00:00+00:00', '2024-01-16T00:00:00+00:00', 0, 1),
        ('75675ddd860a44e3ad91ac0c2e53f020', @ProjectId, NULL, 18, 'VI-0018', '2025-02-01T00:00:00+00:00', 18456.67, 18456.67, 2, '2025-02-12T00:00:00+00:00', '2025-02-12T00:00:00+00:00', '2025-02-12T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('91f20cf412db429f9f74e533fc3683f7', '3d8f434ff7ae4ba5990a47240343f8cb', 8, '2022-12-14T00:00:00+00:00', 'Backfilled from Xero invoice INV-0001 — 24 Sherwood Park - Valuation 1. Historic completed works (accounts export, Aug 2026).', 53921.05),
        ('de18f2689b144ab49a73998d6436700f', '533770ebfa4c4d77b394f67f5e307d67', 8, '2023-01-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0003 — 24 Sherwood Park - Valuation 2. Historic completed works (accounts export, Aug 2026).', 18245.70),
        ('8a66c046322b4019a857dd6550b7e562', 'd466dd0d961d40a5b221621dda0ae219', 8, '2023-02-14T00:00:00+00:00', 'Backfilled from Xero invoice INV-0006 — 24 Sherwood Park - Valuation 3. Historic completed works (accounts export, Aug 2026).', 40401.60),
        ('fd7ddecbf9ac46849c99c77bd1e79a54', 'ff6e7a8d0b5d4922b5595ad1a396a98e', 8, '2023-03-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0012 — 24 Sherwood Park - Valuation 4. Historic completed works (accounts export, Aug 2026).', 37403.40),
        ('224a0e1b7ad346d189522f6bef062264', '00944dbcba69405684e7da279d676c37', 8, '2023-04-12T00:00:00+00:00', 'Backfilled from Xero invoice INV-0016 — 24 Sherwood Park - Valuation 5. Historic completed works (accounts export, Aug 2026).', 66694.07),
        ('34cd4f3eecfd466f8fa8f484c010d16c', 'df39d020bc714e26914bd68f014aa54e', 8, '2023-05-09T00:00:00+00:00', 'Backfilled from Xero invoice INV-0022 — 24 Sherwood Park - Valuation 6. Historic completed works (accounts export, Aug 2026).', 53447.47),
        ('ceed955fdc8e4c8fb17fba247f4b52f7', 'f88c74f3fb444dfeba4441fa003d62be', 8, '2023-06-08T00:00:00+00:00', 'Backfilled from Xero invoice INV-0025 — 24 Sherwood Park - Valuation 7. Historic completed works (accounts export, Aug 2026).', 46083.08),
        ('764fe919e24442e9a84f8bb69e26ddc7', 'e94c22bedf5c4440a9f935f39434cb45', 8, '2023-06-28T00:00:00+00:00', 'Backfilled from Xero invoice INV-0027 — 24 Sherwood Park - Valuation 8. Historic completed works (accounts export, Aug 2026).', 38225.15),
        ('4ba13a1786854b8c8a7de5b722237abe', '690fa40449c046e3814ca6d1a2b86b11', 8, '2023-07-20T00:00:00+00:00', 'Backfilled from Xero invoice INV-0029 — 24 Sherwood Park - Valuation 9. Historic completed works (accounts export, Aug 2026).', 60167.77),
        ('f17ea74937a8440789571eb1d824589d', '866d3c6e79594322a99ef2f441a4f835', 8, '2023-08-09T00:00:00+00:00', 'Backfilled from Xero invoice INV-0031 — 24 Sherwood Park - Valuation 10. Historic completed works (accounts export, Aug 2026).', 40862.51),
        ('61cb139bf8fb4dbe937c8f5547be9737', 'c9792f85f5b148d0b980a6c65b95586c', 8, '2023-08-23T00:00:00+00:00', 'Backfilled from Xero invoice INV-0033 — 24 Sherwood Park - Valuation 11. Historic completed works (accounts export, Aug 2026).', 28613.53),
        ('c68297acf5b0478fa84198b53e8ee511', '77510bcd4b96453d974c748d3d7b7782', 8, '2023-09-20T00:00:00+00:00', 'Backfilled from Xero invoice INV-0036 — 24 Sherwood Park - Valuation 12. Historic completed works (accounts export, Aug 2026).', 60290.02),
        ('dac0b3adb2264d0b908b98ec2f713b5e', '76aa86c754e844e880683e29801d6404', 8, '2023-10-18T00:00:00+00:00', 'Backfilled from Xero invoice INV-0038 — 24 Sherwood Park - Valuation 13. Historic completed works (accounts export, Aug 2026).', 43198.53),
        ('1c61ae03593c44ca8af3f36599a088dc', '6272f27b970e41239b0ba6b1451e0ea5', 8, '2023-11-15T00:00:00+00:00', 'Backfilled from Xero invoice INV-0043 — 24 Sherwood Park - Valuation 14. Historic completed works (accounts export, Aug 2026).', 20972.32),
        ('1768fbdf417146ab901ee46eb5c70d41', 'e841a0226c4945e1bc58efb7b057c29e', 8, '2023-12-12T00:00:00+00:00', 'Backfilled from Xero invoice INV-0051 — 24 Sherwood Park - Valuation 15. Historic completed works (accounts export, Aug 2026).', 8181.87),
        ('4bf14883d9044783903226d429c3ff6f', 'c6abe9d2a271470d8d345a2d4e7b71b4', 8, '2024-01-16T00:00:00+00:00', 'Backfilled from Xero invoice INV-0057 — 24 Sherwood Park - Final Account. Historic completed works (accounts export, Aug 2026).', 9025.33),
        ('ef81659a7dc54ca79c90cbe880438b96', 'af3dfbb8f132453c86e5bb94c628651c', 8, '2024-01-16T00:00:00+00:00', 'Backfilled from Xero invoice INV-0058 — 24 Sherwood Park - Retention Release. Historic completed works (accounts export, Aug 2026).', 16466.67),
        ('16de4033b1dd43efb856b0bc3eb21f55', '75675ddd860a44e3ad91ac0c2e53f020', 8, '2025-02-12T00:00:00+00:00', 'Backfilled from Xero invoice INV-0136 — 24 Sherwood Park - Final Retention Release. Historic completed works (accounts export, Aug 2026).', 18456.67);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 660656.74 WHERE ProjectId = @ProjectId;
    PRINT 'OK    24 Sherwood Park SM1 2SQ — 18 invoices backfilled, net 660,656.74 (paid 660,656.74).';
END
COMMIT;

GO
-- ===== 6 Forest Crescent — 10 invoices, net 218,124.50, of which paid 212,671.39 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent'
       OR LOWER(REPLACE(Name, ' ', '')) = '6forestcrescent'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  6 Forest Crescent — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  6 Forest Crescent — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  6 Forest Crescent — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('8b383397c8c0475a99005a6796e64f4b', @ProjectId, NULL, 1, 'VI-0001', '2024-09-01T00:00:00+00:00', 10473.75, 10473.75, 2, '2024-09-03T00:00:00+00:00', '2024-09-03T00:00:00+00:00', '2024-09-03T00:00:00+00:00', 0, 1),
        ('35f65791d4634fdd80c59cd36a3a7dbb', @ProjectId, NULL, 2, 'VI-0002', '2024-09-01T00:00:00+00:00', 23206.79, 23206.79, 2, '2024-09-30T00:00:00+00:00', '2024-09-30T00:00:00+00:00', '2024-09-30T00:00:00+00:00', 0, 1),
        ('27d7df93a6754afd90ff00c71142c0ec', @ProjectId, NULL, 3, 'VI-0003', '2024-10-01T00:00:00+00:00', 22668.14, 22668.14, 2, '2024-10-31T00:00:00+00:00', '2024-10-31T00:00:00+00:00', '2024-10-31T00:00:00+00:00', 0, 1),
        ('44edaaa605124b19979323824c943233', @ProjectId, NULL, 4, 'VI-0004', '2024-11-01T00:00:00+00:00', 32251.08, 32251.08, 2, '2024-11-29T00:00:00+00:00', '2024-11-29T00:00:00+00:00', '2024-11-29T00:00:00+00:00', 0, 1),
        ('73e66550662048f790dabe1f1dacb221', @ProjectId, NULL, 5, 'VI-0005', '2024-12-01T00:00:00+00:00', 21908.59, 21908.59, 2, '2024-12-17T00:00:00+00:00', '2024-12-17T00:00:00+00:00', '2024-12-17T00:00:00+00:00', 0, 1),
        ('1cf6a972fe5c442a8fb595441b14eb40', @ProjectId, NULL, 6, 'VI-0006', '2025-02-01T00:00:00+00:00', 46606.96, 46606.96, 2, '2025-02-07T00:00:00+00:00', '2025-02-07T00:00:00+00:00', '2025-02-07T00:00:00+00:00', 0, 1),
        ('d49852d3e936493dbedeb80bd601fb16', @ProjectId, NULL, 7, 'VI-0007', '2025-03-01T00:00:00+00:00', 30828.89, 30828.89, 2, '2025-03-10T00:00:00+00:00', '2025-03-10T00:00:00+00:00', '2025-03-10T00:00:00+00:00', 0, 1),
        ('edc514a743464dc9b92253c47b03f8a5', @ProjectId, NULL, 8, 'VI-0008', '2025-04-01T00:00:00+00:00', 23914.04, 23914.04, 2, '2025-04-14T00:00:00+00:00', '2025-04-14T00:00:00+00:00', '2025-04-14T00:00:00+00:00', 0, 1),
        ('c8807a00ac50422f92288acee44c0d73', @ProjectId, NULL, 9, 'VI-0009', '2025-05-01T00:00:00+00:00', 813.15, 813.15, 2, '2025-05-08T00:00:00+00:00', '2025-05-08T00:00:00+00:00', '2025-05-08T00:00:00+00:00', 0, 1),
        ('d1742faac6bb4499a51eb2f340301db2', @ProjectId, NULL, 10, 'VI-0010', '2025-12-01T00:00:00+00:00', 5453.11, 0.00, 1, '2025-12-19T00:00:00+00:00', '2025-12-19T00:00:00+00:00', NULL, 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('ee4153a552bd4dc08467cb11c6a055df', '8b383397c8c0475a99005a6796e64f4b', 8, '2024-09-03T00:00:00+00:00', 'Backfilled from Xero invoice INV-0108 — 6 Forest Crescent - Valuation 1. Historic completed works (accounts export, Aug 2026).', 10473.75),
        ('b3e8f24d0def4106ab357cfc033bebe5', '35f65791d4634fdd80c59cd36a3a7dbb', 8, '2024-09-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0113 — 6 Forest Crescent - Valuation 2. Historic completed works (accounts export, Aug 2026).', 23206.79),
        ('e901fb5f7cff447c8a4333a6a77d1911', '27d7df93a6754afd90ff00c71142c0ec', 8, '2024-10-31T00:00:00+00:00', 'Backfilled from Xero invoice INV-0118 — 6 Forest Crescent - Valuation 3. Historic completed works (accounts export, Aug 2026).', 22668.14),
        ('5c86f001607745e5903a217543730408', '44edaaa605124b19979323824c943233', 8, '2024-11-29T00:00:00+00:00', 'Backfilled from Xero invoice INV-0124 — 6 Forest Crescent - Valuation 4. Historic completed works (accounts export, Aug 2026).', 32251.08),
        ('3da88584e49a4406ad3e42092548cddc', '73e66550662048f790dabe1f1dacb221', 8, '2024-12-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0128 — 6 Forest Crescent - Valuation 5. Historic completed works (accounts export, Aug 2026).', 21908.59),
        ('00a596a45ba94faeaf596609fcbfe3ae', '1cf6a972fe5c442a8fb595441b14eb40', 8, '2025-02-07T00:00:00+00:00', 'Backfilled from Xero invoice INV-0133 — 6 Forest Crescent - Valuation 6. Historic completed works (accounts export, Aug 2026).', 46606.96),
        ('46643bf342a1402d801eae1c3b130b58', 'd49852d3e936493dbedeb80bd601fb16', 8, '2025-03-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0140 — 6 Forest Crescent - Valuation 7. Historic completed works (accounts export, Aug 2026).', 30828.89),
        ('e1f5a8e2c7db4ac486f34a8ac3ab7b14', 'edc514a743464dc9b92253c47b03f8a5', 8, '2025-04-14T00:00:00+00:00', 'Backfilled from Xero invoice INV-0146 — 6 Forest Crescent - Valuation 8. Historic completed works (accounts export, Aug 2026).', 23914.04),
        ('0dacdaec5d6b49239cd8dc0e8a73162b', 'c8807a00ac50422f92288acee44c0d73', 8, '2025-05-08T00:00:00+00:00', 'Backfilled from Xero invoice INV-0152 — 6 Forest Crescent - Valuation 9. Historic completed works (accounts export, Aug 2026).', 813.15),
        ('5a9556ab854d4caa833fb3f3b7e5f9aa', 'd1742faac6bb4499a51eb2f340301db2', 8, '2025-12-19T00:00:00+00:00', 'Backfilled from Xero invoice INV-0190 — 6 Forest Crescent - Valuation 10 Retention. Historic completed works (accounts export, Aug 2026).', 5453.11);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 212671.39 WHERE ProjectId = @ProjectId;
    PRINT 'OK    6 Forest Crescent — 10 invoices backfilled, net 218,124.50 (paid 212,671.39).';
END
COMMIT;

GO
-- ===== 64 Ravenswood Avenue — 4 invoices, net 103,439.74, of which paid 85,523.17 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '64ravenswoodavenue'
       OR LOWER(REPLACE(Name, ' ', '')) = '64ravenswoodavenue'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '64ravenswoodavenue' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  64 Ravenswood Avenue — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  64 Ravenswood Avenue — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  64 Ravenswood Avenue — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('c680224a994e4a33990abdbd63cde919', @ProjectId, NULL, 1, 'VI-0001', '2026-03-01T00:00:00+00:00', 52243.60, 52243.60, 2, '2026-03-01T00:00:00+00:00', '2026-03-01T00:00:00+00:00', '2026-03-01T00:00:00+00:00', 0, 1),
        ('63a8a73637694e1d9bd0e90bbb1ada8f', @ProjectId, NULL, 2, 'VI-0002', '2026-04-01T00:00:00+00:00', 13843.69, 13843.69, 2, '2026-04-30T00:00:00+00:00', '2026-04-30T00:00:00+00:00', '2026-04-30T00:00:00+00:00', 0, 1),
        ('b10cc44c57b34f2cb8ec5160acbfe57b', @ProjectId, NULL, 3, 'VI-0003', '2026-07-01T00:00:00+00:00', 19435.88, 19435.88, 2, '2026-07-08T00:00:00+00:00', '2026-07-08T00:00:00+00:00', '2026-07-08T00:00:00+00:00', 0, 1),
        ('7ff34d51f4c6462bbe2821a31a2147d5', @ProjectId, NULL, 4, 'VI-0004', '2026-08-01T00:00:00+00:00', 17916.57, 0.00, 1, '2026-08-01T00:00:00+00:00', '2026-08-01T00:00:00+00:00', NULL, 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('c440439d026143ab840ae30ca21969c2', 'c680224a994e4a33990abdbd63cde919', 8, '2026-03-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0193 — Deposit invoice. Historic completed works (accounts export, Aug 2026).', 52243.60),
        ('5058071050e34924aa8b157695fa9d91', '63a8a73637694e1d9bd0e90bbb1ada8f', 8, '2026-04-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0205 — Valuation 01. Historic completed works (accounts export, Aug 2026).', 13843.69),
        ('1b0dbb3b26d24473b3811323821acc77', 'b10cc44c57b34f2cb8ec5160acbfe57b', 8, '2026-07-08T00:00:00+00:00', 'Backfilled from Xero invoice INV-0214 — Valuation 02. Historic completed works (accounts export, Aug 2026).', 19435.88),
        ('4f77c3c96702407d820cb63b30c04aa6', '7ff34d51f4c6462bbe2821a31a2147d5', 8, '2026-08-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0218 — Valuation 03. Historic completed works (accounts export, Aug 2026).', 17916.57);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 85523.17 WHERE ProjectId = @ProjectId;
    PRINT 'OK    64 Ravenswood Avenue — 4 invoices backfilled, net 103,439.74 (paid 85,523.17).';
END
COMMIT;

GO
-- ===== 67 Beresford Road Sutton — 11 invoices, net 284,927.44, of which paid 284,927.44 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '67beresfordroadsutton'
       OR LOWER(REPLACE(Name, ' ', '')) = '67beresfordroadsutton'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '67beresfordroadsutton' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  67 Beresford Road Sutton — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  67 Beresford Road Sutton — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  67 Beresford Road Sutton — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('b7b322748acf4e6c8cffd7a5eecc65d6', @ProjectId, NULL, 1, 'VI-0001', '2023-11-01T00:00:00+00:00', 26450.40, 26450.40, 2, '2023-11-10T00:00:00+00:00', '2023-11-10T00:00:00+00:00', '2023-11-10T00:00:00+00:00', 0, 1),
        ('75721f5059f8449e96de73271d7cc10f', @ProjectId, NULL, 2, 'VI-0002', '2023-12-01T00:00:00+00:00', 10623.30, 10623.30, 2, '2023-12-18T00:00:00+00:00', '2023-12-18T00:00:00+00:00', '2023-12-18T00:00:00+00:00', 0, 1),
        ('19596714bba94b2a872fb352d382e8d6', @ProjectId, NULL, 3, 'VI-0003', '2024-01-01T00:00:00+00:00', 8082.09, 8082.09, 2, '2024-01-29T00:00:00+00:00', '2024-01-29T00:00:00+00:00', '2024-01-29T00:00:00+00:00', 0, 1),
        ('5330106f7a5b4cd3adda1226768b19cf', @ProjectId, NULL, 4, 'VI-0004', '2024-02-01T00:00:00+00:00', 34677.24, 34677.24, 2, '2024-02-26T00:00:00+00:00', '2024-02-26T00:00:00+00:00', '2024-02-26T00:00:00+00:00', 0, 1),
        ('38066567644645d4a3e704dd6b28b20d', @ProjectId, NULL, 5, 'VI-0005', '2024-03-01T00:00:00+00:00', 45976.66, 45976.66, 2, '2024-03-26T00:00:00+00:00', '2024-03-26T00:00:00+00:00', '2024-03-26T00:00:00+00:00', 0, 1),
        ('b07e151525104ec4b405d41026ac9a11', @ProjectId, NULL, 6, 'VI-0006', '2024-04-01T00:00:00+00:00', 58699.47, 58699.47, 2, '2024-04-26T00:00:00+00:00', '2024-04-26T00:00:00+00:00', '2024-04-26T00:00:00+00:00', 0, 1),
        ('063901cb97bc4da093d6f27cdf17fe82', @ProjectId, NULL, 7, 'VI-0007', '2024-05-01T00:00:00+00:00', 46109.23, 46109.23, 2, '2024-05-29T00:00:00+00:00', '2024-05-29T00:00:00+00:00', '2024-05-29T00:00:00+00:00', 0, 1),
        ('2acb2f9612234a65a12b472f0d5b65f2', @ProjectId, NULL, 8, 'VI-0008', '2024-06-01T00:00:00+00:00', 32221.41, 32221.41, 2, '2024-06-24T00:00:00+00:00', '2024-06-24T00:00:00+00:00', '2024-06-24T00:00:00+00:00', 0, 1),
        ('49ac70f0d5be4306aab55400a00799d1', @ProjectId, NULL, 9, 'VI-0009', '2024-07-01T00:00:00+00:00', 17483.86, 17483.86, 2, '2024-07-24T00:00:00+00:00', '2024-07-24T00:00:00+00:00', '2024-07-24T00:00:00+00:00', 0, 1),
        ('12ced6cb867d4e7d8ce83f94bca4d5e4', @ProjectId, NULL, 10, 'VI-0010', '2025-03-01T00:00:00+00:00', 5853.78, 5853.78, 2, '2025-03-03T00:00:00+00:00', '2025-03-03T00:00:00+00:00', '2025-03-03T00:00:00+00:00', 0, 1),
        ('78ea38befa424c5d9fb8eff362c007bc', @ProjectId, NULL, 11, 'VI-0011', '2026-05-01T00:00:00+00:00', -1250.00, -1250.00, 2, '2026-05-31T00:00:00+00:00', '2026-05-31T00:00:00+00:00', '2026-05-31T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('8cae178eb2074550b46970661562a801', 'b7b322748acf4e6c8cffd7a5eecc65d6', 8, '2023-11-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0041 — 67 Beresford 10% deposit. Historic completed works (accounts export, Aug 2026).', 26450.40),
        ('bdd5593d3f914332915e9f15bdd06395', '75721f5059f8449e96de73271d7cc10f', 8, '2023-12-18T00:00:00+00:00', 'Backfilled from Xero invoice INV-0048 — 67 Beresford - 1st Valuation. Historic completed works (accounts export, Aug 2026).', 10623.30),
        ('2d57cba12fe2416093e42158dc8d4da8', '19596714bba94b2a872fb352d382e8d6', 8, '2024-01-29T00:00:00+00:00', 'Backfilled from Xero invoice INV-0060 — 67 Beresford - 2nd Valuation. Historic completed works (accounts export, Aug 2026).', 8082.09),
        ('0b67f74a4a1c4842a5d5a6103d577d71', '5330106f7a5b4cd3adda1226768b19cf', 8, '2024-02-26T00:00:00+00:00', 'Backfilled from Xero invoice INV-0064 — 67 Beresford - 3rd Valuation. Historic completed works (accounts export, Aug 2026).', 34677.24),
        ('58b68f4cdd5548f9a2d0bd51397f35b2', '38066567644645d4a3e704dd6b28b20d', 8, '2024-03-26T00:00:00+00:00', 'Backfilled from Xero invoice INV-0073 — 67 Beresford - 4th Valuation. Historic completed works (accounts export, Aug 2026).', 45976.66),
        ('e1761399b8da44a8868afe24f15fbe25', 'b07e151525104ec4b405d41026ac9a11', 8, '2024-04-26T00:00:00+00:00', 'Backfilled from Xero invoice INV-0077 — 67 Beresford - 5th Valuation. Historic completed works (accounts export, Aug 2026).', 58699.47),
        ('313a9265817b495f8db10c841127f0e3', '063901cb97bc4da093d6f27cdf17fe82', 8, '2024-05-29T00:00:00+00:00', 'Backfilled from Xero invoice INV-0084 — 67 Beresford - 6th Valuation. Historic completed works (accounts export, Aug 2026).', 46109.23),
        ('5d71bad13ba24f6ba76e4d0c5a9c56c1', '2acb2f9612234a65a12b472f0d5b65f2', 8, '2024-06-24T00:00:00+00:00', 'Backfilled from Xero invoice INV-0092 — 67 Beresford - 7th Valuation. Historic completed works (accounts export, Aug 2026).', 32221.41),
        ('dabef6fb92d04c8ebeb43a2c3da3ec01', '49ac70f0d5be4306aab55400a00799d1', 8, '2024-07-24T00:00:00+00:00', 'Backfilled from Xero invoice INV-0100 — 67 Beresford - 8th Valuation. Historic completed works (accounts export, Aug 2026).', 17483.86),
        ('602e3471f88c4aefa9a94b57aab7c319', '12ced6cb867d4e7d8ce83f94bca4d5e4', 8, '2025-03-03T00:00:00+00:00', 'Backfilled from Xero invoice INV-0138 — 67 Beresford - 9th Valuation Revised. Historic completed works (accounts export, Aug 2026).', 5853.78),
        ('be20a3bacda64c1db4465fbe754bafc9', '78ea38befa424c5d9fb8eff362c007bc', 8, '2026-05-31T00:00:00+00:00', 'Backfilled from Xero credit note CN-0210 — 67 Beresford - 9th Valuation Revised. Historic completed works (accounts export, Aug 2026).', -1250.00);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 284927.44 WHERE ProjectId = @ProjectId;
    PRINT 'OK    67 Beresford Road Sutton — 11 invoices backfilled, net 284,927.44 (paid 284,927.44).';
END
COMMIT;

GO
-- ===== 72 Montagu Road — 13 invoices, net 338,988.76, of which paid 338,988.76 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '72montaguroad'
       OR LOWER(REPLACE(Name, ' ', '')) = '72montaguroad'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '72montaguroad' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  72 Montagu Road — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  72 Montagu Road — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  72 Montagu Road — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('9bd260eea5534ce2be74b8cac47cf3a8', @ProjectId, NULL, 1, 'VI-0001', '2024-07-01T00:00:00+00:00', 33933.05, 33933.05, 2, '2024-07-24T00:00:00+00:00', '2024-07-24T00:00:00+00:00', '2024-07-24T00:00:00+00:00', 0, 1),
        ('3f01688ad5af4ca79edd92d8853a1e39', @ProjectId, NULL, 2, 'VI-0002', '2024-08-01T00:00:00+00:00', 10317.00, 10317.00, 2, '2024-08-28T00:00:00+00:00', '2024-08-28T00:00:00+00:00', '2024-08-28T00:00:00+00:00', 0, 1),
        ('25001beca18242d092f0eb9e46460b19', @ProjectId, NULL, 3, 'VI-0003', '2024-09-01T00:00:00+00:00', 4802.25, 4802.25, 2, '2024-09-25T00:00:00+00:00', '2024-09-25T00:00:00+00:00', '2024-09-25T00:00:00+00:00', 0, 1),
        ('4a1a5c88932645e89686194850b40a1f', @ProjectId, NULL, 4, 'VI-0004', '2024-11-01T00:00:00+00:00', 61463.39, 61463.39, 2, '2024-11-18T00:00:00+00:00', '2024-11-18T00:00:00+00:00', '2024-11-18T00:00:00+00:00', 0, 1),
        ('aaccb0c2832c4ef6a3219e2d11ae73d6', @ProjectId, NULL, 5, 'VI-0005', '2024-12-01T00:00:00+00:00', 18962.00, 18962.00, 2, '2024-12-16T00:00:00+00:00', '2024-12-16T00:00:00+00:00', '2024-12-16T00:00:00+00:00', 0, 1),
        ('9939b8306acb422794c9a321393e6e5f', @ProjectId, NULL, 6, 'VI-0006', '2025-01-01T00:00:00+00:00', 29461.87, 29461.87, 2, '2025-01-21T00:00:00+00:00', '2025-01-21T00:00:00+00:00', '2025-01-21T00:00:00+00:00', 0, 1),
        ('03160447bc6047689570534cdcfdb53c', @ProjectId, NULL, 7, 'VI-0007', '2025-02-01T00:00:00+00:00', 16407.35, 16407.35, 2, '2025-02-05T00:00:00+00:00', '2025-02-05T00:00:00+00:00', '2025-02-05T00:00:00+00:00', 0, 1),
        ('00f02e00af38406bb5d464d3fd89e952', @ProjectId, NULL, 8, 'VI-0008', '2025-03-01T00:00:00+00:00', 36538.10, 36538.10, 2, '2025-03-05T00:00:00+00:00', '2025-03-05T00:00:00+00:00', '2025-03-05T00:00:00+00:00', 0, 1),
        ('207d9bc5451e4453a655713d48e1596b', @ProjectId, NULL, 9, 'VI-0009', '2025-04-01T00:00:00+00:00', 21379.84, 21379.84, 2, '2025-04-02T00:00:00+00:00', '2025-04-02T00:00:00+00:00', '2025-04-02T00:00:00+00:00', 0, 1),
        ('85e7293425704531ace94ff8433fe042', @ProjectId, NULL, 10, 'VI-0010', '2025-04-01T00:00:00+00:00', 10959.11, 10959.11, 2, '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', 0, 1),
        ('5476c14c33484c0487eed4b524e4acf3', @ProjectId, NULL, 11, 'VI-0011', '2025-06-01T00:00:00+00:00', 46768.40, 46768.40, 2, '2025-06-13T00:00:00+00:00', '2025-06-13T00:00:00+00:00', '2025-06-13T00:00:00+00:00', 0, 1),
        ('10a3509a2de747f1b340eba6ed002b3c', @ProjectId, NULL, 12, 'VI-0012', '2025-07-01T00:00:00+00:00', 17608.44, 17608.44, 2, '2025-07-11T00:00:00+00:00', '2025-07-11T00:00:00+00:00', '2025-07-11T00:00:00+00:00', 0, 1),
        ('454bab2da2594b13bc69efc4233ed857', @ProjectId, NULL, 13, 'VI-0013', '2025-09-01T00:00:00+00:00', 30387.96, 30387.96, 2, '2025-09-09T00:00:00+00:00', '2025-09-09T00:00:00+00:00', '2025-09-09T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('bea108e4221e4fa988e87613b1a3cbb8', '9bd260eea5534ce2be74b8cac47cf3a8', 8, '2024-07-24T00:00:00+00:00', 'Backfilled from Xero invoice INV-0102 — 72 Montagu Road Valuation 01. Historic completed works (accounts export, Aug 2026).', 33933.05),
        ('dbe0d6e48c884cea9df7bcd3e6390676', '3f01688ad5af4ca79edd92d8853a1e39', 8, '2024-08-28T00:00:00+00:00', 'Backfilled from Xero invoice INV-0107 — 72 Montagu Road Valuation 02. Historic completed works (accounts export, Aug 2026).', 10317.00),
        ('35ded2bbab6d4489af8cc66badb70824', '25001beca18242d092f0eb9e46460b19', 8, '2024-09-25T00:00:00+00:00', 'Backfilled from Xero invoice INV-0114 — 72 Montagu Road Valuation 03. Historic completed works (accounts export, Aug 2026).', 4802.25),
        ('6a0a9ba38ebf414fa5d6fed0e210af69', '4a1a5c88932645e89686194850b40a1f', 8, '2024-11-18T00:00:00+00:00', 'Backfilled from Xero invoice INV-0121 — 72 Montagu Road Valuation 04. Historic completed works (accounts export, Aug 2026).', 61463.39),
        ('43c845f9a2514cdf9150362173ce76a5', 'aaccb0c2832c4ef6a3219e2d11ae73d6', 8, '2024-12-16T00:00:00+00:00', 'Backfilled from Xero invoice INV-0125 — 72 Montagu Road Valuation 05. Historic completed works (accounts export, Aug 2026).', 18962.00),
        ('cee60f2a251140d997e7a856f865fd3a', '9939b8306acb422794c9a321393e6e5f', 8, '2025-01-21T00:00:00+00:00', 'Backfilled from Xero invoice INV-0131 — 72 Montagu Road Valuation 06. Historic completed works (accounts export, Aug 2026).', 29461.87),
        ('a540a62540a1423a9e67cc881306aa6a', '03160447bc6047689570534cdcfdb53c', 8, '2025-02-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0134 — 72 Montagu Road Valuation 07. Historic completed works (accounts export, Aug 2026).', 16407.35),
        ('bb7d3a84d7d6499abff9da7a5afe5de8', '00f02e00af38406bb5d464d3fd89e952', 8, '2025-03-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0141 — 72 Montagu Road Valuation 08. Historic completed works (accounts export, Aug 2026).', 36538.10),
        ('e29143cf16dc4c7583d8cc5d6fd4090e', '207d9bc5451e4453a655713d48e1596b', 8, '2025-04-02T00:00:00+00:00', 'Backfilled from Xero invoice INV-0145 — 72 Montagu Road Valuation 09. Historic completed works (accounts export, Aug 2026).', 21379.84),
        ('c43e4db1295c4ecb867372ae59d21404', '85e7293425704531ace94ff8433fe042', 8, '2025-04-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0153 — 72 Montagu Road Valuation 10. Historic completed works (accounts export, Aug 2026).', 10959.11),
        ('a8bd703bb1ef4da5b71bd9e116296081', '5476c14c33484c0487eed4b524e4acf3', 8, '2025-06-13T00:00:00+00:00', 'Backfilled from Xero invoice INV-0163 — 72 Montagu Road Valuation 11. Historic completed works (accounts export, Aug 2026).', 46768.40),
        ('849f7706f27d496ba88a472e023e8fcd', '10a3509a2de747f1b340eba6ed002b3c', 8, '2025-07-11T00:00:00+00:00', 'Backfilled from Xero invoice INV-0166 — 72 Montagu Road Valuation 12. Historic completed works (accounts export, Aug 2026).', 17608.44),
        ('6774c509c71847d1a68f2376c523638f', '454bab2da2594b13bc69efc4233ed857', 8, '2025-09-09T00:00:00+00:00', 'Backfilled from Xero invoice INV-0172 — 72 Montagu Road Final Valuation. Historic completed works (accounts export, Aug 2026).', 30387.96);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 338988.76 WHERE ProjectId = @ProjectId;
    PRINT 'OK    72 Montagu Road — 13 invoices backfilled, net 338,988.76 (paid 338,988.76).';
END
COMMIT;

GO
-- ===== By France — 26 invoices, net 1,623,271.80, of which paid 1,623,271.80 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'byfrance'
       OR LOWER(REPLACE(Name, ' ', '')) = 'byfrance'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'byfrance' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  By France — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  By France — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  By France — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('0abe5dafaf344c0d9aab6fe300044701', @ProjectId, NULL, 1, 'VI-0001', '2024-06-01T00:00:00+00:00', 33827.77, 33827.77, 2, '2024-06-05T00:00:00+00:00', '2024-06-05T00:00:00+00:00', '2024-06-05T00:00:00+00:00', 0, 1),
        ('47a4c293b4954ebdbf6e446ac4273af0', @ProjectId, NULL, 2, 'VI-0002', '2024-07-01T00:00:00+00:00', 17354.39, 17354.39, 2, '2024-07-02T00:00:00+00:00', '2024-07-02T00:00:00+00:00', '2024-07-02T00:00:00+00:00', 0, 1),
        ('f4aaf259d8c144b9a5db88e7bc6de553', @ProjectId, NULL, 3, 'VI-0003', '2024-07-01T00:00:00+00:00', 5585.53, 5585.53, 2, '2024-07-30T00:00:00+00:00', '2024-07-30T00:00:00+00:00', '2024-07-30T00:00:00+00:00', 0, 1),
        ('fcbbf7b3b181482f893b96fd72818dc7', @ProjectId, NULL, 4, 'VI-0004', '2024-08-01T00:00:00+00:00', 1529.50, 1529.50, 2, '2024-08-29T00:00:00+00:00', '2024-08-29T00:00:00+00:00', '2024-08-29T00:00:00+00:00', 0, 1),
        ('2a3c3d5de7c34daaa571afc462b32ef4', @ProjectId, NULL, 5, 'VI-0005', '2024-09-01T00:00:00+00:00', 1292.00, 1292.00, 2, '2024-09-24T00:00:00+00:00', '2024-09-24T00:00:00+00:00', '2024-09-24T00:00:00+00:00', 0, 1),
        ('c39365e0160a47bdb9e087829718bd42', @ProjectId, NULL, 6, 'VI-0006', '2024-10-01T00:00:00+00:00', 1292.00, 1292.00, 2, '2024-10-22T00:00:00+00:00', '2024-10-22T00:00:00+00:00', '2024-10-22T00:00:00+00:00', 0, 1),
        ('2aa2a28bb69340c4a8e95da8cb7839eb', @ProjectId, NULL, 7, 'VI-0007', '2024-11-01T00:00:00+00:00', 1292.00, 1292.00, 2, '2024-11-21T00:00:00+00:00', '2024-11-21T00:00:00+00:00', '2024-11-21T00:00:00+00:00', 0, 1),
        ('e7b73008b2ab453a8149b3f67e52867b', @ProjectId, NULL, 8, 'VI-0008', '2024-12-01T00:00:00+00:00', 30668.83, 30668.83, 2, '2024-12-13T00:00:00+00:00', '2024-12-13T00:00:00+00:00', '2024-12-13T00:00:00+00:00', 0, 1),
        ('adef310da9324b9bbf1d122066e8ed4f', @ProjectId, NULL, 9, 'VI-0009', '2025-01-01T00:00:00+00:00', 138168.47, 138168.47, 2, '2025-01-28T00:00:00+00:00', '2025-01-28T00:00:00+00:00', '2025-01-28T00:00:00+00:00', 0, 1),
        ('3584d551c21a47669803dabadb7cb0f4', @ProjectId, NULL, 10, 'VI-0010', '2025-02-01T00:00:00+00:00', 112423.76, 112423.76, 2, '2025-02-17T00:00:00+00:00', '2025-02-17T00:00:00+00:00', '2025-02-17T00:00:00+00:00', 0, 1),
        ('47250aa71b6944978d50188ab78336ef', @ProjectId, NULL, 11, 'VI-0011', '2025-03-01T00:00:00+00:00', 133564.59, 133564.59, 2, '2025-03-17T00:00:00+00:00', '2025-03-17T00:00:00+00:00', '2025-03-17T00:00:00+00:00', 0, 1),
        ('e9bc752539614f769e52d8f3b13686ed', @ProjectId, NULL, 12, 'VI-0012', '2025-04-01T00:00:00+00:00', 78973.50, 78973.50, 2, '2025-04-16T00:00:00+00:00', '2025-04-16T00:00:00+00:00', '2025-04-16T00:00:00+00:00', 0, 1),
        ('ee4cfa9deb8f46368947a2bf86b328b7', @ProjectId, NULL, 13, 'VI-0013', '2025-05-01T00:00:00+00:00', 48260.00, 48260.00, 2, '2025-05-22T00:00:00+00:00', '2025-05-22T00:00:00+00:00', '2025-05-22T00:00:00+00:00', 0, 1),
        ('fea00a208e8945b9a0c4cbc3a2da3b63', @ProjectId, NULL, 14, 'VI-0014', '2025-06-01T00:00:00+00:00', 72006.25, 72006.25, 2, '2025-06-11T00:00:00+00:00', '2025-06-11T00:00:00+00:00', '2025-06-11T00:00:00+00:00', 0, 1),
        ('503107d73c2546e2a5715d1e773203e4', @ProjectId, NULL, 15, 'VI-0015', '2025-07-01T00:00:00+00:00', 70076.37, 70076.37, 2, '2025-07-09T00:00:00+00:00', '2025-07-09T00:00:00+00:00', '2025-07-09T00:00:00+00:00', 0, 1),
        ('343322b05a8f431186b2b054648fea16', @ProjectId, NULL, 16, 'VI-0016', '2025-08-01T00:00:00+00:00', 85333.41, 85333.41, 2, '2025-08-19T00:00:00+00:00', '2025-08-19T00:00:00+00:00', '2025-08-19T00:00:00+00:00', 0, 1),
        ('c53f5810a0d04b4b83125068c204afb7', @ProjectId, NULL, 17, 'VI-0017', '2025-09-01T00:00:00+00:00', 59364.61, 59364.61, 2, '2025-09-17T00:00:00+00:00', '2025-09-17T00:00:00+00:00', '2025-09-17T00:00:00+00:00', 0, 1),
        ('fa7792da72b9437eb62cc29a92fac5d4', @ProjectId, NULL, 18, 'VI-0018', '2025-10-01T00:00:00+00:00', 120471.84, 120471.84, 2, '2025-10-17T00:00:00+00:00', '2025-10-17T00:00:00+00:00', '2025-10-17T00:00:00+00:00', 0, 1),
        ('d899b1cb27e74d7ba064f1fc50499a79', @ProjectId, NULL, 19, 'VI-0019', '2025-11-01T00:00:00+00:00', 122156.26, 122156.26, 2, '2025-11-27T00:00:00+00:00', '2025-11-27T00:00:00+00:00', '2025-11-27T00:00:00+00:00', 0, 1),
        ('ee72107c720349d196b93b3794e64d13', @ProjectId, NULL, 20, 'VI-0020', '2025-12-01T00:00:00+00:00', 57242.78, 57242.78, 2, '2025-12-17T00:00:00+00:00', '2025-12-17T00:00:00+00:00', '2025-12-17T00:00:00+00:00', 0, 1),
        ('c899013a60df4411ba63a421f5216866', @ProjectId, NULL, 21, 'VI-0021', '2026-01-01T00:00:00+00:00', 116056.71, 116056.71, 2, '2026-01-23T00:00:00+00:00', '2026-01-23T00:00:00+00:00', '2026-01-23T00:00:00+00:00', 0, 1),
        ('425426e3c4ae4c998f4588958d884998', @ProjectId, NULL, 22, 'VI-0022', '2026-02-01T00:00:00+00:00', 59719.42, 59719.42, 2, '2026-02-17T00:00:00+00:00', '2026-02-17T00:00:00+00:00', '2026-02-17T00:00:00+00:00', 0, 1),
        ('84607dbad735429c9d7eca9585b927fd', @ProjectId, NULL, 23, 'VI-0023', '2026-04-01T00:00:00+00:00', 35710.02, 35710.02, 2, '2026-04-02T00:00:00+00:00', '2026-04-02T00:00:00+00:00', '2026-04-02T00:00:00+00:00', 0, 1),
        ('c57b2f6c4baf41d9b54619fcefedd3c1', @ProjectId, NULL, 24, 'VI-0024', '2026-05-01T00:00:00+00:00', 72865.09, 72865.09, 2, '2026-05-01T00:00:00+00:00', '2026-05-01T00:00:00+00:00', '2026-05-01T00:00:00+00:00', 0, 1),
        ('ff40ad3436814f0ab5bb6f935ae19555', @ProjectId, NULL, 25, 'VI-0025', '2026-06-01T00:00:00+00:00', 97425.76, 97425.76, 2, '2026-06-03T00:00:00+00:00', '2026-06-03T00:00:00+00:00', '2026-06-03T00:00:00+00:00', 0, 1),
        ('8aeefbfbfaed484f879ff3734bfd3bc1', @ProjectId, NULL, 26, 'VI-0026', '2026-07-01T00:00:00+00:00', 50610.94, 50610.94, 2, '2026-07-01T00:00:00+00:00', '2026-07-01T00:00:00+00:00', '2026-07-01T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('940094a743f643f8860bd41d6fd12bfd', '0abe5dafaf344c0d9aab6fe300044701', 8, '2024-06-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0090 — By France - Valuation 1 - PRO-064. Historic completed works (accounts export, Aug 2026).', 33827.77),
        ('50767e1201b247a2b88b2acb7164764f', '47a4c293b4954ebdbf6e446ac4273af0', 8, '2024-07-02T00:00:00+00:00', 'Backfilled from Xero invoice INV-0094 — By France - Valuation 2 - PRO-064. Historic completed works (accounts export, Aug 2026).', 17354.39),
        ('feb1910065ce4006a2f474d72e78a905', 'f4aaf259d8c144b9a5db88e7bc6de553', 8, '2024-07-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0103 — By France - Valuation 3 - PRO-064. Historic completed works (accounts export, Aug 2026).', 5585.53),
        ('6f834ca73f484767899bdbf6bbb2fdc9', 'fcbbf7b3b181482f893b96fd72818dc7', 8, '2024-08-29T00:00:00+00:00', 'Backfilled from Xero invoice INV-0106 — By France - Valuation 4 - PRO-064. Historic completed works (accounts export, Aug 2026).', 1529.50),
        ('f806221904fb43d1ae875ea6f61c1ec8', '2a3c3d5de7c34daaa571afc462b32ef4', 8, '2024-09-24T00:00:00+00:00', 'Backfilled from Xero invoice INV-0112 — By France - Valuation 5 - PRO-064. Historic completed works (accounts export, Aug 2026).', 1292.00),
        ('99c243b9ef6047619ed47989d93b1811', 'c39365e0160a47bdb9e087829718bd42', 8, '2024-10-22T00:00:00+00:00', 'Backfilled from Xero invoice INV-0117 — By France - Valuation 6 - PRO-064. Historic completed works (accounts export, Aug 2026).', 1292.00),
        ('3d664467963c4affb65703bf2272992f', '2aa2a28bb69340c4a8e95da8cb7839eb', 8, '2024-11-21T00:00:00+00:00', 'Backfilled from Xero invoice INV-0123 — By France - Valuation 7 - PRO-064. Historic completed works (accounts export, Aug 2026).', 1292.00),
        ('a9848a828b6e40148901c13ec5a389ae', 'e7b73008b2ab453a8149b3f67e52867b', 8, '2024-12-13T00:00:00+00:00', 'Backfilled from Xero invoice INV-0126 — By France - Valuation 8 - PRO-064. Historic completed works (accounts export, Aug 2026).', 30668.83),
        ('5e57a257c48e4e039b8e59ac6541a7d2', 'adef310da9324b9bbf1d122066e8ed4f', 8, '2025-01-28T00:00:00+00:00', 'Backfilled from Xero invoice INV-0132 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 138168.47),
        ('10f19e89e5c24324900c43b93ea5c04c', '3584d551c21a47669803dabadb7cb0f4', 8, '2025-02-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0135 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 112423.76),
        ('bcac5a4d2309447792851e19c1563a22', '47250aa71b6944978d50188ab78336ef', 8, '2025-03-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0142 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 133564.59),
        ('ce2ca2d8ea044c96af492cde37adde07', 'e9bc752539614f769e52d8f3b13686ed', 8, '2025-04-16T00:00:00+00:00', 'Backfilled from Xero invoice INV-0148 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 78973.50),
        ('9e3a0a06274840689938682b3a273205', 'ee4cfa9deb8f46368947a2bf86b328b7', 8, '2025-05-22T00:00:00+00:00', 'Backfilled from Xero invoice INV-0154 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 48260.00),
        ('60891df5345442b5b03cc8094a7472a6', 'fea00a208e8945b9a0c4cbc3a2da3b63', 8, '2025-06-11T00:00:00+00:00', 'Backfilled from Xero invoice INV-0157 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 72006.25),
        ('47a109c0a5104187a98aaf0780cfcf9b', '503107d73c2546e2a5715d1e773203e4', 8, '2025-07-09T00:00:00+00:00', 'Backfilled from Xero invoice INV-0165 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 70076.37),
        ('1a13b518510743ecbd19baeca6a34f72', '343322b05a8f431186b2b054648fea16', 8, '2025-08-19T00:00:00+00:00', 'Backfilled from Xero invoice INV-0169 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 85333.41),
        ('c37db381050245599f4b65bc2dd43eea', 'c53f5810a0d04b4b83125068c204afb7', 8, '2025-09-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0175 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 59364.61),
        ('79ab37d28495473fa09ea0705c1771fb', 'fa7792da72b9437eb62cc29a92fac5d4', 8, '2025-10-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0177 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 120471.84),
        ('a62b36bf8f164edc88d51d8b8ac57be0', 'd899b1cb27e74d7ba064f1fc50499a79', 8, '2025-11-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0185 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 122156.26),
        ('dbe7499b15ef4edfaf40aa06550d1a43', 'ee72107c720349d196b93b3794e64d13', 8, '2025-12-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0186 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 57242.78),
        ('798695e01eae49ce9e43fd1c01d065ff', 'c899013a60df4411ba63a421f5216866', 8, '2026-01-23T00:00:00+00:00', 'Backfilled from Xero invoice INV-0195 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 116056.71),
        ('5a0308156e23445688bc90447b4213df', '425426e3c4ae4c998f4588958d884998', 8, '2026-02-17T00:00:00+00:00', 'Backfilled from Xero invoice INV-0197 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 59719.42),
        ('5a8604389eb54a669167af187f649634', '84607dbad735429c9d7eca9585b927fd', 8, '2026-04-02T00:00:00+00:00', 'Backfilled from Xero invoice INV-0202 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 35710.02),
        ('39b02a736c594e71a56383a602f0f7e6', 'c57b2f6c4baf41d9b54619fcefedd3c1', 8, '2026-05-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0206 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 72865.09),
        ('707b752408654648bb12b7affaf7022e', 'ff40ad3436814f0ab5bb6f935ae19555', 8, '2026-06-03T00:00:00+00:00', 'Backfilled from Xero invoice INV-0211 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 97425.76),
        ('990bc0c65a1043429000d387dc3d98dd', '8aeefbfbfaed484f879ff3734bfd3bc1', 8, '2026-07-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0213 — PRO-064 - By France. Historic completed works (accounts export, Aug 2026).', 50610.94);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 1623271.80 WHERE ProjectId = @ProjectId;
    PRINT 'OK    By France — 26 invoices backfilled, net 1,623,271.80 (paid 1,623,271.80).';
END
COMMIT;

GO
-- ===== Cornerways East Ewell KT17 3ER — 24 invoices, net 754,590.16, of which paid 754,590.16 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'cornerwayseastewellkt173er'
       OR LOWER(REPLACE(Name, ' ', '')) = 'cornerwayseastewellkt173er'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'cornerwayseastewellkt173er' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Cornerways East Ewell KT17 3ER — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Cornerways East Ewell KT17 3ER — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Cornerways East Ewell KT17 3ER — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('e52009256b7a4cfd86f937745f560bd4', @ProjectId, NULL, 1, 'VI-0001', '2023-02-01T00:00:00+00:00', 25225.98, 25225.98, 2, '2023-02-14T00:00:00+00:00', '2023-02-14T00:00:00+00:00', '2023-02-14T00:00:00+00:00', 0, 1),
        ('7d4ae1ab4e11401994fc9d157bdf61af', @ProjectId, NULL, 2, 'VI-0002', '2023-03-01T00:00:00+00:00', 21178.03, 21178.03, 2, '2023-03-13T00:00:00+00:00', '2023-03-13T00:00:00+00:00', '2023-03-13T00:00:00+00:00', 0, 1),
        ('cd2182b721b54dd994352031b7cfd84f', @ProjectId, NULL, 3, 'VI-0003', '2023-04-01T00:00:00+00:00', 33242.08, 33242.08, 2, '2023-04-12T00:00:00+00:00', '2023-04-12T00:00:00+00:00', '2023-04-12T00:00:00+00:00', 0, 1),
        ('70558d0397cd4f259ecf1a3796f6481c', @ProjectId, NULL, 4, 'VI-0004', '2023-05-01T00:00:00+00:00', 38832.83, 38832.83, 2, '2023-05-04T00:00:00+00:00', '2023-05-04T00:00:00+00:00', '2023-05-04T00:00:00+00:00', 0, 1),
        ('722fab940d7144e8b6b34327865724fe', @ProjectId, NULL, 5, 'VI-0005', '2023-06-01T00:00:00+00:00', 30981.57, 30981.57, 2, '2023-06-05T00:00:00+00:00', '2023-06-05T00:00:00+00:00', '2023-06-05T00:00:00+00:00', 0, 1),
        ('7614e40804c447b39f04f0beb96a7b80', @ProjectId, NULL, 6, 'VI-0006', '2023-07-01T00:00:00+00:00', 23458.03, 23458.03, 2, '2023-07-03T00:00:00+00:00', '2023-07-03T00:00:00+00:00', '2023-07-03T00:00:00+00:00', 0, 1),
        ('e65333396e394276adf48cd44bee17d3', @ProjectId, NULL, 7, 'VI-0007', '2023-07-01T00:00:00+00:00', 11410.12, 11410.12, 2, '2023-07-24T00:00:00+00:00', '2023-07-24T00:00:00+00:00', '2023-07-24T00:00:00+00:00', 0, 1),
        ('3915f0db03d641aaac659816231f587a', @ProjectId, NULL, 8, 'VI-0008', '2023-08-01T00:00:00+00:00', 27059.48, 27059.48, 2, '2023-08-23T00:00:00+00:00', '2023-08-23T00:00:00+00:00', '2023-08-23T00:00:00+00:00', 0, 1),
        ('8e1108221ce24ebebe4b94c21705a95a', @ProjectId, NULL, 9, 'VI-0009', '2023-09-01T00:00:00+00:00', 42190.36, 42190.36, 2, '2023-09-20T00:00:00+00:00', '2023-09-20T00:00:00+00:00', '2023-09-20T00:00:00+00:00', 0, 1),
        ('218aa30d24124965955b33bcb1f2a025', @ProjectId, NULL, 10, 'VI-0010', '2023-10-01T00:00:00+00:00', 52192.21, 52192.21, 2, '2023-10-18T00:00:00+00:00', '2023-10-18T00:00:00+00:00', '2023-10-18T00:00:00+00:00', 0, 1),
        ('66aa7e13a4614f17b0be5b6b4ea84a1e', @ProjectId, NULL, 11, 'VI-0011', '2023-11-01T00:00:00+00:00', 69335.97, 69335.97, 2, '2023-11-15T00:00:00+00:00', '2023-11-15T00:00:00+00:00', '2023-11-15T00:00:00+00:00', 0, 1),
        ('adf581c08b8f40cabd2de495386f86cd', @ProjectId, NULL, 12, 'VI-0012', '2023-12-01T00:00:00+00:00', 67395.94, 67395.94, 2, '2023-12-12T00:00:00+00:00', '2023-12-12T00:00:00+00:00', '2023-12-12T00:00:00+00:00', 0, 1),
        ('2b6e4d4be86d4f43b8764fca9da06474', @ProjectId, NULL, 13, 'VI-0013', '2023-12-01T00:00:00+00:00', -1244.51, -1244.51, 2, '2023-12-19T00:00:00+00:00', '2023-12-19T00:00:00+00:00', '2023-12-19T00:00:00+00:00', 0, 1),
        ('731f0b3a7cfe47f7ba1e79e1d37d17e7', @ProjectId, NULL, 14, 'VI-0014', '2023-12-01T00:00:00+00:00', 1244.51, 1244.51, 2, '2023-12-19T00:00:00+00:00', '2023-12-19T00:00:00+00:00', '2023-12-19T00:00:00+00:00', 0, 1),
        ('32c59d72367e4702a8c964eca7bb34c4', @ProjectId, NULL, 15, 'VI-0015', '2024-01-01T00:00:00+00:00', 23867.08, 23867.08, 2, '2024-01-10T00:00:00+00:00', '2024-01-10T00:00:00+00:00', '2024-01-10T00:00:00+00:00', 0, 1),
        ('2634540752dc412084840683dccbb96c', @ProjectId, NULL, 16, 'VI-0016', '2024-02-01T00:00:00+00:00', 14536.28, 14536.28, 2, '2024-02-07T00:00:00+00:00', '2024-02-07T00:00:00+00:00', '2024-02-07T00:00:00+00:00', 0, 1),
        ('6e2e8b5eb9c0494f9b1ba7c8cbf43b0b', @ProjectId, NULL, 17, 'VI-0017', '2024-03-01T00:00:00+00:00', 50539.01, 50539.01, 2, '2024-03-05T00:00:00+00:00', '2024-03-05T00:00:00+00:00', '2024-03-05T00:00:00+00:00', 0, 1),
        ('ee46cd2b888f4763b0087f2f56269072', @ProjectId, NULL, 18, 'VI-0018', '2024-04-01T00:00:00+00:00', 38555.90, 38555.90, 2, '2024-04-03T00:00:00+00:00', '2024-04-03T00:00:00+00:00', '2024-04-03T00:00:00+00:00', 0, 1),
        ('2ec4812067ec415a84434929a48eff3d', @ProjectId, NULL, 19, 'VI-0019', '2024-05-01T00:00:00+00:00', 46967.53, 46967.53, 2, '2024-05-01T00:00:00+00:00', '2024-05-01T00:00:00+00:00', '2024-05-01T00:00:00+00:00', 0, 1),
        ('9f11babc32ec4b9cb106e97e6f050117', @ProjectId, NULL, 20, 'VI-0020', '2024-05-01T00:00:00+00:00', 50613.57, 50613.57, 2, '2024-05-30T00:00:00+00:00', '2024-05-30T00:00:00+00:00', '2024-05-30T00:00:00+00:00', 0, 1),
        ('7be8b81a1c0843e8918cf073a29df496', @ProjectId, NULL, 21, 'VI-0021', '2024-06-01T00:00:00+00:00', 48874.17, 48874.17, 2, '2024-06-28T00:00:00+00:00', '2024-06-28T00:00:00+00:00', '2024-06-28T00:00:00+00:00', 0, 1),
        ('85bccaf3bb5045aca118a97fa3871cf1', @ProjectId, NULL, 22, 'VI-0022', '2024-07-01T00:00:00+00:00', 17628.61, 17628.61, 2, '2024-07-11T00:00:00+00:00', '2024-07-11T00:00:00+00:00', '2024-07-11T00:00:00+00:00', 0, 1),
        ('87d9362efa6649d6a44190d523b3fe9b', @ProjectId, NULL, 23, 'VI-0023', '2024-11-01T00:00:00+00:00', 1693.38, 1693.38, 2, '2024-11-21T00:00:00+00:00', '2024-11-21T00:00:00+00:00', '2024-11-21T00:00:00+00:00', 0, 1),
        ('c3841cab2221496abd613419c899c09f', @ProjectId, NULL, 24, 'VI-0024', '2025-11-01T00:00:00+00:00', 18812.03, 18812.03, 2, '2025-11-27T00:00:00+00:00', '2025-11-27T00:00:00+00:00', '2025-11-27T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('ab6c780d20f04d60a975e9155adf6e42', 'e52009256b7a4cfd86f937745f560bd4', 8, '2023-02-14T00:00:00+00:00', 'Backfilled from Xero invoice INV-0007 — Cornerways - Valuation 1. Historic completed works (accounts export, Aug 2026).', 25225.98),
        ('9ba219d8ddc34c41943c9afcb97be63d', '7d4ae1ab4e11401994fc9d157bdf61af', 8, '2023-03-13T00:00:00+00:00', 'Backfilled from Xero invoice INV-0014 — Cornerways - Valuation 2. Historic completed works (accounts export, Aug 2026).', 21178.03),
        ('8e71e469d73c401a84b512d3a6ce7a97', 'cd2182b721b54dd994352031b7cfd84f', 8, '2023-04-12T00:00:00+00:00', 'Backfilled from Xero invoice INV-0017 — Cornerways - Valuation 3. Historic completed works (accounts export, Aug 2026).', 33242.08),
        ('1ce01c63d2674d4aab4d5b2671276e62', '70558d0397cd4f259ecf1a3796f6481c', 8, '2023-05-04T00:00:00+00:00', 'Backfilled from Xero invoice INV-0020 — Cornerways - Valuation 4 - INS 586. Historic completed works (accounts export, Aug 2026).', 38832.83),
        ('a1b2dd6bf3f94e4ea9e90f2058a41bb6', '722fab940d7144e8b6b34327865724fe', 8, '2023-06-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0024 — Cornerways - Valuation 5 - INS 586. Historic completed works (accounts export, Aug 2026).', 30981.57),
        ('3cbbfa05fe1a47e089ab6966ceb1b795', '7614e40804c447b39f04f0beb96a7b80', 8, '2023-07-03T00:00:00+00:00', 'Backfilled from Xero invoice INV-0026 — Cornerways - Valuation 6 - INS 586. Historic completed works (accounts export, Aug 2026).', 23458.03),
        ('0fb44dab75d54e1ea16335204870aff0', 'e65333396e394276adf48cd44bee17d3', 8, '2023-07-24T00:00:00+00:00', 'Backfilled from Xero invoice INV-0030 — Cornerways - Valuation 6 - INS 586. Historic completed works (accounts export, Aug 2026).', 11410.12),
        ('d47e03bd40774cf4b0d5525808e5906d', '3915f0db03d641aaac659816231f587a', 8, '2023-08-23T00:00:00+00:00', 'Backfilled from Xero invoice INV-0032 — Cornerways - Valuation 8 - INS 586. Historic completed works (accounts export, Aug 2026).', 27059.48),
        ('d5ce74b3b99542c89f1ac62aca6d5cb8', '8e1108221ce24ebebe4b94c21705a95a', 8, '2023-09-20T00:00:00+00:00', 'Backfilled from Xero invoice INV-0035 — Cornerways - Valuation 9 - INS 586. Historic completed works (accounts export, Aug 2026).', 42190.36),
        ('3b18676f1af14232a189f0cce91fa16e', '218aa30d24124965955b33bcb1f2a025', 8, '2023-10-18T00:00:00+00:00', 'Backfilled from Xero invoice INV-0039 — Cornerways - Valuation 10 - INS 586. Historic completed works (accounts export, Aug 2026).', 52192.21),
        ('7957e7a646794e328f8cfcccd7c3e157', '66aa7e13a4614f17b0be5b6b4ea84a1e', 8, '2023-11-15T00:00:00+00:00', 'Backfilled from Xero invoice INV-0045 — Cornerways - Valuation 11 - INS 586. Historic completed works (accounts export, Aug 2026).', 69335.97),
        ('045c1a7702574d0ab1c1d07e1f67f316', 'adf581c08b8f40cabd2de495386f86cd', 8, '2023-12-12T00:00:00+00:00', 'Backfilled from Xero invoice INV-0049 — Cornerways - Valuation 12 - INS 586. Historic completed works (accounts export, Aug 2026).', 67395.94),
        ('f2be7b30dd514e56a292b3671b6c230d', '2b6e4d4be86d4f43b8764fca9da06474', 8, '2023-12-19T00:00:00+00:00', 'Backfilled from Xero credit note CN-0065 — Cornerways - Valuation 11 extra - INS 586. Historic completed works (accounts export, Aug 2026).', -1244.51),
        ('6d40ccd0fcac4fd1889ae18d57300aa5', '731f0b3a7cfe47f7ba1e79e1d37d17e7', 8, '2023-12-19T00:00:00+00:00', 'Backfilled from Xero invoice INV-0050 — Cornerways - Valuation 11 extra - INS 586. Historic completed works (accounts export, Aug 2026).', 1244.51),
        ('985626472d074319819b041ed867db1b', '32c59d72367e4702a8c964eca7bb34c4', 8, '2024-01-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0055 — Cornerways - Valuation 13 - INS 586. Historic completed works (accounts export, Aug 2026).', 23867.08),
        ('2a32b86bbb9b489a888943ea6ca63cdc', '2634540752dc412084840683dccbb96c', 8, '2024-02-07T00:00:00+00:00', 'Backfilled from Xero invoice INV-0063 — Cornerways - Valuation 14 - INS 586. Historic completed works (accounts export, Aug 2026).', 14536.28),
        ('360cf3c1564440318e075187ff1612da', '6e2e8b5eb9c0494f9b1ba7c8cbf43b0b', 8, '2024-03-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0068 — Cornerways - Valuation 15 - INS 586. Historic completed works (accounts export, Aug 2026).', 50539.01),
        ('90e029390adc426ebd67c7ac8e39923f', 'ee46cd2b888f4763b0087f2f56269072', 8, '2024-04-03T00:00:00+00:00', 'Backfilled from Xero invoice INV-0076 — Cornerways - Valuation 16 - INS 586. Historic completed works (accounts export, Aug 2026).', 38555.90),
        ('6d4519659fa04b97b5715badf1038c27', '2ec4812067ec415a84434929a48eff3d', 8, '2024-05-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0082 — Cornerways - Valuation 17 - INS 586. Historic completed works (accounts export, Aug 2026).', 46967.53),
        ('bc458805502344ef8231dadfb13404a9', '9f11babc32ec4b9cb106e97e6f050117', 8, '2024-05-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0087 — Cornerways - Valuation 18 - INS 586. Historic completed works (accounts export, Aug 2026).', 50613.57),
        ('e08da1bc710f417dac9cfccbc16e6f69', '7be8b81a1c0843e8918cf073a29df496', 8, '2024-06-28T00:00:00+00:00', 'Backfilled from Xero invoice INV-0097 — Cornerways - Valuation 19 - INS 586. Historic completed works (accounts export, Aug 2026).', 48874.17),
        ('6787bb00564a44878c17c422adf5ffd0', '85bccaf3bb5045aca118a97fa3871cf1', 8, '2024-07-11T00:00:00+00:00', 'Backfilled from Xero invoice INV-0098 — Cornerways - Valuation 20 - INS 586. Historic completed works (accounts export, Aug 2026).', 17628.61),
        ('6a55e9733b014d1486050bcdfa2417b2', '87d9362efa6649d6a44190d523b3fe9b', 8, '2024-11-21T00:00:00+00:00', 'Backfilled from Xero invoice INV-0122 — Cornerways - Valuation PPC - INS 586. Historic completed works (accounts export, Aug 2026).', 1693.38),
        ('2bd7b42768e444259c386f66d0d32fec', 'c3841cab2221496abd613419c899c09f', 8, '2025-11-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0184 — Cornerways - Retention - INS 586. Historic completed works (accounts export, Aug 2026).', 18812.03);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 754590.16 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Cornerways East Ewell KT17 3ER — 24 invoices backfilled, net 754,590.16 (paid 754,590.16).';
END
COMMIT;

GO
-- ===== Horsham Road Longwood Cranleigh — 1 invoice, net 6,270.00, of which paid 6,270.00 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'horshamroadlongwoodcranleigh'
       OR LOWER(REPLACE(Name, ' ', '')) = 'horshamroadlongwoodcranleigh'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'horshamroadlongwoodcranleigh' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Horsham Road Longwood Cranleigh — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Horsham Road Longwood Cranleigh — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Horsham Road Longwood Cranleigh — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('be1f06c0fef349228ffd2459679512ab', @ProjectId, NULL, 1, 'VI-0001', '2025-03-01T00:00:00+00:00', 6270.00, 6270.00, 2, '2025-03-05T00:00:00+00:00', '2025-03-05T00:00:00+00:00', '2025-03-05T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('192c73ca114c4518a7b8b531b49aef57', 'be1f06c0fef349228ffd2459679512ab', 8, '2025-03-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0139 — Longwood -Valuation 01. Historic completed works (accounts export, Aug 2026).', 6270.00);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 6270.00 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Horsham Road Longwood Cranleigh — 1 invoice backfilled, net 6,270.00 (paid 6,270.00).';
END
COMMIT;

GO
-- ===== Jewel Property Serve — 6 invoices, net 7,785.60, of which paid 7,785.60 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'jewelpropertyserve'
       OR LOWER(REPLACE(Name, ' ', '')) = 'jewelpropertyserve'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'jewelpropertyserve' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Jewel Property Serve — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Jewel Property Serve — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Jewel Property Serve — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('b6106382f23f4ba78d5cb75b597c3dc3', @ProjectId, NULL, 1, 'VI-0001', '2025-03-01T00:00:00+00:00', 1257.60, 1257.60, 2, '2025-03-28T00:00:00+00:00', '2025-03-28T00:00:00+00:00', '2025-03-28T00:00:00+00:00', 0, 1),
        ('fcdd1409edc9491e9efcacb2b4c57570', @ProjectId, NULL, 2, 'VI-0002', '2025-04-01T00:00:00+00:00', 920.00, 920.00, 2, '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', 0, 1),
        ('325bb9e6c64f433f9beedf5fbca739c6', @ProjectId, NULL, 3, 'VI-0003', '2025-04-01T00:00:00+00:00', 3504.00, 3504.00, 2, '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', 0, 1),
        ('5523c8bb53ae4edbacc1ce3e0eff0da4', @ProjectId, NULL, 4, 'VI-0004', '2025-04-01T00:00:00+00:00', 368.00, 368.00, 2, '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', '2025-04-30T00:00:00+00:00', 0, 1),
        ('4112b381d0c645c58b3ecf3bb30aa434', @ProjectId, NULL, 5, 'VI-0005', '2025-06-01T00:00:00+00:00', 1552.00, 1552.00, 2, '2025-06-30T00:00:00+00:00', '2025-06-30T00:00:00+00:00', '2025-06-30T00:00:00+00:00', 0, 1),
        ('728612a6711c43d3b6c642ce85899945', @ProjectId, NULL, 6, 'VI-0006', '2025-06-01T00:00:00+00:00', 184.00, 184.00, 2, '2025-06-30T00:00:00+00:00', '2025-06-30T00:00:00+00:00', '2025-06-30T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('7386919e5f0d4943a57bea2525add951', 'b6106382f23f4ba78d5cb75b597c3dc3', 8, '2025-03-28T00:00:00+00:00', 'Backfilled from Xero invoice INV-0143 — March 25 hours. Historic completed works (accounts export, Aug 2026).', 1257.60),
        ('8e6a9035e0f3467590de430e192e2e10', 'fcdd1409edc9491e9efcacb2b4c57570', 8, '2025-04-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0149 — April 25 hours 198 Chiltern Court. Historic completed works (accounts export, Aug 2026).', 920.00),
        ('f66554bfe28648a2a852d8465b5a8945', '325bb9e6c64f433f9beedf5fbca739c6', 8, '2025-04-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0150 — April 25 hours Ruislip. Historic completed works (accounts export, Aug 2026).', 3504.00),
        ('ec2a12def34243e29f2d9380a4a71140', '5523c8bb53ae4edbacc1ce3e0eff0da4', 8, '2025-04-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0151 — April 25 hours 160 Harwoods road. Historic completed works (accounts export, Aug 2026).', 368.00),
        ('b937d3b57095490aab314b65171e9f54', '4112b381d0c645c58b3ecf3bb30aa434', 8, '2025-06-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0161 — June 2025 96 Chiltern Court. Historic completed works (accounts export, Aug 2026).', 1552.00),
        ('c4fd975e6b5d4e07bb3cf672a6c16fa0', '728612a6711c43d3b6c642ce85899945', 8, '2025-06-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0162 — June 2025 Tooting. Historic completed works (accounts export, Aug 2026).', 184.00);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 7785.60 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Jewel Property Serve — 6 invoices backfilled, net 7,785.60 (paid 7,785.60).';
END
COMMIT;

GO
-- ===== Metropolitan Crescent — 10 invoices, net 162,944.30, of which paid 162,944.30 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'metropolitancrescent'
       OR LOWER(REPLACE(Name, ' ', '')) = 'metropolitancrescent'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'metropolitancrescent' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Metropolitan Crescent — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Metropolitan Crescent — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Metropolitan Crescent — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('f569a141ee6f4a11b971180bc567221e', @ProjectId, NULL, 1, 'VI-0001', '2023-09-01T00:00:00+00:00', 4397.55, 4397.55, 2, '2023-09-11T00:00:00+00:00', '2023-09-11T00:00:00+00:00', '2023-09-11T00:00:00+00:00', 0, 1),
        ('23985b4506b147ae8d771f57c93a2943', @ProjectId, NULL, 2, 'VI-0002', '2023-12-01T00:00:00+00:00', 79314.29, 79314.29, 2, '2023-12-08T00:00:00+00:00', '2023-12-08T00:00:00+00:00', '2023-12-08T00:00:00+00:00', 0, 1),
        ('52737667ba2d43a7b7f426d0d2bca9e3', @ProjectId, NULL, 3, 'VI-0003', '2024-01-01T00:00:00+00:00', -0.05, -0.05, 2, '2024-01-01T00:00:00+00:00', '2024-01-01T00:00:00+00:00', '2024-01-01T00:00:00+00:00', 0, 1),
        ('bfc292345428485c9625c5b0735d02b0', @ProjectId, NULL, 4, 'VI-0004', '2024-01-01T00:00:00+00:00', 25043.90, 25043.90, 2, '2024-01-10T00:00:00+00:00', '2024-01-10T00:00:00+00:00', '2024-01-10T00:00:00+00:00', 0, 1),
        ('9465be7421c6449ba18c6b599f0c4047', @ProjectId, NULL, 5, 'VI-0005', '2024-02-01T00:00:00+00:00', 19102.51, 19102.51, 2, '2024-02-07T00:00:00+00:00', '2024-02-07T00:00:00+00:00', '2024-02-07T00:00:00+00:00', 0, 1),
        ('4823fa05ef8d49398ac163ec585950d3', @ProjectId, NULL, 6, 'VI-0006', '2024-03-01T00:00:00+00:00', 22026.39, 22026.39, 2, '2024-03-08T00:00:00+00:00', '2024-03-08T00:00:00+00:00', '2024-03-08T00:00:00+00:00', 0, 1),
        ('4917574f77a847c9b227143dcd09ae26', @ProjectId, NULL, 7, 'VI-0007', '2024-03-01T00:00:00+00:00', 3944.34, 3944.34, 2, '2024-03-08T00:00:00+00:00', '2024-03-08T00:00:00+00:00', '2024-03-08T00:00:00+00:00', 0, 1),
        ('9def961b7ee249d182817817709150a9', @ProjectId, NULL, 8, 'VI-0008', '2024-09-01T00:00:00+00:00', 5229.65, 5229.65, 2, '2024-09-10T00:00:00+00:00', '2024-09-10T00:00:00+00:00', '2024-09-10T00:00:00+00:00', 0, 1),
        ('22683765f55647e7ae4d7321811f99a7', @ProjectId, NULL, 9, 'VI-0009', '2024-10-01T00:00:00+00:00', -0.53, -0.53, 2, '2024-10-31T00:00:00+00:00', '2024-10-31T00:00:00+00:00', '2024-10-31T00:00:00+00:00', 0, 1),
        ('43f542e0bdb64815833e52576c5089b7', @ProjectId, NULL, 10, 'VI-0010', '2025-06-01T00:00:00+00:00', 3886.25, 3886.25, 2, '2025-06-25T00:00:00+00:00', '2025-06-25T00:00:00+00:00', '2025-06-25T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('2b320dbf32204b80b3c24f36f19a991e', 'f569a141ee6f4a11b971180bc567221e', 8, '2023-09-11T00:00:00+00:00', 'Backfilled from Xero invoice INV-0034 — Flat 1, 3 Metropoloitan Crescent - Valuation 1. Historic completed works (accounts export, Aug 2026).', 4397.55),
        ('17a07d182c6048b6a707fcd35e3305a5', '23985b4506b147ae8d771f57c93a2943', 8, '2023-12-08T00:00:00+00:00', 'Backfilled from Xero invoice INV-0046 — Flat 1, 3 Metropoloitan Crescent - Valuation 2. Historic completed works (accounts export, Aug 2026).', 79314.29),
        ('cf6f0f9e06bf4cd28a720f8da07e7c95', '52737667ba2d43a7b7f426d0d2bca9e3', 8, '2024-01-01T00:00:00+00:00', 'Backfilled from Xero credit note CN-0056 — Flat 1, 3 Metropoloitan Crescent - Valuation 1. Historic completed works (accounts export, Aug 2026).', -0.05),
        ('f1bf2e3cf16846688e2031b1383939dc', 'bfc292345428485c9625c5b0735d02b0', 8, '2024-01-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0053 — Flat 1, 3 Metropoloitan Crescent - Valuation 3. Historic completed works (accounts export, Aug 2026).', 25043.90),
        ('a20bde83b5134c03b314c222acedbd06', '9465be7421c6449ba18c6b599f0c4047', 8, '2024-02-07T00:00:00+00:00', 'Backfilled from Xero invoice INV-0062 — Flat 1, 3 Metropolitan Crescent - Valuation 4. Historic completed works (accounts export, Aug 2026).', 19102.51),
        ('9fa9913ba1404a9882eb882df39b79b6', '4823fa05ef8d49398ac163ec585950d3', 8, '2024-03-08T00:00:00+00:00', 'Backfilled from Xero invoice INV-0070 — Flat 1, 3 Metropolitan Crescent - Valuation 5. Historic completed works (accounts export, Aug 2026).', 22026.39),
        ('67b70c51343145f0839fd8eea5d9bbbf', '4917574f77a847c9b227143dcd09ae26', 8, '2024-03-08T00:00:00+00:00', 'Backfilled from Xero invoice INV-0071 — Flat 1, 3 Metropolitan Crescent - Retention Release. Historic completed works (accounts export, Aug 2026).', 3944.34),
        ('2518269a6b304ffe8ee14003eb13eca1', '9def961b7ee249d182817817709150a9', 8, '2024-09-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0109 — Flat 1, 3 Metropolitan Crescent - Cert 6. Historic completed works (accounts export, Aug 2026).', 5229.65),
        ('3bf74afb8dd14af1911bcf9d768fe550', '22683765f55647e7ae4d7321811f99a7', 8, '2024-10-31T00:00:00+00:00', 'Backfilled from Xero credit note CN-0119 — Flat 1, 3 Metropolitan Crescent - Cert 6. Historic completed works (accounts export, Aug 2026).', -0.53),
        ('e35646785a8d41b7a20a9ef66ae36196', '43f542e0bdb64815833e52576c5089b7', 8, '2025-06-25T00:00:00+00:00', 'Backfilled from Xero invoice INV-0158 — Flat 1, 3 Metropolitan Crescent - Cert 7. Historic completed works (accounts export, Aug 2026).', 3886.25);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 162944.30 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Metropolitan Crescent — 10 invoices backfilled, net 162,944.30 (paid 162,944.30).';
END
COMMIT;

GO
-- ===== Newnham Ave Ruislip — 1 invoice, net 453.59, of which paid 453.59 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'newnhamaveruislip'
       OR LOWER(REPLACE(Name, ' ', '')) = 'newnhamaveruislip'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'newnhamaveruislip' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Newnham Ave Ruislip — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Newnham Ave Ruislip — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Newnham Ave Ruislip — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('827e8e884bf6479089cb3ff5820acaae', @ProjectId, NULL, 1, 'VI-0001', '2025-04-01T00:00:00+00:00', 453.59, 453.59, 2, '2025-04-23T00:00:00+00:00', '2025-04-23T00:00:00+00:00', '2025-04-23T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('3aa9ce99a91a44aa83d06928e9778c43', '827e8e884bf6479089cb3ff5820acaae', 8, '2025-04-23T00:00:00+00:00', 'Backfilled from Xero invoice INV-0147 — Materials 12 Newnham Avenue Ruislip. Historic completed works (accounts export, Aug 2026).', 453.59);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 453.59 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Newnham Ave Ruislip — 1 invoice backfilled, net 453.59 (paid 453.59).';
END
COMMIT;

GO
-- ===== Oakhill House Godalming — 7 invoices, net 134,667.00, of which paid 134,667.00 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming'
       OR LOWER(REPLACE(Name, ' ', '')) = 'oakhillhousegodalming'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Oakhill House Godalming — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Oakhill House Godalming — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Oakhill House Godalming — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('bce64345a4af489691802f2e477af72b', @ProjectId, NULL, 1, 'VI-0001', '2024-03-01T00:00:00+00:00', 25427.30, 25427.30, 2, '2024-03-01T00:00:00+00:00', '2024-03-01T00:00:00+00:00', '2024-03-01T00:00:00+00:00', 0, 1),
        ('a718fc3e31da4a028bcaee07638f59ee', @ProjectId, NULL, 2, 'VI-0002', '2024-03-01T00:00:00+00:00', 53974.48, 53974.48, 2, '2024-03-28T00:00:00+00:00', '2024-03-28T00:00:00+00:00', '2024-03-28T00:00:00+00:00', 0, 1),
        ('ce22273e3ba247c0b8311fec47c96ce9', @ProjectId, NULL, 3, 'VI-0003', '2024-04-01T00:00:00+00:00', 23908.34, 23908.34, 2, '2024-04-29T00:00:00+00:00', '2024-04-29T00:00:00+00:00', '2024-04-29T00:00:00+00:00', 0, 1),
        ('225735dd5b3d4894adfc61815d1e040c', @ProjectId, NULL, 4, 'VI-0004', '2024-05-01T00:00:00+00:00', 15525.85, 15525.85, 2, '2024-05-27T00:00:00+00:00', '2024-05-27T00:00:00+00:00', '2024-05-27T00:00:00+00:00', 0, 1),
        ('56493d59989146db9672470275335606', @ProjectId, NULL, 5, 'VI-0005', '2024-06-01T00:00:00+00:00', 10738.60, 10738.60, 2, '2024-06-10T00:00:00+00:00', '2024-06-10T00:00:00+00:00', '2024-06-10T00:00:00+00:00', 0, 1),
        ('bd40ccdd9f634290afed19a6e23095db', @ProjectId, NULL, 6, 'VI-0006', '2024-07-01T00:00:00+00:00', 1725.75, 1725.75, 2, '2024-07-02T00:00:00+00:00', '2024-07-02T00:00:00+00:00', '2024-07-02T00:00:00+00:00', 0, 1),
        ('1a3356d1293648ed8a260f5af24cff21', @ProjectId, NULL, 7, 'VI-0007', '2024-12-01T00:00:00+00:00', 3366.68, 3366.68, 2, '2024-12-20T00:00:00+00:00', '2024-12-20T00:00:00+00:00', '2024-12-20T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('449222273d6c42e9aa8997371721721a', 'bce64345a4af489691802f2e477af72b', 8, '2024-03-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0067 — Oakhill House - L/1895 - Valuation 1. Historic completed works (accounts export, Aug 2026).', 25427.30),
        ('a24b8eb9c1a74e8aa3aedb5a9fe738ef', 'a718fc3e31da4a028bcaee07638f59ee', 8, '2024-03-28T00:00:00+00:00', 'Backfilled from Xero invoice INV-0074 — Oakhill House - L/1895 - Valuation 2. Historic completed works (accounts export, Aug 2026).', 53974.48),
        ('77f07bb3302e4ebca34feb5053524545', 'ce22273e3ba247c0b8311fec47c96ce9', 8, '2024-04-29T00:00:00+00:00', 'Backfilled from Xero invoice INV-0079 — Oakhill House - L/1895 - Valuation 3. Historic completed works (accounts export, Aug 2026).', 23908.34),
        ('db19177b0def465cb5495b441fc734b9', '225735dd5b3d4894adfc61815d1e040c', 8, '2024-05-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0083 — Oakhill House - L/1895 - Valuation 4. Historic completed works (accounts export, Aug 2026).', 15525.85),
        ('17fcf851731044bb97e81f22546c85c4', '56493d59989146db9672470275335606', 8, '2024-06-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0089 — Oakhill House - L/1895 - Valuation 5. Historic completed works (accounts export, Aug 2026).', 10738.60),
        ('6e3ed3bcdea542788aa29907282d4438', 'bd40ccdd9f634290afed19a6e23095db', 8, '2024-07-02T00:00:00+00:00', 'Backfilled from Xero invoice INV-0096 — Oakhill House - L/1895 - Valuation 6. Historic completed works (accounts export, Aug 2026).', 1725.75),
        ('7d11aec5a1274b07b382cc406b44ca03', '1a3356d1293648ed8a260f5af24cff21', 8, '2024-12-20T00:00:00+00:00', 'Backfilled from Xero invoice INV-0129 — Oakhill House - L/1895 - Valuation 7. Historic completed works (accounts export, Aug 2026).', 3366.68);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 134667.00 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Oakhill House Godalming — 7 invoices backfilled, net 134,667.00 (paid 134,667.00).';
END
COMMIT;

GO
-- ===== Vets — 5 invoices, net 62,734.02, of which paid 62,734.02 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'vets'
       OR LOWER(REPLACE(Name, ' ', '')) = 'vets'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'vets' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Vets — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Vets — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Vets — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('374bb3df681b454da57a817c2b7277f5', @ProjectId, NULL, 1, 'VI-0001', '2023-01-01T00:00:00+00:00', 35624.90, 35624.90, 2, '2023-01-20T00:00:00+00:00', '2023-01-20T00:00:00+00:00', '2023-01-20T00:00:00+00:00', 0, 1),
        ('2890cec4666b440583b960e11f2bfc54', @ProjectId, NULL, 2, 'VI-0002', '2023-03-01T00:00:00+00:00', 3054.42, 3054.42, 2, '2023-03-01T00:00:00+00:00', '2023-03-01T00:00:00+00:00', '2023-03-01T00:00:00+00:00', 0, 1),
        ('90c189d05b544d10b69163238a191160', @ProjectId, NULL, 3, 'VI-0003', '2023-03-01T00:00:00+00:00', 11931.30, 11931.30, 2, '2023-03-01T00:00:00+00:00', '2023-03-01T00:00:00+00:00', '2023-03-01T00:00:00+00:00', 0, 1),
        ('cdce0e23b1b4423eae430d8fd253e2fd', @ProjectId, NULL, 4, 'VI-0004', '2023-03-01T00:00:00+00:00', 9595.70, 9595.70, 2, '2023-03-10T00:00:00+00:00', '2023-03-10T00:00:00+00:00', '2023-03-10T00:00:00+00:00', 0, 1),
        ('a6cf5d8146d048ae949277ba742d8518', @ProjectId, NULL, 5, 'VI-0005', '2025-04-01T00:00:00+00:00', 2527.70, 2527.70, 2, '2025-04-03T00:00:00+00:00', '2025-04-03T00:00:00+00:00', '2025-04-03T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('89460410693143de9bb32eae4e05b2f7', '374bb3df681b454da57a817c2b7277f5', 8, '2023-01-20T00:00:00+00:00', 'Backfilled from Xero invoice INV-0004 — Vets Valuation 2 - Phase 3. Historic completed works (accounts export, Aug 2026).', 35624.90),
        ('aae849300aef42fd890a761650a73c6a', '2890cec4666b440583b960e11f2bfc54', 8, '2023-03-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0009 — Vets retention - Phase 2. Historic completed works (accounts export, Aug 2026).', 3054.42),
        ('953c02efb2924e20abd921e7c19dfb4c', '90c189d05b544d10b69163238a191160', 8, '2023-03-01T00:00:00+00:00', 'Backfilled from Xero invoice INV-0010 — Vets retention - Phase 1. Historic completed works (accounts export, Aug 2026).', 11931.30),
        ('a90c7c90cffb43ce93452dc72e9764a8', 'cdce0e23b1b4423eae430d8fd253e2fd', 8, '2023-03-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0013 — Vets Valuation 5 - Phase 3. Historic completed works (accounts export, Aug 2026).', 9595.70),
        ('d8e842d6bc194bf1b679e77c1a84bb0a', 'a6cf5d8146d048ae949277ba742d8518', 8, '2025-04-03T00:00:00+00:00', 'Backfilled from Xero invoice INV-0144 — Vets retention - Phase 3. Historic completed works (accounts export, Aug 2026).', 2527.70);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 62734.02 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Vets — 5 invoices backfilled, net 62,734.02 (paid 62,734.02).';
END
COMMIT;

GO
-- ===== Windy Ridge Godalming — 9 invoices, net 208,194.81, of which paid 208,194.81 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'windyridgegodalming'
       OR LOWER(REPLACE(Name, ' ', '')) = 'windyridgegodalming'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'windyridgegodalming' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Windy Ridge Godalming — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Windy Ridge Godalming — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Windy Ridge Godalming — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('73edb8a1f4924d73abc1639731a041f2', @ProjectId, NULL, 1, 'VI-0001', '2024-04-01T00:00:00+00:00', 53661.30, 53661.30, 2, '2024-04-26T00:00:00+00:00', '2024-04-26T00:00:00+00:00', '2024-04-26T00:00:00+00:00', 0, 1),
        ('9fa7b2cd9a924b47a4e0c6a9ee393b71', @ProjectId, NULL, 2, 'VI-0002', '2024-05-01T00:00:00+00:00', 29718.56, 29718.56, 2, '2024-05-31T00:00:00+00:00', '2024-05-31T00:00:00+00:00', '2024-05-31T00:00:00+00:00', 0, 1),
        ('b35f5bf0bd6c4eba926bcaa2a26f6c4f', @ProjectId, NULL, 3, 'VI-0003', '2024-06-01T00:00:00+00:00', 23475.03, 23475.03, 2, '2024-06-27T00:00:00+00:00', '2024-06-27T00:00:00+00:00', '2024-06-27T00:00:00+00:00', 0, 1),
        ('94538766e6c940b3a090dc2592da6b34', @ProjectId, NULL, 4, 'VI-0004', '2024-07-01T00:00:00+00:00', 42724.37, 42724.37, 2, '2024-07-25T00:00:00+00:00', '2024-07-25T00:00:00+00:00', '2024-07-25T00:00:00+00:00', 0, 1),
        ('61fd05740ab240d6bdc085e7f7159e63', @ProjectId, NULL, 5, 'VI-0005', '2024-08-01T00:00:00+00:00', 17509.68, 17509.68, 2, '2024-08-30T00:00:00+00:00', '2024-08-30T00:00:00+00:00', '2024-08-30T00:00:00+00:00', 0, 1),
        ('9f00692c1a4e4f62bb4ad999b7c9498e', @ProjectId, NULL, 6, 'VI-0006', '2024-09-01T00:00:00+00:00', 21056.79, 21056.79, 2, '2024-09-20T00:00:00+00:00', '2024-09-20T00:00:00+00:00', '2024-09-20T00:00:00+00:00', 0, 1),
        ('37d61f8b36864909b3dd652d375bb424', @ProjectId, NULL, 7, 'VI-0007', '2024-10-01T00:00:00+00:00', 11693.33, 11693.33, 2, '2024-10-28T00:00:00+00:00', '2024-10-28T00:00:00+00:00', '2024-10-28T00:00:00+00:00', 0, 1),
        ('21dbc2cd1f9b443686ef87e5d2afea37', @ProjectId, NULL, 8, 'VI-0008', '2025-05-01T00:00:00+00:00', 4142.76, 4142.76, 2, '2025-05-27T00:00:00+00:00', '2025-05-27T00:00:00+00:00', '2025-05-27T00:00:00+00:00', 0, 1),
        ('53f2e2ebe0384cbe8e0920cad2b43e4d', @ProjectId, NULL, 9, 'VI-0009', '2026-01-01T00:00:00+00:00', 4212.99, 4212.99, 2, '2026-01-27T00:00:00+00:00', '2026-01-27T00:00:00+00:00', '2026-01-27T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('db59b2fbef4b49b1bffd99ce768d66c1', '73edb8a1f4924d73abc1639731a041f2', 8, '2024-04-26T00:00:00+00:00', 'Backfilled from Xero invoice INV-0078 — Valuation 1 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 53661.30),
        ('df334b19251941aea02857b0e0800745', '9fa7b2cd9a924b47a4e0c6a9ee393b71', 8, '2024-05-31T00:00:00+00:00', 'Backfilled from Xero invoice INV-0086 — Valuation 2 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 29718.56),
        ('7fd9c2301c4747ca9bfc5a13c830c1f6', 'b35f5bf0bd6c4eba926bcaa2a26f6c4f', 8, '2024-06-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0093 — Valuation 3 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 23475.03),
        ('9430da65cc494652b2ada309d16aa854', '94538766e6c940b3a090dc2592da6b34', 8, '2024-07-25T00:00:00+00:00', 'Backfilled from Xero invoice INV-0101 — Valuation 4 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 42724.37),
        ('c27b100a597843da96e94d69f0006fd9', '61fd05740ab240d6bdc085e7f7159e63', 8, '2024-08-30T00:00:00+00:00', 'Backfilled from Xero invoice INV-0105 — Valuation 5 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 17509.68),
        ('eded7128a9f04f4e89edb8b2fdc26f2e', '9f00692c1a4e4f62bb4ad999b7c9498e', 8, '2024-09-20T00:00:00+00:00', 'Backfilled from Xero invoice INV-0110 — Valuation 6 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 21056.79),
        ('867816cf29db41faa5e604eb9e986cbe', '37d61f8b36864909b3dd652d375bb424', 8, '2024-10-28T00:00:00+00:00', 'Backfilled from Xero invoice INV-0116 — Valuation 7 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 11693.33),
        ('7193e05763ab4ab49017b9c2de4916be', '21dbc2cd1f9b443686ef87e5d2afea37', 8, '2025-05-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0155 — Valuation 8 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 4142.76),
        ('271865baa6d348c49f7048253b519087', '53f2e2ebe0384cbe8e0920cad2b43e4d', 8, '2026-01-27T00:00:00+00:00', 'Backfilled from Xero invoice INV-0192 — Valuation 9 - Windy Ridge. Historic completed works (accounts export, Aug 2026).', 4212.99);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 208194.81 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Windy Ridge Godalming — 9 invoices backfilled, net 208,194.81 (paid 208,194.81).';
END
COMMIT;

GO
-- ===== Woodhouse Lane — 8 invoices, net 255,242.77, of which paid 255,242.77 =====
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'woodhouselane'
       OR LOWER(REPLACE(Name, ' ', '')) = 'woodhouselane'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'woodhouselane' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
    PRINT 'SKIP  Woodhouse Lane — no project matches this Xero site.';
ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)
    PRINT 'SKIP  Woodhouse Lane — project already has valuation invoices; nothing touched.';
ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)
    PRINT 'SKIP  Woodhouse Lane — project holds a Preapproved claim; use the app''s manual-invoice flow.';
ELSE
BEGIN
    INSERT INTO ValuationInvoices
        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,
         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)
    VALUES
        ('91d93c19fd6d42f68286f97f02d0a3e2', @ProjectId, NULL, 1, 'VI-0001', '2025-11-01T00:00:00+00:00', 21116.84, 21116.84, 2, '2025-11-19T00:00:00+00:00', '2025-11-19T00:00:00+00:00', '2025-11-19T00:00:00+00:00', 0, 1),
        ('80da1f38c6ec44ff9ae3e752ff2c9a84', @ProjectId, NULL, 2, 'VI-0002', '2025-12-01T00:00:00+00:00', 53137.11, 53137.11, 2, '2025-12-22T00:00:00+00:00', '2025-12-22T00:00:00+00:00', '2025-12-22T00:00:00+00:00', 0, 1),
        ('f076e36fed8f4b379e960c724ac36c52', @ProjectId, NULL, 3, 'VI-0003', '2026-02-01T00:00:00+00:00', 19076.95, 19076.95, 2, '2026-02-02T00:00:00+00:00', '2026-02-02T00:00:00+00:00', '2026-02-02T00:00:00+00:00', 0, 1),
        ('d014b8914fe74df293a7ee9ad0dd7e44', @ProjectId, NULL, 4, 'VI-0004', '2026-03-01T00:00:00+00:00', 55424.74, 55424.74, 2, '2026-03-09T00:00:00+00:00', '2026-03-09T00:00:00+00:00', '2026-03-09T00:00:00+00:00', 0, 1),
        ('6abcf8be359f4147b9e0b97bf17522fa', @ProjectId, NULL, 5, 'VI-0005', '2026-04-01T00:00:00+00:00', 44121.56, 44121.56, 2, '2026-04-13T00:00:00+00:00', '2026-04-13T00:00:00+00:00', '2026-04-13T00:00:00+00:00', 0, 1),
        ('fddd474a25dd4c01ac754346998739ac', @ProjectId, NULL, 6, 'VI-0006', '2026-05-01T00:00:00+00:00', 13836.18, 13836.18, 2, '2026-05-05T00:00:00+00:00', '2026-05-05T00:00:00+00:00', '2026-05-05T00:00:00+00:00', 0, 1),
        ('aa69c6c1782c4ddbb16cd1a0cd7fc50e', @ProjectId, NULL, 7, 'VI-0007', '2026-06-01T00:00:00+00:00', 18113.65, 18113.65, 2, '2026-06-10T00:00:00+00:00', '2026-06-10T00:00:00+00:00', '2026-06-10T00:00:00+00:00', 0, 1),
        ('a14a4c50dcee4b0dbd8b629b9e9f2263', @ProjectId, NULL, 8, 'VI-0008', '2026-07-01T00:00:00+00:00', 30415.74, 30415.74, 2, '2026-07-16T00:00:00+00:00', '2026-07-16T00:00:00+00:00', '2026-07-16T00:00:00+00:00', 0, 1);
    INSERT INTO ValuationInvoiceEvents
        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)
    VALUES
        ('a46391c7fa844c248f6a59161ae1329b', '91d93c19fd6d42f68286f97f02d0a3e2', 8, '2025-11-19T00:00:00+00:00', 'Backfilled from Xero invoice INV-0180 — Woodhouse Lane - Valuation 01. Historic completed works (accounts export, Aug 2026).', 21116.84),
        ('49f0cabd47214a73945f2e0bf18d1151', '80da1f38c6ec44ff9ae3e752ff2c9a84', 8, '2025-12-22T00:00:00+00:00', 'Backfilled from Xero invoice INV-0189 — Woodhouse Lane - Valuation 02. Historic completed works (accounts export, Aug 2026).', 53137.11),
        ('95372143f82544e4ac3cc8211630df56', 'f076e36fed8f4b379e960c724ac36c52', 8, '2026-02-02T00:00:00+00:00', 'Backfilled from Xero invoice INV-0194 — Woodhouse Lane - Valuation 02. Historic completed works (accounts export, Aug 2026).', 19076.95),
        ('5a6fc632c4694ae5b6efe73fe559afd7', 'd014b8914fe74df293a7ee9ad0dd7e44', 8, '2026-03-09T00:00:00+00:00', 'Backfilled from Xero invoice INV-0199 — Woodhouse Lane - Valuation 04. Historic completed works (accounts export, Aug 2026).', 55424.74),
        ('9122b1adff424ae9be7e678299f559e4', '6abcf8be359f4147b9e0b97bf17522fa', 8, '2026-04-13T00:00:00+00:00', 'Backfilled from Xero invoice INV-0203 — Woodhouse Lane - Valuation 05. Historic completed works (accounts export, Aug 2026).', 44121.56),
        ('c1f2fae3bc9e48229c41d1c6db930942', 'fddd474a25dd4c01ac754346998739ac', 8, '2026-05-05T00:00:00+00:00', 'Backfilled from Xero invoice INV-0207 — Woodhouse Lane - Valuation 06. Historic completed works (accounts export, Aug 2026).', 13836.18),
        ('0349fddc60b4464dafe22c01458d7f8c', 'aa69c6c1782c4ddbb16cd1a0cd7fc50e', 8, '2026-06-10T00:00:00+00:00', 'Backfilled from Xero invoice INV-0212 — Woodhouse Lane - Valuation 07. Historic completed works (accounts export, Aug 2026).', 18113.65),
        ('635c5d1fb03448caacaee126ff077d8e', 'a14a4c50dcee4b0dbd8b629b9e9f2263', 8, '2026-07-16T00:00:00+00:00', 'Backfilled from Xero invoice INV-0215 — Woodhouse Lane - Valuation 07. Historic completed works (accounts export, Aug 2026).', 30415.74);
    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + 255242.77 WHERE ProjectId = @ProjectId;
    PRINT 'OK    Woodhouse Lane — 8 invoices backfilled, net 255,242.77 (paid 255,242.77).';
END
COMMIT;

GO
-- Sanity check: certified (issued+paid) per backfilled project, A-Z.
SELECT p.Name, COUNT(*) AS Invoices, SUM(vi.Amount) AS Certified, SUM(vi.AmountPaid) AS Paid
FROM ValuationInvoices vi JOIN Projects p ON p.ProjectId = vi.ProjectId
WHERE vi.IsManual = 1 GROUP BY p.Name ORDER BY p.Name;
