using Jewel.JPMS.Api.Features.Manual.Commands;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The site manual's writes as connector actions (2026-09-29). Mirrors the command endpoints under
/// Features/Manual: the same gate class carries an Allows and a Check per command, so each action
/// enforces exactly what its endpoint does. Reads are list_manual_modules, get_manual_module and
/// get_manual_view.
/// </summary>
internal sealed partial class ManualActions : IAiActionSource
{
    private const string Area = "Site manual";

    public IEnumerable<AiAction> Build() => AuthoringActions().Concat(LifecycleActions());

    private static IEnumerable<AiAction> AuthoringActions() => new[] { CreateModule(), UpdateDraft(), ImportBaseline() };

    private static AiAction CreateModule() =>
        new(
            Name: "create_manual_module",
            Area: Area,
            Description: "Creates a module of the site manual as a DRAFT — code (a family and a number, "
                + "like RES-07), title, purpose, markdown body, owner and approver emails, the role views "
                + "it is for (audience: isForSiteManagers, isForHealthAndSafetyOfficer, isForForemen), "
                + "linked portal form slugs, linked standards and the manual sections it came from. "
                + "Nothing reaches a site view until it is approved.",
            CommandType: typeof(CreateManualModule),
            ResultType: typeof(ManualModule),
            AuthorisationType: typeof(ManualAuthorisationGate),
            ValidationType: typeof(ManualValidationGate),
            VisibleTo: ManualRoles.AllowedToManage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Read list_manual_modules first so the code is new and the family fits (GOV, ROLE, "
                + "MOB, SET, RUN, REP, WFL, RES, STD, COM, REF). Form slugs are the portal's own, "
                + "as the Forms page lists them (list_form_folders).");

    private static AiAction UpdateDraft() =>
        new(
            Name: "update_manual_module_draft",
            Area: Area,
            Description: "Replaces a DRAFT module's title, purpose, body, owner, approver, audience, "
                + "linked forms, linked standards, change summary and next review date in one write. "
                + "Refused once the module is in review or approved — return or revise it first.",
            CommandType: typeof(UpdateManualModuleDraft),
            ResultType: typeof(ManualModule),
            AuthorisationType: typeof(ManualAuthorisationGate),
            ValidationType: typeof(ManualValidationGate),
            VisibleTo: ManualRoles.AllowedToManage,
            EmailStamps: new[] { "UpdatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Every field is replaced: get_manual_module first and carry forward what should "
                + "not change. Say in the change summary what changed and why.");

    private static AiAction ImportBaseline() =>
        new(
            Name: "import_manual_baseline",
            Area: Area,
            Description: "Loads the JBB Site Manager Manual v0.13 (May 2026, a working draft) as DRAFT "
                + "modules, one per module of the modular restructure (21 in all). A code already "
                + "present is skipped, so this never overwrites the office's own work.",
            CommandType: typeof(ImportManualBaseline),
            ResultType: typeof(ManualBaselineImport),
            AuthorisationType: typeof(ManualAuthorisationGate),
            ValidationType: typeof(ManualValidationGate),
            VisibleTo: ManualRoles.AllowedToManage,
            EmailStamps: new[] { "ImportedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "The loaded modules have no owner or approver: the office names them before "
                + "anything is sent for review.");
}
