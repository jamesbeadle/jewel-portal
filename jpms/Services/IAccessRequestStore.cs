
namespace Jewel.JPMS.Services;

public interface IAccessRequestStore
{
    IReadOnlyList<AccessRequest> Pending();

    /// <summary>False until the first fetch has landed — see <see cref="IUserDirectory.IsLoaded"/>.
    /// "No pending requests" and "not loaded yet" look identical without it.</summary>
    bool IsLoaded { get; }

    AccessRequest Submit(AuthenticatedUser user);

    bool Remove(string email);

    event Action? OnChange;
}
