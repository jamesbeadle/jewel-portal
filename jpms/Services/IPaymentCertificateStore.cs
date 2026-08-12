using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// The payment certificate register under Finance. Uncached: one short list read on entry to one
/// page (all projects, or one when filtered).
/// </summary>
public interface IPaymentCertificateStore
{
    Task<IReadOnlyList<PaymentCertificate>> ListAsync(string? projectId = null, CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams a certificate's stored copy (proxied — the container is private).</summary>
    string FileUrl(string paymentCertificateId, bool inline = false) =>
        $"api/finance/payment-certificates/{paymentCertificateId}/file" + (inline ? "?inline=1" : "");
}

public sealed class HttpPaymentCertificateStore : IPaymentCertificateStore
{
    private readonly Cqrs.IQueryClient queries;

    public HttpPaymentCertificateStore(Cqrs.IQueryClient queries) { this.queries = queries; }

    public Task<IReadOnlyList<PaymentCertificate>> ListAsync(string? projectId = null, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new Contracts.DocumentControl.ListPaymentCertificates(projectId), cancellationToken);
}
