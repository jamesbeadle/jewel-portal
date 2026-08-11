using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// Corrects the title, date or notes on a recorded amendment. Deliberately never touches the
/// document columns — a wrong file is fixed by removing the amendment and uploading again, so
/// re-wording a title can never detach the signed deed.
/// </summary>
public sealed class SetProjectContractAmendmentDetailsHandler
    : ICommandHandler<SetProjectContractAmendmentDetails, ProjectContractAmendment>
{
    private readonly JpmsContext context;

    public SetProjectContractAmendmentDetailsHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<ProjectContractAmendment> HandleAsync(
        SetProjectContractAmendmentDetails command, CancellationToken cancellationToken)
    {
        // Both ids, not just the key: a stale amendment id from another project must read as "not
        // found", never as a cross-project edit.
        var entity = await context.ProjectContractAmendments
            .FirstOrDefaultAsync(
                row => row.ProjectContractAmendmentId == command.ProjectContractAmendmentId
                    && row.ProjectId == command.ProjectId,
                cancellationToken);
        if (entity is null)
            throw new InvalidOperationException("That amendment no longer exists — it may have been removed.");

        entity.Title = command.Title.Trim();
        entity.AmendmentDate = command.AmendmentDate;
        entity.Notes = Trimmed(command.Notes);

        entity.UpdatedByEmail = command.UpdatedByEmail;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
