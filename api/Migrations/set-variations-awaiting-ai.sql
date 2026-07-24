/* ===========================================================================
   set-variations-awaiting-ai.sql
   Move selected variation orders to "Awaiting AI" (Architect's Instruction)

   Added 2026-07-24 alongside the new VariationOrderStatus.AwaitingArchitectInstruction (= 4).

   VariationOrderStatus is persisted as an int in the Status column of the unified
   variation table, dbo.VariationOrderQuotes (the DbSet is named VariationOrders,
   but ToTable("VariationOrderQuotes") is the physical table since the 2026-07-23
   UnifyVariationOrders migration):

        0 = Quoting        1 = Issued         2 = Approved
        3 = Rejected       4 = AwaitingArchitectInstruction  ("Awaiting AI")

   "Awaiting AI" is a side-effect-free, PRE-APPROVAL stage. This script therefore
   only moves rows currently in Quoting (0) or Issued (1). It deliberately refuses
   to touch Approved (2) or Rejected (3): those carry commercial writes (Valuation
   Report / CVR / cost-centre budget) that must only ever be unwound through the
   app's approve / reject / return-to-quoting flows — never by a raw UPDATE.

   It is safe to keep on file even with nothing to migrate: with an empty target
   list it reports "0 rows" and changes no data.

   Run:
     sqlcmd -S <server> -d <database> -i set-variations-awaiting-ai.sql        (Windows auth: add -E)
     sqlcmd -S <server> -d <database> -U <user> -P <pass> -i set-variations-awaiting-ai.sql
=========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* -- 1) Which variation orders to move --------------------------------------
   Add one INSERT per order, using the human reference exactly as shown in the
   app (the "VOQ-0077" quoting reference — approved rows carry a "V18" V-ref but
   are refused below anyway). Leave them all commented out to preview only. */

DECLARE @Targets TABLE (Reference nvarchar(64) PRIMARY KEY);

-- INSERT INTO @Targets (Reference) VALUES ('VOQ-0077');
-- INSERT INTO @Targets (Reference) VALUES ('VOQ-0071');

/* -- 2) Dry-run switch -------------------------------------------------------
   @Commit = 0 rolls the change back after showing the row count (dry run).
   @Commit = 1 commits the qualifying rows. */

DECLARE @Commit bit = 0;   -- <-- set to 1 to actually apply

/* -- 3) Preview: what will move, what will be skipped, and why -------------- */

PRINT '--- Matched variation orders ---';
SELECT  v.Reference,
        v.Title,
        v.Status AS CurrentStatusInt,
        CASE v.Status WHEN 0 THEN 'Quoting'  WHEN 1 THEN 'Issued'
                      WHEN 2 THEN 'Approved' WHEN 3 THEN 'Rejected'
                      WHEN 4 THEN 'Awaiting AI' ELSE 'Unknown' END AS CurrentStatus,
        CASE WHEN v.Status IN (0, 1) THEN 'WILL MOVE -> Awaiting AI (4)'
             WHEN v.Status = 4       THEN 'already Awaiting AI'
             ELSE 'SKIP - not a pre-approval stage (use the app flow)' END AS Action
FROM    dbo.VariationOrderQuotes v
JOIN    @Targets t ON t.Reference = v.Reference
ORDER BY v.Reference;

PRINT '--- References that matched no variation order ---';
SELECT  t.Reference AS UnmatchedReference
FROM    @Targets t
WHERE   NOT EXISTS (SELECT 1 FROM dbo.VariationOrderQuotes v WHERE v.Reference = t.Reference);

/* -- 4) Apply (transactional) ---------------------------------------------- */

BEGIN TRAN;

UPDATE  v
SET     v.Status = 4                    -- AwaitingArchitectInstruction
FROM    dbo.VariationOrderQuotes v
JOIN    @Targets t ON t.Reference = v.Reference
WHERE   v.Status IN (0, 1);             -- Quoting / Issued only; never Approved / Rejected

DECLARE @moved int = @@ROWCOUNT;
PRINT CONCAT(@moved, ' variation order(s) qualified and were set to Awaiting AI.');

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT 'Committed.';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT 'Dry run (@Commit = 0) - rolled back, no data changed. Set @Commit = 1 to apply.';
END
GO
