namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Requests/RFIs, Document Control and correspondence-filing commands as connector
/// actions. Mirrors Features/Requests/Commands, Features/DocumentControl/Commands and the
/// record-agnostic Features/RecordLinks filing commands — each entry's VisibleTo
/// copies its Authorisation class's role set (replicated where the set is a private field), and
/// the stamps copy exactly what the endpoint stamps server-side. Follows CalendarActions, the
/// exemplar file.</summary>
internal sealed partial class RequestsActions : IAiActionSource
{
    public IEnumerable<AiAction> Build() =>
        RequestActions()
            .Concat(RequestEmailActions())
            .Concat(DocumentControlActions())
            .Concat(FilingActions())
            .Concat(TriageActions());

    // Skipped: PostRequestMessage — already dispatched by AiWriteTools.post_request_message; do not duplicate.
    // (DiscardMessage / RestoreMessage / RemoveTagFromMessage / CreateRequestFromMessage are no
    //  longer skipped — gate classes added 2026-08-31, declared in the Correspondence area above.)
    // Skipped: AssignMessageToRequest (MailboxTriageEndpoints) — file_email_to_record covers the link path; the bare assign stays inline TriageRoles gate, no authorisation class to declare.
    // Skipped: ReplyInThreadFromMessage (MailboxTriageEndpoints) — same reason: inline TriageRoles gate, no authorisation class (stamps RaisedByEmail inline).
    // Skipped: RetagRequestWorkflowTags — one-off admin sweep with an inline TriageRoles gate; no authorisation or validation classes exist.
    // Skipped: SendMailboxEmail (MailboxIntake/Compose) — multipart/form-data upload shape dispatched to a concrete handler (not an ICommandHandler registration), inline role gate, no authorisation class.
    // (LinkMessageToRecord is no longer skipped — gate classes added 2026-08-28, actions file_email_to_record and file_unfiled_replies above; RecordLinksEndpoints.Gate reads the same TriageRoles.AllowedToTriage set.)
    // Skipped: PrepareProgrammeReplyDraft (RecordLinks) — no Authorisation class: the role set is a private field of the endpoint itself, and there is no validation class either.
    // Skipped: BackfillBucketsEndpoint (RecordLinks) — no command dispatch: the endpoint performs the Graph sweep directly.
    // Skipped: DiscardDocumentControlItem — no Authorisation class: inline DocumentControlRoles gate in DocumentControlItemCommandEndpoints.
    // Skipped: RestoreDocumentControlItem — no Authorisation class: inline DocumentControlRoles gate in DocumentControlItemCommandEndpoints.
    // Skipped: AttachDrawingsToRequest (RequestAttachmentEndpoints) — no Authorisation class: inline private RoleSet gate.
    // Skipped: RemoveRequestAttachment (RequestAttachmentEndpoints) — no Authorisation class: inline private RoleSet gate.
    // Skipped: UploadRequestAttachments (RequestAttachmentEndpoints) — multipart/form-data file upload with no command dispatch.
}
