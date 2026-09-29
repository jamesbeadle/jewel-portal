using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Manual;

/// <summary>A new module, born a draft. The email stamps are the signed-in user's, never the client's.</summary>
public sealed record CreateManualModule(
    string Code,
    string Title,
    string Purpose,
    string Body,
    string OwnerEmail,
    string ApproverEmail,
    ManualAudience Audience,
    IReadOnlyList<string> LinkedFormSlugs,
    string LinkedStandards,
    string SourceSections,
    DateTimeOffset? NextReviewAt,
    string CreatedByEmail = "") : ICommand<ManualModule>;

/// <summary>Replaces a draft's text and controls in one write. Refused once the draft is in review or approved.</summary>
public sealed record UpdateManualModuleDraft(
    string ManualModuleId,
    string Title,
    string Purpose,
    string Body,
    string OwnerEmail,
    string ApproverEmail,
    ManualAudience Audience,
    IReadOnlyList<string> LinkedFormSlugs,
    string LinkedStandards,
    string ChangeSummary,
    DateTimeOffset? NextReviewAt,
    string UpdatedByEmail = "") : ICommand<ManualModule>;

/// <summary>Draft → In review: the owner hands the draft to its approver.</summary>
public sealed record SubmitManualModuleForReview(string ManualModuleId, string SubmittedByEmail = "") : ICommand<ManualModule>;

/// <summary>In review → Approved: publishes this version to its views and supersedes the last.</summary>
public sealed record ApproveManualModule(string ManualModuleId, DateTimeOffset? NextReviewAt, string ApprovedByEmail = "") : ICommand<ManualModule>;

/// <summary>In review → Draft, with the reason, for the owner to work on again.</summary>
public sealed record ReturnManualModuleToDraft(string ManualModuleId, string Reason, string ReturnedByEmail = "") : ICommand<ManualModule>;

/// <summary>Opens the next version of an approved module as a draft; the approved text stays published meanwhile.</summary>
public sealed record ReviseManualModule(string ManualModuleId, string RevisedByEmail = "") : ICommand<ManualModule>;

/// <summary>Retires a module: it leaves every site view and its versions stay as the record.</summary>
public sealed record RetireManualModule(string ManualModuleId, string RetiredByEmail = "") : ICommand<ManualModule>;

/// <summary>The reader's own acknowledgement of the published version: a typed name against that exact version.</summary>
public sealed record AcknowledgeManualModule(string ManualModuleId, string TypedName, string AcknowledgedByEmail = "") : ICommand<ManualAcknowledgement>;

/// <summary>Loads the JBB Site Manager Manual (v0.13, May 2026) as draft modules, one per module of the modular restructure, skipping any code already present.</summary>
public sealed record ImportManualBaseline(string ImportedByEmail = "") : ICommand<ManualBaselineImport>;
