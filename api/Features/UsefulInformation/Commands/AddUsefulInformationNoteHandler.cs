using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.UsefulInformation;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class AddUsefulInformationNoteHandler : ICommandHandler<AddUsefulInformationNote, UsefulInformationNote>
{
    private readonly JpmsContext context;
    public AddUsefulInformationNoteHandler(JpmsContext context) { this.context = context; }

    public async Task<UsefulInformationNote> HandleAsync(AddUsefulInformationNote command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var entity = new UsefulInformationNoteEntity
        {
            UsefulInformationNoteId = UsefulInformationIdentifierFactory.Next(),
            ProjectId = command.ProjectId,
            Title = Clamp(command.Title.Trim(), 256),
            Body = Clamp(command.Body.Trim(), 4000),
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.UsefulInformationNotes.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
