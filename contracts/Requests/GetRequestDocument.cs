using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>Render and return the request's document (RFI etc.) PDF. Null when the request is not found.</summary>
public sealed record GetRequestDocument(string RequestId) : IQuery<RequestDocumentFile?>;
