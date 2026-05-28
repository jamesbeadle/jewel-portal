using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class SignOffBoqForProjectHandler
    : ICommandHandler<SignOffBoqForProject, BoqSignOff>
{
    private readonly JpmsContext context;

    public SignOffBoqForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<BoqSignOff> HandleAsync(SignOffBoqForProject command, CancellationToken cancellationToken)
    {
        var entity = new BoqSignOffEntity
        {
            BoqSignOffId = BoqIdentifierFactory.NextBoqSignOffId(),
            ProjectId = command.ProjectId,
            SignedOffByEmail = command.SignedOffByEmail,
            SignedOffAt = DateTimeOffset.UtcNow,
            TenderTotalAtSignOff = command.TenderTotalAtSignOff
        };
        context.BoqSignOffs.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
