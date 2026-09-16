namespace Jewel.JPMS.Services;

/// <summary>
/// The three answers to "is this visitor signed in?" — and the one that used to be missing.
/// Until /api/auth/me has answered nobody knows, and a screen drawn for "signed out" during
/// that wait is the login page flashing at a signed-in user. Every layout reads this before it
/// draws a page; nothing renders for <see cref="Checking"/>.
/// </summary>
public enum SignInState
{
    Checking,
    SignedOut,
    SignedIn
}
