# Workflow 00 — Sales, Marketing & CRM

**Lifecycle stage:** 00 — the front end of the business, before a project shell exists.
**Purpose:** Run the journey from first lead touchpoint to qualified opportunity to won contract, preserving relationship, scope, budget and estimating context so nothing is re-keyed at the handoff into project delivery.
**Trigger:** Lead arrives — website, referral, architect introduction, repeat client, or manual entry.
**Frequency:** Continuous.
**Owner (target):** Sales / Estimating function (Project Manager + QS in current JBB structure; future dedicated sales role possible). Directors approve high-value bids and the win.
**Status:** Draft

---

## Why this stage exists

JPMS used to start at drawing receipt. In reality, JBB's lifecycle begins much earlier — leads, qualification, site visits, drawings chase, bid/no-bid, proposal, negotiation. Losing context across that handoff is where margin and client expectation drift. This stage closes that gap by making the won opportunity become a project shell carrying every prior conversation, photo, drawing, budget and assumption.

---

## CRM pipeline stages

A lead moves through these states in JPMS:

1. **New Lead** — captured but not yet triaged.
2. **Qualified** — passes initial qualification scoring.
3. **Survey / Visit Booked**.
4. **Survey Complete** — site visit done, notes captured.
5. **Awaiting Information** — drawings, planning consents, brief still pending.
6. **Drawings Received** — ready to feed Workflow 01 once won.
7. **Feasibility / Budget Review**.
8. **Tendering / Estimating** — feeds Workflow 02 in parallel.
9. **Proposal Issued**.
10. **Negotiation**.
11. **Won** → triggers project-shell creation; handoff to Workflow 01.
12. **Lost** — captured with reason for win/loss analytics.
13. **Nurture / Future** — re-introduce later.

---

## Target flow

1. **Lead capture** from any channel (website form, referral, manual entry, architect introduction, repeat client). Source / campaign auto-tagged.
2. **Triage and qualification** — owner assigned; qualification score and notes captured.
3. **Site visit booking and recording** — with photos, notes, voice memos against the opportunity.
4. **Drawings / information chase** — JPMS tracks who owes what and chases automatically.
5. **Bid / no-bid gate** — explicit decision with reason logged.
6. **Estimating queue** — passed to QS in priority order with deadline.
7. **Proposal issued** with follow-up reminders.
8. **Negotiation tracked** in-system; no decisions in inboxes.
9. **Won** — project shell auto-created carrying the lead context (client, architect, scope summary, budget, drawings, site visit notes) forward to Workflow 01. Win reason captured.
10. **Lost** — captured with reason for analytics.
11. **Nurture** — lost leads stay live for future follow-up.

---

## JPMS functionality required

- Lead capture from web (form), email, manual.
- Lead source / campaign attribution.
- Contact / Company / Architect-practice / Referrer directory.
- Qualification scorecard with notes.
- Site visit booking, capture and follow-up on mobile.
- Drawings / information chase tracker.
- Bid / no-bid gate.
- Estimating queue with priority and deadline.
- Proposal issue, reminder, negotiation tracking.
- Won / Lost capture with reason.
- **Handoff:** on Won, auto-create a project shell carrying all lead context into Workflow 01.
- Lost-lead nurture / future-opportunity follow-up reminders.

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-00-01 | P03 Project Manager | As a PM, I want to capture a new lead from a website form, referral, or manual entry with one form, so that every lead lands in JPMS with attribution. | Drafted |
| US-00-02 | P03 Project Manager | As a PM, I want to qualify a lead with a structured scorecard and notes, so that qualification decisions are consistent and defensible. | Drafted |
| US-00-03 | P03 Project Manager | As a PM, I want to book a site visit against a lead with the date, attendees and address pre-populated, so that scheduling is one screen. | Drafted |
| US-00-04 | P05 Site Manager | As a Site Manager, I want to record a site visit on mobile with photos, notes and voice memos against the lead, so that none of the in-person context is lost. | Drafted |
| US-00-05 | P03 Project Manager | As a PM, I want JPMS to chase outstanding drawings / planning consents / briefs from the lead, so that I'm not the human reminder. | Drafted |
| US-00-06 | P01 Director / MD | As a Director, I want to record a bid / no-bid decision with reason against the lead, so that we have analytics on what we choose to pursue. | Drafted |
| US-00-07 | P04 Quantity Surveyor | As a QS / Estimator, I want to see my estimating queue prioritised with deadlines, so that I work on what matters most. | Drafted |
| US-00-08 | P03 Project Manager | As a PM, I want to issue a proposal from JPMS with auto-reminders for follow-up, so that follow-up isn't manual. | Drafted |
| US-00-09 | P03 Project Manager | As a PM, I want to track negotiation rounds against the proposal in-system, so that the negotiation history is auditable. | Drafted |
| US-00-10 | P01 Director / MD | As a Director, I want to mark a lead as Won and have JPMS auto-create the project shell carrying the client, architect, scope, budget, drawings and site visit notes into Workflow 01, so that nothing is re-keyed at the handoff. | Drafted |
| US-00-11 | P03 Project Manager | As a PM, I want to record a Lost decision with reason, so that win/loss analytics in Workflow 09 are accurate. | Drafted |
| US-00-12 | P03 Project Manager | As a PM, I want lost leads to be set to Nurture with follow-up reminders, so that we don't lose future opportunities. | Drafted |
| US-00-13 | P08 Architect | As an architect referring a prospect, I want to introduce a lead through the JPMS portal with a single form, so that the referral lands cleanly in the right hands. | Drafted |
| US-00-14 | P01 Director / MD | As a Director, I want to see lead source attribution by channel (website, Instagram, LinkedIn, referral, architect, repeat client) and ROI per source, so that marketing spend is informed by data. | Drafted |

---

## Integrations

- Website form (Webhook → JPMS).
- Email-in for inbound enquiries.
- (Future) Marketing scheduling tool — content / campaign data flowing out.

---

## Acceptance criteria — "done looks like"

- Every lead is in JPMS with source attribution and a clear status.
- Site visits are captured against the lead, not in someone's phone.
- The win → project-shell handoff carries the full lead context forward; nothing is re-keyed.
- Win/loss analytics show pipeline health by source, value, owner and reason.

---

## Entities touched

`Lead` · `Opportunity` · `Contact` · `Company` · `Architect Practice` · `Site Visit` · `Proposal` · `Win/Loss Reason` · `Project` (shell, on Won)

See [`/05-data-model/entities.md`](../05-data-model/entities.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P01 Director / MD | Approver — bid / no-bid, high-value proposal sign-off, Won decision |
| P03 Project Manager | **Owner** — lead capture, qualification, site visit booking, drawings chase, proposal, negotiation |
| P04 QS / Estimator | Contributor — estimating queue, proposal pricing |
| P05 Site Manager | Contributor — site visit recording |
| P08 Architect | Source — referrals / introductions |

See [`/05-data-model/permissions-matrix.md`](../05-data-model/permissions-matrix.md).

---

## Open questions

- [ ] Lead-source attribution — closed list of channels, or free-text + dedupe?
- [ ] Site visit booking — sync with which calendar (Outlook, Google)?
- [ ] Proposal templating — single template or per-project-type variants?
- [ ] Nurture cadence — fixed (e.g. 30 / 90 / 180 days) or per-lead?

---

## Confirmation checklist

- [ ] Walked through end-to-end with PM, QS and Director
- [ ] Pipeline stages confirmed
- [ ] Project-shell handoff confirmed
- [ ] Analytics confirmed against the Workflow 09 portfolio view
- [ ] Permissions confirmed
- [ ] Signed off
