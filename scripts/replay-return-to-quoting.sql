-- replay-return-to-quoting.sql (v3 — results as a grid; the portal editor hides PRINT)
-- Replays ReturnVariationOrderToQuotingHandler's steps against the real VO, timing each.
-- Write test is BEGIN TRAN ... ROLLBACK — nothing is changed.
SET NOCOUNT ON;
SET LOCK_TIMEOUT 8000;

DECLARE @out TABLE (step nvarchar(40), ms int, rows_found int NULL, note nvarchar(200) NULL);
DECLARE @voId nvarchar(64) = '9a5c2987e6844d949b354f97db3b8f27';
DECLARE @projectId nvarchar(64), @vref nvarchar(32), @costCode nvarchar(64), @status int;
DECLARE @t datetime2, @lineId nvarchar(64), @n int;

SET @t = SYSUTCDATETIME();
SELECT @projectId = ProjectId, @vref = VariationRef, @costCode = CostCode, @status = Status
FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @voId;
INSERT @out VALUES ('0 load-order', DATEDIFF(ms, @t, SYSUTCDATETIME()), NULL,
                    CONCAT('status=', @status, ' vref=', ISNULL(@vref,'NULL')));

SET @t = SYSUTCDATETIME();
SELECT @n = COUNT(*) FROM WorkOrders WHERE VariationOrderId = @voId;
INSERT @out VALUES ('1 workorders', DATEDIFF(ms, @t, SYSUTCDATETIME()), @n, NULL);

SET @t = SYSUTCDATETIME();
SELECT @n = COUNT(*) FROM QsAccruals
WHERE ProjectId = @projectId AND Category = N'Variation'
  AND Description LIKE @vref + N' — %'
  AND Description LIKE N'%(revised %' AND Description LIKE N'% → %';
INSERT @out VALUES ('2 qsaccruals-revised', DATEDIFF(ms, @t, SYSUTCDATETIME()), @n,
                    'rows>0 here would BLOCK the return');

SET @t = SYSUTCDATETIME();
SELECT @lineId = MAX(ValuationLineItemId), @n = COUNT(*)
FROM ValuationLineItems
WHERE ProjectId = @projectId AND ElementType = 3 AND VariationRef = @vref;
INSERT @out VALUES ('3 valuationlines', DATEDIFF(ms, @t, SYSUTCDATETIME()), @n,
                    'ElementType 3 = Variation (corrected)');

SET @t = SYSUTCDATETIME();
SELECT @n = COUNT(*) FROM ClaimLines WHERE ValuationLineItemId = @lineId;
INSERT @out VALUES ('4 claimlines', DATEDIFF(ms, @t, SYSUTCDATETIME()), @n, NULL);

SET @t = SYSUTCDATETIME();
SELECT @n = COUNT(*) FROM QsAccruals
WHERE ProjectId = @projectId AND Category = N'Variation'
  AND Description LIKE @vref + N' — %'
  AND Description NOT LIKE N'%(rejected)%';
INSERT @out VALUES ('5 qsaccruals-approval', DATEDIFF(ms, @t, SYSUTCDATETIME()), @n, NULL);

SET @t = SYSUTCDATETIME();
SELECT @n = COUNT(*) FROM CostCodeBudgets WHERE ProjectId = @projectId AND CostCode = @costCode;
INSERT @out VALUES ('6 budget', DATEDIFF(ms, @t, SYSUTCDATETIME()), @n, NULL);

SET @t = SYSUTCDATETIME();
BEGIN TRAN;
    UPDATE VariationOrderQuotes SET Title = Title WHERE VariationOrderQuoteId = @voId;
    IF @lineId IS NOT NULL
        UPDATE ValuationLineItems SET Comments = Comments WHERE ValuationLineItemId = @lineId;
    UPDATE QsAccruals SET Description = Description
    WHERE ProjectId = @projectId AND Category = N'Variation' AND Description LIKE @vref + N' — %';
ROLLBACK TRAN;
INSERT @out VALUES ('7 write-test', DATEDIFF(ms, @t, SYSUTCDATETIME()), NULL, 'rolled back');

SELECT step, ms, rows_found, note FROM @out ORDER BY step;
