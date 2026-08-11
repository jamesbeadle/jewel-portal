using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

// Deletes the note outright. Notes have no reference, no mail tags and no links to other records,
// so there is nothing to sweep — the row simply goes.
public sealed class DeleteUsefulInformationNoteHandler : ICommandHandler<DeleteUsefulInformationNote, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteUsefulInformationNoteHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteUsefulInformationNote command, CancellationToken cancellationToken)
    {
        var entity = await context.UsefulInformationNotes.FindAsync(new object[] { command.UsefulInformationNoteId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Useful Information note {command.UsefulInformationNoteId} not found.");
        context.UsefulInformationNotes.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.UsefulInformationNoteId);
    }
}
