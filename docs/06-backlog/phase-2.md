# Phase 2

Items deferred from v1. Valuable, but not blocking — added once v1 is in flight.

## Resource & long-lead procurement (Procore-style)
- Procurement schedule by long-lead item.
- Material approval and order status (independent of subcontractor work orders).
- Labour / subcontractor resource lookahead beyond the current period.
- Plant allocation and hire tracking.
- Delivery tracking against programme and work package.

## Marketing automation
- Campaign tagging and ROI reporting feeding back into workflow 09 portfolio analytics.
- Lost-lead nurture automation with cadence.
- Case-study / testimonial capture from completed projects.

## BIM and advanced model coordination
- Not in scope. Revisit only if a project demands it.

## Specs intelligence ("Procore Intelligent Specs" style)
- Spec register with key-requirement extraction for high-risk packages. Phase-2 because workflow 01 already covers the drawing register and basic spec storage.

## External integrations beyond v1
- HMRC reporting beyond CIS.
- Insurance broker portals.
- E-signature provider selection (DocuSign / Adobe Sign / M365 native — TBD per workflow 04).

## Bluebeam — deeper integration
- **Markups API direct take-off (workflow 02)** — JPMS reads take-off markups directly from the linked Studio Project, lands BoQ line items, refreshes on demand or on revision change. Depends on the QS having adopted the JPMS-published tool-set (cost-code custom column on every take-off) consistently in v1. CSV import remains as a fall-back.
- **Sessions Roundtrip (workflows 01 / 05)** — launch a Bluebeam Studio Session from inside JPMS to run a real-time mark-up review with the architect / CA / QS without leaving JPMS. Useful for architects who don't already use Bluebeam day-to-day.
- **Webhooks beyond drawings** — subscribe to markup-status-change events so review workflows (sign-off, "ready for issue") drive JPMS state without polling.

## Multi-tenant beyond JBB
- The current scope is JBB delivery (with BB / PS / PFP cross-entity). Multi-firm tenancy (running JPMS for other construction businesses) would be a phase-3+ shift.
