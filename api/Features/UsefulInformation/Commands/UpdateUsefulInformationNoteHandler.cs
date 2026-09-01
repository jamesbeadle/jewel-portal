using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class UpdateUsefulInformationNoteHandler : ICommandHandler<UpdateUsefulInformationNote, UsefulInformationNote>
{
    private readonly JpmsContext context;
    public UpdateUsefulInformationNoteHandler(JpmsContext context) { this.context = context; }

    public async Task<UsefulInformationNote> HandleAsync(UpdateUsefulInformationNote command, CancellationToken cancellationToken)
    {
        var entity = await context.UsefulInformationNotes.FindAsync(new object[] { command.UsefulInformationNoteId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Useful Information note {command.UsefulInformationNoteId} not found.");

        entity.Title = Clamp(command.Title.Trim(), 256);
        entity.Body = Clamp(command.Body.Trim(), 4000);
        entity.UpdatedByEmail = command.UpdatedByEmail;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
