using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class UpsertDirectoryUserHandler
    : ICommandHandler<UpsertDirectoryUser, DirectoryUser>
{
    private readonly JpmsContext context;

    public UpsertDirectoryUserHandler(JpmsContext context) { this.context = context; }

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

        await ReplaceRolesAsync(command, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
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
