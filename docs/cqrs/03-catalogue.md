# 3. The Command and Query catalogue

Every current Azure Function entry point, renamed to the user story it serves. Where one current endpoint silently covers multiple stories, it splits into multiple commands. Where a user story has no current endpoint, the row is marked **NEW** so it surfaces in the slice plan.

Naming convention (from `CLAUDE.md`):

- **Commands** are imperative verbs in present tense, named after the action a user takes: `CreateProject`, `IssueValuation`, `ApproveSubcontractorBid`. Never `Upsert`, never `Save`, never `Update` on its own.
- **Queries** start with `Get`, `List`, or `Find`, and the rest of the name describes the result *and* the predicate: `GetProjectsForUser`, `ListOpenRfisForProject`, `FindSubcontractorByCompanyNumber`.
- The route stays close to today's where possible; renames are flagged with **route changes**.

## Workflow 00 — CRM (currently `LeadsApi.cs`, `LeadAttachmentsApi.cs`)

| Today's `[Function]` | New Command or Query | Story | Route |
|---|---|---|---|
| `ListLeads` | `ListLeadsInPipeline` (query) | US-00-01,11,12 | `GET /api/leads` |
| `UpsertLead` (create case) | `CaptureLead` (command) | US-00-01 | `POST /api/leads` |
| `UpsertLead` (edit case) | `UpdateLeadDetails` (command) | US-00-01 | `PUT /api/leads/{leadId}` |
| `GetQualification` | `GetLeadQualification` (query) | US-00-02 | `GET /api/leads/{leadId}/qualification` |
| `UpsertQualification` | `RecordLeadQualificationScore` (command) | US-00-02 | `POST /api/leads/{leadId}/qualification` |
| `ListSiteVisits` | `ListSiteVisitsForLead` (query) | US-00-03 | `GET /api/leads/{leadId}/site-visits` |
| `UpsertSiteVisit` (create) | `BookSiteVisit` (command) | US-00-04 | `POST /api/leads/{leadId}/site-visits` |
| `UpsertSiteVisit` (capture) | `RecordSiteVisitNotes` (command) | US-00-04 | `PUT /api/site-visits/{siteVisitId}` |
| `ListInfoChase` | `ListInformationChaseItemsForLead` (query) | US-00-05 | `GET /api/leads/{leadId}/info-chase` |
| `UpsertInfoChase` | `RecordInformationChaseItem` (command) | US-00-05 | `POST /api/leads/{leadId}/info-chase` |
| `UpsertBidDecision` | `RecordBidDecision` (command) | US-00-06 | `POST /api/leads/{leadId}/bid-decision` |
| `GetProposal` | `GetProposalForLead` (query) | US-00-08 | `GET /api/leads/{leadId}/proposal` |
| `UpsertProposal` (issue) | `IssueProposal` (command) | US-00-08 | `POST /api/leads/{leadId}/proposal` |
| `UpsertProposal` (revise) | `ReviseProposal` (command) | US-00-09 | `PUT /api/leads/{leadId}/proposal` |
| `GetLeadOutcome` | `GetLeadOutcome` (query) | US-00-10,11 | `GET /api/leads/{leadId}/outcome` |
| `UpsertLeadOutcome` (won) | `MarkLeadAsWon` (command, **spawns project shell**) | US-00-10 | `POST /api/leads/{leadId}/won` |
| `UpsertLeadOutcome` (lost) | `MarkLeadAsLost` (command, records reason) | US-00-11 | `POST /api/leads/{leadId}/lost` |
| **NEW** | `ListEstimatingQueue` (query) | US-00-07 | `GET /api/estimating-queue` |
| **NEW** | `ListNurtureFollowUpsDue` (query) | US-00-12 | `GET /api/nurture/due` |
| **NEW** | `GetSalesAttributionReport` (query) | US-00-14 | `GET /api/sales-analytics/attribution` |

## Workflow 01 — Drawings (currently `DrawingsApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListDrawings` | `ListDrawingsForProject` (query) | US-01-02,03 | `GET /api/projects/{projectId}/drawings` |
| `UpsertDrawing` (create) | `RegisterDrawing` (command) | US-NEW-01 | `POST /api/projects/{projectId}/drawings` |
| `UpsertDrawing` (edit) | `UpdateDrawingMetadata` (command) | US-01-02 | `PUT /api/drawings/{drawingId}` |
| `ListRevisions` | `ListRevisionsForDrawing` (query) | US-01-04,05 | `GET /api/drawings/{drawingId}/revisions` |
| `AddRevision` | `IssueDrawingRevision` (command, **supersedes prior**) | US-01-05, US-NEW-04 | `POST /api/drawings/{drawingId}/revisions` |
| **NEW** | `ListAmbiguousRevisionsForProject` (query) | US-01-04 | `GET /api/projects/{projectId}/drawings/ambiguous` |
| **NEW** | `AcknowledgeDrawingViewedOnSite` (command) | US-01-06,08 | `POST /api/drawings/{drawingId}/acknowledgements` |

## Workflow 02 — BoQ and Rates (currently `BoqApi.cs`, `RatesApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListBoqLines` | `ListBoqLinesForProject` (query) | US-02-05,07 | `GET /api/projects/{projectId}/boq` |
| `UpsertBoqLine` (create) | `AddBoqLine` (command) | US-NEW-02 | `POST /api/projects/{projectId}/boq` |
| `UpsertBoqLine` (edit) | `UpdateBoqLine` (command) | US-02-08 | `PUT /api/boq-lines/{boqLineItemId}` |
| `DeleteBoqLine` | `RemoveBoqLine` (command) | US-02-08 | `DELETE /api/boq-lines/{boqLineItemId}` |
| `GetBoqSignOff` | `GetBoqSignOffForProject` (query) | US-02-11 | `GET /api/projects/{projectId}/boq/sign-off` |
| `RecordBoqSignOff` | `SignOffBoqForProject` (command, **Director-only gate**) | US-02-11 | `POST /api/projects/{projectId}/boq/sign-off` |
| **NEW** | `ImportBoqFromCsv` (command) | US-NEW-03 | `POST /api/projects/{projectId}/boq/import` |
| **NEW** | `CompareCurrentBoqAgainstLastTender` (query) | US-02-08 | `GET /api/projects/{projectId}/boq/compare` |
| **NEW** | `RecordWalkRoundNote` (command) | US-02-09 | `POST /api/projects/{projectId}/boq/walk-round` |
| `ListRates` | `ListRatesInLibrary` (query) | US-02-05 | `GET /api/rates` |
| `UpsertRate` (create) | `AddRate` (command) | US-02-05 | `POST /api/rates` |
| `UpsertRate` (edit) | `ReviseRate` (command, **new version**) | US-02-05 | `PUT /api/rates/{rateId}` |
| **NEW** | `ListStaleRates` (query) | US-NEW-06 | `GET /api/rates/stale` |

## Workflow 03 — Procurement (currently `ProcurementApi.cs`, `SubcontractorsApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListBidPackages` | `ListBidPackagesForProject` (query) | US-03-01,02 | `GET /api/projects/{projectId}/bid-packages` |
| `UpsertBidPackage` (create) | `CreateBidPackage` (command) | US-03-01 | `POST /api/projects/{projectId}/bid-packages` |
| `UpsertBidPackage` (edit) | `UpdateBidPackageScope` (command) | US-03-02 | `PUT /api/bid-packages/{bidPackageId}` |
| **NEW** | `InviteSubcontractorToBid` (command) | US-03-03 | `POST /api/bid-packages/{bidPackageId}/invitations` |
| `ListQuotes` | `ListQuotesForBidPackage` (query) | US-03-08 | `GET /api/bid-packages/{bidPackageId}/quotes` |
| `UpsertQuote` (submit) | `SubmitQuoteForBidPackage` (command) | US-03-05,06 | `POST /api/bid-packages/{bidPackageId}/quotes` |
| `UpsertQuote` (revise) | `ReviseQuote` (command) | US-03-06 | `PUT /api/quotes/{quoteId}` |
| **NEW** | `DeclineBidInvitation` (command) | US-03-07 | `POST /api/bid-packages/{bidPackageId}/declines` |
| **NEW** | `CompareQuotesSideBySide` (query) | US-03-08 | `GET /api/bid-packages/{bidPackageId}/comparison` |
| `ListWorkOrders` | `ListWorkOrders` (query) | US-03-10 | `GET /api/work-orders` |
| `UpsertWorkOrder` (award) | `AwardBidPackage` (command, **compliance gate**) | US-03-10,11,21 | `POST /api/bid-packages/{bidPackageId}/award` |
| `UpsertWorkOrder` (edit) | `UpdateWorkOrder` (command) | US-03-10 | `PUT /api/work-orders/{workOrderId}` |
| **NEW** | `GetTenderHistoryForProject` (query) | US-03-12 | `GET /api/projects/{projectId}/tender-history` |
| `ListSubcontractors` | `ListSubcontractors` (query) | US-03-13 | `GET /api/subcontractors` |
| `UpsertSubcontractor` (create) | `AddSubcontractorToDirectory` (command) | US-03-13 | `POST /api/subcontractors` |
| `UpsertSubcontractor` (edit) | `UpdateSubcontractor` (command) | US-03-14 | `PUT /api/subcontractors/{subcontractorId}` |
| `ListCompliance` | `ListComplianceDocumentsForSubcontractor` (query) | US-03-13,18 | `GET /api/subcontractors/{subcontractorId}/compliance` |
| `UpsertCompliance` | `UploadComplianceDocument` (command) | US-03-14,18,22 | `POST /api/subcontractors/{subcontractorId}/compliance` |
| **NEW** | `RecordRamsAcceptance` (command) | US-03-19,20 | `POST /api/subcontractors/{subcontractorId}/rams` |

## Workflow 04 — H&S and Mobilisation (currently `HsApi.cs`, `MobilisationApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListMobilisationItems` | `GetMobilisationChecklistForProject` (query) | US-04-02 | `GET /api/projects/{projectId}/mobilisation` |
| `UpsertMobilisationItem` (toggle) | `ConfirmMobilisationChecklistItem` (command) | US-04-03 | `POST /api/mobilisation-items/{mobilisationItemId}/confirm` |
| `UpsertMobilisationItem` (edit) | `UpdateMobilisationChecklistItem` (command) | US-04-02 | `PUT /api/mobilisation-items/{mobilisationItemId}` |
| **NEW** | `OpenMobilisationGate` (command, **hard block until checklist clear**) | US-04-04 | `POST /api/projects/{projectId}/mobilisation/gate` |
| `ListHsRecords` | `ListHsRecords` (query) | US-04-05,06,07,08 | `GET /api/hs-records` |
| `UpsertHsRecord` (create per kind) | `LogInspection`, `LogObservation`, `LogIncident`, `LogToolboxTalk`, `LogTemporaryWorks` (five commands) | US-04-05..13 | `POST /api/hs-records/{kind}` |
| `UpsertHsRecord` (edit) | `UpdateHsRecord` (command) | US-04-09 | `PUT /api/hs-records/{hsRecordId}` |
| `ListAttendance` | `ListAttendanceForHsRecord` (query) | US-04-11 | `GET /api/hs-records/{hsRecordId}/attendance` |
| `AddAttendance` | `RecordAttendanceForToolboxTalk` (command) | US-04-11, US-NEW-05 | `POST /api/hs-records/{hsRecordId}/attendance` |
| **NEW** | `OpenCorrectiveAction` (command) | US-04-09 | `POST /api/hs-records/{hsRecordId}/corrective-actions` |
| **NEW** | `CloseCorrectiveAction` (command) | US-04-16 | `POST /api/corrective-actions/{correctiveActionId}/close` |
| **NEW** | `GetHsGoldenThreadForProject` (query) | US-04-14 | `GET /api/projects/{projectId}/hs/golden-thread` |

## Workflow 05 — Changes, RFIs, Variations (currently `ChangesApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListChanges` | `ListChangesForProject` (query, optional kind filter) | US-05-01,05,08,09 | `GET /api/projects/{projectId}/changes` |
| `UpsertChange` (raise) | `RaiseChange` (command — kind = `Rfi` \| `Variation` \| `Delay`) | US-05-01,05,09 | `POST /api/projects/{projectId}/changes` |
| `UpsertChange` (respond) | `RespondToRfi` (command) | US-05-06,07 | `POST /api/changes/{changeRecordId}/response` |
| `UpsertChange` (approve VO) | `ApproveVariation` (command, **approver role gate**) | US-05-04,10 | `POST /api/changes/{changeRecordId}/approval` |
| `UpsertChange` (edit) | `UpdateChangeDetails` (command) | US-05-02 | `PUT /api/changes/{changeRecordId}` |
| **NEW** | `GetVariationReportForProject` (query) | US-05-11 | `GET /api/projects/{projectId}/variations/report` |
| **NEW** | `IssueRfiChaseReminder` (command, **server-triggered**) | US-05-08 | `POST /api/rfis/{changeRecordId}/chase` |

## Workflow 06 — Site Delivery (currently `SiteApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListSiteReports` | `ListSiteReportsForProject` (query) | US-06-09 | `GET /api/projects/{projectId}/site-reports` |
| `UpsertSiteReport` (assemble) | `AssembleSiteReport` (command) | US-06-09 | `POST /api/projects/{projectId}/site-reports` |
| `UpsertSiteReport` (approve) | `ApproveSiteReport` (command) | US-06-10 | `POST /api/site-reports/{siteReportId}/approval` |
| `ListProgrammeTasks` | `GetProgrammeForProject` (query) | US-07-01 | `GET /api/projects/{projectId}/programme` |
| `UpsertProgrammeTask` (create) | `AddProgrammeTask` (command) | US-07-01 | `POST /api/projects/{projectId}/programme` |
| `UpsertProgrammeTask` (edit) | `UpdateProgrammeTask` (command) | US-07-01 | `PUT /api/programme-tasks/{programmeTaskId}` |
| **NEW** | `RecordSiteAttendance` (command) | US-06-02,03 | `POST /api/projects/{projectId}/site/attendance` |
| **NEW** | `CapturePhotoFromSite` (command) | US-06-05,06 | `POST /api/projects/{projectId}/site/photos` |
| **NEW** | `RecordSiteProgressPercentage` (command) | US-06-04 | `POST /api/projects/{projectId}/site/progress` |
| **NEW** | `RaiseSnag` (command) | US-06-07 | `POST /api/projects/{projectId}/site/snags` |
| **NEW** | `SyncOfflineSiteQueue` (command) | US-06-08 | `POST /api/site/sync` |

## Workflow 07 — Commercial, CVR, Cashflow (currently `CommercialApi.cs`, `CvrApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListValuations` | `ListValuationsForProject` (query) | US-07-03,06 | `GET /api/projects/{projectId}/valuations` |
| `UpsertValuation` (draft) | `DraftValuation` (command) | US-07-03,04,05 | `POST /api/projects/{projectId}/valuations` |
| `UpsertValuation` (issue) | `IssueValuation` (command, **Director-approver gate**) | US-07-06,08,09 | `POST /api/valuations/{valuationId}/issue` |
| `UpsertValuation` (revise) | `ReviseValuation` (command) | US-07-04 | `PUT /api/valuations/{valuationId}` |
| `ListBudgets` | `ListCostCodeBudgetsForProject` (query) | US-07-20 | `GET /api/projects/{projectId}/cost-code-budgets` |
| **NEW** | `SetCostCodeBudget` (command) | US-07-20 | `POST /api/projects/{projectId}/cost-code-budgets` |
| **NEW** | `ListCostCodeOverrunsForProject` (query) | US-07-20,21 | `GET /api/projects/{projectId}/cost-codes/overruns` |
| `ListTimesheets` | `ListTimesheetsForProject` (query) | US-07-13,17,18 | `GET /api/projects/{projectId}/timesheets` |
| `UpsertTimesheet` (submit) | `SubmitTimesheet` (command, **cost-code hard-block**) | US-07-13,22 | `POST /api/timesheets` |
| `UpsertTimesheet` (approve) | `ApproveTimesheet` (command) | US-07-17,18 | `POST /api/timesheets/{timesheetId}/approval` |
| `ListSnapshots` | `ListCvrSnapshotsForProject` (query) | US-07-23,27 | `GET /api/projects/{projectId}/cvr-snapshots` |
| **NEW** | `CaptureCvrSnapshot` (command) | US-07-23 | `POST /api/projects/{projectId}/cvr-snapshots` |
| `ListForecastComponents` | `ListForecastComponentsForProject` (query) | US-07-26 | `GET /api/projects/{projectId}/forecast-components` |
| `ListAccruals` | `ListQsAccrualsForProject` (query) | US-07-24 | `GET /api/projects/{projectId}/qs-accruals` |
| `UpsertAccrual` (create) | `RecordQsAccrual` (command) | US-07-24 | `POST /api/projects/{projectId}/qs-accruals` |
| `UpsertAccrual` (edit) | `UpdateQsAccrual` (command) | US-07-24 | `PUT /api/qs-accruals/{qsAccrualId}` |
| `ListPrelims` | `ListPrelimItemsForProject` (query) | US-07-28 | `GET /api/projects/{projectId}/prelims` |
| `ListPrelimEntries` | `ListPrelimEntriesForItem` (query) | US-07-28,30 | `GET /api/prelims/{prelimItemId}/entries` |
| **NEW** | `RecordPrelimForecastForWeek` (command) | US-07-28,30 | `POST /api/prelims/{prelimItemId}/entries` |
| `ListEots` | `ListEotsForProject` (query) | US-07-31 | `GET /api/projects/{projectId}/eots` |
| `UpsertEot` (grant) | `GrantEot` (command) | US-07-31 | `POST /api/projects/{projectId}/eots` |
| `UpsertEot` (edit) | `UpdateEot` (command) | US-07-31 | `PUT /api/eots/{eotId}` |
| **NEW** | `GetLatestCashflowSnapshot` (query) | US-07-36,37 | `GET /api/cashflow/latest` |
| **NEW** | `ListCashflowAttentionItems` (query) | US-07-39 | `GET /api/cashflow/attention` |
| **NEW** | `CaptureCashflowSnapshot` (command) | US-07-42 | `POST /api/cashflow/snapshots` |

## Workflow 08 — Closeout (currently `CloseoutApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListDefects` | `ListDefectsForProject` (query) | US-08-01,03,04 | `GET /api/projects/{projectId}/defects` |
| `UpsertDefect` (raise) | `RaiseDefect` (command) | US-08-01,04 | `POST /api/projects/{projectId}/defects` |
| `UpsertDefect` (sign off) | `SignOffDefect` (command) | US-08-05 | `POST /api/defects/{defectId}/sign-off` |
| `UpsertDefect` (edit) | `UpdateDefect` (command) | US-08-03 | `PUT /api/defects/{defectId}` |
| `GetSettlement` | `GetSettlementForProject` (query) | US-08-09,10 | `GET /api/projects/{projectId}/settlement` |
| `UpsertSettlement` (open) | `OpenSettlement` (command) | US-08-09 | `POST /api/projects/{projectId}/settlement` |
| `UpsertSettlement` (agree) | `AgreeSettlement` (command, **Director gate**) | US-08-11,18,20 | `POST /api/settlements/{settlementId}/agreement` |
| `GetVat` | `GetVatAnalysisForProject` (query) | US-08-13 | `GET /api/projects/{projectId}/vat` |
| `UpsertVat` (draft) | `DraftZeroRatedVatAnalysis` (command) | US-08-13,14 | `POST /api/projects/{projectId}/vat` |
| `UpsertVat` (agree) | `AgreeVatAnalysisWithClient` (command) | US-08-16,17 | `POST /api/vat-analyses/{vatAnalysisId}/agreement` |
| `UpsertRetention` | `ReleaseRetention` (command) | US-08-07,19 | `POST /api/projects/{projectId}/retention/release` |
| **NEW** | `AssembleCloseoutPack` (command) | US-08-06 | `POST /api/projects/{projectId}/closeout-pack` |
| **NEW** | `ConfirmPracticalCompletion` (command) | US-08-12 | `POST /api/projects/{projectId}/practical-completion` |

## Workflow — Projects hub (currently `ProjectsApi.cs`)

| Today | New | Story | Route |
|---|---|---|---|
| `ListProjects` | `ListProjectsVisibleToUser` (query) | Hub | `GET /api/projects` |
| `GetProject` | `GetProjectById` (query) | Hub | `GET /api/projects/{projectId}` |
| `UpsertProject` (create) | `CreateProjectShell` (command, **from won lead**) | US-00-10 | `POST /api/projects` |
| `UpsertProject` (edit) | `UpdateProjectDetails` (command) | Hub | `PUT /api/projects/{projectId}` |
| **NEW** | `SetProjectContractTerms` (command, **Claim Period + retention %**) | US-07-02 | `POST /api/projects/{projectId}/contract-setup` |

## Summary counts

- **69** existing entry points across fifteen `*Api.cs` files. Many of them double as both a create and an edit path under one `Upsert` name; splitting along intention roughly doubles the command count.
- **Every** existing entry point appears in the catalogue under a renamed command or query — nothing is dropped silently.
- **30-plus new commands and queries** surfaced by user stories that have no current endpoint, each marked **NEW** and each tied to a story already listed in `docs/site-map.md`.
- **Zero** entry points have current gates; **all** of them gain authentication, authorisation, and (commands only) validation.

The catalogue is the contract the rest of the refactor implements. Adding a row here is how the team agrees a new endpoint exists; renaming a row here is how the team agrees what it is called everywhere.
