using Jewel.JPMS.Api.Features.Rates.Commands;
using Jewel.JPMS.Contracts.Rates;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> RateActions() => new AiAction[]
    {
        new AiAction(
            Name: "add_rate",
            Area: "Rates",
            Description: "Adds a rate to the company rate library (trade, description, unit, £ "
                + "value, supplier) — the priced reference the commercial team estimates and "
                + "prices work from. Money-facing: a wrong value here feeds wrong pricing.",
            CommandType: typeof(AddRate),
            ResultType: typeof(Rate),
            AuthorisationType: typeof(AddRateAuthorisation),
            ValidationType: typeof(AddRateValidation),
            VisibleTo: RateEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "revise_rate",
            Area: "Rates",
            Description: "Revises an existing rate in the company rate library, replacing its "
                + "trade, description, unit, £ value and supplier in one write. Money-facing: the "
                + "revised value is what future pricing reads.",
            CommandType: typeof(ReviseRate),
            ResultType: typeof(Rate),
            AuthorisationType: typeof(ReviseRateAuthorisation),
            ValidationType: typeof(ReviseRateValidation),
            VisibleTo: RateEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "rateId identifies the existing rate. All fields are replaced — carry forward "
                + "the values that should not change."),
    };
}
