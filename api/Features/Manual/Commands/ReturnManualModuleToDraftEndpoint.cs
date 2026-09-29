
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class ReturnManualModuleToDraftEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<ReturnManualModuleToDraft, ManualModule> handler;

    public ReturnManualModuleToDraftEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<ReturnManualModuleToDraft, ManualModule> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ReturnManualModuleToDraft))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manual/modules/{manualModuleId}/return")] HttpRequest request, string manualModuleId) =>
        gate.RunAsync(request, (command, user) => command with { ManualModuleId = manualModuleId, ReturnedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
