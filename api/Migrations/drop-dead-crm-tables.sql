-- Drop the six prototype CRM tables nothing reads (2026-09-21, Nigel's answer to the security
-- review's last data-protection question). They are left over from the May sales prototype; the
-- live sales pipeline replaced them in September and no code has read or written them since.
-- They still hold prospects' notes and attendee email addresses, which is why they go rather
-- than sit there.
--
-- NOT TOUCHED: SalesProposals. That is the LIVE proposals table. The dead one is Proposals.
--
-- Guarded and idempotent: every drop is skipped if the table has already gone, so re-running is
-- safe. Counts are printed before each drop so the log records what was destroyed.

SET NOCOUNT ON;

DECLARE @rows int;

IF OBJECT_ID('dbo.QualificationAssessments', 'U') IS NOT NULL
BEGIN
    SELECT @rows = COUNT(*) FROM dbo.QualificationAssessments;
    PRINT CONCAT('QualificationAssessments: dropping, ', @rows, ' row(s)');
    DROP TABLE dbo.QualificationAssessments;
END
ELSE PRINT 'QualificationAssessments: already gone';

IF OBJECT_ID('dbo.SiteVisits', 'U') IS NOT NULL
BEGIN
    SELECT @rows = COUNT(*) FROM dbo.SiteVisits;
    PRINT CONCAT('SiteVisits: dropping, ', @rows, ' row(s)');
    DROP TABLE dbo.SiteVisits;
END
ELSE PRINT 'SiteVisits: already gone';

IF OBJECT_ID('dbo.InfoChaseItems', 'U') IS NOT NULL
BEGIN
    SELECT @rows = COUNT(*) FROM dbo.InfoChaseItems;
    PRINT CONCAT('InfoChaseItems: dropping, ', @rows, ' row(s)');
    DROP TABLE dbo.InfoChaseItems;
END
ELSE PRINT 'InfoChaseItems: already gone';

IF OBJECT_ID('dbo.BidDecisions', 'U') IS NOT NULL
BEGIN
    SELECT @rows = COUNT(*) FROM dbo.BidDecisions;
    PRINT CONCAT('BidDecisions: dropping, ', @rows, ' row(s)');
    DROP TABLE dbo.BidDecisions;
END
ELSE PRINT 'BidDecisions: already gone';

IF OBJECT_ID('dbo.Proposals', 'U') IS NOT NULL
BEGIN
    SELECT @rows = COUNT(*) FROM dbo.Proposals;
    PRINT CONCAT('Proposals: dropping, ', @rows, ' row(s)');
    DROP TABLE dbo.Proposals;
END
ELSE PRINT 'Proposals: already gone';

IF OBJECT_ID('dbo.LeadOutcomes', 'U') IS NOT NULL
BEGIN
    SELECT @rows = COUNT(*) FROM dbo.LeadOutcomes;
    PRINT CONCAT('LeadOutcomes: dropping, ', @rows, ' row(s)');
    DROP TABLE dbo.LeadOutcomes;
END
ELSE PRINT 'LeadOutcomes: already gone';

PRINT '--- remaining, should list nothing ---';
SELECT name FROM sys.tables
WHERE name IN ('QualificationAssessments','SiteVisits','InfoChaseItems','BidDecisions','Proposals','LeadOutcomes');

PRINT '--- SalesProposals, should still be here ---';
SELECT name FROM sys.tables WHERE name = 'SalesProposals';
