using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// In-memory implementation of <see cref="IUserDirectory"/>. Seeded with the
/// initial set of approved users; admins can mutate the list at runtime (in the
/// current browser session). Replace with an API-backed implementation once the
/// JPMS backend exists.
/// Emails are matched case-insensitively.
/// </summary>
public sealed class AllowListUserDirectory : IUserDirectory
{
    private readonly List<DirectoryUser> _users = new()
    {
        // First admin — the developer / system owner.
        new DirectoryUser("jamesbeadle1989@gmail.com",  "James Beadle",   Role.Admin),

        // Internal Jewel staff seeded with their personas' roles.
        new DirectoryUser("nigel.reilly@jewelgroup.co.uk", "Nigel Reilly",     Role.ManagingDirector),
        new DirectoryUser("admin@jewelgroup.co.uk",        "Jewel Admin",      Role.Admin),
        new DirectoryUser("accountant@jewelgroup.co.uk",   "Jewel Accountant", Role.Accountant),
        new DirectoryUser("qs@jewelgroup.co.uk",           "Jewel QS",         Role.QuantitySurveyor),
    };

    public event Action? OnChange;

    public DirectoryUser? Find(string email) =>
        string.IsNullOrWhiteSpace(email)
            ? null
            : _users.FirstOrDefault(u =>
                string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<DirectoryUser> All() => _users.AsReadOnly();

    public DirectoryUser Upsert(DirectoryUser user)
    {
        var existing = Find(user.Email);
        if (existing is not null)
        {
            _users.Remove(existing);
        }
        _users.Add(user);
        OnChange?.Invoke();
        return user;
    }

    public bool Remove(string email)
    {
        var existing = Find(email);
        if (existing is null) return false;
        _users.Remove(existing);
        OnChange?.Invoke();
        return true;
    }
}
