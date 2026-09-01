using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class UpsertDirectoryUserHandler
    : ICommandHandler<UpsertDirectoryUser, DirectoryUser>
{
    private readonly JpmsContext context;
    private readonly SignedInUserCache userCache;

    public UpsertDirectoryUserHandler(JpmsContext context, SignedInUserCache userCache)
    {
        this.context = context;
        this.userCache = userCache;
    }

    public async Task<DirectoryUser> HandleAsync(UpsertDirectoryUser command, CancellationToken cancellationToken)
    {
        var entity = await context.DirectoryUsers
            .FirstOrDefaultAsync(user => user.Email == command.Email, cancellationToken);
        if (entity is null)
        {
            entity = new DirectoryUserEntity { Email = command.Email };
            context.DirectoryUsers.Add(entity);
        }
        entity.DisplayName = command.DisplayName;
        entity.RevertToOwnRole = command.RevertToOwnRole;

        await ReplaceRolesAsync(command, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // The to-do fall-back rule: a to-do pinned to this person is only pinned WITH a role they
        // hold, so losing a role clears the pin on that role's items and they fall back to the
        // role — whoever holds it now sees them, instead of the items following an assignment the
        // person no longer has.
        var keptRoleValues = command.Roles.Select(role => (int)role).ToList();
        await context.TodoItems
            .Where(t => t.AssigneePersonEmail != null
                && t.AssigneePersonEmail.ToLower() == command.Email.ToLower()
                && (t.AssigneeRole == null || !keptRoleValues.Contains(t.AssigneeRole.Value)))
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.AssigneePersonEmail, (string?)null),
                cancellationToken);

        // Their permissions just changed — drop any cached copy so the next request re-reads them
        // rather than waiting out the cache TTL.
        userCache.InvalidateEmail(command.Email);
        return entity.ToModel(command.Roles);
    }

    private async Task ReplaceRolesAsync(UpsertDirectoryUser command, CancellationToken cancellationToken)
    {
        var existingRoles = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == command.Email)
            .ToListAsync(cancellationToken);
        context.DirectoryUserRoles.RemoveRange(existingRoles);
        context.DirectoryUserRoles.AddRange(command.Roles.Select(role => new DirectoryUserRoleEntity
        {
            DirectoryUserRoleId = DirectoryIdentifierFactory.NextRoleId(),
            DirectoryUserEmail = command.Email,
            Role = (int)role
        }));
    }
}
