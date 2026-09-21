-- ============================================================================
-- Restore: the contract context of two locked valuations  (2026-09-21)
-- ============================================================================
-- FIX: a locked valuation's contract-sum headline moved at Confirm. A variation
-- approved between the claim being locked and the client paying re-wrote the
-- claim's NetVariations / RevisedContractSum from the live bill, so a re-downloaded
-- statement showed a revised contract sum the client never saw. The code fix keeps
-- the locked context from now on; this puts the two rows that moved back to what
-- their issued statement said. ContractSum itself did not move on either row.
--
--   Abbot Road,    Valuation 15 - Sept 2026: 49,712.55 / 348,658.59 -> 42,125.85 / 341,071.89
--   Nichols Nymet, Valuation 9 - July 2026:  -3,431.58 / 532,609.37 ->   -680.78 / 535,360.17
--
-- Each update is guarded on the figures the row holds today, so it does nothing
-- to a row that has already been restored or has moved again since — re-running
-- is safe, and the printed counts say what happened. Nothing else is touched.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i 2026-09-21-restore-locked-claim-contract-context.sql -b -o restore-locked-claim-contract-context.log
-- ============================================================================

BEGIN TRANSACTION;
GO

UPDATE [ValuationClaims]
   SET [NetVariations] = 42125.85, [RevisedContractSum] = 341071.89
 WHERE [ValuationClaimId] = N'3ae833e3a28542b3a57b5b2f7488407b'
   AND [LockedAt] IS NOT NULL
   AND [NetVariations] = 49712.55 AND [RevisedContractSum] = 348658.59;
PRINT CONCAT('Abbot Road Valuation 15: ', @@ROWCOUNT, ' row restored');

UPDATE [ValuationClaims]
   SET [NetVariations] = -680.78, [RevisedContractSum] = 535360.17
 WHERE [ValuationClaimId] = N'f05e85dfa99742ada0618488d27abe2c'
   AND [LockedAt] IS NOT NULL
   AND [NetVariations] = -3431.58 AND [RevisedContractSum] = 532609.37;
PRINT CONCAT('Nichols Nymet Valuation 9: ', @@ROWCOUNT, ' row restored');

SELECT [ValuationClaimId], [Name], [ContractSum], [NetVariations], [RevisedContractSum], [LockedAt]
  FROM [ValuationClaims]
 WHERE [ValuationClaimId] IN (N'3ae833e3a28542b3a57b5b2f7488407b', N'f05e85dfa99742ada0618488d27abe2c');
GO

COMMIT TRANSACTION;
GO
