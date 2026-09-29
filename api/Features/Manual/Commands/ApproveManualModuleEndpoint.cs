
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class ApproveManualModuleEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<ApproveManualModule, ManualModule> handler;

    public ApproveManualModuleEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<ApproveManualModule, ManualModule> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ApproveManualModule))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manual/modules/{manualModuleId}/approve")] HttpRequest request, string manualModuleId) =>
        gate.RunAsync(request, (command, user) => command with { ManualModuleId = manualModuleId, ApprovedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
