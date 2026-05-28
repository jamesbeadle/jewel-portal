# 6. API surface and authorisation map (Phase 2)

The complete HTTP surface exposed by `JpmsApi`, and the role gate on every state-changing
operation. Authentication is uniform: every endpoint resolves the signed-in user from the
Static Web Apps `X-MS-CLIENT-PRINCIPAL` header (`401` if absent). **Roles are resolved from
the persisted directory** (`DirectoryUserRole` rows) plus the administrator allow-list, not
from the sign-in token — see `SignedInUserResolver`. Queries authenticate only; commands
additionally authorise and validate.

Persona codes follow the README (P01 Director … P11 Foreman). "Admin" is the application
administrator allow-list (`JpmsAdministrators`), distinct from the eleven personas; admins
resolve to every role.

## Commands (authorise → validate → handle)

| Command | Method · route | Allowed roles |
|---|---|---|
| CreateProjectShell | POST /api/projects | Director, ProjectManager |
| UpdateProjectDetails | PUT /api/projects/{projectId} | Director, ProjectManager |
| CaptureLead / UpdateLeadDetails / BookSiteVisit / RecordInformationChaseItem / IssueProposal / ReviseProposal / RecordLeadQualificationScore | POST·PUT /api/leads… | Director, ProjectManager, QuantitySurveyor |
| RecordSiteVisitNotes | PUT /api/site-visits/{id} | Director, ProjectManager, QuantitySurveyor, SiteManager |
| RecordBidDecision / MarkLeadAsWon / MarkLeadAsLost | POST /api/leads/{id}/… | Director, ProjectManager |
| AddBoqLine / UpdateBoqLine / RemoveBoqLine | POST·PUT·DELETE /api/…boq… | Director, ProjectManager, QuantitySurveyor |
| SignOffBoqForProject | POST /api/projects/{id}/boq/sign-off | Director |
| AddRate / ReviseRate | POST·PUT /api/rates… | Director, QuantitySurveyor |
| RegisterDrawing | POST /api/projects/{id}/drawings | Director, ProjectManager, QuantitySurveyor |
| IssueDrawingRevision | POST /api/drawings/{id}/revisions | Director, ProjectManager, Architect |
| UpdateDrawingMetadata | PUT /api/drawings/{id} | Director, ProjectManager |
| CreateBidPackage / UpdateBidPackageScope / UpdateWorkOrder | POST·PUT /api/…bid-packages… | Director, ProjectManager, OfficeComplianceCoordinator |
| SubmitQuoteForBidPackage / ReviseQuote | POST·PUT /api/…quotes | Director, ProjectManager, OfficeComplianceCoordinator, Subcontractor |
| AwardBidPackage | POST /api/bid-packages/{id}/award | Director, ProjectManager |
| AddSubcontractorToDirectory / UpdateSubcontractor | POST·PUT /api/subcontractors… | Director, OfficeComplianceCoordinator |
| UploadComplianceDocument | POST /api/subcontractors/{id}/compliance | Director, OfficeComplianceCoordinator, Subcontractor |
| LogHsRecord / UpdateHsRecord / RecordAttendanceForHsRecord | POST·PUT /api/hs-records… | Director, ProjectManager, SiteManager, HealthSafetyOfficer |
| UpdateMobilisationChecklistItem | PUT /api/mobilisation-items/{id} | Director, ProjectManager, SiteManager, HealthSafetyOfficer |
| RaiseChange | POST /api/projects/{id}/changes | Director, ProjectManager, SiteManager, Architect, Subcontractor |
| UpdateChangeDetails | PUT /api/changes/{id} | Director, ProjectManager, Architect |
| AddProgrammeTask / ApproveSiteReport | POST /api/…programme·site-reports | Director, ProjectManager |
| AssembleSiteReport / UpdateProgrammeTask | POST·PUT /api/…site… | Director, ProjectManager, SiteManager |
| DraftValuation / ReviseValuation | POST·PUT /api/…valuations | Director, ProjectManager, QuantitySurveyor |
| IssueValuation | POST /api/valuations/{id}/issue | Director |
| SubmitTimesheet | POST /api/timesheets | Director, ProjectManager, SiteManager, Subcontractor |
| ApproveTimesheet | POST /api/timesheets/{id}/approval | Director, ProjectManager |
| RecordQsAccrual / UpdateQsAccrual | POST·PUT /api/…qs-accruals | Director, QuantitySurveyor |
| GrantEot / UpdateEot | POST·PUT /api/…eots | Director |
| RaiseDefect | POST /api/projects/{id}/defects | Director, ProjectManager, SiteManager, Client, Architect |
| UpdateDefect | PUT /api/defects/{id} | Director, ProjectManager, SiteManager |
| AgreeSettlement / AgreeVatAnalysis / ReleaseRetention | POST /api/…settlement·vat·retention | Director, FinanceDirector |
| UpsertDirectoryUser / RemoveDirectoryUser | POST·DELETE /api/directory… | Admin |
| SubmitAccessRequest | POST /api/access-requests | the requester (own email) |
| ResolveAccessRequest | POST /api/access-requests/{email}/resolve | Admin |

## Queries (authenticate only, unless noted)

`GET` endpoints returning read models: projects, leads (+ qualification/site-visits/info-chase/
proposal/outcome), boq (+ sign-off), rates, drawings (+ revisions), bid-packages, quotes,
work-orders, subcontractors (+ compliance), hs-records (+ attendance), mobilisation, changes,
site-reports, programme, valuations, cost-code-budgets, timesheets, cvr-snapshots,
forecast-components, qs-accruals, prelim-items (+ entries), eots, defects, settlement, vat.

Admin-only queries: `GET /api/directory`, `GET /api/access-requests`.
Self-or-admin: `GET /api/directory/{email}`.

## Notes

- `[HttpTrigger(AuthorizationLevel.Anonymous)]` is transport-level only; the real gates are the
  named `*Authorisation` classes read in sequence at the top of each endpoint.
- Commands returning nothing meaningful return `Acknowledgement(EntityId)`.
- Gaps still to build (no endpoint yet) are tracked against the catalogue in `03-catalogue.md`
  — notably the commercial calculation commands (cashflow snapshots, CVR capture, cost-code
  budgets) which are Phase 3.
