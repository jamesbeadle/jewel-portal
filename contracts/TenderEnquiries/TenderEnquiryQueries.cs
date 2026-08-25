using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.TenderEnquiries;

public sealed record ListTenderEnquiriesForProject(string ProjectId) : IQuery<IReadOnlyList<TenderEnquiry>>;

public sealed record GetTenderEnquiryById(string TenderEnquiryId) : IQuery<TenderEnquiry?>;

public sealed record ListTenderEnquiryAnswers(string TenderEnquiryId) : IQuery<IReadOnlyList<TenderEnquiryAnswer>>;

/// <summary>The PQQ response as a PDF, rendered fresh from the answers on every call.</summary>
public sealed record GetTenderEnquiryDocument(string TenderEnquiryId) : IQuery<TenderEnquiryDocumentFile?>;

// Attachments kept on an enquiry (uploads travel as multipart through the client's attachment
// store, not through the JSON command sender — mirroring BidPackageAttachmentContracts).
public sealed record ListTenderEnquiryAttachments(string TenderEnquiryId)
    : IQuery<IReadOnlyList<TenderEnquiryAttachment>>;

public sealed record RemoveTenderEnquiryAttachment(
    string TenderEnquiryId,
    string TenderEnquiryAttachmentId) : ICommand<IReadOnlyList<TenderEnquiryAttachment>>;
