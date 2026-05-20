using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Looks up whether a signed-in email belongs to a user the company has approved
/// for the platform, and lets admins manage that directory.
/// The shape of this interface is deliberately backend-agnostic so the
/// implementation can swap from an in-memory seed to a real API call without
/// changing any UI code.
/// </summary>
public interface IUserDirectory
{
    /// <summary>
    /// Returns the directory record for an email, or null if the email is not
    /// on the approved list.
    /// </summary>
    DirectoryUser? Find(string email);

    /// <summary>True if the email is on the approved list.</summary>
    bool IsApproved(string email) => Find(email) is not null;

    /// <summary>All approved users. Used by the admin users panel.</summary>
    IReadOnlyList<DirectoryUser> All();

    /// <summary>
    /// Add (or replace) an approved user. Fires <see cref="OnChange"/> on success.
    /// Returns the stored record.
    /// </summary>
    DirectoryUser Upsert(DirectoryUser user);

    /// <summary>Remove an approved user. Fires <see cref="OnChange"/> if a record was removed.</summary>
    bool Remove(string email);

    /// <summary>Fires whenever the directory mutates.</summary>
    event Action? OnChange;
}
