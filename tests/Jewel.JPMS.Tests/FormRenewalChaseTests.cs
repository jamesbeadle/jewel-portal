using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Retention;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The insurance renewal chase keeps the promise the insurance form makes ("your expiry date will be
// used to remind you"), and only that promise: a certificate that came in on a form is asked for
// before it lapses, in the name of the Jewel company whose form it was; one the office filed itself
// was promised nothing and is never emailed about. Three asks at most, a week apart.
public sealed class FormRenewalChaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 7, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task OnlyACertificateThatCameInOnAForm_isChased_inThatCompanysName()
    {
        await using var forms = new PublicFormFixture();
        AddCompany(forms, "sub-roof", "Pat Roofer", "pat@roofco.example");
        AddCompany(forms, "sub-brick", "Bea Brick", "bea@brickco.example");
        forms.Context.ComplianceDocuments.Add(Certificate("doc-roof", "sub-roof", (int)JewelCompany.JewelPropertyServe));
        forms.Context.ComplianceDocuments.Add(Certificate("doc-brick", "sub-brick", null));
        await forms.Context.SaveChangesAsync();

        var outcome = await new FormRenewalChase(forms.Context, forms.Mailer, forms.Options).RunAsync(Now, CancellationToken.None);

        Assert.Equal(1, outcome.Insurance);
        var chase = Assert.Single(forms.Mailer.Sent);
        Assert.Equal(new[] { "pat@roofco.example" }, chase.To);
        Assert.Equal(JewelCompany.JewelPropertyServe, chase.Company);
        Assert.Contains("/f/jps/insurance?k=", chase.Text);
    }

    [Fact]
    public async Task ACertificate_isAskedForAtMostThreeTimes_aWeekApart()
    {
        await using var forms = new PublicFormFixture();
        AddCompany(forms, "sub-roof", "Pat Roofer", "pat@roofco.example");
        forms.Context.ComplianceDocuments.Add(Certificate("doc-roof", "sub-roof", (int)JewelCompany.JewelBespokeBuild));
        await forms.Context.SaveChangesAsync();
        var chase = new FormRenewalChase(forms.Context, forms.Mailer, forms.Options);

        foreach (var day in new[] { 0, 1, 7, 14, 21 }) await chase.RunAsync(Now.AddDays(day), CancellationToken.None);

        Assert.Equal(FormRenewalChase.MostChases, forms.Mailer.Sent.Count);
    }

    private static void AddCompany(PublicFormFixture forms, string subcontractorId, string contactName, string contactEmail) =>
        forms.Context.Subcontractors.Add(new SubcontractorEntity
        {
            SubcontractorId = subcontractorId, CompanyName = contactName + "'s company", ContactName = contactName, ContactEmail = contactEmail
        });

    private static ComplianceDocumentEntity Certificate(string documentId, string subcontractorId, int? formCompany) => new()
    {
        ComplianceDocumentId = documentId, SubcontractorId = subcontractorId, Kind = "Public liability insurance",
        FileName = "certificate.pdf", ExpiresAt = Now.AddDays(28), UploadedAt = Now.AddYears(-1), FormCompany = formCompany
    };
}
