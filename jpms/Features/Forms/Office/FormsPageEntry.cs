namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// The door every forms office screen opens through: the session known, an anonymous visitor sent
/// to sign in, and the screen's reads asked for only by someone its gate admits — so a role that
/// sees "you don't have access" is not also shown the API's refusal as an error.
/// </summary>
public sealed class FormsPageEntry
{
    private readonly SessionService session;
    private readonly AuthService auth;
    private readonly NavigationManager navigation;

    public FormsPageEntry(SessionService session, AuthService auth, NavigationManager navigation)
    {
        this.session = session;
        this.auth = auth;
        this.navigation = navigation;
    }

    public async Task<bool> MayReadAsync(RoleSet readers)
    {
        await session.EnsureLoadedAsync();
        if (!auth.IsSignedIn) navigation.NavigateTo("/login", forceLoad: true);
        return auth.IsSignedIn && session.CanOpen(readers);
    }

    public bool Admits(RoleSet readers) => session.CanOpen(readers);
}
