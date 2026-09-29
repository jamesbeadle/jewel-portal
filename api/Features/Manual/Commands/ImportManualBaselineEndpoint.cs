
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class ImportManualBaselineEndpoint
{
    private readonly ManualCommandGate gate;
    private readonly ManualAuthorisationGate authorisation;
    private readonly ManualValidationGate validation;
    private readonly ICommandHandler<ImportManualBaseline, ManualBaselineImport> handler;

    public ImportManualBaselineEndpoint(ManualCommandGate gate, ManualAuthorisationGate authorisation, ManualValidationGate validation, ICommandHandler<ImportManualBaseline, ManualBaselineImport> handler)
    { this.gate = gate; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ImportManualBaseline))]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manual/baseline")] HttpRequest request) =>
        gate.RunAsync(request, (command, user) => command with { ImportedByEmail = user.Email },
            authorisation.Allows, validation.Check, handler);
}
