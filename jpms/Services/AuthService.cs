using System.Net;
using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Auth;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Talks to the local (database-driven) auth API: /api/auth/me, /api/auth/login and
/// /api/auth/logout. The session lives in an HTTP-only cookie set by the API, so the
/// browser carries it automatically on every same-origin request.
/// </summary>
public sealed class AuthService
{
    private const string MeEndpoint = "/api/auth/me";
    private const string LoginEndpoint = "/api/auth/login";
    private const string LogoutEndpoint = "/api/auth/logout";
    private const string SetPasswordEndpoint = "/api/auth/set-password";
    private const string ValidateInviteEndpoint = "/api/auth/invite/";

    private readonly HttpClient httpClient;
    private bool isInitialised;

    public AuthService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public AuthenticatedUser? CurrentUser { get; private set; }

    public IReadOnlyList<Role> CurrentRoles { get; private set; } = Array.Empty<Role>();

    /// <summary>Set only for portal-scoped subcontractor contacts (resolved server-side).</summary>
    public string? CurrentSubcontractorId { get; private set; }

    public bool IsSignedIn => CurrentUser is not null;

    public event Action? OnChange;

    public async Task EnsureInitialisedAsync()
    {
        if (isInitialised) return;
        await LoadCurrentUserAsync();
        isInitialised = true;
    }

    public async Task RefreshAsync()
    {
        await LoadCurrentUserAsync();
        OnChange?.Invoke();
    }

    /// <summary>Signs in with email + password. Returns null on success, or a message to show the user.</summary>
    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(LoginEndpoint, new LoginRequest(email, password));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return "That email and password didn't match. Please try again.";
            if (!response.IsSuccessStatusCode)
                return "Something went wrong signing you in. Please try again.";

            var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
            Adopt(user);
            isInitialised = true;
            OnChange?.Invoke();
            return null;
        }
        catch
        {
            return "Couldn't reach the server. Check your connection and try again.";
        }
    }

    /// <summary>Checks an invite/reset token so the set-password page can greet the user. Never throws.</summary>
    public async Task<InviteValidation> ValidateInviteAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new InviteValidation(false, null, null);
        try
        {
            var result = await httpClient.GetFromJsonAsync<InviteValidation>(ValidateInviteEndpoint + Uri.EscapeDataString(token));
            return result ?? new InviteValidation(false, null, null);
        }
        catch
        {
            return new InviteValidation(false, null, null);
        }
    }

    /// <summary>Completes an invite/reset by setting a password. Returns null on success (and signs the user in), else a message.</summary>
    public async Task<string?> SetPasswordAsync(string token, string password)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(SetPasswordEndpoint, new SetPasswordRequest(token, password));
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
                Adopt(user);
                isInitialised = true;
                OnChange?.Invoke();
                return null;
            }

            var problem = await TryReadErrorAsync(response);
            return problem ?? "Couldn't set your password. Please try again.";
        }
        catch
        {
            return "Couldn't reach the server. Check your connection and try again.";
        }
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return string.IsNullOrWhiteSpace(problem?.Error) ? null : problem!.Error;
        }
        catch
        {
            return null;
        }
    }

    private sealed record ErrorResponse(string? Error);

    public async Task LogoutAsync()
    {
        try { await httpClient.PostAsync(LogoutEndpoint, content: null); }
        catch { /* clear the local state regardless */ }
        Adopt(null);
        isInitialised = true;
        OnChange?.Invoke();
    }

    private async Task LoadCurrentUserAsync()
    {
        Adopt(await TryFetchMeAsync());
    }

    private async Task<AuthenticatedUserResponse?> TryFetchMeAsync()
    {
        try
        {
            var response = await httpClient.GetAsync(MeEndpoint);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        }
        catch
        {
            return null;
        }
    }

    private void Adopt(AuthenticatedUserResponse? response)
    {
        if (response is null)
        {
            CurrentUser = null;
            CurrentRoles = Array.Empty<Role>();
            CurrentSubcontractorId = null;
            return;
        }
        CurrentUser = new AuthenticatedUser(response.Email, response.DisplayName);
        CurrentRoles = response.Roles;
        CurrentSubcontractorId = response.SubcontractorId;
    }
}
