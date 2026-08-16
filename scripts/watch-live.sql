-- watch-live.sql — ground truth: what the API is actually doing in the DB right now.
-- USE: click "Confirm — return to quoting", then run this within the 45s pending
-- window (run it twice if you can). Works in sqlcmd / Azure Data Studio / portal editor:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i scripts/watch-live.sql -y 300
-- Diagnostic only: reads DMVs, changes nothing.

-- 0) Sanity: which DB is this window connected to, and is Query Store even on?
--    (blank query-timeouts.sql results are explained here: wrong DB, or state <> READ_WRITE)
SELECT DB_NAME() AS connected_db;
GO
SELECT actual_state_desc AS query_store_state, readonly_reason
FROM sys.database_query_store_options;
GO

-- 1) EVERY live request right now (not just blocked ones): what it runs, what it
--    waits on, who issued it. While return-to-quoting is pending, the handler's
--    statement should appear here — its wait_type is the answer:
--      LCK_M_*            = blocked by another session (see blocked_by + section 2)
--      PAGEIOLATCH/CXPACKET/SOS_SCHEDULER_YIELD = scan / bad plan / starved compute
--    If NOTHING from the API shows here during the whole pending window, the 45s
--    is NOT being spent in this database → the stall is in the Function App.
SELECT
    r.session_id,
    s.program_name,
    s.host_name,
    r.status,
    r.command,
    r.wait_type,
    r.wait_time / 1000.0   AS waited_s,
    r.blocking_session_id  AS blocked_by,
    r.total_elapsed_time / 1000.0 AS elapsed_s,
    r.cpu_time,
    SUBSTRING(t.text, 1, 400) AS running_sql
FROM sys.dm_exec_requests r
JOIN sys.dm_exec_sessions s ON s.session_id = r.session_id
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE s.is_user_process = 1
  AND r.session_id <> @@SPID
ORDER BY r.total_elapsed_time DESC;
GO

-- 2) Sessions holding OPEN transactions (lingering SSMS/portal window, forgotten
--    sqlcmd). An old transaction_begin_time here + LCK_M_* above = found it.
SELECT
    s.session_id,
    s.host_name,
    s.program_name,
    s.login_name,
    s.status,
    at.transaction_begin_time,
    DATEDIFF(SECOND, at.transaction_begin_time, SYSUTCDATETIME()) AS open_seconds,
    SUBSTRING(t.text, 1, 400) AS last_sql
FROM sys.dm_tran_session_transactions st
JOIN sys.dm_tran_active_transactions at ON at.transaction_id = st.transaction_id
JOIN sys.dm_exec_sessions s            ON s.session_id = st.session_id
LEFT JOIN sys.dm_exec_connections c    ON c.session_id = s.session_id
OUTER APPLY sys.dm_exec_sql_text(c.most_recent_sql_handle) t
WHERE s.session_id <> @@SPID
ORDER BY at.transaction_begin_time;
GO
