namespace Jewel.JPMS.Contracts.Ai;

/// <summary>The site manual: the office master, a module's page and the published role views. Data only.</summary>
public static class ManualPageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/manual", "Site manual",
            "The site manual as controlled modules (2026-09-29): one row per module in family order "
            + "(Governance, Role, Pre-start and mobilisation, Site setup, Routines, Reporting, Workflows, "
            + "Resources and procurement, Standards, Compliance, Reference) with its status — Draft, In "
            + "review, Approved, Superseded — working and published version, owner, approver, next review "
            + "date, the views it is published to and how many have acknowledged the published version. "
            + "The office (directors, PMs, compliance, office admin, the H&S lead) adds a module with "
            + "\"New module\" and, while the manual is empty, loads the JBB baseline — the May 2026 "
            + "working draft as 21 draft modules — with \"Load the JBB baseline\". Everyone on staff "
            + "reads the row and opens the module. A site view is reached from \"Read as…\". Over the "
            + "connector: list_manual_modules reads this list; create_manual_module and "
            + "import_manual_baseline are the two doors in, confirmed by the user first."),

        new("/manual/{manualModuleId}", "Manual module",
            "One module's own page: its facts (code, owner, approver, status, version, published version, "
            + "approved when and by whom, next review, audience, linked forms, linked standards, change "
            + "summary, source sections), the working text, and beneath it the text the site currently "
            + "sees when a draft is in progress, every approved version with who approved it and when it "
            + "was superseded, and every acknowledgement by version. A draft is edited in place "
            + "(\"Edit…\"); \"Send for review\" moves it to In review; an approver then \"Approve\"s it — "
            + "which publishes it to its views and supersedes the last — or \"Return to draft\" with a "
            + "reason. An approved module is \"Revise\"d to open the next version as a draft while the "
            + "approved text stays published, or \"Retire\"d. Over the connector the same verbs are "
            + "update_manual_module_draft, submit_manual_module_for_review, approve_manual_module, "
            + "return_manual_module_to_draft, revise_manual_module and retire_manual_module; "
            + "get_manual_module reads the page."),

        new("/manual/view/{view}", "Manual view",
            "A published view of the manual — SiteManager, HealthAndSafetyOfficer, Foreman or Office — "
            + "as its readers see it: only approved, unretired modules published to that view, the "
            + "approved text, its version and approval date, its linked forms as links, and per module "
            + "\"I have read this\" which records the reader's typed name against that version. A new "
            + "approved version asks again. \"Print or save as PDF\" is the versioned site-team issue. "
            + "Site managers and operatives land on the Site Manager view, the H&S lead on theirs, "
            + "foremen on the quick view. Over the connector get_manual_view reads a view and "
            + "acknowledge_manual_module records the acknowledgement, with the user's yes."),
    };
}
