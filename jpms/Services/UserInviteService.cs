using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Auth;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>Admin-only: creates/updates a user and mints a single-use "set your password" link.</summary>
public sealed class UserInviteService
{
    private const string InviteEndpoint = "/api/auth/invite";

    private readonly HttpClient httpClient;

    public UserInviteService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public sealed record InviteOutcome(bool Success, InviteResult? Result, string? Error);

    public async Task<InviteOutcome> InviteAsync(string email, string displayName, IReadOnlyList<Role> roles)
    {
        try
        {
            var request = new InviteUserRequest(email, displayName, roles);
            var response = await httpClient.PostAsJsonAsync(InviteEndpoint, request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<InviteResult>();
                return result is null
                    ? new InviteOutcome(false, null, "The server returned an unexpected response.")
                    : new InviteOutcome(true, result, null);
            }

            var error = await TryReadErrorAsync(response);
            return new InviteOutcome(false, null, error ?? "Couldn't create the invite. Please try again.");
        }
        catch
        {
            return new InviteOutcome(false, null, "Couldn't reach the server. Check your connection and try again.");
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
}
