using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Boq;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class SignOffBoqForProjectValidation
{
    private readonly JpmsContext context;

    public SignOffBoqForProjectValidation(JpmsContext context) { this.context = context; }

    public async Task<ValidationOutcome> CheckAsync(SignOffBoqForProject command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.SignedOffByEmail)) errors.Add("Signing-off email is required.");
        if (command.TenderTotalAtSignOff <= 0) errors.Add("Tender total must be positive at sign-off.");

        var alreadySignedOff = await context.BoqSignOffs.AnyAsync(s => s.ProjectId == command.ProjectId, cancellationToken);
        if (alreadySignedOff) errors.Add("This project has already been signed off.");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
