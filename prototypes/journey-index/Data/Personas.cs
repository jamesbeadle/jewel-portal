using Jewel.JourneyIndex.Models;

namespace Jewel.JourneyIndex.Data;

/// <summary>
/// In-memory persona catalog. Source of truth is <c>docs/requirements/personas.md</c>.
/// Update both when a persona changes.
/// </summary>
public static class Personas
{
    public static readonly IReadOnlyList<Persona> All = new Persona[]
    {
        new(
            Slug: "architect",
            Code: "P01",
            Name: "Architect",
            ShortDescription: "External client who sends tenders with drawings and specs; defines cost codes carried through the system.",
            Role: "External client architect commissioning work from Jewel Bespoke Build",
            ReportsTo: "Their own firm / their own client",
            ToolingToday: "Architecture / CAD software, email, drawing packages, client portals",
            Frequency: "Periodic — at tender submission and during VO discussions",
            Goals: new[]
            {
                "Get work delivered on time and to specification.",
                "Track their cost codes accurately through to billing.",
                "Be informed of Variation Orders before they affect timeline or cost."
            },
            PainPoints: new[]
            {
                "No shared system; documents passed via email or portal handoffs.",
                "Visibility into on-site progress is opaque."
            },
            KeyResponsibilities: new[]
            {
                "Submit tender documentation (drawings, specs).",
                "Define and maintain client-facing cost codes.",
                "Approve or reject Variation Orders.",
                "Receive cash-call documentation."
            },
            DevicesAndEnvironment: "Desktop primarily. May use tablet on site visits.",
            Note: "The architect's client-facing cost codes must propagate through every screen and report that touches their tender — a cross-cutting requirement.",
            AccentDotClass: "bg-sky-500",
            Journeys: Array.Empty<JourneyStub>()
        ),

        new(
            Slug: "qs",
            Code: "P02",
            Name: "Quantity Surveyor",
            ShortDescription: "Prices tenders into line items, captures site measurements, updates line items on VOs.",
            Role: "Pricing and measurement specialist",
            ReportsTo: "Managing Director (to confirm)",
            ToolingToday: "Spreadsheets, measurement tools, drawing review software",
            Frequency: "Daily during tender phase; periodic during projects when VOs are raised",
            Goals: new[]
            {
                "Produce accurate line-by-line pricing for tenders.",
                "Capture site measurements quickly and accurately.",
                "Keep line items in sync as scope changes via VOs."
            },
            PainPoints: new[]
            {
                "Line items live in spreadsheets disconnected from completion tracking and billing.",
                "Manual reconciliation between tender pricing and actual work delivered."
            },
            KeyResponsibilities: new[]
            {
                "Review tender requirements and drawings.",
                "Generate line-item breakdowns.",
                "Conduct site visits for measurements.",
                "Update line items when VOs are raised."
            },
            DevicesAndEnvironment: "Laptop in office, tablet on site.",
            Note: "Whether the QS is internal Jewel staff or an external consultant needs confirming.",
            AccentDotClass: "bg-emerald-500",
            Journeys: Array.Empty<JourneyStub>()
        ),

        new(
            Slug: "subcontractor",
            Code: "P03",
            Name: "Subcontractor",
            ShortDescription: "Works on site. Updates line-item completion, submits timesheets, raises RFIs, actions VOs.",
            Role: "Field worker delivering work against tender line items",
            ReportsTo: "Site manager or QS (to confirm)",
            ToolingToday: "Phone, paper, ad-hoc messaging",
            Frequency: "Daily — on site",
            Goals: new[]
            {
                "Quickly log progress and time on site.",
                "Get RFIs answered fast so work isn't blocked.",
                "Get paid on time and accurately."
            },
            PainPoints: new[]
            {
                "Paper timesheets get lost or delayed.",
                "RFI handling is slow and untracked.",
                "No personal view of their progress against the tender."
            },
            KeyResponsibilities: new[]
            {
                "Update completion status on tender line items.",
                "Submit accurate timesheets.",
                "Raise RFIs when scope is unclear.",
                "Action VOs once approved."
            },
            DevicesAndEnvironment: "Mobile phone primarily. Touch-friendly UI is essential, ideally offline-tolerant.",
            Note: "Subcontractors are external. Onboarding should be lightweight — probably an invite link rather than a full account-creation flow.",
            AccentDotClass: "bg-amber-500",
            Journeys: Array.Empty<JourneyStub>()
        ),

        new(
            Slug: "accountant",
            Code: "P04",
            Name: "Accountant",
            ShortDescription: "Produces cashflow forecast, issues cash calls, allocates incoming cash. Drives the platform's primary pain point.",
            Role: "On-site accountant — owns financial visibility for the business",
            ReportsTo: "Managing Director",
            ToolingToday: "Excel; an accounting package (to confirm which)",
            Frequency: "Daily / weekly",
            Goals: new[]
            {
                "Produce an accurate cashflow forecast despite fast-moving project data.",
                "Ring-fence incoming cash to the correct project.",
                "Time cash calls to actual % completion so projects stay funded."
            },
            PainPoints: new[]
            {
                "Forecast accuracy depends on real completion %, which is currently unreliable.",
                "Cash sometimes arrives without being clearly allocated to a job → mis-funded projects.",
                "Cash calls misaligned with completion → projects get under-funded and interrupted."
            },
            KeyResponsibilities: new[]
            {
                "Track project completion %.",
                "Issue cash calls to clients.",
                "Allocate incoming cash to the correct project.",
                "Produce cashflow forecast for the MD."
            },
            DevicesAndEnvironment: "Desktop primary. Tablet for ad-hoc.",
            Note: "Litmus test for every scoping decision: does this help the Accountant produce an accurate forecast?",
            AccentDotClass: "bg-violet-500",
            Journeys: Array.Empty<JourneyStub>()
        ),

        new(
            Slug: "md",
            Code: "P05",
            Name: "Managing Director",
            ShortDescription: "Executive decisions across all projects. Consumes forecast and project status.",
            Role: "Business owner — executive decisions across all projects",
            ReportsTo: "—",
            ToolingToday: "Email, Teams, reports from the Accountant",
            Frequency: "Daily",
            Goals: new[]
            {
                "At-a-glance view of business health.",
                "Confidence in the cashflow forecast.",
                "Identify at-risk projects early."
            },
            PainPoints: new[]
            {
                "Decisions rely on lagging or inaccurate cashflow data.",
                "Pipeline and active-project visibility is fragmented across emails and spreadsheets."
            },
            KeyResponsibilities: new[]
            {
                "Approve major commercial decisions.",
                "Set business strategy.",
                "Sign off cashflow forecasts."
            },
            DevicesAndEnvironment: "Mobile + desktop.",
            Note: null,
            AccentDotClass: "bg-rose-500",
            Journeys: Array.Empty<JourneyStub>()
        ),
    };

    public static Persona? GetBySlug(string slug) =>
        All.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
