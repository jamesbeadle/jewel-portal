namespace Jewel.JPMS.Api.Features.Forms.Storage;

/// <summary>
/// Where the office's own files sit in a store: the right-to-work evidence a checker files is kept
/// under its check, so the check's destruction finds every file it ever had.
/// </summary>
internal static class FormUploadFolders
{
    public const string OfficeSession = "office";

    public static string RightToWorkEvidence(string rightToWorkCheckId) => $"{OfficeSession}/rtw/{rightToWorkCheckId}/";
}
