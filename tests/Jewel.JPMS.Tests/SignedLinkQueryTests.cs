using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed class SignedLinkQueryTests
{
    private const string AccountName = "jpmstest";
    private const string AccountKey = "dGVzdC1rZXktdGhhdC1pcy1vbmx5LXVzZWQtdG8tc2lnbg==";
    private const string BlobName = "20261002/contractors-reports/abc/Report No. 32.pdf";
    private const string ContentDisposition =
        "attachment; filename=\"JBB-2026-001 - Contractor's Report No. 32 - w-e 1 Oct 2026.pdf\"";
    private const string ContentDispositionParameter = "rscd=";

    [Fact]
    public void A_signed_link_carries_no_form_encoded_space()
    {
        var link = SignedLinkQuery.WithPercentEncodedSpaces(SignedLinkWithSpacedFileName());

        Assert.DoesNotContain("+", link.Query);
    }

    [Fact]
    public void The_content_disposition_decodes_to_the_value_that_was_signed()
    {
        var link = SignedLinkQuery.WithPercentEncodedSpaces(SignedLinkWithSpacedFileName());

        Assert.Equal(ContentDisposition, DecodedContentDisposition(link));
    }

    [Fact]
    public void The_signature_is_left_as_it_was_signed()
    {
        var signedLink = SignedLinkWithSpacedFileName();
        var link = SignedLinkQuery.WithPercentEncodedSpaces(signedLink);

        Assert.Equal(SignatureOf(signedLink), SignatureOf(link));
    }

    [Fact]
    public void The_link_as_handed_out_carries_no_bare_space()
    {
        var link = SignedLinkQuery.WithPercentEncodedSpaces(SignedLinkWithSpacedFileName());

        Assert.DoesNotContain(" ", link.AbsoluteUri);
    }

    private static Uri SignedLinkWithSpacedFileName()
    {
        var credential = new StorageSharedKeyCredential(AccountName, AccountKey);
        var blobUri = new Uri($"https://{AccountName}.blob.core.windows.net/email-shares/{BlobName}");
        var blob = new BlobClient(blobUri, credential);
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(7))
        {
            BlobContainerName = AzureBlobEmailFileShareStore.ContainerName,
            BlobName = BlobName,
            Resource = "b",
            ContentDisposition = ContentDisposition,
        };
        return blob.GenerateSasUri(sas);
    }

    private static string DecodedContentDisposition(Uri link)
    {
        var parameter = QueryParameters(link).Single(part => part.StartsWith(ContentDispositionParameter));
        return Uri.UnescapeDataString(parameter[ContentDispositionParameter.Length..]);
    }

    private static string SignatureOf(Uri link) =>
        QueryParameters(link).Single(part => part.StartsWith("sig="));

    private static string[] QueryParameters(Uri link) => link.Query.TrimStart('?').Split('&');
}
