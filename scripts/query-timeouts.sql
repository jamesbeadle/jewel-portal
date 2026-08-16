-- query-timeouts.sql — which statement inside return-to-quoting is timing out.
-- Run:  sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i scripts/query-timeouts.sql -y 300
-- (also fine in Azure Data Studio / the portal Query Editor)
-- Diagnostic only: reads Query Store DMVs, changes nothing.

-- 1) Non-regular executions in the last 3 hours. 'Aborted' = the client gave up
--    (our 25s command timeout) — the statement that shows here repeatedly around
--    the times you clicked return-to-quoting is the culprit.
SELECT
    rs.last_execution_time,
    rs.execution_type_desc,
    rs.count_executions,
    CAST(rs.avg_duration / 1000.0 AS DECIMAL(10,1)) AS avg_ms,
    SUBSTRING(qt.query_sql_text, 1, 400) AS query_text
FROM sys.query_store_runtime_stats rs
JOIN sys.query_store_runtime_stats_interval i ON i.runtime_stats_interval_id = rs.runtime_stats_interval_id
JOIN sys.query_store_plan p  ON p.plan_id  = rs.plan_id
JOIN sys.query_store_query q ON q.query_id = p.query_id
JOIN sys.query_store_query_text qt ON qt.query_text_id = q.query_text_id
WHERE rs.execution_type_desc <> 'Regular'
  AND i.end_time > DATEADD(HOUR, -3, SYSUTCDATETIME())
ORDER BY rs.last_execution_time DESC;
GO

-- 2) Slowest statements over the same window (catches a bad plan that finishes
--    just under the timeout, which never shows as Aborted).
SELECT TOP 20
    CAST(rs.max_duration / 1000.0 AS DECIMAL(10,1)) AS max_ms,
    CAST(rs.avg_duration / 1000.0 AS DECIMAL(10,1)) AS avg_ms,
    rs.count_executions,
    rs.last_execution_time,
    SUBSTRING(qt.query_sql_text, 1, 400) AS query_text
FROM sys.query_store_runtime_stats rs
JOIN sys.query_store_runtime_stats_interval i ON i.runtime_stats_interval_id = rs.runtime_stats_interval_id
JOIN sys.query_store_plan p  ON p.plan_id  = rs.plan_id
JOIN sys.query_store_query q ON q.query_id = p.query_id
JOIN sys.query_store_query_text qt ON qt.query_text_id = q.query_text_id
WHERE i.end_time > DATEADD(HOUR, -3, SYSUTCDATETIME())
ORDER BY rs.max_duration DESC;
GO

-- 3) Where the waiting statements spent their time, last 3 hours.
--    wait_category_desc = 'Lock' with big waits = writers queuing behind an
--    open transaction (find and kill it with scripts/check-blocking.sql).
--    'Buffer IO' / 'CPU' / 'Parallelism' = a scan / bad plan → code or index fix.
SELECT TOP 20
    ws.wait_category_desc,
    CAST(ws.avg_query_wait_time_ms   AS DECIMAL(12,1)) AS avg_wait_ms,
    CAST(ws.total_query_wait_time_ms AS DECIMAL(14,1)) AS total_wait_ms,
    i.end_time AS interval_end,
    SUBSTRING(qt.query_sql_text, 1, 400) AS query_text
FROM sys.query_store_wait_stats ws
JOIN sys.query_store_runtime_stats_interval i ON i.runtime_stats_interval_id = ws.runtime_stats_interval_id
JOIN sys.query_store_plan p  ON p.plan_id  = ws.plan_id
JOIN sys.query_store_query q ON q.query_id = p.query_id
JOIN sys.query_store_query_text qt ON qt.query_text_id = q.query_text_id
WHERE i.end_time > DATEADD(HOUR, -3, SYSUTCDATETIME())
  AND ws.wait_category_desc IN ('Lock', 'Buffer IO', 'CPU', 'Parallelism')
ORDER BY ws.total_query_wait_time_ms DESC;
GO
