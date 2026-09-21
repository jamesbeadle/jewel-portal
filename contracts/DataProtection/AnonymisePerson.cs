using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.DataProtection;

/// <summary>
/// Erasure, the way a business with statutory records can honour it: every row that IS the
/// person (their contact record, their lead, their imagine rounds) loses its name, email, phone
/// and free text, and every column that merely names them — the "raised by" stamps, the audit
/// actor — is rewritten to one pseudonym derived from the email, so the trail still shows one
/// person acted while nobody can say who. Nothing is deleted: invoices, orders and audit rows
/// keep their money and their dates. Refused while the address still has a live sign-in
/// (revoke and delete the user first) and while a worker under it still has history that
/// RetireWorker has not closed. Irreversible. AnonymisedByEmail is stamped by the server.
/// </summary>
public sealed record AnonymisePerson(string Email, string Reason, string AnonymisedByEmail = "")
    : ICommand<PersonAnonymisation>;

public sealed record PersonAnonymisation(
    string Pseudonym,
    int RecordsAnonymised,
    int MentionsRewritten,
    IReadOnlyList<PersonMention> Rewritten);
