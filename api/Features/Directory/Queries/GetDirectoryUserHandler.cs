using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

public sealed class GetDirectoryUserHandler
    : IQueryHandler<GetDirectoryUser, DirectoryUser?>
{
    private readonly JpmsContext context;

    public GetDirectoryUserHandler(JpmsContext context) { this.context = context; }

    public async Task<DirectoryUser?> HandleAsync(
        GetDirectoryUser query, CancellationToken cancellationToken)
    {
        var entity = await context.DirectoryUsers.AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == query.Email, cancellationToken);
        // A revoked user reads as absent: everywhere this query feeds (approval checks, people
        // pickers) "revoked" and "not in the directory" must mean the same thing.
        if (entity is null || entity.RevokedAt is not null) return null;

        var roles = await context.DirectoryUserRoles.AsNoTracking()
            .Where(row => row.DirectoryUserEmail == query.Email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);

        return entity.ToModel(roles);
    }
}
