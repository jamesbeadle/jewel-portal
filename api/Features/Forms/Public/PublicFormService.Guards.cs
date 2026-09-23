using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

/// <summary>
/// The compensating controls on a door anyone can knock on (api/forms-intake.js): a fixed list of
/// forms, a session the page made up, and a ceiling on files and forms from one address in an hour.
/// </summary>
public sealed partial class PublicFormService
{
    private const int MostUploadsFromOneAddressPerHour = 200;
    private const int MostFormsFromOneAddressPerHour = 30;

    private static (JewelCompany Company, FormDefinition Form) Known(string companyCode, string slug)
    {
        var company = JewelCompanies.ForCode(companyCode);
        var form = FormCatalogue.For(slug);
        var isKnown = company is not null && form is not null;
        return isKnown ? (company!.Company, form!) : throw new PublicFormRefusal("Unknown form.");
    }

    private static void CheckSession(string? sessionId)
    {
        if (!PublicFormFiles.IsASession(sessionId)) throw new PublicFormRefusal("Bad session.");
    }

    private async Task CheckUploadLimitsAsync(string sessionId, string clientHash, CancellationToken cancellationToken)
    {
        var inThisSession = await context.FormUploads.CountAsync(row => row.SessionId == sessionId, cancellationToken);
        if (inThisSession >= PublicFormLimits.MostFilesPerSession)
            throw new PublicFormRefusal(FormWording.TooManyFiles);
        var hourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var fromThisAddress = await context.FormUploads.CountAsync(row => row.ClientHash == clientHash && row.UploadedAt > hourAgo, cancellationToken);
        if (fromThisAddress >= MostUploadsFromOneAddressPerHour)
            throw new PublicFormRefusal("That is a lot of files in an hour — please try again a little later.");
    }

    private async Task CheckSubmissionLimitAsync(string clientHash, CancellationToken cancellationToken)
    {
        var hourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var fromThisAddress = await context.FormSubmissions.CountAsync(row => row.ClientHash == clientHash && row.SubmittedAt > hourAgo, cancellationToken);
        if (fromThisAddress >= MostFormsFromOneAddressPerHour)
            throw new PublicFormRefusal("That is a lot of forms from one place in an hour — please try again a little later.");
    }

    private async Task<PublicFormReceipt?> AlreadySentAsync(string sessionId, string formSlug, CancellationToken cancellationToken)
    {
        var earlier = await context.FormSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SessionId == sessionId && row.FormSlug == formSlug, cancellationToken);
        return earlier is null ? null : new PublicFormReceipt(earlier.FormSubmissionId, earlier.IsVerifiedLink);
    }
}
