
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class SubmitManualModuleForReviewEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<SubmitManualModuleForReview, ManualModule> handler;

    public SubmitManualModuleForReviewEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<SubmitManualModuleForReview, ManualModule> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(SubmitManualModuleForReview))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manual/modules/{manualModuleId}/submit")] HttpRequest request, string manualModuleId) =>
        gate.RunAsync(request, (command, user) => command with { ManualModuleId = manualModuleId, SubmittedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
