using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>
/// Logs a tender enquiry from the architect's email. Order of work: pre-flight the cross-pathway
/// confirm and download the ticked attachments (both can refuse, and both are free to refuse
/// before anything exists); then the Lead project if the enquiry needs one; then the enquiry row;
/// then the copied files; then the email tag through the shared link path, so the tag matches the
/// provider. The tag is the only association — the enquiry reads its mail back live by it.
/// </summary>
public sealed class LogTenderEnquiryFromMessageHandler : ICommandHandler<LogTenderEnquiryFromMessage, TenderEnquiry>
{
    private const string NewRecordLabel = "the new tender enquiry";

    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly TenderEnquiryEmailAttachmentFetcher emailAttachments;
    private readonly TenderEnquiryProjectResolver projectResolver;
    private readonly TenderEnquiryRegister register;
    private readonly TenderEnquiryAttachmentWriter attachments;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public LogTenderEnquiryFromMessageHandler(
        JpmsContext context, IMailboxGraphClient graph, TenderEnquiryEmailAttachmentFetcher emailAttachments,
        TenderEnquiryProjectResolver projectResolver, TenderEnquiryRegister register,
        TenderEnquiryAttachmentWriter attachments, ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    {
        this.context = context;
        this.graph = graph;
        this.emailAttachments = emailAttachments;
        this.projectResolver = projectResolver;
        this.register = register;
        this.attachments = attachments;
        this.link = link;
    }

    public async Task<TenderEnquiry> HandleAsync(LogTenderEnquiryFromMessage command, CancellationToken cancellationToken)
    {
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");
        CrossPathwayGuard.EnsureConfirmed(
            snapshot.Categories, TriageCategories.BucketFor(RecordType.TenderEnquiry), command.AllowCrossPathway, NewRecordLabel);

        var files = await emailAttachments.DownloadAsync(command.MessageId, command.AttachmentIds, cancellationToken);
        var (projectId, createdProject) = await projectResolver.ResolveAsync(
            command.ProjectId, command.NewProject, command.Details, command.LoggedByEmail, cancellationToken);
        var enquiry = await register.LogAsync(projectId, command.Details, command.LoggedByEmail, cancellationToken);

        foreach (var file in files)
            await attachments.StoreAsync(enquiry, file, TenderEnquiryAttachmentSource.Email, command.LoggedByEmail, cancellationToken);
        if (files.Count > 0) await context.SaveChangesAsync(cancellationToken);

        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.TenderEnquiry, enquiry.TenderEnquiryId, command.InternetMessageId,
                AllowCrossPathway: command.AllowCrossPathway, Scope: command.Scope),
            cancellationToken);

        await register.RecordLoggedAsync(enquiry, createdProject, cancellationToken);
        return enquiry.ToModel();
    }
}
