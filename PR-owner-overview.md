# As the managing director, I want one Owner Overview page — cash, forecast profit, the jobs needing a decision and what we owe — so I can see the whole business without opening every finance tab

Branch `feature/owner-overview` → `main`. YBT task 26f179f3.

## What changed

- **`/owner-overview`** (jpms/Pages/OwnerOverview.razor + .razor.cs), directors only (`OwnerOverviewRoles.Owners`, the bank position's gate), at the foot of the side nav beside the Control Centre. Sections: four headline tiles (cash in bank · lowest week of the 13-week plan · forecast project profit · company operating profit *not yet calculated*); the 13-week closing-balance line with the "needs a decision" list beside it; every live job's forecast profit and profit-to-finish on one bar scale; owed to suppliers / owed by clients / left to certify / open leads; and the "Not yet calculated" panel.
- **`ProjectProfitReadModel`** (jpms/Features/Cvr): the Profit Summary's per-project loads and `ProfitRow` arithmetic, moved out of the page so the Owner Overview reads the identical row. The Profit Summary now delegates to it — no change in what it shows.
- **`ForecastBalanceChart`** takes an optional `LabelFor`/`AriaLabel` so the same line draws weeks as well as months.
- **`OwnerDecisions`**: every test is an existing figure against zero — a week below zero, a forecast whole-job loss, a loss on the work remaining, overdue payables/receivables. No thresholds.
- Page guide for the connector; sidebar row.

## The rule, and what fell outside it

Every figure on the page is one a finance page already calculates, read through the same read model. What Nigel's prototype asks for that the portal does **not** calculate is shown greyed in place and listed in the closing panel: company operating profit after overhead; one reconciled cash outlook (weekly omits unissued valuations, monthly's overheads are stored **per user**); an early-warning cash buffer; weighted pipeline value; the five "data checks"; programme days late. Each needs its definition agreeing before it can be a number.

## Verification

- Compiled in a scaffolded sandbox clone (recipe: finding 2026-09-17): **0 errors, the same 7 warnings as pristine `main`**.
- `tools.permissions.check`: 11/11 pass. `resolve_type_names`: no new findings (the `Plan` note is the same false positive Weekly Cashflow carries).
- Audit + gate: only `tangledConditionLines 278 → 279` fails, and `main` is already at 279 — the stale baseline, not this branch. Else-blocks and comment lines are within the baseline.
- Not run in a browser: the desktop VM has no SDK. First live check: sign in as MD, open Owner Overview, compare the forecast-profit tile with the Profit Summary's total for the default (live jobs) selection and the lowest-week tile with Weekly Cashflow's.

## Left

- The monthly Cash Forecast's low month is not on the page yet — its model is page-bound (eleven reads per project) and would need the same extraction the profit row got. Worth doing once the overheads-per-user question is settled.
- Push: the desktop VM holds no git credentials; branch committed locally.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_01Xpgh8sCovR4JyGNn1megze
