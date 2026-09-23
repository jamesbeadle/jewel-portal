---
name: jpms-connector-mechanics
description: "Cross-cutting mechanics of writing through the connector — the rules that stop a well-meant edit erasing data, and the rules that stop a well-meant 'the portal cannot' being wrong. Load with any portal write. Encodes state-from-a-read-never-a-write's-reply, the full-record-write rule, read-before-write, absolute figures, complete lists, draft work orders having no number, look-before-you-say-cannot, a-refusal-is-never-cannot, Xero-numbers-are-read-never-remembered, one yes covers the described chain, re-read before retrying a timed-out write, and the standing answers for supplier work orders."
---

# JPMS — Connector write mechanics

- **The portal's state comes from a read, never from a write's reply** (the FD's own diagnosis,
  15/09/2026: both the wrong "cannot" and the false alarm came from reporting state off a
  write's response). A write's reply tells you what that call did; it does not tell you what the
  portal now holds. Before you report anything as the position — a record exists / does not,
  a link is set, a to-do is open, an order is draft — make the read that shows it
  (search_directory, list_work_orders, list_todos, get_*_context, read_record_emails) and
  report from that. Two things follow: a "cannot" is only ever declared after a fresh read has
  shown the thing missing, and an alarm ("it wrote twice", "the link is gone") is only ever
  raised after a fresh read has shown it. If the read and the reply disagree, the read wins and
  you say so.
- **Full-record writes**: many update actions (update_subcontractor, update_request_details,
  update_inventory_item, update_weekly_cashflow_item, update_architect_instruction, and kin)
  replace the record's editable face WHOLE. Read the record first, change only what the user
  asked, and resend every other field exactly as read — a partial send ERASES the fields you
  omitted. When you did not read it, you may not write it.
- **Absolute figures, complete lists**: set_cost_code_budget takes absolute amounts (read
  current budgets first); set_xero_line_work_order_links and the skill/attachment savers take the
  COMPLETE new set — include everything that should remain, not just the change.
- **Draft work orders have no number** until approval — find them with list_work_orders by
  status, never by reference.
- **Confirm-first actions** (requiresConfirmation) refuse their first call by design: check for
  an existing record, show the user exactly what will happen — every value — and only send
  confirm true after their explicit yes in THIS conversation. The same spirit applies beyond the
  flag: anything financial, external-facing or irreversible gets stated first, performed second.
  **Confirm-first means confirm, then DO.** Once the user has said yes, perform the action. It
  never means handing the user a document, a markdown draft or a set of steps to do it themselves
  — a draft the portal can create in the mailbox is created by the portal, and an email the
  portal can send is sent by the portal.
- **One yes covers the described chain** (Nigel, 15/09/2026). When a job is several writes —
  import the supplier, raise the order, link the email, add the to-do — lay the WHOLE chain out
  in one table before the first write: every action, every value it will send, in order. The
  user's yes to that table is the yes for each confirm-first step in it; do not stop for another
  yes between steps. The yes is spent the moment any value differs from the table — a refusal,
  a different id, a figure the portal corrected — so stop, show the change, and ask again. Never
  ask one yes per write for a chain the user has already approved whole.
- **Look before you say the portal cannot** (Jeremy's 15/09/2026 morning: three "portal cannot"
  verdicts, one of them true). A blocked route is a reason to look further, not a verdict.
  Before telling the user something cannot be done from here: (1) list_actions with a search
  term for the noun — "trade", "xero", "supplier" — and read the matching describe_action notes,
  which name the read that supplies each id; (2) scan the tool list for a read that carries the
  value under another name (a Xero contact id lives on list_xero_suppliers; trade ids on
  list_trades; a directory id on search_directory or list_unlinked_directory_records);
  (3) only then say "not exposed", quoting exactly what you looked for. Never ask the user to
  copy an id out of another system until you have done all three. When it really is a gap, say
  the one thing the user can do on the page to unblock it and carry on with the rest of the job;
  the change request is an offer at the end, never the deliverable.
- **A refusal is never "the portal cannot"** (Jeremy's 23/09/2026 morning: the portal refused a
  variation whose lines totalled zero, and the run reported "PORTAL CANNOT — James Beadle" when
  the truth was that the summary sheet's cell was blank and the figure was on the item's own
  tab). A 400/409/422 is the portal saying what the write is missing — a value, a cost centre,
  a mapping — and the missing thing is YOURS to find. Report it as "the portal needs X; I looked
  in A and B and have not found it", quote the refusal, and go back to the source before
  assigning it to anyone. "Portal cannot" is reserved for a tool or action that does not exist
  after the three looks above; it is never the status of a validation answer. A refusal has a
  colour of its own in a status table — "NEEDS A VALUE", "NEEDS A MAPPING" — never the red of
  a blocker.
- **A Xero number against a portal record is read, never remembered** (the same morning: VI-0004
  was reported as carrying INV-0219; the register has held INV-0106 against it since August). The
  Xero number on a valuation invoice is `XeroInvoiceNumber` on the row `list_valuation_invoices`
  returns — read that row before naming the number, every time, and never carry a number across
  from an earlier `list_xero_sales_invoices` look-up by memory. A number the user challenges is
  re-read, not defended.
- **A timed-out write is re-read before any retry.** A gateway error or a timeout does not mean
  the write failed — the portal may have finished after the connection dropped. Before retrying
  an add/create/import, read for the thing you were creating (list_todos with search, search_directory,
  list_work_orders by status) and continue from what is there. Retrying blind writes it twice.
- **Relay refusals verbatim.** Validation answers and guard messages are the portal telling you
  (and the user) what is really true — never summarise them into something softer, and never
  retry a refused call unchanged.
- **Everything is logged under the user's name.** Every call lands in Agent Activity and the
  audit trail exactly as if the user clicked it; act with that weight.

## Standing answers for supplier work orders (Nigel, 15/09/2026)

Settled once so they stop costing a round trip. Apply them; do not ask again unless the record
contradicts them.

- **The buying company is Jewel Bespoke Build, always.** A supplier quote made out to Jewel
  Enterprises Ltd or any other name does not change who the order comes from: raise the work
  order on the JBB project and mention in the PO email that the invoice is to be addressed to
  Jewel Bespoke Build. Say it once in the chain table; do not stop on it.
- **Draft or released: released when the user's yes covered it.** Lay out the chain with the
  approval step in it (raise, then approve — approval mints the number and the PO PDF). If the
  user's yes covered the approval, approve; if they said "draft", stop at the draft. Nothing is
  emailed by either — the PO email is a separate step, and since 17/09/2026
  send_work_order_po_email SENDS it to the supplier on the user's yes, with the PO PDF attached
  (saveAsDraftOnly stages it in Drafts instead).
- **Payment terms come from the directory record** (search_directory → paymentTermsDays; the PO
  prints them). Read them, state them in the table, never ask. If the record's terms are blank
  or wrong, update_subcontractor is the fix, offered in the same chain.
- **Cost code**: read list_cost_codes and propose one per line from the works described (goods
  from a merchant or fabricator land on the SUP-* codes); the user's yes to the table confirms
  it. If nothing clearly fits, leave it blank on that line and say so — a wrong code sends real
  money to the wrong place.
