# End-to-end regression test script

The seven flows that have to keep working together after a refactor round, a hosting change or a
security fix: Xero sync and allocation, valuations to invoices, variations, bid packages,
requests/RFIs, timesheets, and email. Run it as one person, in one sitting, on the deployed site,
and record the outcome in the run log at the foot. The "Test All Login Roles" brief covers who may
reach what; this script covers whether the flows still join up for someone who may.

Write down the reference of everything you create (REQ, V, WO, BPI, DEF, claim number) — the
later steps use them, and the cleanup step at the end needs them.

## Before you start

| | What you need |
|---|---|
| A project | A **test project** in Live Delivery with a client party, an architect party, a Xero contact mapped (Project settings → Edit details) and at least two cost centres with budget. Not a client's real job: several steps send email and raise Xero invoices. |
| Logins | Your own director login. For step 7 a second mailbox you can read (your personal one, as the "architect") so you can see what the portal sent. |
| Xero | The Xero connection live (Admin → Integrations shows it connected). At least one unallocated purchase bill in Xero for the test supplier, and a draft bill with a WO number in its reference. |
| A supplier | A directory record with a valid email you control (the "subcontractor"), vetted, with a trade. |
| A drawing | Any PDF to upload as a drawing revision. |
| The build | Note the deploy number from Admin → System before you start; it goes in the run log. |

Mark each step **Pass**, **Fail** (say what happened) or **Skip** (say why). A Fail in a flow does
not stop the run — carry on with the next flow so the log shows everything at once.

## Flow 1 — Requests and RFIs

| # | Do | Expect | Result |
|---|---|---|---|
| 1.1 | Project → RFIs → **Raise request**, kind RFI, a title and description, response due in a week. | The request appears at the top of the register as `REQ-nnnn`, status NeedsAction, Issued = today. | |
| 1.2 | Open it. Attach the drawing PDF (Attachments → attach). | The attachment lists with its name and size; opening it renders the PDF in the viewer. | |
| 1.3 | Fill the official document (the RFI tab): basis of queries, response required. Save. | The form saves; the PDF download from the Actions menu carries what you typed. | |
| 1.4 | Actions → **Email** (the Outlook draft). | An Outlook draft opens addressed to the architect on the project, PDF attached; nothing is sent by the portal. Close the draft. | |
| 1.5 | Post an internal message on the request; post a shared one. | Both appear in the conversation; the shared one is marked as shared. | |
| 1.6 | Move the status to Open, then back to NeedsAction. | The pill changes each time; the register agrees. | |
| 1.7 | `/rfis` (the cross-project dashboard). | The new RFI is listed with its due date. | |

## Flow 2 — Variations

| # | Do | Expect | Result |
|---|---|---|---|
| 2.1 | On the request from 1.1, **Create Variation Order Quote**. Give it a description and an estimate. | The variation tab appears on the record bar; the variation is `V nn`, status Quoting. | |
| 2.2 | Variation Orders register → status chip → Issued. | Status reads Issued; IssuedAt is stamped (detail page). | |
| 2.3 | Move it to Awaiting AI. | Status reads Awaiting AI; nothing else changes. | |
| 2.4 | Architect's Instructions → file an instruction (any PDF), tick this variation. | The instruction lists; the variation's page shows the link. | |
| 2.5 | On the variation's page, **Approve**: one priced line per cost centre. | Status Approved; `VariationRef` matches the V number; the Valuation Report now carries a Variations row for it on those cost centres; Financials shows the budget moved. | |
| 2.6 | Variation Orders → **Add variation manually** with one line. | It lands **Issued** (not Quoting) with the line staged; the approve dialog opens pre-seeded with it. | |
| 2.7 | On the approved variation, **Return to quoting**. | The valuation row and budget movement are reversed; status Quoting. Approve it again for the next flow. | |

## Flow 3 — Bid packages and work orders

| # | Do | Expect | Result |
|---|---|---|---|
| 3.1 | Bid Package Invites → **New package**, trade = the supplier's trade. Add two lines on the Details tab. | Package `BPI-nnnn`, Draft, lines sum on the summary. | |
| 3.2 | Tender list → add the supplier → **Invite** (the composer). Send. | The invite goes from the projects mailbox, BCC, T&Cs attached; the supplier's inbox receives it; the package shows Invited. | |
| 3.3 | Submissions → record a quote for the supplier with a value. | The quote lists; revise it once and the new value shows. | |
| 3.4 | **Award** to the supplier. | A work order is raised, Draft, with the package's lines and value; the package reads Awarded. | |
| 3.5 | Work Orders tab → the new order → two-click **Approve**. | The WO number is minted (`WO-nnnn`, per project), the PO PDF downloads, the PO email arrives at the supplier's address, status Awaiting supplier acceptance. | |
| 3.6 | Work Orders → **Add work order** for the same supplier with one £0 line and one priced line. Approve it. | Both lines print on the PO — the £0 line at £0.00 with its quantity; the order's value is the priced line alone. | |
| 3.7 | Recode a line of the first order to the other cost centre. | The order's value is unchanged; Financials shows the committed cost on the new centre. | |

## Flow 4 — Xero sync and allocation

| # | Do | Expect | Result |
|---|---|---|---|
| 4.1 | `/finance/allocation` → **Sync**. | The sync completes with counts; the unallocated bill you left in Xero appears in the To code queue with its Xero reference and line detail. | |
| 4.2 | Code one line to the test project and a cost centre. | The line moves to Allocated; Xero shows the bill approved with the Sites/cost-code tracking (open it in Xero). | |
| 4.3 | The draft bill whose reference carries the WO number from 3.5. | It appears on the **Work Order bills** tab as a card naming that order, the whole bill proposed on it. | |
| 4.4 | **Approve** the card. | The line reads "Work order WO-nnnn"; the order's WO Allocation shows the bill linked; Xero holds the bill AUTHORISED. | |
| 4.5 | Allocated → **Undo bill** on it. | The link is removed and the tracking cleared in Xero; the toast says the bill stays awaiting payment there. | |
| 4.6 | Project → Work orders → supplier row → **Account**. | The account lists the two orders, the bill received, and the position (received − ordered). | |
| 4.7 | Xero Transactions, Aged Payables, Aged Receivables. | Each loads with figures; the aged reports include the draft bill. | |

## Flow 5 — Valuations to invoices

| # | Do | Expect | Result |
|---|---|---|---|
| 5.1 | Project → Valuation Report. Enter a % complete on two bill lines. | The report totals move; the claim card shows the draft claim's value. | |
| 5.2 | **Value & lock**. | The claim is locked (LockedAt shown); View statement prints the frozen lines, the approved variation from 2.5 among them; a later % edit on the bill does NOT change the statement. | |
| 5.3 | **Email statement** → Save as draft. | An Outlook draft with the statement PDF; nothing sent. (Or send it to yourself as the architect — then it must arrive with the `JPMS/VAL-…` tag readable in Control Centre → Client → Valuation reports.) | |
| 5.4 | **Raise** the valuation invoice, then **Record claim sent**, then **Record approval**. | Statuses Raised → Submitted → Approved in turn; Record approval opens a **draft programme update** on the Programme tab proposing % per task. | |
| 5.5 | **Raise in Xero & issue…** — read the preview first (contact, VAT treatment, due-date note), then run it. | An AUTHORISED sales invoice appears in Xero on the project's contact, reference `Valuation NN`, the certificate PDF attached; the portal invoice reads Issued with the Xero number. | |
| 5.6 | In Xero, record a payment against that invoice. Back in the portal: **Sync payments from Xero…** → preview → run. | The invoice moves to Paid with Xero's date; the project's paid total moves. | |
| 5.7 | **Confirm & roll over**. | The claim reads Confirmed; a new draft claim opens with the next number and its own tag. | |
| 5.8 | Programme → Draft from valuation… against the locked claim. | A draft proposes task % from the claim; apply it; the Gantt shows the progress. | |
| 5.9 | Cash Forecast, Weekly Cashflow, Profit Summary. | Each loads; the issued invoice and the approved bill appear where expected. | |

## Flow 6 — Timesheets and labour

| # | Do | Expect | Result |
|---|---|---|---|
| 6.1 | Time → Workers → **Add worker** (day rate, assigned to the test project). | The worker lists with the rate; Project → Labour shows them assigned. | |
| 6.2 | Sign in as a site operative (or use the worker's own link) and log a day on the test project; else enter the day on their behalf from Project → Labour. | The timesheet appears Submitted with hours and no money for the site view. | |
| 6.3 | Project → Labour → tick the day → **Approve**. | Approved; Financials → labour shows the cost (hours × rate) on the project; as a non-commercial role the money columns are absent. | |
| 6.4 | Approve a day past the budget block as MD/FD with a reason. | The override is accepted and audited; as a PM it is refused. | |
| 6.5 | Labour overview for the month → the worker's settlement. | The month shows the approved day; the Xero coding run is offered and refuses until the week is signed off. | |
| 6.6 | Sign the week off, run the Xero coding for that worker. | The coding run records its outcome per worker-month; the settlement view reads Matches / the verdict. | |

## Flow 7 — Email: Control Centre, communications, drafts

| # | Do | Expect | Result |
|---|---|---|---|
| 7.1 | From your "architect" mailbox, email the projects mailbox: subject naming the test project and the RFI from 1.1, with a PDF attached and an inline image. | Control Centre shows it in the queue within a couple of minutes, with the preview, the attachment and the inline image rendered. | |
| 7.2 | Select it → Client pane → tag it to the RFI, stage a reply, tick "Send to document triage" for the attachment, add a to-do → **Apply**. | One press: the mail is tagged `JPMS/…REQ…`, the reply arrives at your architect mailbox from the projects mailbox, the attachment waits at `/document-triage`, the to-do exists on the project. | |
| 7.3 | The request page → Correspondence. | The tagged email and the sent reply both read there. | |
| 7.4 | `/document-triage` → file the attachment as a drawing on the test project. | The drawing lists on Documents; a revision extraction is queued (Extract data shows Queued/Done). | |
| 7.5 | Send an email from the "subcontractor" address to the projects mailbox mentioning the WO number from 3.5. | Control Centre → Subcontractor pane offers the work order; tag it; Subcontractor Communications shows it under the order. | |
| 7.6 | Project → Communications → Reply to a client email. | The reply sends from the projects mailbox and threads. | |
| 7.7 | Defects → raise a defect with the supplier → **Send to supplier**. | The email arrives at the supplier's address; the defect moves Open → In progress; a second send is a chase and changes nothing. | |
| 7.8 | Sales → an enquiry email tagged to a lead (raise the lead from the email). | The lead reads Engaged, Source Inbound; its Enquiry emails section shows the mail. | |

## Flow 8 — The connector (one read, one write)

| # | Do | Expect | Result |
|---|---|---|---|
| 8.1 | In Claude with the Jewel_Portal connector, ask for the test project's open RFIs and the variation's status. | The answer names REQ/V references that match the pages. | |
| 8.2 | Ask it to post an internal message on the RFI. | It asks nothing it need not, posts, and the message reads on the request page under your name; `/agents/activity` logs the call. | |
| 8.3 | Ask it to email the statement to the client. | It shows the email and asks for a yes first; say no; nothing is sent. | |

## Cleanup

Cancel the work orders (MD/FD), reject the test variations (return to quoting first), void the Xero
sales invoice and the coded bill's tracking, delete the test lead, and mark the test project's claim
as a test in its notes. Leave the test project itself — the next run reuses it.

## Run log

| Date | Build | Run by | Trigger | Flows passed | Failures (step numbers, one line each) |
|---|---|---|---|---|---|
| | | | after the refactoring | | |
| | | | after the Azure upgrade | | |
