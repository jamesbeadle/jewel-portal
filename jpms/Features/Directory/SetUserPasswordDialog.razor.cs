using Jewel.JPMS.Contracts.Auth;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Features.Directory;

/// <summary>An administrator sets a user's password by hand from their Admin → Users row
/// (2026-09-28), for someone who cannot use an emailed link. The form mirrors PasswordPolicy, the
/// statement the API checks; the password is sent once, never shown again, and forgotten by the
/// dialog as soon as it is set.</summary>
public partial class SetUserPasswordDialog
{
    [Inject] private ICommandSender Commands { get; set; } = default!;

    [Parameter, EditorRequired] public string Email { get; set; } = "";
    [Parameter, EditorRequired] public string DisplayName { get; set; } = "";
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    private string password = "";
    private string confirmation = "";
    private string? passwordProblem;
    private string? confirmationProblem;
    private string? error;
    private bool busy;
    private bool isSet;

    private string Title => isSet ? "Password set" : $"Set a password for {DisplayName}";

    private bool IsReadyToSet => !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(confirmation);

    private async Task SetAsync()
    {
        if (!IsAcceptable()) return;
        busy = true;
        error = null;
        try
        {
            await Commands.SendAsync(new SetUserPassword(Email, password), CancellationToken.None);
            ForgetTheTypedPassword();
            isSet = true;
        }
        catch (CommandFailedException failure) { error = failure.Message; }
        catch { error = "Couldn't set the password. Please try again."; }
        finally { busy = false; }
    }

    private bool IsAcceptable()
    {
        passwordProblem = PasswordPolicy.Validate(password);
        var isConfirmed = confirmation == password;
        confirmationProblem = isConfirmed ? null : "Those passwords don't match.";
        return passwordProblem is null && confirmationProblem is null;
    }

    private void ForgetTheTypedPassword()
    {
        password = "";
        confirmation = "";
        passwordProblem = null;
        confirmationProblem = null;
    }

    private async Task CloseAsync()
    {
        ForgetTheTypedPassword();
        error = null;
        isSet = false;
        await OnClosed.InvokeAsync();
    }
}
