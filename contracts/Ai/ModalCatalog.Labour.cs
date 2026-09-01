using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;


public static partial class ModalCatalog
{
    /// <summary>
    /// The manual timesheet entry dialog on a project's Labour tab — the chat's way into "put
    /// Danny down for 8 hours on the Chiltern job yesterday, second fix"
    /// (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §4b). Filling it writes nothing:
    /// the user reads worker, date, hours and cost code resolved on their own screen and presses
    /// Add day themselves, which creates an ordinary Submitted timesheet — same validation, same
    /// approval, same budget hard-block as any other entry.
    /// </summary>
    public static readonly ModalDescriptor ManualTimesheet = new(
        "manual_timesheet",
        "Add a day",
        "It enters one worker's day on this project: who, the date, the hours and the cost code. "
        + "Use it for missed sign-outs and verbal reports. The entry lands as a Submitted "
        + "timesheet for normal approval — never approved by this dialog. Ask rather than assume "
        + "when the worker, date or cost code is unclear; a wrong cost code miscosts real labour.",
        "/projects/{project}/labour",
        // Exactly LabourRoleSets.ApproveTimesheets — whoever the API will accept AddWorkerTimesheet
        // from, and nobody else.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("workerName", "string",
                "The worker's name exactly as the Workers registry spells it. If more than one "
                + "worker could match what the user said, ask — never guess between two names.",
                Required: true),
            new("date", "string",
                "The worked date as yyyy-MM-dd. Resolve relative dates (\"yesterday\", \"Monday\") "
                + "against today and say the resolved date back in the chat.",
                Required: true),
            new("hours", "number",
                "Hours worked, in half-hour steps of at least 0.5. A full day is 8.",
                Required: true),
            new("costCode", "string",
                "A cost code from this project's list, spelled exactly. If none clearly fits, "
                + "leave it out — the user picks from the dropdown.")
        });

    /// <summary>
    /// The Record absence dialog on the Labour overview — "Frank's on holiday Thursday and
    /// Friday" from the chat. One date per confirm; the assistant stages consecutive days one
    /// after another. Absence explains a missing day (it leaves the chase list) and reduces the
    /// month's projected labour spend at the day rate.
    /// </summary>
    public static readonly ModalDescriptor RecordAbsence = new(
        "record_absence",
        "Record absence",
        "It records one worker's absence on one date: holiday, half day, not worked, or sick. "
        + "The user confirms each day; for a run of days, stage them one at a time.",
        "/labour/overview",
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("workerName", "string",
                "The worker's name exactly as the Workers registry spells it.",
                Required: true),
            new("date", "string",
                "The absent date as yyyy-MM-dd. Resolve relative dates against today and say the "
                + "resolved date back in the chat.",
                Required: true),
            new("kind", "string",
                "One of: holiday, half-day, not-worked, sick. Defaults to holiday.",
                Required: true),
            new("note", "string",
                "A short optional note — only what the user actually said.")
        });

    /// <summary>
    /// The "Enter a worker's week" dialog on the Labour overview — the accountant's transcription
    /// path for how the crews actually report: a WhatsApp message naming a site per day. One
    /// worker, one week, all seven days in ONE update (the one-dialog-one-update rule from
    /// bid_package_details: never a flow that relies on the model acting again after a save).
    /// Days land as Submitted timesheets on each site's approval queue; the MD codes the cost
    /// code and approves on the project's Labour tab. Days already recorded show locked in the
    /// dialog and are skipped on save — never overwritten.
    /// </summary>
    public static readonly ModalDescriptor WorkerWeek = new(
        "worker_week",
        "Enter a worker's week",
        "It enters ONE worker's whole week — a site (and hours) per day, transcribed from what "
        + "the user has: a WhatsApp attendance message, the conversation, an attached list. Send "
        + "the whole week in ONE update. Each day lands as a Submitted timesheet on its site for "
        + "normal approval — the MD codes and approves it on the project's Labour tab, so leave "
        + "cost codes out unless one clearly applies. Days shown as already recorded are locked; "
        + "leave them alone. For several workers, do one worker per fill: after the user presses "
        + "Save, open this dialog again for the next and keep count out loud.",
        "/labour/overview",
        // Exactly LabourRoleSets.ApproveTimesheets — whoever the API will accept SubmitWorkerWeek
        // from, and nobody else.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("workerName", "string",
                "The worker's name exactly as the Workers registry spells it. If more than one "
                + "worker could match what the user said, ask — never guess between two names.",
                Required: true),
            new("weekStart", "string",
                "The MONDAY of the week as yyyy-MM-dd. Resolve what the user gave: \"wk ending "
                + "16/08\" is the Sunday, so the Monday is the 10th; \"last week\" resolves "
                + "against today. Say the resolved w/c date back in the chat.",
                Required: true),
            new("days", "array",
                "One item per day the worker worked — weekends included when the message names "
                + "them. Leave out days with nothing reported; days the dialog shows as already "
                + "recorded stay out too. Send the whole week in one update.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("date", "string", "The day as yyyy-MM-dd, inside the stated week.", Required: true),
                    new("siteName", "string",
                        "The site as the user said it (\"Guildford\", \"by france\"). The page "
                        + "matches it against the live project list and shows what it could not "
                        + "match, so pass the name through rather than guessing an id — the user "
                        + "picks unmatched sites from the list themselves.",
                        Required: true),
                    new("hours", "number",
                        "Hours worked, in half-hour steps. Leave out for a normal full day — the "
                        + "form defaults to 8."),
                    new("costCode", "string",
                        "A cost code, spelled exactly as list_cost_codes returns it — but ONLY "
                        + "when the user's data actually names the work. Normally leave it out: "
                        + "the MD codes the day when he approves it.")
                })
        });

}
