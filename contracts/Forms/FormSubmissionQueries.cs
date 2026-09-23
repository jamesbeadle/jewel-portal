using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Forms;

/// <summary>
/// The forms that came in, newest first, without answers — every one still to handle however old, and
/// the newest of the rest; with a folder, every form filed under that person or company. A restricted
/// form is listed but not opened here.
/// </summary>
public sealed record ListFormSubmissions(string? FormFolderId = null) : IQuery<IReadOnlyList<FormSubmission>>;

/// <summary>One form's answers and files, for a reader the form's store admits.</summary>
public sealed record OpenFormSubmission(string FormSubmissionId) : IQuery<FormSubmissionView>;

/// <summary>Reveals an emergency form's health answers. Each reveal is written to the audit trail, naming the form and who looked.</summary>
public sealed record RevealHealthAnswers(string FormSubmissionId) : IQuery<IReadOnlyDictionary<string, string>>;

/// <summary>The people and companies forms are filed under, with their retention dates.</summary>
public sealed record ListFormFolders : IQuery<IReadOnlyList<FormFolder>>;

/// <summary>Each person's latest emergency contact, for whoever is on site when something happens.</summary>
public sealed record ListEmergencyContacts : IQuery<IReadOnlyList<EmergencyContactCard>>;
