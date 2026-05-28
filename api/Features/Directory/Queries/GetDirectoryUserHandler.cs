using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

public sealed class GetDirectoryUserHandler
    : IQueryHandler<GetDirectoryUser, DirectoryUser?>
{
    private readonly JpmsContext context;

    public GetDirectoryUserHandler(JpmsContext context) { this.context = context; }

    public async Task<DirectoryUser?> HandleAsync(
        GetDirectoryUser query, CancellationToken cancellationToken)
    {
        var entity = await context.DirectoryUsers
            .FirstOrDefaultAsync(user => user.Email == query.Email, cancellationToken);
        if (entity is null) return null;

        var roles = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == query.Email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);

        return entity.ToModel(roles);
    }
}
