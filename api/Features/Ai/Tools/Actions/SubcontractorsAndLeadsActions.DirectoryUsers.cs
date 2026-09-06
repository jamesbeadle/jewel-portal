using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Api.Features.Clients.Commands;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SubcontractorsAndLeadsActions
{
    private static IEnumerable<AiAction> DirectoryUsersActions() => new AiAction[]
    {
        new AiAction(
            Name: "upsert_directory_user",
            Area: "Directory & users",
            Description: "Creates or updates a portal user account and REPLACES their role list with "
                + "exactly the roles supplied — this is how portal permissions are granted and taken "
                + "away. Roles omitted from the list are removed. Creating a user does not send an "
                + "invitation email.",
            CommandType: typeof(UpsertDirectoryUser),
            ResultType: typeof(DirectoryUser),
            AuthorisationType: typeof(UpsertDirectoryUserAuthorisation),
            ValidationType: typeof(UpsertDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm the exact role list with the user before calling — read the current user "
                + "first and carry forward roles that should not change. The Admin role carries every "
                + "permission."),

        new AiAction(
            Name: "remove_directory_user",
            Area: "Directory & users",
            Description: "REVOKES a user's portal access immediately — they can no longer sign in and "
                + "disappear from every active-user list. A soft removal: the record and roles survive "
                + "and restore_directory_user can reinstate them. Revoking the last active administrator "
                + "is refused.",
            CommandType: typeof(RemoveDirectoryUser),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveDirectoryUserAuthorisation),
            ValidationType: typeof(RemoveDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: new[] { "RevokedBy" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which account, by email, before calling."),

        new AiAction(
            Name: "restore_directory_user",
            Area: "Directory & users",
            Description: "Reinstates a revoked user's portal access: their directory record, the roles "
                + "they held at revocation, and their ability to sign in (with their existing password, "
                + "or a fresh invite if they never set one).",
            CommandType: typeof(RestoreDirectoryUser),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RestoreDirectoryUserAuthorisation),
            ValidationType: typeof(RestoreDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The email comes from the revoked-users list (ListRevokedDirectoryUsers). Restoring "
                + "gives the account back every role it held — confirm with the user before calling."),

        new AiAction(
            Name: "delete_directory_user",
            Area: "Directory & users",
            Description: "PERMANENTLY DELETES a revoked user's record — directory row, roles, credential, "
                + "outstanding links and sessions. Only available once the user has been revoked "
                + "(remove_directory_user first). There is no undo.",
            CommandType: typeof(DeleteDirectoryUser),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDirectoryUserAuthorisation),
            ValidationType: typeof(DeleteDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user which account, by email, before calling — the "
                + "email comes from the revoked-users list (ListRevokedDirectoryUsers).")
    };
}
