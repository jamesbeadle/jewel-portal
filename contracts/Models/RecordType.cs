namespace Jewel.JPMS.Models;

// The kind of record in the system, used to key record-scoped behaviour
// (tags, links, AI tools) by type. Today every request-family record is a Request;
// Bid Package Invites are the first additional record type (built in a later phase).
public enum RecordType
{
    Request = 0,           // the RF* family (RFI/RFA/RFC/RFQ/RFP) plus NOD/EOT
    BidPackageInvite = 1,  // a bid package and the subcontractors invited to tender (Part B)
    CostCentre = 2,        // a valuation-report cost centre on a project (project + cost-centre grouping)
    Scheduling = 3,        // a project's scheduling bucket — one per project, feeds the Programme tab
    Todo = 4,              // a project to-do item — created at triage or on the project's Overview tab
    Lad = 5,               // a Liquidated Damages claim — a claims document on the Programme tab
    Variation = 6,         // a Variation Order — the approved change feeding the valuation report
    VariationQuote = 7,    // a Variation Order Quote (VOQ) — the pre-approval quote a VO is raised from
    WorkOrder = 8,         // a work order (purchase order) awarded to a subcontractor — subcontract-side
    Defect = 9,            // a defect logged on the project — remediation chased with the subcontractor
    SubcontractorComms = 10, // the record-less "subcontractor communication" tag family (general + categories) — subcontract-side correspondence tied to no record
    ValuationReportSnapshot = 11, // a frozen valuation report snapshot — the client-facing statement a valuation email travels with
    InternalComms = 12,    // the record-less "internal communication" tag family (general + categories) — staff-to-staff correspondence tied to no record
    TenderEnquiry = 13,    // an architect's invitation for Jewel to tender (PQQ → shortlist → tender) — client-side, on a Lead-stage project
    CalendarEvent = 14,    // a project calendar entry (site visit, delivery, meeting, attendance) — shown on the project's Calendar tab
    BuildingControlCase = 15,       // the project's case with a building control body — case-level correspondence (the notice, the acknowledgement, the contact)
    BuildingControlInspection = 16  // one building control inspection stage — the inspector's booking/report thread files against it
}
