using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Features.TenderEnquiries;

public static class TenderEnquiriesRouteRegistration
{
    public static void RegisterTenderEnquiriesRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListTenderEnquiries, IReadOnlyList<TenderEnquiry>>(
            new QueryRoute("/api/tender-enquiries", _ => "/api/tender-enquiries"));

        queries.Register<ListTenderEnquiriesForProject, IReadOnlyList<TenderEnquiry>>(
            new QueryRoute("/api/projects/{projectId}/tender-enquiries",
                query => $"/api/projects/{((ListTenderEnquiriesForProject)query).ProjectId}/tender-enquiries"));

        queries.Register<GetTenderEnquiryById, TenderEnquiry?>(
            new QueryRoute("/api/tender-enquiries/{tenderEnquiryId}",
                query => $"/api/tender-enquiries/{((GetTenderEnquiryById)query).TenderEnquiryId}"));

        queries.Register<ListTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>>(
            new QueryRoute("/api/tender-enquiries/{tenderEnquiryId}/answers",
                query => $"/api/tender-enquiries/{((ListTenderEnquiryAnswers)query).TenderEnquiryId}/answers"));

        // Uploads travel as multipart through HttpTenderEnquiryAttachmentStore, not through this
        // table; the PDF is fetched straight from /api/tender-enquiries/{id}/document (a file).
        queries.Register<ListTenderEnquiryAttachments, IReadOnlyList<TenderEnquiryAttachment>>(
            new QueryRoute("/api/tender-enquiries/{tenderEnquiryId}/attachments",
                query => $"/api/tender-enquiries/{((ListTenderEnquiryAttachments)query).TenderEnquiryId}/attachments"));

        commands.Register<RemoveTenderEnquiryAttachment, IReadOnlyList<TenderEnquiryAttachment>>(
            new CommandRoute("DELETE", "/api/tender-enquiries/{tenderEnquiryId}/attachments/{attachmentId}",
                command =>
                {
                    var remove = (RemoveTenderEnquiryAttachment)command;
                    return $"/api/tender-enquiries/{remove.TenderEnquiryId}/attachments/{remove.TenderEnquiryAttachmentId}";
                }));

        commands.Register<LogTenderEnquiryFromMessage, TenderEnquiry>(
            new CommandRoute("POST", "/api/mailbox/message/log-tender-enquiry",
                _ => "/api/mailbox/message/log-tender-enquiry"));

        commands.Register<LogTenderEnquiry, TenderEnquiry>(
            new CommandRoute("POST", "/api/tender-enquiries", _ => "/api/tender-enquiries"));

        commands.Register<UpdateTenderEnquiryDetails, TenderEnquiry>(
            new CommandRoute("PUT", "/api/tender-enquiries/{tenderEnquiryId}/details",
                command => $"/api/tender-enquiries/{((UpdateTenderEnquiryDetails)command).TenderEnquiryId}/details"));

        commands.Register<SetTenderEnquiryStatus, TenderEnquiry>(
            new CommandRoute("POST", "/api/tender-enquiries/{tenderEnquiryId}/status",
                command => $"/api/tender-enquiries/{((SetTenderEnquiryStatus)command).TenderEnquiryId}/status"));

        commands.Register<SetTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>>(
            new CommandRoute("PUT", "/api/tender-enquiries/{tenderEnquiryId}/answers",
                command => $"/api/tender-enquiries/{((SetTenderEnquiryAnswers)command).TenderEnquiryId}/answers"));
    }
}
