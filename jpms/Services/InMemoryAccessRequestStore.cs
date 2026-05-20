using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// In-memory queue of access requests. Lasts for the lifetime of the browser
/// session. Replace with an API-backed implementation once the backend lands.
/// </summary>
public sealed class InMemoryAccessRequestStore : IAccessRequestStore
{
    private readonly List<AccessRequest> _requests = new();

    public event Action? OnChange;

    public IReadOnlyList<AccessRequest> Pending() =>
        _requests
            .OrderByDescending(r => r.RequestedAt)
            .ToList()
            .AsReadOnly();

    public AccessRequest Submit(AuthenticatedUser user)
    {
        var existing = _requests.FirstOrDefault(r =>
            string.Equals(r.Email, user.Email, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _requests.Remove(existing);
        }

        var request = new AccessRequest(
            Email: user.Email,
            DisplayName: user.DisplayName,
            Provider: user.Provider,
            RequestedAt: DateTimeOffset.UtcNow);

        _requests.Add(request);
        OnChange?.Invoke();
        return request;
    }

    public bool Remove(string email)
    {
        var existing = _requests.FirstOrDefault(r =>
            string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return false;
        _requests.Remove(existing);
        OnChange?.Invoke();
        return true;
    }
}
