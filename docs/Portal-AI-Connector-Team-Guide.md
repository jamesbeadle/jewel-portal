# Using the Jewel portal from your own AI — team setup guide

*Issued 2026-08-27. The in-portal “Jewel Assistant” side chat has been retired. In its place the
portal is now a connector you add to your own AI tool — Claude or Perplexity, on the subscription
you already have. You ask questions and get work done in the tool you prefer; the portal answers
with live data, as you, with everything logged.*

**The one address you need:** `https://mcp.jewelbb.co.uk/api/mcp`

You sign in with your normal portal email and password. Nothing new to install on the portal side,
and no shared keys — the connection is yours alone.

---

## What it can do

Once connected, your AI can read what you can see in the portal, live: projects, requests and
RFIs (including the full tagged correspondence), variations and their build-ups, valuation
reports, work orders, bid packages, tender enquiries, cost codes, to-dos, filed documents,
drawings and email attachments. Ask things like:

- “What’s the latest on RFI-049 — read the correspondence and summarise where it stands.”
- “Compare V72’s build-up with the spreadsheet I’ve attached.”
- “Which work orders on By France are still draft?”
- “Read the tender email attachments on BPI-0003 and pull out the priced schedule.”

And it can **do** things — everything your portal account can do, not just read. Creating and
editing requests, RFIs, variations, work orders and bid packages; moving statuses; approving;
raising and issuing valuation invoices; managing to-dos and calendar events; sending the
portal’s emails. Ask it *“what can you do for me here?”* and it will list the actions your role
allows. Your permissions are the AI’s permissions: if you can’t do it by clicking, your AI can’t
do it either — and every action is performed as you and logged under your name. For anything
consequential (approvals, money, emails to clients or subcontractors, deletions) a good AI will
read the details back and get your yes first — and you should expect it to.

Everything runs under your own portal account: the AI sees exactly what your role sees, and every
call is recorded in the portal’s activity log under your name.

---

## Connect Claude (web / desktop / mobile)

1. Open **claude.ai** → **Settings** → **Connectors** (on desktop: Settings → Connectors).
2. Choose **Add custom connector**.
3. Name: **Jewel Portal**. URL: `https://mcp.jewelbb.co.uk/api/mcp`. Leave the advanced
   OAuth fields empty. Add it.
4. Click **Connect** on the new connector. A portal window opens — sign in with your portal email
   and password if asked, then press **Approve**.
5. Back in Claude, the connector shows as connected. In a chat, check the tools icon — “Jewel
   Portal” should be listed. Ask it something: *“List my live projects from the Jewel portal.”*

Connected once, it works in Claude on the web, the desktop app and the mobile apps under the same
Claude account.

## Connect Claude Code (terminal)

```bash
claude mcp add --transport http jewel-portal https://mcp.jewelbb.co.uk/api/mcp
```

Then inside Claude Code run `/mcp`, pick **jewel-portal**, and follow the sign-in prompt — the
browser opens the portal’s approval page; sign in and press Approve.

## Connect Perplexity

1. Perplexity → **Settings** → **Connectors** → **Add connector** → **Custom**.
2. Server URL: `https://mcp.jewelbb.co.uk/api/mcp`, transport **HTTP**. Perplexity discovers
   the sign-in flow itself (OAuth) — you don’t need a client ID or secret.
3. Save, then connect: the portal’s approval page opens — sign in and press **Approve**.

*(Custom connectors require a paid Perplexity plan, and may need enabling under Settings →
Connectors depending on plan.)*

---

## Working well with it

- **Name records the way you say them** — “RFI-049”, “V72”, “WO-0045”, “the By France job”. The
  connector resolves references itself (`find_by_reference`).
- **Ask it to read before it writes.** For anything that ends in an action (“post a note on
  REQ-0113”), have it show you the wording first — the write happens the moment it calls the tool.
- **Teach it once, benefit everyone.** Directors can save “skills” (house rules, how we price,
  what a good variation narrative looks like) with *“save that as a portal skill”* — every
  connected user’s AI can then load them (`list_skills` / `load_skill`).
- Attach files in your AI tool as normal (spreadsheets, photos); the AI reads those itself and
  can compare them against portal data.

## Disconnecting, and if something goes wrong

- **Disconnect / see what’s connected:** portal → **AI Connections**
  (`https://portal.jewelbb.co.uk/settings/ai-connections`). Disconnect revokes that tool
  immediately. Administrators can see and revoke anyone’s.
- **“Authentication required” after a long gap:** connections expire after 90 days of no use —
  just reconnect (same steps, takes seconds).
- **“Your portal roles do not allow this action”:** the connector enforces the same permissions
  as the portal. If you can’t do it by clicking, your AI can’t do it either.
- **A tool the AI mentions isn’t there:** the tool list is filtered to your role. Ask an
  administrator if you think you’re missing something you should have.
- **Anything odd:** disconnect the tool on the AI Connections page, reconnect, and if it
  persists tell an administrator — the activity log shows exactly what was called.

## The fine print

- The AI acts **as you**. Don’t connect shared or personal AI accounts you don’t control, and
  treat your AI tool’s account like your portal password.
- Every read and write is logged (Agent Activity, and the audit trail for writes).
- The portal no longer pays per-token API costs — usage rides on your Claude/Perplexity
  subscription, so use it freely.
