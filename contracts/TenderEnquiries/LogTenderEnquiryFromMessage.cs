using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.TenderEnquiries;

/// <summary>
/// The Control Centre's "Log Tender Enquiry": turns the architect's invitation email into a tender
/// enquiry record and tags the email to it. Exactly one of <see cref="ProjectId"/> (an existing
/// project) or <see cref="NewProject"/> (a Lead-stage shell the handler creates) is given — an
/// enquiry is usually the first Jewel hears of a job, so creating the project here is the common
/// path. The ticked email attachments (the PQQ, the drawings) are copied mailbox → blob store
/// server-side, downloaded BEFORE anything persists so a vanished attachment fails cleanly.
/// LoggedByEmail is stamped server-side from the signed-in user.
/// </summary>
public sealed record LogTenderEnquiryFromMessage(
    string MessageId,
    string? InternetMessageId,
    string? ProjectId,
    TenderEnquiryProjectDraft? NewProject,
    TenderEnquiryDetails Details,
    IReadOnlyList<string>? AttachmentIds = null,
    string LoggedByEmail = "",
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Explicit consent to file the thread under Client as well as a pathway it already carries.
    // Pre-flighted before anything is created (CrossPathwayGuard), so a rejection creates nothing.
    bool AllowCrossPathway = false) : ICommand<TenderEnquiry>;

/// <summary>Logs an enquiry by hand on a project that already exists — the phone-call case.</summary>
public sealed record LogTenderEnquiry(
    string ProjectId,
    TenderEnquiryDetails Details,
    string LoggedByEmail = "") : ICommand<TenderEnquiry>;

public sealed record UpdateTenderEnquiryDetails(
    string TenderEnquiryId,
    TenderEnquiryDetails Details) : ICommand<TenderEnquiry>;

/// <summary>Moves the enquiry along its journey. The handler stamps the matching date (PQQ
/// submitted, tender submitted, decided) and refuses a status the current one can't reach.</summary>
public sealed record SetTenderEnquiryStatus(
    string TenderEnquiryId,
    TenderEnquiryStatus Status,
    string Note = "",
    string ChangedByEmail = "") : ICommand<TenderEnquiry>;

/// <summary>Replaces the questionnaire answers wholesale — the PQQ editor saves the whole sheet.</summary>
public sealed record SetTenderEnquiryAnswers(
    string TenderEnquiryId,
    IReadOnlyList<TenderEnquiryAnswerDraft> Answers) : ICommand<IReadOnlyList<TenderEnquiryAnswer>>;
