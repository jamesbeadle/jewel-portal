# Security review — 21 September 2026

The Stage 7 sweep of `tools/refactor/playbook.md`, run against `main` at `90b2fe71` (PR #70), and the
OWASP-shaped review the "Security Review" task asks for, with the data-protection check beside it.
Every finding cites the file it was read in. Each one ends in one of five states:

- **Fixed** — in the pull request this log arrives with (`fix/security-review-2026-09-21`).
- **Fixed in #107 PR** — in the architect-identity pull request, which this review found the connector half of.
- **Decision** — needs Nigel's call before it is changed; asked on the Security Review task.
- **Follow-up** — its own task in Your Business Today, named here.
- **Accepted** — left as it is, with the reason.

Severity: **High** — reachable by an outsider or by any signed-in user to reach data or actions they
should not; **Medium** — weakens a control, or needs an insider or a misconfiguration; **Low** —
hardening. Everything checked and found sound is listed too, so the review can say each area was read.

The check is static. It says what a role *can* reach and never whether a person can do their job;
the run with each login (the "Test All Login Roles" task) is what confirms the second.

## Summary

| Area | High | Medium | Low | Fixed now |
|---|---|---|---|---|
| Authentication and sessions | 1 | 1 | 3 | 4 |
| Authorisation (spot-check beside the permission check) | 1 | 0 | 1 | 2 |
| Injection, uploads, rendering | 3 | 3 | 1 | 7 |
| Secrets | 2 | 3 | 1 | 4 |
| Azure configuration and headers | 0 | 4 | 3 | 4 |
| Dependencies | 1 | 2 | 3 | 1 |
| The AI connector | 1 | 3 | 2 | 3 |
| Data protection | 4 | 7 | 6 | 4 |

The two things that must happen outside the code, today: **rotate the Google Places API key** that
was committed in `api/local.settings.json` (it is untracked by this PR but still in history), and
**purge that file from history** with the process in `claude/git-history-purge-2026-09-19.md`.

## 1. Authentication and sessions

How it works: passwords are PBKDF2-HMAC-SHA256 at 210,000 iterations with a 16-byte salt and a
constant-time compare (`api/Auth/PasswordHasher.cs`); sessions are 32-byte CSPRNG secrets stored as
their SHA-256 in `UserSessions`, revalidated against `RevokedAt` and `ExpiresAt` on every request
(`api/Auth/SessionManager.cs`); the cookie is `HttpOnly`, `Secure`, `SameSite=Lax`, seven days
(`api/Auth/SessionCookie.cs`); invite and reset tokens are hashed at rest, single-use and time-limited
(`UserInviter.cs`, `PasswordResetSender.cs`, `SetPasswordEndpoint.cs`).

| Sev | Finding | Where | State |
|---|---|---|---|
| High | Setting a new password left every other session and every connected tool alive: a stolen cookie kept working for its seven days, an MCP token for its seven (access) or ninety (refresh). | `api/Features/Auth/SetPasswordEndpoint.cs` | **Fixed** — the reset now revokes the person's live sessions, every OAuth token they hold (`OAuthTokenManager.RevokeAllForUserAsync`) and the resolver cache, before signing them in afresh. |
| Medium | No per-address limit on sign-in or forgot-password: one host could spray five guesses at every account, lock any known account out at will, or mail-bomb staff through forgot-password. | `api/Features/Auth/LoginEndpoint.cs`, `ForgotPasswordEndpoint.cs` | **Fixed** — `AuthRateLimitMiddleware`: 30 requests per address per 15 minutes on the `auth/*` doors (`me` and `logout` excepted), answered 429. Per instance and in memory; the platform WAF is the stronger backstop if it is ever wanted. |
| Low | Sign-in answered an unknown, disabled or locked account faster than a wrong password (no PBKDF2 run), so response time said which addresses hold an account. | `LoginEndpoint.cs` | **Fixed** — those paths now spend a verification against a decoy hash first (`PasswordHasher.SpendAVerification`). |
| Low | The reset and invite secret travels in the URL (`/set-password?token=…`, `auth/invite/{token}`), so it lands in edge and telemetry request logs for its lifetime. | `UserInviter.cs`, `ValidateInviteEndpoint.cs` | **Accepted** for now: lifetimes are two hours (reset) and seven days (invite), tokens are single-use, and the page consumes them at once. Moving the invite check to a POST is a small follow-up if log access ever widens. |
| Low | Email case is left to SQL Server's case-insensitive collation rather than normalised at the edge. | `LoginEndpoint.cs`, `UserInviter.cs` | **Accepted** — works today; note kept so a restore under a case-sensitive collation is recognised for what it is. |
| OK | Password hashing, session entropy and hash-at-rest, per-request revocation, server-side logout, single-use hashed tokens, neutral forgot-password answers, cookie flags. | as above | Checked. |

## 2. Authorisation

The permission check (`tools/permissions`) is the systematic read; it passes every rule on the branches
this review ships with. Two things it could not see:

| Sev | Finding | Where | State |
|---|---|---|---|
| High | `perform_action` on the MCP connector ran an action's Authorisation and Validation and nothing else; the record scopes (`VariationOrderScope`, `QuoteScope`, `DefectScope`, and the request and instruction scopes #107 adds) live in the endpoint bodies. Any external portal login can connect an MCP client, so an architect could edit any project's requests and instructions, a client approve another client's variation, a tenderer revise a competitor's quote — all with one leaked id, and the architect path with none. | `api/Features/Ai/Tools/Actions/AiActionExecutor.cs` | **Fixed in #107 PR** — `AiActionScopes` runs the same scope the endpoint does, for every scoped command, and the permission check gains the rule "a scoped command is scoped on the connector too". Pinned by `AiActionScopesTests`. |
| Low | The two building-control upload routes read the case before authenticating, so an unauthenticated caller learned whether a case id exists (404 vs 401). | `BuildingControlAttachmentEndpoints.cs` | **Fixed** — the sign-in and role gate now run first. |
| OK | Every endpoint resolves the caller through one resolver; the 21 anonymous routes are exactly `policy.json`'s `openRoutes` (health, version, sign-in doors, OAuth metadata, the Imagine token door, the MCP transport); the six "signed-in only" routes are caller-scoped by construction. | `tools/permissions/check.py` | Checked. |

## 3. Injection, uploads and rendering

How it works: there is no raw SQL in application code (every read is EF LINQ; the 103 migration
statements are literals); Graph OData filters are built from portal-minted tag stems and URL-encoded;
blob keys are a generated id followed by `Path.GetFileName`, so nothing traverses; exports are `.xlsx`
inline strings, never formulas; outbound mail is sanitised by the pipeline's explicit allow-list
(`ComposeHtmlPipeline.Sanitisers.cs`, pinned by `ComposeHtmlPipelineTests`).

| Sev | Finding | Where | State |
|---|---|---|---|
| High | Stored XSS on the portal's own origin: a dozen download endpoints honoured `?inline=1` by omitting `Content-Disposition` and echoing the *uploaded* content type, with no allow-list and no `nosniff` — an `evil.html` attached to a request ran as whoever opened the link. | `DownloadDrawingRevisionFileEndpoint.cs` and eleven siblings | **Fixed** — `api/Storage/InlineRendering`: only the embeddable raster types and PDF ever render inline, everything else downloads whatever was asked; every file response says `X-Content-Type-Options: nosniff`. |
| High | The mailbox attachment preview promoted an emailed `.svg` to `image/svg+xml` and served it inline — an outsider's script, run by the triager who previewed it. | `DownloadMailboxAttachmentEndpoint.cs` | **Fixed** — the SVG promotion is gone and the endpoint uses the same allow-list. |
| High | The bid-package invite draft was stored raw and rendered back with `MarkupString`: one user's pasted `<img onerror>` ran for the next user who opened the composer. | `BidPackageInviteComposerDraftHandlers.cs`, `TenderInviteComposerModal.razor` | **Fixed** — the draft is sanitised on save with the typed-body rule (`ComposeHtmlPipeline.AsDraft`). |
| Medium | Inbound email HTML was sanitised with the library's defaults, which keep form controls: a sender could draw a credential form inside the triage queue, on the portal's origin. | `InboundEmailBodyBuilder.Sanitise` | **Fixed** — form, input, button, select, textarea and the rest of the form family are removed from the allow-list. |
| Medium | Magick.NET opened images with no resource limits: a few-MB PNG declaring a 40,000-pixel canvas decodes to tens of GB and kills the host. Reachable from the Progress page, the site photo pool and the WhatsApp import. | `ProgressPhotoPreparation.cs` | **Fixed** — `ResourceLimits` (12,000 px per edge, 1 GB) set once, before the first decode. |
| Medium | The WhatsApp export zip was read with `ReadToEnd` / `CopyTo` and no decompressed bound — a crafted export deflating a thousandfold exhausts memory. | `WhatsAppExportArchive.cs` | **Fixed** — entries are read through a bounded copy (100 MB per entry, enforced on the real bytes), the shape `ArchiveEntryScreen` already used for document triage. |
| Medium | Drawing revision uploads had no size cap; every sibling caps at 64 MB. | `UploadDrawingRevisionEndpoint.cs` | **Fixed** — 64 MB. |
| Low | Six category OData filters skip the apostrophe-doubling their siblings apply. Not reachable today (every tag is portal-minted). | `MailboxGraphClient.Reading.cs`, `.Tagging.cs` | **Accepted** — noted for the day a free-text tag reaches them; one shared quoting helper is the fix. |
| OK | No raw SQL; `$search` stays one KQL phrase; blob keys cannot traverse; no user-controlled outbound URL (no SSRF); `System.Text.Json` only, no polymorphic deserialisation; the stored house model is capped at 512k characters; `.xlsx` cells are inline strings; the post-login redirect is local-only; OAuth `redirect_uri` is exact-matched and never redirected to on error; actor fields are overwritten from the signed-in user on every command; body ids are checked against the route; the document-triage archive reader is bounded and zip-slip-free. | throughout | Checked. |

## 4. Secrets

| Sev | Finding | Where | State |
|---|---|---|---|
| High | A live Google Places API key is committed in `api/local.settings.json` (entered at `c303729b`, still at HEAD). | `api/local.settings.json:7` | **Fixed in the tree** — the file is untracked (`git rm --cached`); it stays on disk for local runs. **Still to do by a person: rotate the key in Google Cloud Console now, restrict its replacement, and purge the file from history.** |
| High | The `.gitignore` rule for `local.settings.json` never applied to that file because it was tracked before the rule landed — the safety net everyone assumed was not there. | `.gitignore:34-36` | **Fixed** by untracking; a secret scanner in CI is the follow-up that keeps it fixed (below). |
| Medium | The admin SQL connection string is assembled in shell and pushed as a plain app setting; no Key Vault reference exists anywhere in the repository. The playbook's open item is confirmed. | `infra/azure-prod-setup-v2.sh:283,320-323` (and the two older scripts) | **Follow-up** — "Secrets and infrastructure hardening" task: Key Vault + managed identity, or Entra-authenticated SQL. |
| Medium | The provisioner echoed the SQL admin password to the terminal. | `infra/azure-prod-setup-v2.sh:120` | **Fixed** — it is written to the `chmod 600` output file only. |
| Medium | The MCP host setup copied every portal secret through a world-readable `/tmp` file. | `infra/azure-mcp-host-setup.sh:66-72` | **Fixed** — `umask 077` and `mktemp`. |
| Low | `infra/.azure-inventory.txt` carries the subscription id and the full resource map. | `infra/.azure-inventory.txt` | **Accepted** — not a credential; the repository is private. |
| Low | A stray `.fuse_hidden…` copy of the SWA deploy workflow was tracked under `.github/workflows/`. | `.github/workflows/` | **Fixed** — removed. |
| OK | Every other `secret`/`ClientSecret` hit is a variable name; both `.example` files use placeholders; no private keys, JWTs or SAS signatures anywhere; no credentials in any SQL script; GitHub Actions use `${{ secrets.* }}` and `pull_request`, not `pull_request_target`; history holds only templated connection strings. | throughout | Checked. |

## 5. Azure configuration and headers

| Sev | Finding | Where | State |
|---|---|---|---|
| Medium | The Static Web App sent no security headers at all: no HSTS, no `nosniff`, no frame-ancestors, no referrer policy. | `jpms/wwwroot/staticwebapp.config.json` | **Fixed** — `globalHeaders`: HSTS, `nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy`, `Permissions-Policy`, and a CSP of `frame-ancestors 'self'; object-src 'none'; base-uri 'self'`. A `script-src` policy needs a run against the built app (Blazor needs `wasm-unsafe-eval` and the import map is inline) — **follow-up**, in the hardening task. |
| Medium | The API stamped `nosniff` on exactly one endpoint. | `DownloadRequestEmailAttachmentEndpoint.cs` | **Fixed** — every file response through `InlineRendering.ForbidSniffing`; the SWA header covers the rest. |
| Medium | The SQL server has no Entra admin, no auditing and no threat detection; the only identity is the SQL-auth sysadmin. | `infra/azure-prod-setup-v2.sh:170` | **Follow-up** — hardening task. |
| Medium | Re-running the provisioner against an existing SQL server silently resets the sysadmin password. | `infra/azure-prod-setup-v2.sh:169-171` | **Decision** — guard behind a flag; asked, since the script is Nigel's runbook. |
| Low | The MCP Function App was created without `--https-only`, minimum TLS or FTPS lockdown — the host that handles OAuth bearer tokens. | `infra/azure-mcp-host-setup.sh:46` | **Fixed** — `--https-only true`, TLS 1.2, FTPS disabled, the line `phase1-provision.sh` already applies. |
| Low | Storage accounts keep shared-key access on; the account key is the app's credential. | `infra/azure-prod-setup-v2.sh:296-309` | **Follow-up** — hardening task (managed identity). |
| Low | The seven-day email-share SAS links had no HTTPS-only pin. | `AzureBlobEmailFileShareStore.cs:83` | **Fixed** — `Protocol = SasProtocol.Https`. |
| Low | One of the three availability probe locations was Moscow. | `infra/setup-alerts.sh:55` | **Fixed** — Paris. |
| OK | Public blob access off, containers private, TLS 1.2 minimum; the SQL firewall is the `0.0.0.0` Azure sentinel plus one operator IP — **not** wide open, as the task feared; no CORS anywhere and no credentialed cross-origin access; no `Console.WriteLine`; third-party failure bodies are logged truncated to 300 characters and never carry a secret. | `infra/*.sh`, `api/host.json` | Checked. |

## 6. Dependencies

From knowledge of published advisories as of 2026, not a live scan — the sandbox reaches neither
NuGet nor the advisory database. The full inventory is in the review's working notes; the findings:

| Sev | Finding | Where | State |
|---|---|---|---|
| High | The Blazor WebAssembly runtime was pinned to the un-serviced 8.0.0 GA release (predates CVE-2024-30045 and eleven servicing rounds), and it ships to every browser. | `jpms/Jewel.JPMS.csproj:13`, `prototypes/journey-index/…csproj:13` | **Fixed** — 8.0.11 (the DevServer alongside). Needs a restore and build to prove. |
| Medium | Magick.NET 13.10 (ImageMagick 7.1.1 line) and Docnet.Core 2.6 (a 2023 pdfium) are the two native parsers that read user uploads. | `api/JpmsApi.csproj:73,81` | **Follow-up** — "Native parser upgrades" task; Magick.NET 14 changes its size types, so it wants a build and the photo tests. |
| Medium | Nothing pins the SDK or freezes transitive versions: no `global.json`, no lock file, no central package management. | repository root | **Follow-up** — same task. |
| Low | No dependency or secret scanning in CI; the test gate was removed from the deploy workflows on 2026-09-04 and not restored. | `.github/workflows/` | **Follow-up** — hardening task (Dependabot, `dotnet list package --vulnerable`, gitleaks). |
| Low | `HtmlSanitizer` is the legacy package id; the project is `Ganss.Xss` and future bypass fixes publish there. | `api/JpmsApi.csproj:45` | **Follow-up** — same task. |
| Low | GitHub Actions are referenced by floating major tag, not commit SHA. | `.github/workflows/*.yml` | **Accepted** — the actions are first-party (`actions/*`, `Azure/*`); pin when Dependabot is in place to keep them current. |
| OK | `Azure.Identity` 1.12.1, `HtmlSanitizer` 8.1.870, EF Core / SqlClient 8.0.10 / 5.1.6, ClosedXML 0.104, OpenXml 3.1.1, three.js r186; `System.Text.Json` rides the patched shared framework; npm is one build-time devDependency. | `*.csproj`, `jpms/package.json` | Checked. |

## 7. The AI connector

How it works: the in-portal chat is retired; the MCP server is the one agent surface. Every tool
and action runs as the signed-in user, from a bearer token resolved on every call (`McpEndpoint.cs`),
with the actor stamped server-side and asserted at boot (`AiActionRegistry`); destructive actions
gate on the user's yes on the **server** (`AiActionGatewayTools`), and a declaration that calls itself
irreversible without that gate stops the app booting.

| Sev | Finding | Where | State |
|---|---|---|---|
| High | The record scopes were not run for actions (section 2). | `AiActionExecutor.cs` | **Fixed in #107 PR**. |
| Medium | Email bodies (30,000 characters of a stranger's HTML), previews and drawing callouts reached the model with no data/instruction boundary; the tender extractor is the one prompt that fences its input. | `AiMailboxTools.cs:167`, `AiSourceTools.cs:43` | **Fixed** — `AiUntrustedContent`: the message body is fenced and labelled as data, the MCP `instructions` say the same for every third-party text, and `AiSourceTools` reads the shared sentence. Previews and callouts are short and unfenced; the instructions cover them. |
| Medium | Dynamic client registration is open, and the name a registrant chooses is what the consent page shows — consent phishing costs one HTTP call. | `RegisterClientEndpoint.cs`, `ApproveAuthorizationEndpoint.cs` | **Decision** — an admin-issued registration token, or a redirect-host allow-list; asked. The rate limit on `oauth/register` rides with the hardening task. |
| Medium | Refresh-token rotation had no reuse detection: a replayed old token failed silently and the compromised family lived on. | `OAuthTokenManager.RefreshAsync` | **Fixed** — a presented refresh token that is already revoked revokes its whole family (OAuth 2.1 §6.1). |
| Low | Access tokens live seven days and were not ended by a password reset. | `OAuthDefaults.cs:17` | **Fixed** for the reset (section 1); the lifetime itself is **accepted** — the connectors handle rotation, and a shorter life is a one-constant change when wanted. |
| Low | `oauth/token` checks `client_id` and `redirect_uri` only when they are sent. | `TokenEndpoint.cs:62-68` | **Accepted** — PKCE binds the exchange; making both mandatory is right by the RFC but wants a run against the real connectors first. |
| OK | No system identity is reachable from MCP; PKCE S256 mandatory and verified; codes single-use and burned first; redirect URIs exact-matched at both ends; every OAuth secret hashed at rest; the confirm-first gate is server-side; filed-document reads re-check their gate on open; the turn log stores arguments clipped to 600 characters and never a result; connections are owner-scoped; **"Disconnect" is immediate** — the token row is re-read on every call before the resolver cache is consulted (the review's own first reading of this was wrong). | `Features/Connect`, `Features/Mcp`, `Features/Ai` | Checked. |

## 8. Data protection (UK GDPR)

Who can see what — the three the task names: **client emails** are read live from the projects
mailbox by Admin, FD, MD and PM only; copies land in the portal in `RequestMessages.Body`,
`KpiEmails`, `AuditEvents` and `BidPackageEmailDispositions.Note`. **Contracts** — terms, party names
and the executed PDF — are readable by every internal role (`AllInternal`), the widest gate for the
most sensitive client document. **Worker timesheets** are correctly split: hours and names to the
internal team, `RateApplied` and `CostAmount` only to the commercial team, and rates at source to four
roles. **How long data is kept**: there was no retention policy and no purge job anywhere in the
application; the only bounded stores were two blob lifecycle rules. The full inventory is in the
review's working notes on the task.

| Sev | Finding | Where | State |
|---|---|---|---|
| High | No privacy notice, terms or transparency information anywhere in the portal — staff, operatives, clients, architects and prospects submitting photographs of their homes get none. | `jpms/Pages/**` | **Follow-up** — "Data protection: notice, rights and retention" task. The controller's details and lawful bases are the business's words, not the code's. |
| High | Executed client contracts are readable by every internal role, including Foreman, H&S and Sales. | `GET projects/{id}/contract`, `/contract/document` | **Decision** — narrow to the commercial set the payment certificates already use? Asked. |
| High | No subject-access export and no erasure path for a client, an architect or a supplier record. | endpoint inventory | **Follow-up** — data protection task. |
| High | The Imagine link token is stored raw, never expires, and grants an anonymous caller the prospect's name, email, home photographs and the power to accept a priced proposal — indefinitely, from a QR code on a letter. | `CoreEntities.cs:47`, `ImaginePublicService.cs` | **Decision** — an expiry breaks letters already sent; asked. |
| Medium | No purge job for sessions, reset tokens, OAuth codes and tokens, or unanswered access requests — the schema anticipated one ("expired-row cleanup scans by expiry") and nothing ran. | `worker/` | **Fixed** — `RetentionSweepWorker`, nightly at 03:45 UTC: spent credentials after 30 days' grace, unanswered access requests after 90 days. |
| Medium | The OCR cache kept the full text of every scanned document forever, detached from the document. | `DocumentOcrResultEntity` | **Fixed** — the same sweep removes scan text after 180 days; the next read re-OCRs on a miss, as designed. |
| Medium | Audit events and agent activity are append-only by design and survive permanent user deletion; `Detail` embeds names. | `AuditEventEntity`, `AgentActivityEntity`, `DeleteDirectoryUserHandler` | **Follow-up** — data protection task: a defined period, and pseudonymising the actor on permanent deletion. |
| Medium | Deleting a lead left the prospect's photographs in the blob container, their estimate lines in `LeadEstimateLines`, and their qualification notes in six dead CRM tables. | `LeadHandlers.cs:281` | **Fixed** for the photographs and the estimate lines (the handler deletes both). The dead CRM tables are a **decision** (a migration that drops them). |
| Medium | Operatives' contact details and pay history cannot be deleted once a timesheet exists. | `DeleteWorkerSlice.cs:43-49` | **Follow-up** — a "retire worker" command that clears contact details and keeps the costing fields. |
| Medium | Absence free-text notes (the natural place a health reason is typed) and handwritten H&S signature images sit behind the widest internal gate. | `WorkerAbsenceEntity.Note`, `HsRecordAttendanceEntity.SignatureBlobRef` | **Decision** — structured reason codes; signature reads to H&S and management. Asked. |
| Medium | KPI email monitoring of named staff has no notice, no retention and no deletion on leaving. | `KpiEntities.cs` | **Decision** — the basis and the notice are the business's; asked. |
| Low | Client error reports send the user's email and 8 KB of stack to Application Insights; request telemetry is never sampled; the workspace has no explicit retention. | `LogClientErrorEndpoint.cs`, `infra/setup-appinsights.sh` | **Follow-up** — hardening task (`--retention-time`, an opaque user id). |
| Low | WhatsApp site-group chat is imported verbatim, third-party senders included. | `ProgressEntities.cs:15` | **Accepted** — the practice is the business's; it belongs in the notice to site staff. |
| Low | Hosting is North/West Europe, not UK South, and the position is undocumented. | `infra/azure-prod-setup-v2.sh:54-55` | **Accepted** — EEA adequacy applies; recorded here. |
| Low | The Anthropic API and Azure AI Vision receive correspondence and scanned contracts with no processing record or region pin. | `AnthropicOptions.cs`, `AzureVisionOcr.cs` | **Follow-up** — data protection task (`docs/data-processors.md`). |
| Low | The Imagine consent bundles service delivery with marketing in one tick; withdrawal is a manual email. | `ImagineSubmissionForm.razor:49` | **Follow-up** — data protection task. |
| Low | The Moscow availability probe (section 5). | | **Fixed**. |
| OK | Mailbox bodies are live-read, not warehoused; pay is separated from hours; credentials, tokens and IP addresses are hashed at rest; invite links are never logged; SQL backups have a defined life (35-day PITR, 4-week LTR, geo-redundant to the paired EEA region). | throughout | Checked. |

## What this PR changes, in one list

`SetPasswordEndpoint` (ends other sessions and tokens) · `OAuthTokenManager` (revoke-all, reuse
detection) · `PasswordHasher` / `LoginEndpoint` (constant-time refusal) · `AuthRateLimit` +
middleware · `InlineRendering` and thirteen download endpoints · `DownloadMailboxAttachmentEndpoint`
(no SVG) · `ComposeHtmlPipeline.AsDraft` + the invite draft handler · `InboundEmailBodyBuilder`
(no form controls) · `ProgressPhotoPreparation` (decoder limits) · `WhatsAppExportArchive` (bounded
reads) · `UploadDrawingRevisionEndpoint` (64 MB) · `BuildingControlAttachmentEndpoints` (gate first) ·
`AiUntrustedContent` + mailbox tool and MCP instructions · `RetentionSweepWorker` ·
`DeleteLeadHandler` (photographs and estimate lines) · `staticwebapp.config.json` (headers) ·
`jpms` WebAssembly 8.0.11 · `api/local.settings.json` untracked · the stray workflow file removed ·
`azure-mcp-host-setup.sh`, `azure-prod-setup-v2.sh`, `setup-alerts.sh` · `AzureBlobEmailFileShareStore`
(HTTPS SAS).

Nothing here has been compiled: no .NET SDK was reachable from the session. The first `dotnet build`
and `dotnet test` on the branch are the compile check.
