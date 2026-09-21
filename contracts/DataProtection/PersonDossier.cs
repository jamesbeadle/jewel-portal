using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.DataProtection;

/// <summary>
/// Everything the portal holds against one email address — the answer to a subject access
/// request, and the reading an administrator takes before anonymising. Records are the rows
/// that ARE the person (a client contact, a lead, a worker); mentions are every column that
/// merely names them (a "raised by" stamp, an audit actor), counted per table and column so
/// the answer is complete even where no page shows the row.
/// </summary>
public sealed record GetPersonDossier(string Email) : IQuery<PersonDossier>;

public sealed record PersonDossier(
    string Email,
    IReadOnlyList<PersonRecord> Records,
    IReadOnlyList<PersonMention> Mentions,
    // Whether a live sign-in still carries this address — anonymising waits for its removal.
    bool HasALiveLogin)
{
    public int MentionCount => Mentions.Sum(mention => mention.Count);
    public bool IsEmpty => Records.Count == 0 && Mentions.Count == 0;
}

/// <summary>One row that is the person: what kind of record, how the portal names it, and the
/// details it holds (name, phone, address…) as label/value pairs for the export.</summary>
public sealed record PersonRecord(
    string Kind,
    string RecordId,
    string Title,
    IReadOnlyList<PersonRecordField> Fields);

public sealed record PersonRecordField(string Label, string Value);

/// <summary>One column that names the person, and how many rows do.</summary>
public sealed record PersonMention(string Table, string Column, int Count);
