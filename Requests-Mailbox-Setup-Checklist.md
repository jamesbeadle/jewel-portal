# Requests Mailbox — External Setup & Hand-off Checklist

This covers everything that lives **outside the JPMS codebase**: the Microsoft 365 / Azure admin work, and the ingestion layer still to be built. The application code (data model, manual triage queue, backend + UI) is done and verified.

Target mailbox: **`projects@jewelbb.co.uk`** (shared M365 mailbox).

---

## 1. What's already built (no action needed)

- `IntakeEmail` record + `IntakeStatus` (NeedsTriage → Claimed → Linked / Discarded / Failed) and EF table `IntakeEmails` (incl. `GraphMessageId`, the handle used to move emails into outcome folders — see §6).
- `RequestMessage` extended with email/threading fields (Direction, EmailMessageId, InReplyTo, ConversationId, SentStatus).
- Triage CQRS: list-open, claim, discard, link-to-existing, create-request — handlers, authorisation, validation, endpoints.
- Triage queue UI at `/requests/triage` — internal staff only, with duplicate-candidate surfacing.
- EF migration `20260626130000_AddRequestsMailboxIntake` (applied at startup).

Triage is **manual** at launch (your decision). LLM-assisted classification can be layered on later without changing this foundation.

---

## 2. Entra app registration (you / your M365 admin)

1. **Azure Portal → Entra ID → App registrations → New registration.**
   - Name e.g. `JPMS Projects Mailbox`.
   - Single tenant.
2. **API permissions → Add → Microsoft Graph → Application permissions** (NOT delegated):
   - `Mail.ReadWrite`
   - `Mail.Send`
3. **Grant admin consent** for the tenant (one click, once). Confirm the two permissions show "Granted".
4. **Certificates & secrets** — create a client secret (or upload a certificate). Record the value immediately; it's shown once.
5. Record for the Functions app config: **Tenant ID, Client ID, Client secret**.

> Application permissions give the app tenant-wide mailbox access by default — Step 3 below scopes it down to only `projects@`.
> `Mail.ReadWrite` (not just `Mail.Read`) is required because JPMS also **moves emails between folders** as triage progresses (see §6a).

---

## 3. Exchange application access policy (lock scope to one mailbox)

Run in Exchange Online PowerShell. This restricts the app so it can only touch `projects@jewelbb.co.uk`, not every mailbox in the tenant.

```powershell
# Optional but recommended: put the shared mailbox in a mail-enabled security group first
New-DistributionGroup -Name "JPMS Projects Mailbox Scope" `
  -Type Security -PrimarySmtpAddress jpms-projects-scope@jewelbb.co.uk
Add-DistributionGroupMember -Identity jpms-projects-scope@jewelbb.co.uk `
  -Member projects@jewelbb.co.uk

New-ApplicationAccessPolicy `
  -AppId <CLIENT_ID> `
  -PolicyScopeGroupId jpms-projects-scope@jewelbb.co.uk `
  -AccessRight RestrictAccess `
  -Description "JPMS app may only access the projects mailbox"

# Verify
Test-ApplicationAccessPolicy -Identity projects@jewelbb.co.uk -AppId <CLIENT_ID>   # Granted
Test-ApplicationAccessPolicy -Identity someone.else@jewelbb.co.uk -AppId <CLIENT_ID> # Denied
```

Policy propagation can take up to ~30 minutes.

---

## 4. ACS — leave as-is

`jpms-email-prod` (Azure Communication Services) stays **outbound-only** for the app's existing transactional email. The projects mailbox uses **Graph** for both read and send (so replies thread correctly in Outlook). No change to ACS.

---

## 5. Ingestion layer — BUILT (Azure Functions, in the `api` app)

This is now implemented in `api/Features/MailboxIntake/` and runs inside the existing Functions app, reusing Azure Storage Queues. It's built around the reliability invariant: **every message reaches a tracked state, nothing is missed, nothing is duplicated.** It stays dormant (logged no-op fallbacks) until you supply the Graph credentials in app settings — see §5g below. The sub-sections (a–f) describe what the code does.

**a. Change notification subscription (near-real-time)**
- Graph webhook subscription on `/users/projects@jewelbb.co.uk/mailFolders('Inbox')/messages`, `changeType=created`.
- Notification URL = an HTTP-triggered Function with the Graph validation handshake.
- Max lifetime ~3 days → see renewal timer below.

**b. Subscription renewal (timer Function)**
- Runs well inside the expiry window (e.g. every 12h) to `PATCH` the subscription's `expirationDateTime`.
- If the subscription is missing/expired, recreate it.

**c. Delta sweep (timer Function) — the safety net**
- Periodically (e.g. every 5–10 min) call `/messages/delta` with a **durable `deltaLink` cursor** persisted in storage.
- This catches anything a missed/expired webhook dropped. The webhook is for speed; delta is for completeness.

**d. Idempotent upsert into `IntakeEmails`**
- Key off the email's **`internetMessageId`**. On ingest, check existence first; if present, skip. This is what prevents duplicate intake rows from webhook + delta both firing.
- New rows default to `NeedsTriage`. Capture `conversationId`, `inReplyTo`, `references` headers so replies can later be matched to an existing request thread.
- Also store the message's **Graph `id`** in the new `GraphMessageId` column — this is the handle used to move the email between folders in §6a. (It is distinct from `internetMessageId` and changes on every move, so refresh it after each move.)

**e. One-time backlog import**
- The webhook only fires on **new** mail, so the existing pile in the Inbox won't arrive on its own. On first run, page the whole Inbox via `/messages/delta` (no cursor) to seed `IntakeEmails`, then persist the returned `deltaLink` as the ongoing cursor.
- After this, the large backlog appears in the triage queue ready to work through, and §6a's folder moves will steadily empty the Inbox as you triage.

**f. Queue + retries + dead-lettering**
- Webhook/delta handlers enqueue work onto Storage Queues (`mailbox-intake-notifications`, `mailbox-actions`) rather than doing DB writes inline, so transient failures retry.
- The Functions host gives each queue automatic retries and a `-poison` queue after max dequeues — **alert on the poison queues**; a poison message must surface, never silently vanish.

**g. App settings to configure (this is the switch that turns it on)**

Add these to the **Functions app** configuration (Key Vault references for anything secret). Until `ClientSecret` is present the feature runs as logged no-ops and nothing touches the mailbox.

| Setting | Value |
|---|---|
| `MailboxIntake:TenantId` | `7d8d8afa-d7a8-468c-bf2f-12a8515c6b3b` |
| `MailboxIntake:ClientId` | `63f25a40-eb24-4e2d-b4c4-bd6b6ede32ce` |
| `MailboxIntake:ClientSecret` | **the secret you created — Key Vault reference only, never in source/repo** |
| `MailboxIntake:Mailbox` | `projects@jewelbb.co.uk` |
| `MailboxIntake:EnableDeltaSweep` | `true` (the safety net + one-time backlog import; runs every 5 min) |
| `MailboxIntake:EnableFolderMoves` | `true` (move emails into outcome folders as you triage — §6) |
| `MailboxIntake:EnableWebhook` | `false` to start (delta sweep alone is enough); set `true` once the webhook URL is public |
| `MailboxIntake:NotificationUrl` | public HTTPS URL of the webhook Function: `https://<app>/api/mailbox/webhook` (only needed if webhook enabled) |
| `MailboxIntake:ClientState` | any long random string (echoed back by Graph to prove a notification is genuine) |
| `MailboxIntake:EnableOutboundSend` | `false` (leave off until you deliberately want JPMS emailing Shared replies — §7) |
| `MailboxIntake:Folders:InProgress` | folder id for "In progress" (see §6) |
| `MailboxIntake:Folders:Logged` | folder id for "Logged in JPMS" |
| `MailboxIntake:Folders:NotActioned` | folder id for "Not actioned" |
| `MailboxIntake:Folders:NeedsAttention` | (optional) folder id for "Needs attention"; if unset, failures stay in the Inbox |

`AzureWebJobsStorage` (already set for the app) is reused for the queues. The delta sweep alone will seed the whole Inbox backlog into the triage queue on first run, then keep it current — so you can leave the webhook off until you're ready.

---

## 6. Organise the mailbox as triage happens (folder moves, JPMS-driven)

Your decision: **as you triage in JPMS, the email is automatically moved into an outcome folder in the mailbox** — so the Inbox is always the "untriaged" pile and you can see at a glance what's left. This is driven by JPMS, not by you in Outlook.

**a. How it works**
- Each triage action raises a small "mailbox action" job (same queue as ingestion) carrying the intake's `GraphMessageId` and the target folder.
- A Function calls Graph `POST /messages/{id}/move` to relocate the email, then stores the **new** `GraphMessageId` it gets back (the id changes on move).
- Moves are best-effort and retried; a failed move never blocks the triage action itself — the DB state is the source of truth, the folder is a convenience mirror.

**b. Suggested folder + status mapping** (create these folders once under the projects mailbox)

| Triage outcome (`IntakeStatus`) | Mailbox folder |
|---|---|
| `NeedsTriage` (default) | **Inbox** — the untriaged pile |
| `Claimed` | `In progress` (someone's working it) |
| `Linked` / created a request | `Logged in JPMS` |
| `Discarded` | `Not actioned` |
| `Failed` | leave in Inbox (or `Needs attention`) so it can't be missed |

Tweak the folder names to taste — they're just config in the move Function.

**c. Set-up steps**
- Create the folders above in the `projects@` mailbox (Outlook or a one-off Graph call).
- Record their folder ids for the move Function's config.
- `Mail.ReadWrite` (already in §2) covers the move; no extra permission needed.

---

## 7. Outbound send via Graph

When a triaged request's thread gets a **Shared / Outbound** message (internal-only messages must NEVER be emailed):
- Send via Graph `sendMail` (or reply to the stored `EmailMessageId`/`conversationId`) so it threads in the original conversation.
- Stamp the message `SentStatus`: `Pending` → `Sent` / `Failed`. Retry `Failed` from the queue; surface persistent failures.

---

## 8. DB unique index on `InternetMessageId` — DONE

A unique index on `IntakeEmails.InternetMessageId` is now in place — the database-level backstop against duplicate intake (belt-and-braces with the app-level idempotency check in 5d). It ships in migration `20260626140000_AddMailboxSyncState` (which also adds the `MailboxSyncStates` table for the delta cursor and subscription state) and is applied automatically at startup. No action needed.

---

## 9. How this meets "nothing missed, nothing duplicated"

| Concern | Safeguard |
|---|---|
| A message is never actioned | Every email becomes an `IntakeEmail` defaulting to `NeedsTriage`; the triage queue shows all open items until a human resolves them. Folder moves (§6) keep the Inbox = the untriaged pile, so the backlog visibly shrinks. |
| A message is lost in transit | Webhook for speed **plus** delta sweep for completeness; queue with retries + dead-letter. |
| The same email ingested twice | App-level idempotency on `internetMessageId` (5d) + DB unique index (8). |
| Two requests for the same thing | Triage UI surfaces duplicate candidates (matching reference/subject) before you create a new request. |
| Discarded work disappears | Discards are recorded (`Discarded` + notes), not deleted; the email moves to `Not actioned`, not the bin. |
| Wrong-mailbox access | Exchange application access policy scopes the app to `projects@` only. |

---

## 10. One decision for you

Your login **`nigel.reilly@jewelgroup.co.uk`** is **not** in the JPMS administrator allowlist (`james.beadle@jewelbb.co.uk`, `admin.james@jewelenterprises.co.uk`, `Nigel.Reilly@jewelenterprises.co.uk`). Admin-only actions (e.g. deleting a request) won't be available to you under that login. Say the word and I'll add it.
