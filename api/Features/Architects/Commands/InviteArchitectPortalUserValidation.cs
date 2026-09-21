namespace Jewel.JPMS.Api.Features.Architects.Commands;

/// <summary>The one thing an invite needs that the practice may not hold: an address that can
/// receive the set-password link.</summary>
public sealed class InviteArchitectPortalUserValidation
{
    public string? Complaint(string email)
    {
        if (IsShapedLikeAnEmail(email)) return null;
        return "The practice has no valid contact email. Provide one to invite.";
    }

    private static bool IsShapedLikeAnEmail(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@') && value.IndexOf('@') < value.LastIndexOf('.');
}
