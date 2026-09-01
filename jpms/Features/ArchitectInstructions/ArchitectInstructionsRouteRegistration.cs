using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Features.ArchitectInstructions;

// Client routes for the Architect's Instruction register. Mirrors ArchitectInstructionEndpoints.
// Filing an instruction is multipart (the document rides with the form) and is posted directly by
// HttpArchitectInstructionStore, so RecordArchitectInstruction is deliberately not registered here.
public static class ArchitectInstructionsRouteRegistration
{
    public static void RegisterArchitectInstructionsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListArchitectInstructionsForProject, IReadOnlyList<ArchitectInstruction>>(
            new QueryRoute("/api/projects/{projectId}/architect-instructions",
                query => $"/api/projects/{((ListArchitectInstructionsForProject)query).ProjectId}/architect-instructions"));

        queries.Register<GetArchitectInstructionById, ArchitectInstruction?>(
            new QueryRoute("/api/architect-instructions/{instructionId}",
                query => $"/api/architect-instructions/{((GetArchitectInstructionById)query).ArchitectInstructionId}"));

        commands.Register<ImportArchitectInstructionFromMessage, ArchitectInstruction>(
            new CommandRoute("POST", "/api/projects/{projectId}/architect-instructions/import-from-message",
                command => $"/api/projects/{((ImportArchitectInstructionFromMessage)command).ProjectId}/architect-instructions/import-from-message"));

        commands.Register<UpdateArchitectInstruction, ArchitectInstruction>(
            new CommandRoute("PUT", "/api/architect-instructions/{instructionId}",
                command => $"/api/architect-instructions/{((UpdateArchitectInstruction)command).ArchitectInstructionId}"));

        commands.Register<LinkArchitectInstructionToVariation, ArchitectInstruction>(
            new CommandRoute("POST", "/api/architect-instructions/{instructionId}/variations/{variationOrderId}",
                command =>
                {
                    var link = (LinkArchitectInstructionToVariation)command;
                    return $"/api/architect-instructions/{link.ArchitectInstructionId}/variations/{link.VariationOrderId}";
                }));

        commands.Register<UnlinkArchitectInstructionFromVariation, ArchitectInstruction>(
            new CommandRoute("DELETE", "/api/architect-instructions/{instructionId}/variations/{variationOrderId}",
                command =>
                {
                    var unlink = (UnlinkArchitectInstructionFromVariation)command;
                    return $"/api/architect-instructions/{unlink.ArchitectInstructionId}/variations/{unlink.VariationOrderId}";
                }));

        commands.Register<DeleteArchitectInstruction, Acknowledgement>(
            new CommandRoute("DELETE", "/api/architect-instructions/{instructionId}",
                command => $"/api/architect-instructions/{((DeleteArchitectInstruction)command).ArchitectInstructionId}"));
    }
}
