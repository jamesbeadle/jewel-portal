-- check-blocking.sql — who is blocking whom on prod, right now.
-- Run:  sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i check-blocking.sql
-- Diagnostic only: reads DMVs, changes nothing.

-- 1) Requests currently waiting, and the session blocking them.
--    A stuck return-to-quoting shows here as a SELECT on WorkOrders / QsAccruals /
--    ValuationLineItems / ClaimLines with wait_type LCK_M_* and a non-zero blocker.
SELECT
    r.session_id,
    r.blocking_session_id AS blocked_by,
    r.wait_type,
    r.wait_time / 1000    AS waited_seconds,
    r.status,
    r.command,
    SUBSTRING(t.text, 1, 300) AS running_sql
FROM sys.dm_exec_requests r
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id <> @@SPID
  AND (r.blocking_session_id <> 0 OR r.wait_time > 5000);

-- 2) Sessions holding OPEN transactions — the usual root cause. Look for a
--    transaction_begin_time minutes/hours old from SSMS, Azure Data Studio,
--    the portal Query Editor, or a forgotten sqlcmd — host_name/program_name
--    tell you whose window it is.
SELECT
    s.session_id,
    s.host_name,
    s.program_name,
    s.login_name,
    s.status,
    at.transaction_begin_time,
    DATEDIFF(SECOND, at.transaction_begin_time, SYSUTCDATETIME()) AS open_seconds,
    SUBSTRING(t.text, 1, 300) AS last_sql
FROM sys.dm_tran_session_transactions st
JOIN sys.dm_tran_active_transactions at ON at.transaction_id = st.transaction_id
JOIN sys.dm_exec_sessions s            ON s.session_id = st.session_id
LEFT JOIN sys.dm_exec_connections c    ON c.session_id = s.session_id
OUTER APPLY sys.dm_exec_sql_text(c.most_recent_sql_handle) t
WHERE s.session_id <> @@SPID
ORDER BY at.transaction_begin_time;

-- 3) What locks the blocker actually holds (fill in the session_id from above):
-- SELECT resource_type, resource_associated_entity_id, request_mode, request_status
-- FROM sys.dm_tran_locks WHERE request_session_id = <blocker_spid>;
--
-- Object name from a resource_associated_entity_id (OBJECT resources):
-- SELECT OBJECT_NAME(<resource_associated_entity_id>);
--
-- Fix: COMMIT/ROLLBACK in the offending window if it's yours, or
-- KILL <blocker_spid>;   -- rolls its transaction back
