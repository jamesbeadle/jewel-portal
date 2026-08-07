using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Sharing;

/// <summary>
/// Azure Blob Storage implementation. Shared files live in their own private container
/// (<c>email-shares</c>, created on first use — the drawings/photos containers are never touched),
/// keyed <c>{yyyyMMdd}/{scope}/{guid}/{fileName}</c>. The date prefix is what cleanup runs on:
/// after minting a link the store best-effort sweeps blobs whose prefix is older than the link
/// lifetime plus a day of grace, at most once per day — no timer, no infra lifecycle policy to
/// remember to deploy. The container stays private (<see cref="PublicAccessType.None"/>); each
/// link works because it carries a read-only SAS signed with the account key, so this survives
/// the storage account's <c>--allow-blob-public-access false</c> (infra/azure-prod-setup-v2.sh).
/// </summary>
public sealed class AzureBlobEmailFileShareStore : IEmailFileShareStore
{
    public const string ContainerName = "email-shares";

    /// <summary>How long a minted link keeps working — the "expires in 7 days" the email promises.</summary>
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromDays(7);

    private readonly BlobContainerClient container;
    private readonly ILogger<AzureBlobEmailFileShareStore> logger;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;
    private DateTimeOffset lastSweepUtc = DateTimeOffset.MinValue;

    public AzureBlobEmailFileShareStore(string connectionString, ILogger<AzureBlobEmailFileShareStore> logger)
    {
        this.logger = logger;
        // Bounded retry so a misconfigured storage account fails fast instead of appearing to hang
        // — the same reasoning as AzureBlobDrawingStore. NetworkTimeout is per-attempt, so large
        // uploads are unaffected.
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Fixed,
                MaxRetries = 2,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(3),
                NetworkTimeout = TimeSpan.FromSeconds(30),
            }
        };
        container = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public bool IsConfigured => true;

    public async Task<EmailFileShareLink?> ShareAsync(
        string scope, string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var safeName = SafeFileName(fileName);
        var blobRef = $"{DateTimeOffset.UtcNow:yyyyMMdd}/{SafeSegment(scope)}/{Guid.NewGuid():N}/{safeName}";
        var blob = container.GetBlobClient(blobRef);

        await blob.UploadAsync(
            new BinaryData(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                }
            },
            cancellationToken);

        // A connection string without an account key (e.g. SAS-only or identity-based) cannot sign
        // per-blob SAS URLs; report "no link" rather than minting a URL nobody can open.
        if (!blob.CanGenerateSasUri)
        {
            logger.LogWarning(
                "Email share store cannot sign SAS links — the storage connection string carries no account key. " +
                "Large attachments cannot be shared as links until it does.");
            return null;
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(LinkLifetime);
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, expiresAt)
        {
            BlobContainerName = container.Name,
            BlobName = blobRef,
            Resource = "b",
            // Download with the real file name rather than rendering in the browser tab.
            ContentDisposition = $"attachment; filename=\"{safeName}\"",
        };
        var url = blob.GenerateSasUri(sas);

        await SweepExpiredAsync(cancellationToken);

        return new EmailFileShareLink(fileName, content.LongLength, url, expiresAt);
    }

    /// <summary>
    /// Best-effort deletion of blobs whose date prefix is past the link lifetime (plus a day of
    /// grace so a link minted at 23:59 never dies early). Runs at most once per day, piggybacking
    /// on a share — volume is a handful of files a week, so a full listing is cheap. Failures are
    /// logged and retried on the next day's first share; an expired blob whose SAS has lapsed is
    /// unreachable anyway, so a delayed sweep costs storage pennies, not correctness.
    /// </summary>
    private async Task SweepExpiredAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - lastSweepUtc < TimeSpan.FromHours(24)) return;
        lastSweepUtc = now;

        try
        {
            var cutoff = now.UtcDateTime.Date - LinkLifetime - TimeSpan.FromDays(1);
            await foreach (var item in container.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                var slash = item.Name.IndexOf('/');
                if (slash != 8) continue;
                if (DateTime.TryParseExact(
                        item.Name[..slash], "yyyyMMdd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var day)
                    && day < cutoff)
                {
                    await container.DeleteBlobIfExistsAsync(item.Name, cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Email share sweep failed — expired share blobs remain until the next sweep.");
        }
    }

    private static string SafeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "file";
        // Quotes would break the Content-Disposition header the SAS bakes in.
        return safeName.Replace("\"", "");
    }

    /// <summary>A record reference ("BPI-0001", "RFI 012") reduced to blob-path-safe characters.</summary>
    private static string SafeSegment(string value)
    {
        var cleaned = new string((value ?? "").Trim()
            .Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-')
            .ToArray());
        return string.IsNullOrWhiteSpace(cleaned.Replace("-", "")) ? "shared" : cleaned;
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (containerEnsured) return;
        await ensureContainerGate.WaitAsync(cancellationToken);
        try
        {
            if (!containerEnsured)
            {
                await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
                containerEnsured = true;
            }
        }
        finally
        {
            ensureContainerGate.Release();
        }
    }
}
