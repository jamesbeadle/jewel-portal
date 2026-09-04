---
name: jpms-cash-forecast
description: "The cash forecast's and weekly cashflow's timing doctrine. Load before reading, quoting or editing the cash forecast, project cash statements, or the 13-week weekly cashflow plan. Encodes amounts-authoritative-months-indicative, the Undated rule, the FD's two timing knobs, timing-never-amounts, and Xero-as-home-of-payment-agreements."
---

# JPMS — Cash timing doctrine

## The forecast (monthly)

- **Amounts are authoritative; months are indicative.** The statement's totals are real; WHICH
  month a flow lands in is a modelled guess. Never present a monthly phasing as a promise.
- Flows with no honest date (no practical-completion date to anchor on) sit in **Undated** and
  never touch the running balance. Overdue flows sit in the CURRENT month, never the past.
- The FD steers timing with exactly two per-project knobs: **set_next_valuation_date** (anchors
  the payment-lag count — the day matters) and **set_expected_monthly_valuation** (claims at that
  monthly rate until left-to-claim runs out; zero returns to even spread). Both change WHEN,
  never HOW MUCH — a phased total that disagrees with the statement is a defect, not judgment.
- Drawdown side only: overspent centres are never netted off; a retention release adds back only
  while still forecast.

## The weekly cashflow (13 weeks)

- The grid is Xero-seeded (bills at due/planned week, invoices at due/expected) plus manual
  items; **moving an entry changes WHEN it is paid, never how much** — the grid total always
  equals payables + receivables + items.
- **Real payment agreements live in Xero** (the bill's planned date; the invoice's expected
  date) — recorded once there, the grid follows. A portal placement is the fallback for
  week-to-week juggling, not the home of an agreement.
- One supplier belongs to at most one supplier group (two would double-count its bills); a group
  move is per-bill placements — a partial failure leaves the moved ones standing.
- Placements are shared truth with a who/when stamp — say who moved what when reporting the plan.
- The Excel export is the grid on paper: "Weekly plan" — one line per supplier (a supplier group
  is one line) with a column per week, band totals, net movement and, for directors, the closing
  balance; "Detail" — every bill/invoice under its line with due and expected dates, parked
  entries listed uncounted; "Data" — the flat list for pivoting. A shaded amount is one the
  accountant moved.
- Over the connector, **get_weekly_cashflow_grid** is that same grid line by line (Xero-seeded,
  placements and exclusions applied, one line per supplier, amounts per week, moved flags) — quote
  it for "who do we pay which week"; get_weekly_cashflow_plan is only the raw overlay.
