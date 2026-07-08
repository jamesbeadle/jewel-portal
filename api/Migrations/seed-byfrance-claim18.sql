-- ============================================================================
-- Seed: By France -- Claim 18 (Draft) with % complete from the Valuation 18
-- workbook ("By France - Valuation 18 - June 26").
-- ----------------------------------------------------------------------------
-- Project : By France, Leas Green, Chislehurst, BR7 6HD  (JBB-2026-001)
-- ProjectId: 3490f944b29545c4b8d5a04130f42ab8
--
-- Companion to seed-byfrance-valuation.sql (bf-cw-* / bf-pc-* / bf-cont-*) and
-- seed-byfrance-variations.sql (bf-vo-v*). Run those FIRST: every ClaimLine
-- here references one of their ValuationLineItemIds.
--
-- Seeds ONE claim (number 18, dated 26 Jun 2026) in DRAFT status so the
-- percentages remain editable on the Valuation Report tab, plus one ClaimLine
-- per counting valuation line carrying the workbook's cumulative Claim %.
-- Summary totals on the claim stay zero until preapproval (app convention).
-- Retention terms from the workbook: 5% held, 2.5% release.
--
-- Variations: the workbook prices each VO across several rows; JPMS seeds one
-- net line per VO, so each VO's % here is its workbook claimed value / net
-- (weighted average). Where a VO's omits are fully claimed but its additions
-- are not yet claimed, that weighted % falls OUTSIDE 0-100 -- intentional, so
-- that % x net reproduces the workbook's claimed value exactly. Edit those
-- through the app only once the additions are being claimed.
--
-- RECONCILIATION NOTE: the workbook's headline "Total Contract & Variation
-- Works Completed" (1,637,559.95) does not agree with its own variations
-- register subtotal (claimed -27,420.80, cell I744 of the export) by
-- 10,430.70. This seed follows the PER-LINE percentages and the register
-- subtotal, which are internally consistent:
--     works (cw + PC sums + contingency)  1,675,411.45
--     variations (net claimed)              -27,420.80
--     grand                               1,647,990.65
--
-- The Financials tab's Completion % / Expected Actual Cost read the LATEST
-- claim's cumulative claimed per cost centre, so running this populates them.
--
-- Idempotent: keyed on stable ids (bf-claim-18 / bf-cl18-*). A re-run resets
-- every % to the workbook values via MERGE (manual edits made in the app since
-- will be overwritten for this claim). Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationClaims] AS target
USING (VALUES
    (N'bf-claim-18', N'3490f944b29545c4b8d5a04130f42ab8', 18, '2026-06-26T00:00:00+00:00', 0, 5.00, 2.50)
) AS source (ValuationClaimId, ProjectId, ClaimNumber, ClaimDate, Status, RetentionPercent, RetentionReleasePercent)
ON target.ValuationClaimId = source.ValuationClaimId
WHEN MATCHED THEN UPDATE SET
    ProjectId               = source.ProjectId,
    ClaimNumber             = source.ClaimNumber,
    ClaimDate               = source.ClaimDate,
    Status                  = source.Status,
    RetentionPercent        = source.RetentionPercent,
    RetentionReleasePercent = source.RetentionReleasePercent
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ValuationClaimId, ProjectId, ClaimNumber, ClaimDate, Status,
            RetentionPercent, RetentionReleasePercent, PreapprovedAt, ConfirmedAt,
            ContractSum, NetVariations, RevisedContractSum, TotalWorksComplete,
            RetentionHeld, RetentionReleased, CertifiedToDate, PaymentDueExVat)
    VALUES (source.ValuationClaimId, source.ProjectId, source.ClaimNumber,
            source.ClaimDate, source.Status, source.RetentionPercent,
            source.RetentionReleasePercent, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0);
GO

MERGE INTO [dbo].[ClaimLines] AS target
USING (VALUES
    (N'bf-cl18-cw-001', N'bf-claim-18', N'bf-cw-001', 100, 65000.00, 65000.00),
    (N'bf-cl18-cw-002', N'bf-claim-18', N'bf-cw-002', 100, 9880.00, 9880.00),
    (N'bf-cl18-cw-003', N'bf-claim-18', N'bf-cw-003', 100, 600.00, 600.00),
    (N'bf-cl18-cw-004', N'bf-claim-18', N'bf-cw-004', 100, 4680.00, 4680.00),
    (N'bf-cl18-cw-005', N'bf-claim-18', N'bf-cw-005', 100, 1200.00, 1200.00),
    (N'bf-cl18-cw-006', N'bf-claim-18', N'bf-cw-006', 100, 6500.00, 6500.00),
    (N'bf-cl18-cw-007', N'bf-claim-18', N'bf-cw-007', 100, 30940.00, 30940.00),
    (N'bf-cl18-cw-008', N'bf-claim-18', N'bf-cw-008', 0, 0.00, 0.00),
    (N'bf-cl18-cw-009', N'bf-claim-18', N'bf-cw-009', 100, 600.00, 600.00),
    (N'bf-cl18-cw-010', N'bf-claim-18', N'bf-cw-010', 100, 750.00, 750.00),
    (N'bf-cl18-cw-011', N'bf-claim-18', N'bf-cw-011', 100, 500.00, 500.00),
    (N'bf-cl18-cw-012', N'bf-claim-18', N'bf-cw-012', 100, 240.00, 240.00),
    (N'bf-cl18-cw-013', N'bf-claim-18', N'bf-cw-013', 100, 520.00, 520.00),
    (N'bf-cl18-cw-014', N'bf-claim-18', N'bf-cw-014', 100, 360.00, 360.00),
    (N'bf-cl18-cw-015', N'bf-claim-18', N'bf-cw-015', 100, 4032.00, 4032.00),
    (N'bf-cl18-cw-016', N'bf-claim-18', N'bf-cw-016', 100, 3120.00, 3120.00),
    (N'bf-cl18-cw-017', N'bf-claim-18', N'bf-cw-017', 100, 324.00, 324.00),
    (N'bf-cl18-cw-018', N'bf-claim-18', N'bf-cw-018', 100, 520.00, 520.00),
    (N'bf-cl18-cw-019', N'bf-claim-18', N'bf-cw-019', 100, 3500.00, 3500.00),
    (N'bf-cl18-cw-020', N'bf-claim-18', N'bf-cw-020', 100, 14720.00, 14720.00),
    (N'bf-cl18-cw-021', N'bf-claim-18', N'bf-cw-021', 100, 2800.00, 2800.00),
    (N'bf-cl18-cw-022', N'bf-claim-18', N'bf-cw-022', 100, 2200.00, 2200.00),
    (N'bf-cl18-cw-023', N'bf-claim-18', N'bf-cw-023', 100, 5852.00, 5852.00),
    (N'bf-cl18-cw-024', N'bf-claim-18', N'bf-cw-024', 100, 450.00, 450.00),
    (N'bf-cl18-cw-025', N'bf-claim-18', N'bf-cw-025', 100, 1000.00, 1000.00),
    (N'bf-cl18-cw-026', N'bf-claim-18', N'bf-cw-026', 100, 2400.00, 2400.00),
    (N'bf-cl18-cw-027', N'bf-claim-18', N'bf-cw-027', 100, 750.00, 750.00),
    (N'bf-cl18-cw-028', N'bf-claim-18', N'bf-cw-028', 100, 10080.00, 10080.00),
    (N'bf-cl18-cw-029', N'bf-claim-18', N'bf-cw-029', 100, 3720.00, 3720.00),
    (N'bf-cl18-cw-030', N'bf-claim-18', N'bf-cw-030', 100, 6090.00, 6090.00),
    (N'bf-cl18-cw-031', N'bf-claim-18', N'bf-cw-031', 100, 13050.00, 13050.00),
    (N'bf-cl18-cw-032', N'bf-claim-18', N'bf-cw-032', 100, 4500.00, 4500.00),
    (N'bf-cl18-cw-033', N'bf-claim-18', N'bf-cw-033', 100, 650.00, 650.00),
    (N'bf-cl18-cw-034', N'bf-claim-18', N'bf-cw-034', 100, 900.00, 900.00),
    (N'bf-cl18-cw-035', N'bf-claim-18', N'bf-cw-035', 100, 3344.00, 3344.00),
    (N'bf-cl18-cw-036', N'bf-claim-18', N'bf-cw-036', 100, 3750.00, 3750.00),
    (N'bf-cl18-cw-037', N'bf-claim-18', N'bf-cw-037', 100, 4350.00, 4350.00),
    (N'bf-cl18-cw-038', N'bf-claim-18', N'bf-cw-038', 100, 3480.00, 3480.00),
    (N'bf-cl18-cw-039', N'bf-claim-18', N'bf-cw-039', 100, 2300.00, 2300.00),
    (N'bf-cl18-cw-040', N'bf-claim-18', N'bf-cw-040', 100, 500.00, 500.00),
    (N'bf-cl18-cw-041', N'bf-claim-18', N'bf-cw-041', 100, 2500.00, 2500.00),
    (N'bf-cl18-cw-042', N'bf-claim-18', N'bf-cw-042', 100, 23095.00, 23095.00),
    (N'bf-cl18-cw-043', N'bf-claim-18', N'bf-cw-043', 100, 1296.00, 1296.00),
    (N'bf-cl18-cw-044', N'bf-claim-18', N'bf-cw-044', 100, 10260.00, 10260.00),
    (N'bf-cl18-cw-045', N'bf-claim-18', N'bf-cw-045', 100, 1008.00, 1008.00),
    (N'bf-cl18-cw-046', N'bf-claim-18', N'bf-cw-046', 100, 10304.00, 10304.00),
    (N'bf-cl18-cw-047', N'bf-claim-18', N'bf-cw-047', 100, 41216.00, 41216.00),
    (N'bf-cl18-cw-048', N'bf-claim-18', N'bf-cw-048', 100, 2208.00, 2208.00),
    (N'bf-cl18-cw-049', N'bf-claim-18', N'bf-cw-049', 100, 16184.00, 16184.00),
    (N'bf-cl18-cw-050', N'bf-claim-18', N'bf-cw-050', 100, 3392.00, 3392.00),
    (N'bf-cl18-cw-051', N'bf-claim-18', N'bf-cw-051', 100, 30024.00, 30024.00),
    (N'bf-cl18-cw-052', N'bf-claim-18', N'bf-cw-052', 100, 51168.00, 51168.00),
    (N'bf-cl18-cw-053', N'bf-claim-18', N'bf-cw-053', 100, 11868.00, 11868.00),
    (N'bf-cl18-cw-054', N'bf-claim-18', N'bf-cw-054', 100, 612.00, 612.00),
    (N'bf-cl18-cw-055', N'bf-claim-18', N'bf-cw-055', 100, 900.00, 900.00),
    (N'bf-cl18-cw-056', N'bf-claim-18', N'bf-cw-056', 100, 1024.00, 1024.00),
    (N'bf-cl18-cw-057', N'bf-claim-18', N'bf-cw-057', 100, 1320.00, 1320.00),
    (N'bf-cl18-cw-058', N'bf-claim-18', N'bf-cw-058', 100, 3216.00, 3216.00),
    (N'bf-cl18-cw-059', N'bf-claim-18', N'bf-cw-059', 100, 3136.00, 3136.00),
    (N'bf-cl18-cw-060', N'bf-claim-18', N'bf-cw-060', 100, 450.00, 450.00),
    (N'bf-cl18-cw-061', N'bf-claim-18', N'bf-cw-061', 100, 132300.00, 132300.00),
    (N'bf-cl18-cw-062', N'bf-claim-18', N'bf-cw-062', 100, 375.00, 375.00),
    (N'bf-cl18-cw-063', N'bf-claim-18', N'bf-cw-063', 100, 3610.00, 3610.00),
    (N'bf-cl18-cw-064', N'bf-claim-18', N'bf-cw-064', 100, 1320.00, 1320.00),
    (N'bf-cl18-cw-065', N'bf-claim-18', N'bf-cw-065', 100, 3500.00, 3500.00),
    (N'bf-cl18-cw-066', N'bf-claim-18', N'bf-cw-066', 100, 22528.00, 22528.00),
    (N'bf-cl18-cw-067', N'bf-claim-18', N'bf-cw-067', 100, 42312.00, 42312.00),
    (N'bf-cl18-cw-068', N'bf-claim-18', N'bf-cw-068', 100, 12980.00, 12980.00),
    (N'bf-cl18-cw-069', N'bf-claim-18', N'bf-cw-069', 100, 5460.00, 5460.00),
    (N'bf-cl18-cw-070', N'bf-claim-18', N'bf-cw-070', 100, 2080.00, 2080.00),
    (N'bf-cl18-cw-071', N'bf-claim-18', N'bf-cw-071', 100, 5040.00, 5040.00),
    (N'bf-cl18-cw-072', N'bf-claim-18', N'bf-cw-072', 100, 4500.00, 4500.00),
    (N'bf-cl18-cw-073', N'bf-claim-18', N'bf-cw-073', 100, 144.00, 144.00),
    (N'bf-cl18-cw-074', N'bf-claim-18', N'bf-cw-074', 100, 8220.00, 8220.00),
    (N'bf-cl18-cw-075', N'bf-claim-18', N'bf-cw-075', 100, 800.00, 800.00),
    (N'bf-cl18-cw-076', N'bf-claim-18', N'bf-cw-076', 100, 1400.00, 1400.00),
    (N'bf-cl18-cw-077', N'bf-claim-18', N'bf-cw-077', 100, 41328.00, 41328.00),
    (N'bf-cl18-cw-078', N'bf-claim-18', N'bf-cw-078', 100, 12080.00, 12080.00),
    (N'bf-cl18-cw-079', N'bf-claim-18', N'bf-cw-079', 100, 8456.00, 8456.00),
    (N'bf-cl18-cw-080', N'bf-claim-18', N'bf-cw-080', 100, 2640.00, 2640.00),
    (N'bf-cl18-cw-081', N'bf-claim-18', N'bf-cw-081', 100, 17712.00, 17712.00),
    (N'bf-cl18-cw-082', N'bf-claim-18', N'bf-cw-082', 100, 2125.00, 2125.00),
    (N'bf-cl18-cw-083', N'bf-claim-18', N'bf-cw-083', 100, 8008.00, 8008.00),
    (N'bf-cl18-cw-084', N'bf-claim-18', N'bf-cw-084', 100, 1200.00, 1200.00),
    (N'bf-cl18-cw-085', N'bf-claim-18', N'bf-cw-085', 100, 5280.00, 5280.00),
    (N'bf-cl18-cw-086', N'bf-claim-18', N'bf-cw-086', 100, 15840.00, 15840.00),
    (N'bf-cl18-cw-087', N'bf-claim-18', N'bf-cw-087', 100, 5000.00, 5000.00),
    (N'bf-cl18-cw-088', N'bf-claim-18', N'bf-cw-088', 70, 1075.20, 1075.20),
    (N'bf-cl18-cw-089', N'bf-claim-18', N'bf-cw-089', 100, 4828.00, 4828.00),
    (N'bf-cl18-cw-090', N'bf-claim-18', N'bf-cw-090', 100, 510.00, 510.00),
    (N'bf-cl18-cw-091', N'bf-claim-18', N'bf-cw-091', 100, 12880.00, 12880.00),
    (N'bf-cl18-cw-092', N'bf-claim-18', N'bf-cw-092', 100, 5152.00, 5152.00),
    (N'bf-cl18-cw-093', N'bf-claim-18', N'bf-cw-093', 100, 21896.00, 21896.00),
    (N'bf-cl18-cw-094', N'bf-claim-18', N'bf-cw-094', 100, 6944.00, 6944.00),
    (N'bf-cl18-cw-095', N'bf-claim-18', N'bf-cw-095', 100, 9052.00, 9052.00),
    (N'bf-cl18-cw-096', N'bf-claim-18', N'bf-cw-096', 100, 2336.00, 2336.00),
    (N'bf-cl18-cw-097', N'bf-claim-18', N'bf-cw-097', 100, 1440.00, 1440.00),
    (N'bf-cl18-cw-098', N'bf-claim-18', N'bf-cw-098', 100, 10640.00, 10640.00),
    (N'bf-cl18-cw-099', N'bf-claim-18', N'bf-cw-099', 100, 5960.00, 5960.00),
    (N'bf-cl18-cw-100', N'bf-claim-18', N'bf-cw-100', 100, 12340.00, 12340.00),
    (N'bf-cl18-cw-101', N'bf-claim-18', N'bf-cw-101', 100, 10640.00, 10640.00),
    (N'bf-cl18-cw-102', N'bf-claim-18', N'bf-cw-102', 100, 18300.00, 18300.00),
    (N'bf-cl18-cw-103', N'bf-claim-18', N'bf-cw-103', 75, 16706.25, 16706.25),
    (N'bf-cl18-cw-104', N'bf-claim-18', N'bf-cw-104', 50, 4256.00, 4256.00),
    (N'bf-cl18-cw-105', N'bf-claim-18', N'bf-cw-105', 100, 35000.00, 35000.00),
    (N'bf-cl18-cw-106', N'bf-claim-18', N'bf-cw-106', 100, 2500.00, 2500.00),
    (N'bf-cl18-cw-107', N'bf-claim-18', N'bf-cw-107', 100, 45000.00, 45000.00),
    (N'bf-cl18-cw-108', N'bf-claim-18', N'bf-cw-108', 100, 11830.00, 11830.00),
    (N'bf-cl18-cw-109', N'bf-claim-18', N'bf-cw-109', 100, 1640.00, 1640.00),
    (N'bf-cl18-cw-110', N'bf-claim-18', N'bf-cw-110', 75, 2610.00, 2610.00),
    (N'bf-cl18-cw-111', N'bf-claim-18', N'bf-cw-111', 75, 5436.00, 5436.00),
    (N'bf-cl18-cw-112', N'bf-claim-18', N'bf-cw-112', 75, 864.00, 864.00),
    (N'bf-cl18-cw-113', N'bf-claim-18', N'bf-cw-113', 100, 9280.00, 9280.00),
    (N'bf-cl18-cw-114', N'bf-claim-18', N'bf-cw-114', 100, 1000.00, 1000.00),
    (N'bf-cl18-cw-115', N'bf-claim-18', N'bf-cw-115', 75, 4905.00, 4905.00),
    (N'bf-cl18-cw-116', N'bf-claim-18', N'bf-cw-116', 75, 2610.00, 2610.00),
    (N'bf-cl18-cw-117', N'bf-claim-18', N'bf-cw-117', 100, 45724.00, 45724.00),
    (N'bf-cl18-cw-118', N'bf-claim-18', N'bf-cw-118', 100, 1200.00, 1200.00),
    (N'bf-cl18-cw-119', N'bf-claim-18', N'bf-cw-119', 100, 3510.00, 3510.00),
    (N'bf-cl18-cw-120', N'bf-claim-18', N'bf-cw-120', 75, 1316.25, 1316.25),
    (N'bf-cl18-cw-121', N'bf-claim-18', N'bf-cw-121', 75, 2475.00, 2475.00),
    (N'bf-cl18-cw-122', N'bf-claim-18', N'bf-cw-122', 85, 1317.50, 1317.50),
    (N'bf-cl18-cw-123', N'bf-claim-18', N'bf-cw-123', 85, 12512.00, 12512.00),
    (N'bf-cl18-cw-124', N'bf-claim-18', N'bf-cw-124', 85, 204.00, 204.00),
    (N'bf-cl18-cw-125', N'bf-claim-18', N'bf-cw-125', 85, 2210.00, 2210.00),
    (N'bf-cl18-cw-126', N'bf-claim-18', N'bf-cw-126', 85, 204.00, 204.00),
    (N'bf-cl18-cw-127', N'bf-claim-18', N'bf-cw-127', 85, 565.25, 565.25),
    (N'bf-cl18-cw-128', N'bf-claim-18', N'bf-cw-128', 85, 18727.20, 18727.20),
    (N'bf-cl18-cw-129', N'bf-claim-18', N'bf-cw-129', 85, 2550.00, 2550.00),
    (N'bf-cl18-cw-130', N'bf-claim-18', N'bf-cw-130', 85, 198.90, 198.90),
    (N'bf-cl18-cw-131', N'bf-claim-18', N'bf-cw-131', 85, 1530.00, 1530.00),
    (N'bf-cl18-cw-132', N'bf-claim-18', N'bf-cw-132', 85, 2975.00, 2975.00),
    (N'bf-cl18-cw-133', N'bf-claim-18', N'bf-cw-133', 85, 1519.80, 1519.80),
    (N'bf-cl18-cw-134', N'bf-claim-18', N'bf-cw-134', 85, 1428.00, 1428.00),
    (N'bf-cl18-cw-135', N'bf-claim-18', N'bf-cw-135', 85, 1147.50, 1147.50),
    (N'bf-cl18-cw-136', N'bf-claim-18', N'bf-cw-136', 85, 200.60, 200.60),
    (N'bf-cl18-cw-137', N'bf-claim-18', N'bf-cw-137', 85, 765.00, 765.00),
    (N'bf-cl18-cw-138', N'bf-claim-18', N'bf-cw-138', 85, 850.00, 850.00),
    (N'bf-cl18-cw-139', N'bf-claim-18', N'bf-cw-139', 85, 425.00, 425.00),
    (N'bf-cl18-cw-140', N'bf-claim-18', N'bf-cw-140', 100, 7500.00, 7500.00),
    (N'bf-cl18-cw-141', N'bf-claim-18', N'bf-cw-141', 100, 20000.00, 20000.00),
    (N'bf-cl18-cw-142', N'bf-claim-18', N'bf-cw-142', 90, 4500.00, 4500.00),
    (N'bf-cl18-cw-143', N'bf-claim-18', N'bf-cw-143', 100, 3000.00, 3000.00),
    (N'bf-cl18-cw-144', N'bf-claim-18', N'bf-cw-144', 100, 2500.00, 2500.00),
    (N'bf-cl18-cw-145', N'bf-claim-18', N'bf-cw-145', 100, 4500.00, 4500.00),
    (N'bf-cl18-cw-146', N'bf-claim-18', N'bf-cw-146', 100, 4640.00, 4640.00),
    (N'bf-cl18-cw-147', N'bf-claim-18', N'bf-cw-147', 100, 4800.00, 4800.00),
    (N'bf-cl18-cw-148', N'bf-claim-18', N'bf-cw-148', 50, 4788.00, 4788.00),
    (N'bf-cl18-cw-149', N'bf-claim-18', N'bf-cw-149', 50, 7320.00, 7320.00),
    (N'bf-cl18-cw-150', N'bf-claim-18', N'bf-cw-150', 0, 0.00, 0.00),
    (N'bf-cl18-cw-151', N'bf-claim-18', N'bf-cw-151', 0, 0.00, 0.00),
    (N'bf-cl18-cw-152', N'bf-claim-18', N'bf-cw-152', 100, 9000.00, 9000.00),
    (N'bf-cl18-cw-153', N'bf-claim-18', N'bf-cw-153', 100, 27170.00, 27170.00),
    (N'bf-cl18-cw-154', N'bf-claim-18', N'bf-cw-154', 100, 12896.00, 12896.00),
    (N'bf-cl18-cw-155', N'bf-claim-18', N'bf-cw-155', 100, 1575.00, 1575.00),
    (N'bf-cl18-cw-156', N'bf-claim-18', N'bf-cw-156', 100, 3480.00, 3480.00),
    (N'bf-cl18-cw-157', N'bf-claim-18', N'bf-cw-157', 0, 0.00, 0.00),
    (N'bf-cl18-cw-158', N'bf-claim-18', N'bf-cw-158', 100, 12544.00, 12544.00),
    (N'bf-cl18-cw-159', N'bf-claim-18', N'bf-cw-159', 80, 14960.00, 14960.00),
    (N'bf-cl18-cw-160', N'bf-claim-18', N'bf-cw-160', 0, 0.00, 0.00),
    (N'bf-cl18-cw-161', N'bf-claim-18', N'bf-cw-161', 0, 0.00, 0.00),
    (N'bf-cl18-cw-162', N'bf-claim-18', N'bf-cw-162', 0, 0.00, 0.00),
    (N'bf-cl18-cw-163', N'bf-claim-18', N'bf-cw-163', 100, 9600.00, 9600.00),
    (N'bf-cl18-cw-164', N'bf-claim-18', N'bf-cw-164', 0, 0.00, 0.00),
    (N'bf-cl18-pc-01', N'bf-claim-18', N'bf-pc-01', 100, 18216.00, 18216.00),
    (N'bf-cl18-pc-02', N'bf-claim-18', N'bf-pc-02', 100, 3256.00, 3256.00),
    (N'bf-cl18-pc-03', N'bf-claim-18', N'bf-pc-03', 100, 2560.00, 2560.00),
    (N'bf-cl18-pc-04', N'bf-claim-18', N'bf-pc-04', 100, 7600.00, 7600.00),
    (N'bf-cl18-pc-05', N'bf-claim-18', N'bf-pc-05', 100, 1700.00, 1700.00),
    (N'bf-cl18-pc-06', N'bf-claim-18', N'bf-pc-06', 100, 32900.00, 32900.00),
    (N'bf-cl18-pc-07', N'bf-claim-18', N'bf-pc-07', 100, 6600.00, 6600.00),
    (N'bf-cl18-pc-08', N'bf-claim-18', N'bf-pc-08', 100, 6250.00, 6250.00),
    (N'bf-cl18-pc-09', N'bf-claim-18', N'bf-pc-09', 100, 12600.00, 12600.00),
    (N'bf-cl18-pc-10', N'bf-claim-18', N'bf-pc-10', 100, 11000.00, 11000.00),
    (N'bf-cl18-pc-11', N'bf-claim-18', N'bf-pc-11', 100, 34500.00, 34500.00),
    (N'bf-cl18-pc-12', N'bf-claim-18', N'bf-pc-12', 100, 5500.00, 5500.00),
    (N'bf-cl18-pc-13', N'bf-claim-18', N'bf-pc-13', 100, 5500.00, 5500.00),
    (N'bf-cl18-pc-14', N'bf-claim-18', N'bf-pc-14', 100, 16500.00, 16500.00),
    (N'bf-cl18-pc-15', N'bf-claim-18', N'bf-pc-15', 100, 11000.00, 11000.00),
    (N'bf-cl18-pc-16', N'bf-claim-18', N'bf-pc-16', 100, 2750.00, 2750.00),
    (N'bf-cl18-pc-17', N'bf-claim-18', N'bf-pc-17', 100, 11000.00, 11000.00),
    (N'bf-cl18-pc-18', N'bf-claim-18', N'bf-pc-18', 100, 42000.00, 42000.00),
    (N'bf-cl18-pc-19', N'bf-claim-18', N'bf-pc-19', 100, 22000.00, 22000.00),
    (N'bf-cl18-pc-20', N'bf-claim-18', N'bf-pc-20', 100, 5000.00, 5000.00),
    (N'bf-cl18-pc-21', N'bf-claim-18', N'bf-pc-21', 100, 17200.00, 17200.00),
    (N'bf-cl18-pc-22', N'bf-claim-18', N'bf-pc-22', 100, 20000.00, 20000.00),
    (N'bf-cl18-pc-23', N'bf-claim-18', N'bf-pc-23', 100, 2250.00, 2250.00),
    (N'bf-cl18-cont-01', N'bf-claim-18', N'bf-cont-01', 100, 50000.00, 50000.00),
    (N'bf-cl18-vo-v01', N'bf-claim-18', N'bf-vo-v01', 100, -5500.00, -5500.00),
    (N'bf-cl18-vo-v03', N'bf-claim-18', N'bf-vo-v03', 100, 27797.00, 27797.00),
    (N'bf-cl18-vo-v04', N'bf-claim-18', N'bf-vo-v04', 100, 1135.00, 1135.00),
    (N'bf-cl18-vo-v05', N'bf-claim-18', N'bf-vo-v05', 100, 2860.00, 2860.00),
    (N'bf-cl18-vo-v06', N'bf-claim-18', N'bf-vo-v06', 100, 1610.00, 1610.00),
    (N'bf-cl18-vo-v07', N'bf-claim-18', N'bf-vo-v07', 100, 1360.00, 1360.00),
    (N'bf-cl18-vo-v08', N'bf-claim-18', N'bf-vo-v08', 100, 1360.00, 1360.00),
    (N'bf-cl18-vo-v09', N'bf-claim-18', N'bf-vo-v09', 100, 1360.00, 1360.00),
    (N'bf-cl18-vo-v10', N'bf-claim-18', N'bf-vo-v10', 100, 5116.00, 5116.00),
    (N'bf-cl18-vo-v11', N'bf-claim-18', N'bf-vo-v11', 100, 4125.00, 4125.00),
    (N'bf-cl18-vo-v12', N'bf-claim-18', N'bf-vo-v12', 100, 2100.00, 2100.00),
    (N'bf-cl18-vo-v13', N'bf-claim-18', N'bf-vo-v13', 100, 6396.00, 6396.00),
    (N'bf-cl18-vo-v14', N'bf-claim-18', N'bf-vo-v14', 100, 42936.00, 42936.00),
    (N'bf-cl18-vo-v15', N'bf-claim-18', N'bf-vo-v15', 100, 5860.00, 5860.00),
    (N'bf-cl18-vo-v16', N'bf-claim-18', N'bf-vo-v16', -28.0824, -7400.00, -7400.00),
    (N'bf-cl18-vo-v17', N'bf-claim-18', N'bf-vo-v17', 100, 1720.00, 1720.00),
    (N'bf-cl18-vo-v18', N'bf-claim-18', N'bf-vo-v18', 76.7568, 8807.15, 8807.15),
    (N'bf-cl18-vo-v20', N'bf-claim-18', N'bf-vo-v20', 100, -22200.00, -22200.00),
    (N'bf-cl18-vo-v21', N'bf-claim-18', N'bf-vo-v21', 100, 400.00, 400.00),
    (N'bf-cl18-vo-v22', N'bf-claim-18', N'bf-vo-v22', 100, 7600.00, 7600.00),
    (N'bf-cl18-vo-v23', N'bf-claim-18', N'bf-vo-v23', 79.8194, 23573.05, 23573.05),
    (N'bf-cl18-vo-v24', N'bf-claim-18', N'bf-vo-v24', 56.0225, 2293.00, 2293.00),
    (N'bf-cl18-vo-v25', N'bf-claim-18', N'bf-vo-v25', 100, 3516.00, 3516.00),
    (N'bf-cl18-vo-v26', N'bf-claim-18', N'bf-vo-v26', 51.3866, 4586.25, 4586.25),
    (N'bf-cl18-vo-v27', N'bf-claim-18', N'bf-vo-v27', 109.8494, -7294.00, -7294.00),
    (N'bf-cl18-vo-v28', N'bf-claim-18', N'bf-vo-v28', 116.4885, -1526.00, -1526.00),
    (N'bf-cl18-vo-v29', N'bf-claim-18', N'bf-vo-v29', 100, 330.00, 330.00),
    (N'bf-cl18-vo-v30', N'bf-claim-18', N'bf-vo-v30', 76.0116, 22184.00, 22184.00),
    (N'bf-cl18-vo-v31', N'bf-claim-18', N'bf-vo-v31', 100, -12240.00, -12240.00),
    (N'bf-cl18-vo-v32', N'bf-claim-18', N'bf-vo-v32', 1296.2462, -21582.50, -21582.50),
    (N'bf-cl18-vo-v33', N'bf-claim-18', N'bf-vo-v33', 100, 1870.00, 1870.00),
    (N'bf-cl18-vo-v34', N'bf-claim-18', N'bf-vo-v34', 75, 8973.75, 8973.75),
    (N'bf-cl18-vo-v35', N'bf-claim-18', N'bf-vo-v35', 100, 8790.00, 8790.00),
    (N'bf-cl18-vo-v36', N'bf-claim-18', N'bf-vo-v36', 90.9953, 16320.00, 16320.00),
    (N'bf-cl18-vo-v37', N'bf-claim-18', N'bf-vo-v37', 100, 4245.00, 4245.00),
    (N'bf-cl18-vo-v38', N'bf-claim-18', N'bf-vo-v38', 100, 23650.00, 23650.00),
    (N'bf-cl18-vo-v40', N'bf-claim-18', N'bf-vo-v40', 90, 711.00, 711.00),
    (N'bf-cl18-vo-v41', N'bf-claim-18', N'bf-vo-v41', -210.1327, -40066.00, -40066.00),
    (N'bf-cl18-vo-v42', N'bf-claim-18', N'bf-vo-v42', 146.5494, -21362.50, -21362.50),
    (N'bf-cl18-vo-v43', N'bf-claim-18', N'bf-vo-v43', 105.5556, -20900.00, -20900.00),
    (N'bf-cl18-vo-v44', N'bf-claim-18', N'bf-vo-v44', 100, 2435.00, 2435.00),
    (N'bf-cl18-vo-v45', N'bf-claim-18', N'bf-vo-v45', 100, -50000.00, -50000.00),
    (N'bf-cl18-vo-v46', N'bf-claim-18', N'bf-vo-v46', 100, 4950.00, 4950.00),
    (N'bf-cl18-vo-v47', N'bf-claim-18', N'bf-vo-v47', 90, 517.50, 517.50),
    (N'bf-cl18-vo-v48', N'bf-claim-18', N'bf-vo-v48', 100, -60175.00, -60175.00),
    (N'bf-cl18-vo-v49', N'bf-claim-18', N'bf-vo-v49', 100, -5000.00, -5000.00),
    (N'bf-cl18-vo-v50', N'bf-claim-18', N'bf-vo-v50', 100, -16500.00, -16500.00),
    (N'bf-cl18-vo-v51', N'bf-claim-18', N'bf-vo-v51', 100, -2560.00, -2560.00),
    (N'bf-cl18-vo-v52', N'bf-claim-18', N'bf-vo-v52', 100, 8393.00, 8393.00),
    (N'bf-cl18-vo-v53', N'bf-claim-18', N'bf-vo-v53', 87.4396, 5430.00, 5430.00),
    (N'bf-cl18-vo-v55', N'bf-claim-18', N'bf-vo-v55', 75, 172.50, 172.50),
    (N'bf-cl18-vo-v56', N'bf-claim-18', N'bf-vo-v56', 100, 750.00, 750.00),
    (N'bf-cl18-vo-v57', N'bf-claim-18', N'bf-vo-v57', 80, 23440.00, 23440.00),
    (N'bf-cl18-vo-v58', N'bf-claim-18', N'bf-vo-v58', 100, 3907.00, 3907.00),
    (N'bf-cl18-vo-v60', N'bf-claim-18', N'bf-vo-v60', 100, 2940.00, 2940.00),
    (N'bf-cl18-vo-v61', N'bf-claim-18', N'bf-vo-v61', 100, 438.00, 438.00),
    (N'bf-cl18-vo-v62', N'bf-claim-18', N'bf-vo-v62', 82.2923, 4749.50, 4749.50),
    (N'bf-cl18-vo-v63', N'bf-claim-18', N'bf-vo-v63', 90, 3208.50, 3208.50),
    (N'bf-cl18-vo-v64', N'bf-claim-18', N'bf-vo-v64', -64.9669, -16470.00, -16470.00),
    (N'bf-cl18-vo-v66', N'bf-claim-18', N'bf-vo-v66', 100, -23845.00, -23845.00),
    (N'bf-cl18-vo-v69', N'bf-claim-18', N'bf-vo-v69', 0, 0.00, 0.00),
    (N'bf-cl18-vo-v72', N'bf-claim-18', N'bf-vo-v72', 100, 2285.00, 2285.00),
    (N'bf-cl18-vo-v73', N'bf-claim-18', N'bf-vo-v73', 0, 0.00, 0.00),
    (N'bf-cl18-vo-v76', N'bf-claim-18', N'bf-vo-v76', 0, 0.00, 0.00)
) AS source (ClaimLineId, ValuationClaimId, ValuationLineItemId, PercentComplete, CumulativeClaimed, PeriodIncrement)
ON target.ClaimLineId = source.ClaimLineId
WHEN MATCHED THEN UPDATE SET
    ValuationClaimId    = source.ValuationClaimId,
    ValuationLineItemId = source.ValuationLineItemId,
    PercentComplete     = source.PercentComplete,
    CumulativeClaimed   = source.CumulativeClaimed,
    PeriodIncrement     = source.PeriodIncrement
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ClaimLineId, ValuationClaimId, ValuationLineItemId, PercentComplete,
            CumulativeClaimed, PeriodIncrement)
    VALUES (source.ClaimLineId, source.ValuationClaimId, source.ValuationLineItemId,
            source.PercentComplete, source.CumulativeClaimed, source.PeriodIncrement);
GO

-- Sanity check: cumulative claimed should reconcile to the workbook.
--   Contract + PC sums + contingency = 1675411.45
--     (the workbook's "Total Works Complete" 1,625,411.45 excludes the
--      claimed 50,000 contingency; this figure includes it)
--   Variations (net claimed)         = -27420.80
--   Grand total                      = 1647990.65
--     (= workbook "Total Contract & Variation Works Completed")
SELECT
    SUM(CASE WHEN li.ElementType IN (0, 1, 2) THEN cl.CumulativeClaimed ELSE 0 END) AS WorksClaimed,
    SUM(CASE WHEN li.ElementType = 3 THEN cl.CumulativeClaimed ELSE 0 END)          AS VariationsClaimed,
    SUM(cl.CumulativeClaimed)                                                       AS TotalClaimed
FROM [dbo].[ClaimLines] cl
JOIN [dbo].[ValuationLineItems] li ON li.ValuationLineItemId = cl.ValuationLineItemId
WHERE cl.ValuationClaimId = N'bf-claim-18';
GO
