# Data processors and where personal data goes

The record of processing the portal relies on (UK GDPR Article 30), written from the code on
21 September 2026. The controller is Jewel Bespoke Build Limited — except for the onboarding forms
(below), where each form's controller is the Jewel company that asked for it. Every processor below acts on
Jewel's instructions under its own terms of service and data processing addendum; none is
permitted to use the data for its own purposes. The public statement of all this is the privacy
notice at `/privacy` (`jpms/Pages/Privacy.razor`).

## Where the portal lives

| What | Where | Why it matters |
|---|---|---|
| SQL Database `jpms` (`sql-jpms-prod-54cf9e`) | Azure **North Europe** (Ireland) — `infra/azure-prod-setup-v2.sh:54` | Every record in this document. 35-day point-in-time restore, weekly backups kept 12 weeks, geo-replicated to the paired EEA region (West Europe). |
| Static Web App, Function Apps (API, MCP host, worker) | Azure **West Europe** (Netherlands) — `:55` | The application and its logs. |
| Blob storage containers | Same resource group | `imagine` (prospects' home photographs and concept renders), `progress-photos`, `drawings`, `document-control`, `project-contracts`, `compliance-documents`, `request-attachments`, `work-order-attachments`, `bid-package-attachments`, `building-control-attachments`, `architect-instructions`, `company-documents`, `email-shares`, and the forms' three: `form-uploads`, `form-right-to-work`, `form-payroll-starters`. Private; served only through the API. |
| Application Insights / Log Analytics (`log-jpms-prod`) | West Europe | ILogger output. Some lines name an email address (an invite that could not be sent, an imagine email ACS refused). Retention is the workspace default until the infrastructure task sets `--retention-time`. |

The EEA is recognised by the UK as offering adequate protection, so no transfer mechanism beyond
the adequacy regulations is needed. Nothing is hosted outside the EEA and the UK.

## Processors

| Processor | Service and code | Personal data it receives | Region |
|---|---|---|---|
| **Microsoft** (Azure) | Hosting above | Everything the portal holds | EEA |
| **Microsoft 365 — Graph** | The shared projects mailbox, read live and written to (drafts, categories): `api/Features/MailboxIntake/Graph/MailboxGraphClient*.cs`, app-only token, scope `https://graph.microsoft.com/.default` | Senders, recipients, subjects, bodies and attachments of every email the projects mailbox sends or receives. The portal reads the mailbox live; it stores message ids and, where a message is filed to a record, its body as a request message | Microsoft 365 tenant (UK) |
| **Azure Communication Services** | The emails the portal sends: invites and password resets (`api/Auth/AzureEmailInviteNotifier.cs`), the imagine journey's emails to prospects (`api/Features/Sales/Imagine/ImagineNotifier.cs`), the forms' emails (`api/Features/Forms/Mail/AcsFormMailer.cs`: a form's or a pack's one-time link, the person's own copy, the office's alert, the renewal chase, the right-to-work confirmation), from `DoNotReply@mail.jewelbb.co.uk` unless `Forms__Sender__<jbb or jps>` names another | Recipient address and display name, a single-use link, concept and proposal wording; for the forms, the answers a copy or an alert may carry — never a sensitive or health answer, and nothing at all from a right-to-work or starter-checklist form | EEA |
| **Azure AI Vision (Read)** | OCR of scanned PDFs the assistant is asked to read: `api/Features/Ai/Scans/AzureVisionOcr.cs`, `POST …/computervision/imageanalysis:analyze?features=read`, the page as a PNG | The content of the scanned page — a contract, a certificate, a letter, whatever the person asked the assistant to read. The recognised text is cached in `DocumentOcrResults` for 180 days | The resource's region: pin it to an EEA region in `infra/` (`DocumentOcr__Endpoint`) — open item on the infrastructure task |
| **Azure OpenAI (`gpt-image-1`)** | The imagine concepts: `api/Features/Sales/Imagine/AzureImageClient.cs`, image edits at `{AzureImage__Endpoint}` | The prospect's own photographs of their home and the concept prompt written from their brief | The resource's region: EEA |
| **Anthropic** (Claude API) | The assistant behind the portal's chat and the MCP connector, the imagine concept writer, sales research, bid-package suggestion, local business search: `api/Features/Ai/ClaudeClient.cs` → `https://api.anthropic.com/v1/messages`; models in `api/Features/Ai/AnthropicOptions.cs` | What a member of staff asks it to read: correspondence, documents, drawings' extracted data, records; for imagine, the prospect's photographs and brief. Never a form's health answers or the answers `SensitiveAnswers` names — the connector withholds them (`api/Features/Ai/Tools/AiFormReading.cs`). Anthropic's commercial API terms: inputs are not used to train models and are retained only for abuse monitoring under their retention policy | USA, under Anthropic's UK GDPR data processing addendum and the UK IDTA / EU SCCs it incorporates |
| **Xero** | Accounts: bills, sales invoices, contacts, tracking, attachments: `api/Features/Xero/XeroClient*.cs`; nightly sync `worker/Xero/XeroNightlyWorker.cs` | Supplier and client contact names, emails and phone numbers (`XeroClient.ContactPeople.cs`, `.SalesContact.cs`), invoice and payment figures, the valuation certificate PDF attached to a sales invoice | Xero's UK/EU hosting under its DPA |
| **Bluebeam Studio** (UK) | Drawing markup extraction: `api/Features/Bluebeam/BluebeamClient*.cs`, `https://api.bluebeamstudio.co.uk`; callback on the worker app | Architects' drawings uploaded into Studio sessions, and the connected account's email (`BluebeamEntities.ConnectedEmail`) | UK |
| **Direct website reads** | `api/Features/Places/WebsiteContactFinder.cs` fetches a company's homepage and `/contact` to find a public email and phone | Publicly published business contact details, written onto a directory record for a person to confirm | The company's own site |

There is no Google Places or Maps API: the only `google` strings in the code are excluded-domain
lists. The Places feature is Claude web search plus the direct website read above.

## The onboarding forms (moved from the JPS Dashboard, 2026-09-23)

New starters, self-employed individuals and sub-contracting companies of both Jewel companies fill
their forms in on the portal (`/f/<jbb or jps>/<form>`, no account). The portal is now the system that
holds them, and the JPS Dashboard's `/forms` addresses redirect to it (`/forms/subcontractor-jbb` to the
JBB questionnaire). Every form, link, pack and register row
carries the Jewel company it speaks for, and that company is the controller of what is sent on it:
Jewel Bespoke Build Ltd for its forms, Jewel Property Serve Ltd for its own, both held in this portal.

| What | Where it is kept | Who reads it |
|---|---|---|
| The forms sent (`FormSubmissions`, answers as JSON) and the person or company they are filed under (`FormFolders`) | SQL, as above | The office (`FormRoleSets.Office`); a form kept in a restricted store only that store's readers |
| Files sent with a form | `form-uploads` | The office |
| Right to work: the form's files and the checker's evidence (`RightToWorkChecks`) | `form-right-to-work` | `FormRoleSets.RightToWorkReaders`: administrators, the MD, the FD, the compliance coordinator |
| The HMRC new starter checklist's files | `form-payroll-starters` | `FormRoleSets.PayrollStarterReaders`: as above, and accounts |
| An emergency contact form's two health answers (Article 9) | In the form's answers, withheld from every reading until revealed | `FormRoleSets.EmergencyContactReaders`; each reveal is `AuditEventType.FormHealthAnswersRevealed` |
| One-time links and packs (`FormInvites`, `FormPacks`) | SQL — only the SHA-256 of a link's secret | The office |
| Training register, workstation actions, licence checks | SQL | The office |

What is never echoed: the person's copy and the office's alert leave out every answer
`SensitiveAnswers` names — the dashboard's SENSITIVE pattern on the question key (UTR, NI number,
company registration number, date of birth, passport and licence details, share codes, the DVLA check
code, points, disqualifications, tachograph offences, convictions, eyesight, medical, health,
disability, bank details, sex) plus every question marked special category — and a form kept in a restricted store alerts
the office with no answers at all, because the alert goes wider than the store's readers. Recording a
licence check deletes the licence photograph there and then and withholds the driving-record answers
and the check code from the stored form for good (`RecordDrivingLicenceCheckHandler`). A questionnaire's
or insurance update's company documents may be filed onto the directory company's compliance record,
which is read far more widely than a form; a photo of someone's ID, a DBS check and a signature never
are (`FormDirectoryFilingPlan.IsFileable`). Only a certificate that came in on a form is chased for
renewal (`ComplianceDocuments.FormCompany`), in the name of the Jewel company whose form it was.

Retention (`FormRetention` in contracts, Jeremy's periods of 26 Aug 2026 from `lib/retention.js`),
carried out nightly at 04:15 by `worker/Forms/FormRetentionWorker.cs` — the date is the decision, so
nothing waits for an approval click. A destruction deletes the files from their store, clears the
answers, leaves the submission row as a tombstone naming nobody, and writes
`AuditEventType.FormRecordsDestroyed` saying what went and under which rule. A clock starts only on a
date the office records (Forms → People & companies): a leaver with no date is never clocked. As the
dashboard's one leaving date clocked every folder, a person's leaving date recorded there is also
written onto the right-to-work checks and training records made from their forms, where still blank.
A date recorded before a form was sent belongs to an earlier engagement and never clocks it, so a
person who comes back is not destroyed on their old clock. Setting a folder's dates needs a reader of
every store its forms are in, refuses a date still to come, warns when a date destroys records that
night, and is on the audit trail (`FormFolderDatesRecorded`, old and new dates, who set them).

| Form evidence | Destroyed |
|---|---|
| Right to work (the form and the checker's record and evidence) | 2 years after the engagement ended |
| Everything else a person sends (starter checklist, emergency contact, workstation assessment, training certificates, accident reports) | 6 years after the engagement ended (the dashboard's 3 years kept and 3 archived) |
| A company vehicle form | 6 years after the vehicle came back (else the engagement's end); the licence photograph on the day the licence is checked |
| A sub-contracting company's questionnaire and insurance updates | 6 years after the company's last form |
| A file whose form was never sent | 18 months after it arrived |
| A link nobody used | 12 months after it expired or was cancelled |

## Who sees what inside the portal

The role gates are the system of record for this (`tools/permissions/`): every page names who may
open it and every endpoint is gated; the check `python3 -m tools.permissions.check .` reads them.
In short: staff see the projects they work on by role; a client login sees its own projects; an
architect login the projects that name its practice; a subcontractor login the orders placed with
it and the projects it holds an issued order on. The MCP connector runs the same gates and scopes.

## Retention (the nightly sweep, `worker/Retention/RetentionPeriods.cs`)

| Store | Kept for |
|---|---|
| Sessions, invite and reset tokens, OAuth codes and tokens | 30 days after they expire or are used |
| Unanswered access requests | 90 days |
| Cached OCR text of a scanned document | 180 days (re-read from the document on demand) |
| Audit trail (`AuditEvents`) | 7 years — six years past the year the financial records it evidences belong to |
| Agent activity log (`AgentActivity`) | 2 years |
| Project, contract, valuation, order and invoice records | Kept as business and statutory records; a person's details on them are erased on request through Admin → Data protection (`AnonymisePerson`), which keeps the money and dates and rewrites who to a pseudonym |
| A lead that did not become a project, with its photographs and concepts | Until the lead is deleted (`DeleteLead`) or the person asks (`AnonymisePerson`) |
| A worker's contact details | Until retired (`RetireWorker`); name, rate history and timesheets stay with the cost records |
| The onboarding forms | Their own clocks — see *The onboarding forms* above (`FormRetention`, `worker/Forms/FormRetentionWorker.cs`) |

## Rights, and the code that honours them

| Right | How |
|---|---|
| Access | Admin → Data protection reads the dossier (`GetPersonDossier`): every row that is the person and every column that names them. A person's forms are read under their name on Forms → People & companies; the dossier counts the forms' columns that carry their address |
| Erasure | `AnonymisePerson` (Admin → Data protection, `anonymise_person`): details erased, mentions rewritten to `erased-<hash>@erased.invalid`, nothing deleted. A sign-in is removed first (`DeleteDirectoryUser`, which also pseudonymises the audit and agent-activity actor); a worker with history is retired first (`RetireWorker`) |
| Withdraw consent | The prospect's own imagine page (`POST imagine/{token}/keep-in-touch/stop`), or the sales team recording it (`WithdrawLeadMarketingConsent`). `SendSalesProposal` refuses once withdrawn |
| Object, restrict, rectify, portability | By email to the privacy contact in the notice; handled by hand against the dossier |

## Open items (on the infrastructure and security-review tasks)

- Pin the Azure AI Vision resource's region in `infra/` and record it above.
- Set Application Insights retention and stop logging email addresses (`LogClientErrorEndpoint`).
- Nigel's decisions on the security review task: KPI email monitoring (its notice and retention), absence notes as reason codes, signature images' gate, dropping the six dead CRM tables.
- The onboarding forms, for Jeremy (on the retention task under the forms goal): whether Jewel Property Serve's forms held in this portal need an intra-group processing agreement between the two companies; whether an erasure request should destroy a person's forms before their retention date (today they go on their date; the right-to-work record must stay two years after the engagement ends); whether the dashboard's three-year archive stage is wanted back; and whether the forms' link emails should point at the privacy notice as the portal's invite emails do.
- The privacy contact mailbox is `info@jewelbb.co.uk` (`jpms/Features/Privacy/PrivacyContacts.cs`), confirmed to exist on 2026-09-21. `privacy@jewelbb.co.uk` was never created, so the notice pointed at nothing between the notice going live and that date.
