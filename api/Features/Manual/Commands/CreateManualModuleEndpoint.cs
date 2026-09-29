
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class CreateManualModuleEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<CreateManualModule, ManualModule> handler;

    public CreateManualModuleEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<CreateManualModule, ManualModule> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(CreateManualModule))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manual/modules")] HttpRequest request) =>
        gate.RunAsync(request, (command, user) => command with { CreatedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
