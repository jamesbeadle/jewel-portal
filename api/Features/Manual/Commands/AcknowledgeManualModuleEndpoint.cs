
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class AcknowledgeManualModuleEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<AcknowledgeManualModule, ManualAcknowledgement> handler;

    public AcknowledgeManualModuleEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<AcknowledgeManualModule, ManualAcknowledgement> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AcknowledgeManualModule))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manual/modules/{manualModuleId}/acknowledge")] HttpRequest request, string manualModuleId) =>
        gate.RunAsync(request, (command, user) => command with { ManualModuleId = manualModuleId, AcknowledgedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
