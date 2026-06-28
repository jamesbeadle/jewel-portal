# Request documents (RFI etc.)

Renders a one-page, JewelBB-branded PDF for a request (RFI/RFA/RFC/…), emails it to the project's
contacts when the request is raised, lets staff download it through the platform, and resend it on
demand. The PDF is **regenerated from SQL every time** — nothing is stored, so it is always current
and recreation is idempotent (two renders of an unchanged request differ only by the generated-at
timestamp).

## How it fits together

- `RequestDocumentBuilder` (api/Features/Requests/Documents) collates a flat `RequestDocumentModel`
  from SQL: the request, its project, the project contacts flagged `ReceivesRequests`, and the
  **Shared** leg of the message thread (internal notes never leave the platform).
- `RequestDocumentRenderer` turns that model into PDF bytes with PDFsharp/MigraDoc. The model and
  renderer are linked into both the api and the worker, so the emailed PDF is byte-for-byte the file
  the download endpoint serves.
- Sending is out-of-band: handlers enqueue a `SendRequestDocument` mailbox action; the worker
  (`MailboxActionWorker`) renders and sends it via Microsoft Graph, logs an outbound activity entry,
  and moves an `Open` request to `Awaiting response`. Failures retry on the queue — the DB stays the
  source of truth.

## Endpoints

- `GET  /api/requests/{requestId}/document` — download the PDF (`application/pdf`).
- `POST /api/requests/{requestId}/document/send` — resend. Optional JSON body
  `{ "recipientOverride": "someone@example.com" }` sends to one ad-hoc address instead of the
  project's flagged contacts. Restricted to Director / Project Manager / Site Manager / Architect.

Auto-send fires when a request is raised (`RaiseRequest`) or created from an intake email
(`CreateRequestFromIntake`), provided it is in the `Open` state.

## Deployment prerequisites

1. **Graph permission — `Mail.Send` (application).** The app registration used by the worker needs
   the application permission `Mail.Send` with admin consent, scoped (ideally via an
   ApplicationAccessPolicy) to the `projects@jewelbb.co.uk` mailbox. Sending uses the existing
   client-credentials flow; no new secret beyond the mailbox credentials already configured.
2. **The client secret stays out of source control.** It lives in app settings / Key Vault only
   (`MailboxIntake:ClientSecret`), as today.
3. **A TrueType font must be resolvable on the host.** The Linux Functions host has no GDI fonts, so
   `DocumentFontResolver` searches common system font directories (DejaVu, Liberation, Lato, …). If
   none are present, set `RequestDocuments:FontPath` (env `RequestDocuments__FontPath`) to a `.ttf`
   file or a directory containing one. The renderer throws an actionable error if no regular font is
   found.

## Feature flags (`MailboxIntake` section)

- `Enabled` (default true) — master switch for the whole mailbox feature.
- `EnableRequestDocuments` (default true) — issue/resend documents. Safe to leave on: it is a no-op
  when Graph is unconfigured (the scheduler falls back to a null queue) or when a project has no
  flagged contacts.
- Sending also requires the worker's Graph client to be configured; with no credentials the action
  is skipped and logged.

## Smoke test

`tools/RequestDocumentSmokeTest` is a standalone console app that links the same renderer source and
produces a sample RFI PDF, verifying the renderer compiles and fonts resolve on the current machine:

```
dotnet run --project tools/RequestDocumentSmokeTest
```

It writes `tools/RequestDocumentSmokeTest/REQ-0001-smoke.pdf` and prints PASS/FAIL. A font-resolution
failure points you at `RequestDocuments:FontPath` above.
