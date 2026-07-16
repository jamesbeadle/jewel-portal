/* ============================================================================
   Reverse cost-centre allocations from voided / deleted Xero invoices
   ----------------------------------------------------------------------------
   Why: lines allocated to a project + cost centre while an invoice was live
   kept counting in project financials (Actual Cost of Sales) after the invoice
   was voided in Xero — the sync used to keep allocated lines and only refresh
   their status. The sync now removes them automatically (SyncXeroLedgerHandler);
   this script clears the historical rows immediately without waiting for the
   next sync.

   What it does (in one transaction, safe to re-run):
     1. Deletes split shares (XeroCostSplits) belonging to voided/deleted lines
        — a split share would otherwise silently keep feeding per-centre actuals.
     2. Deletes the voided/deleted lines themselves (XeroLedgerLines), whatever
        their allocation status. This also clears any work-order links, which
        live on the line.

   Note: a line whose invoice was voided in Xero AFTER the last sync may still
   carry an older status here and won't be caught — run a Xero sync (Refresh on
   the Xero page) after deploying the updated handler to sweep those.
   ============================================================================ */

BEGIN TRANSACTION;

/* Preview — run these SELECTs alone first if you want to inspect what goes.
SELECT lines.XeroLedgerLineId, lines.InvoiceNumber, lines.ContactName,
       lines.InvoiceStatus, lines.Net, lines.ProjectId, lines.CostCenterCode,
       lines.AllocationStatus
FROM XeroLedgerLines AS lines
WHERE lines.InvoiceStatus IN ('VOIDED', 'DELETED');

SELECT splits.*
FROM XeroCostSplits AS splits
JOIN XeroLedgerLines AS lines ON lines.XeroLedgerLineId = splits.XeroLedgerLineId
WHERE lines.InvoiceStatus IN ('VOIDED', 'DELETED');

SELECT links.*
FROM XeroLineWorkOrderLinks AS links
JOIN XeroLedgerLines AS lines ON lines.XeroLedgerLineId = links.XeroLedgerLineId
WHERE lines.InvoiceStatus IN ('VOIDED', 'DELETED');
*/

DELETE splits
FROM XeroCostSplits AS splits
JOIN XeroLedgerLines AS lines
    ON lines.XeroLedgerLineId = splits.XeroLedgerLineId
WHERE lines.InvoiceStatus IN ('VOIDED', 'DELETED');

PRINT CONCAT(@@ROWCOUNT, ' split share(s) removed.');

DELETE links
FROM XeroLineWorkOrderLinks AS links
JOIN XeroLedgerLines AS lines
    ON lines.XeroLedgerLineId = links.XeroLedgerLineId
WHERE lines.InvoiceStatus IN ('VOIDED', 'DELETED');

PRINT CONCAT(@@ROWCOUNT, ' work-order link slice(s) removed.');

/* Direct-cost slices inside reconciliation packages reference the line the same
   way splits and links do — an orphan would keep inflating its package's
   invoiced figure forever (the calculator sums slices without re-checking the
   ledger line still exists). Table exists from migration
   20260716110000_AddReconciliationPackageCostLines onward. */
IF OBJECT_ID('ReconciliationPackageCostLines', 'U') IS NOT NULL
BEGIN
    DELETE slices
    FROM ReconciliationPackageCostLines AS slices
    JOIN XeroLedgerLines AS lines
        ON lines.XeroLedgerLineId = slices.XeroLedgerLineId
    WHERE lines.InvoiceStatus IN ('VOIDED', 'DELETED');

    PRINT CONCAT(@@ROWCOUNT, ' package direct-cost slice(s) removed.');
END

DELETE FROM XeroLedgerLines
WHERE InvoiceStatus IN ('VOIDED', 'DELETED');

PRINT CONCAT(@@ROWCOUNT, ' voided/deleted ledger line(s) removed.');

COMMIT TRANSACTION;
