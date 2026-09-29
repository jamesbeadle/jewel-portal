
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class ReviseManualModuleEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<ReviseManualModule, ManualModule> handler;

    public ReviseManualModuleEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<ReviseManualModule, ManualModule> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ReviseManualModule))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manual/modules/{manualModuleId}/revise")] HttpRequest request, string manualModuleId) =>
        gate.RunAsync(request, (command, user) => command with { ManualModuleId = manualModuleId, RevisedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
