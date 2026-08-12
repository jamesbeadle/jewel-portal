using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// The payment certificate register: all projects (ProjectId null) or one project's, newest
// issued first.
public sealed record ListPaymentCertificates(string? ProjectId = null) : IQuery<IReadOnlyList<PaymentCertificate>>;
