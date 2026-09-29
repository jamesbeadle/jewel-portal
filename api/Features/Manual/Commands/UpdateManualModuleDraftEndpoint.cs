
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class UpdateManualModuleDraftEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<UpdateManualModuleDraft, ManualModule> handler;

    public UpdateManualModuleDraftEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<UpdateManualModuleDraft, ManualModule> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateManualModuleDraft))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manual/modules/{manualModuleId}")] HttpRequest request, string manualModuleId) =>
        gate.RunAsync(request, (command, user) => command with { ManualModuleId = manualModuleId, UpdatedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
