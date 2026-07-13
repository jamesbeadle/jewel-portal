using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// Resolves the signed-in user to their Worker record by email — the single link between a
/// portal account (SiteOperative role) and the labour registry. Case-insensitive; inactive
/// workers can't log time.
/// </summary>
internal static class WorkerByEmail
{
    public static async Task<WorkerEntity> ResolveAsync(JpmsContext context, string email, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FirstOrDefaultAsync(
            candidate => candidate.ContactEmail == email, cancellationToken);
        if (worker is null)
            throw new InvalidOperationException("Your account isn't linked to a worker record yet — ask your Project Manager to add your email on the Workers page.");
        if (!worker.IsActive)
            throw new InvalidOperationException("Your worker record is inactive — ask your Project Manager.");
        return worker;
    }
}
