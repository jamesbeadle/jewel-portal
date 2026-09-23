namespace Jewel.JPMS.Models;

/// <summary>
/// The words a public form's page says around its questions, carried from the dashboard's page
/// (api/pubforms.js), and the pack page's. The pack page is what the pack email's three links
/// used to be, so it speaks as that email spoke (api/invite.js op:'sendpack').
/// </summary>
public static class FormSheetWording
{
    public const string NotAvailable = "This form is not available.";
    public const string Submit = "Submit";
    public const string Sending = "Sending…";
    public const string CouldNotSend = "Could not send - try again.";
    public const string ChooseFile = "Choose file or take photo";
    public const string ClearSignature = "Clear signature";
    public const string TypeYourName = "Type your full name";
    public const string SelectAnOption = "Select…";
    public const string TryAgain = "Try again";
    public const string PackTitle = "Your new starter forms";
    public const string OpenThisForm = "Open this form";
    public const string BackToYourForms = "Back to your forms";
    public const string Sent = "✓ Sent";
    public const string EverythingSent = "Every form in your pack has been sent to the office.";
    public const string SignatureUnavailable = "The signature box did not load. Reload the page to sign.";

    public static string PackIntro(string shortName) =>
        $"Welcome to {shortName}. Before your first day we need these forms from you. Each one takes a few minutes and "
        + "works on your phone. You do not have to do them all in one sitting: what you send is kept, and this link "
        + "brings you back to what is left.";

    public static string Uploading(string fileName) => $"Uploading {fileName}…";

    public static string Uploaded(string fileName) => "✓ " + fileName;
}
