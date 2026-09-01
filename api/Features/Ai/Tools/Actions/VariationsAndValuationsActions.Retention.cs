using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class VariationsAndValuationsActions
{
    private static IEnumerable<AiAction> RetentionActions() => new AiAction[]
    {
        new AiAction(
            Name: "set_project_retention",
            Area: "Retention",
            Description: "Sets or updates a project's deposit and retention terms (upsert, one "
                + "record per project) — retention percent, completion release percent, defects "
                + "period, practical completion date and deposit percent. These are contract terms "
                + "that drive every future claim's deductions, so this is a real financial-facing "
                + "change.",
            CommandType: typeof(SetProjectRetention),
            ResultType: typeof(ProjectRetention),
            AuthorisationType: typeof(SetProjectRetentionAuthorisation),
            ValidationType: typeof(SetProjectRetentionValidation),
            VisibleTo: RetentionDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the terms with the user before calling. Percentages are whole numbers "
                + "(5 means 5%). projectId comes from list_projects."),

        new AiAction(
            Name: "confirm_retention_release",
            Area: "Retention",
            Description: "CONFIRMS that a retention release milestone actually happened — a real "
                + "financial record that client money moved (the schedule only ever forecasts). "
                + "The amount is frozen on the record and the confirmation timestamp is set "
                + "server-side.",
            CommandType: typeof(ConfirmRetentionRelease),
            ResultType: typeof(ProjectRetention),
            AuthorisationType: typeof(ConfirmRetentionReleaseAuthorisation),
            ValidationType: typeof(ConfirmRetentionReleaseValidation),
            VisibleTo: RetentionDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm milestone and amount with the user before calling. The project must "
                + "already have retention terms (set_project_retention)."),

        // ── LADs ──────────────────────────────────────────────────────────────────────────────

    };
}
