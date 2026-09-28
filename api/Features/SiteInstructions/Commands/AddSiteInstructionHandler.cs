using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class AddSiteInstructionHandler : ICommandHandler<AddSiteInstruction, SiteInstruction>
{
    private readonly JpmsContext context;
    public AddSiteInstructionHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteInstruction> HandleAsync(AddSiteInstruction command, CancellationToken cancellationToken)
    {
        var nextNumber = await SiteInstructionNumbers.NextAsync(context, cancellationToken);

        var entity = new SiteInstructionEntity
        {
            SiteInstructionId = Guid.NewGuid().ToString("N"),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Title = command.Title,
            Instruction = command.Instruction,
            Location = command.Location,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.SiteInstructions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
