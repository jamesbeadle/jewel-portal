using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Queue of pending access requests — users who signed in successfully but
/// weren't on the approved directory and have asked to be added.
/// In-memory today; backed by SQL once the JPMS API exists.
/// Emails are unique (one outstanding request per email).
/// </summary>
public interface IAccessRequestStore
{
    /// <summary>All outstanding requests, newest first.</summary>
    IReadOnlyList<AccessRequest> Pending();

    /// <summary>
    /// Add a new request (or refresh the timestamp on an existing one for the same email).
    /// Fires <see cref="OnChange"/>.
    /// </summary>
    AccessRequest Submit(AuthenticatedUser user);

    /// <summary>Remove a pending request. Fires <see cref="OnChange"/> if one existed.</summary>
    bool Remove(string email);

    /// <summary>Fires whenever the queue mutates.</summary>
    event Action? OnChange;
}
