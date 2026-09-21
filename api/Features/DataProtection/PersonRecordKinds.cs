using Jewel.JPMS.Api.Data.Entities;
using static Jewel.JPMS.Api.Features.DataProtection.PersonRecordFields;
using static Jewel.JPMS.Api.Features.DataProtection.PersonPseudonym;

namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// The rows that ARE a person, kind by kind. A company or household account keeps its own name
/// (it is the party to the contract, not the person); a worker keeps theirs, because recorded
/// cost and CIS returns are built on it. Everything that is the person alone — their name on a
/// contact row, their phone, their home's address, their brief — is erased.
/// </summary>
internal static class PersonRecordKinds
{
    public static readonly IReadOnlyList<IPersonRecordKind> All = new IPersonRecordKind[]
    {
        new PersonRecordKind<ClientEntity>("Client's primary contact", row => row.PrimaryContactEmail,
            row => row.ClientId, row => row.Name,
            row => new[] { Of(Name, row.PrimaryContactName) },
            (row, pseudonym) => { row.PrimaryContactName = ErasedName; row.PrimaryContactEmail = pseudonym; }),
        new PersonRecordKind<ArchitectEntity>("Architect's contact", row => row.ContactEmail,
            row => row.ArchitectId, row => row.Name,
            row => new[] { Of(Name, row.ContactName) },
            (row, pseudonym) => { row.ContactName = ErasedName; row.ContactEmail = pseudonym; }),
        new PersonRecordKind<PartyContactEntity>("Person at a client or architect", row => row.Email,
            row => row.PartyContactId, row => row.Name,
            row => new[] { Of(Name, row.Name), Of("Job title", row.JobTitle) },
            (row, pseudonym) => { row.Name = ErasedName; row.Email = pseudonym; row.JobTitle = null; }),
        new PersonRecordKind<ProjectContactEntity>("Project contact", row => row.Email,
            row => row.ContactId, row => row.Name,
            row => new[] { Of(Name, row.Name), Of("Organisation", row.Organisation) },
            (row, pseudonym) => { row.Name = ErasedName; row.Email = pseudonym; }),
        new PersonRecordKind<SubcontractorEntity>("Directory record's contact", row => row.ContactEmail,
            row => row.SubcontractorId, row => row.CompanyName,
            row => new[] { Of(Name, row.ContactName), Of("Phone", row.ContactPhone), Of("Mobile", row.MobileNumber) },
            (row, pseudonym) => { row.ContactName = ErasedName; row.ContactEmail = pseudonym; row.ContactPhone = ""; row.MobileNumber = ""; }),
        new PersonRecordKind<CompanyContactEntity>("Person at a directory record", row => row.Email,
            row => row.CompanyContactId, row => row.Name,
            row => new[] { Of(Name, row.Name), Of("Phone", row.Phone) },
            (row, pseudonym) => { row.Name = ErasedName; row.Email = pseudonym; row.Phone = ""; }),
        new PersonRecordKind<LeadEntity>("Sales lead", row => row.ContactEmail,
            row => row.LeadId, row => row.DisplayReference,
            row => new[] { Of(Name, row.ContactName), Of("Phone", row.ContactPhone), Of("Property", row.SiteAddress), Of("Summary", row.Summary), Of("Notes", row.Notes) },
            (row, pseudonym) => { row.ContactName = ErasedName; row.ContactEmail = pseudonym; row.ContactPhone = ""; row.SiteAddress = ErasedText; row.Notes = ""; }),
        new PersonRecordKind<ImagineRoundEntity>("Imagine round", row => row.ProspectEmail,
            row => row.RoundId, row => $"Round {row.Number}",
            row => new[] { Of(Name, row.ProspectName), Of("Brief", row.Brief) },
            (row, pseudonym) => { row.ProspectName = ErasedName; row.ProspectEmail = pseudonym; row.Brief = ErasedText; }),
        new PersonRecordKind<SalesProposalEntity>("Proposal they accepted", row => row.AcceptedByEmail,
            row => row.ProposalId, row => row.Title,
            row => new[] { Of(Name, row.AcceptedByName) },
            (row, pseudonym) => { row.AcceptedByName = ErasedName; row.AcceptedByEmail = pseudonym; }),
        new PersonRecordKind<RequestMessageEntity>("Message they wrote on a request", row => row.AuthorEmail,
            row => row.MessageId, row => $"Posted {row.PostedAt:d MMM yyyy}",
            row => new[] { Of(Name, row.AuthorName), Of("Message", row.Body) },
            (row, pseudonym) => { row.AuthorName = ErasedName; row.AuthorEmail = pseudonym; }),
        new PersonRecordKind<KpiPersonEntity>("KPI register entry", row => row.Email,
            row => row.KpiPersonId, row => row.Name,
            row => new[] { Of(Name, row.Name) },
            (row, pseudonym) => { row.Name = ErasedName; row.Email = pseudonym; }),
        new PersonRecordKind<WorkerEntity>("Worker", row => row.ContactEmail,
            row => row.WorkerId, row => row.Name,
            row => new[] { Of("Worker name (kept: recorded cost is built on it)", row.Name), Of("Phone", row.ContactPhone) },
            (row, pseudonym) => { row.ContactEmail = pseudonym; row.ContactPhone = ""; })
    };
}
