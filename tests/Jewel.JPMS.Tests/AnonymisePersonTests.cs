using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.DataProtection;
using Jewel.JPMS.Api.Features.DataProtection.Commands;
using Jewel.JPMS.Api.Features.DataProtection.Queries;
using Jewel.JPMS.Contracts.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// Erasure with the ledger intact (data protection, 2026-09-21): the dossier reads every row that
// is the person and every column that names them; anonymising erases the one and rewrites the
// other to a pseudonym derived from the email, and refuses while a sign-in still carries it.
public sealed class AnonymisePersonTests
{
    private const string Email = "mary.homeowner@example.com";

    [Fact]
    public void Pseudonym_isTheSameForTheSameEmail_andNeverDeliverable()
    {
        Assert.Equal(PersonPseudonym.For(Email), PersonPseudonym.For(" Mary.Homeowner@Example.com "));
        Assert.NotEqual(PersonPseudonym.For(Email), PersonPseudonym.For("someone.else@example.com"));
        Assert.EndsWith("@erased.invalid", PersonPseudonym.For(Email));
        Assert.True(PersonPseudonym.IsOne(PersonPseudonym.For(Email)));
        Assert.False(PersonPseudonym.IsOne(Email));
    }

    [Fact]
    public void EveryEmailColumn_isReadOffTheModel_keysLeftOut()
    {
        using var context = Context();
        var columns = PersonColumns.Of(context.Model);
        Assert.Contains(columns, column => column.Table == "AuditEvent" && column.Column == "ActorEmail");
        Assert.Contains(columns, column => column.Table == "Lead" && column.Column == "OwnerEmail");
        Assert.DoesNotContain(columns, column => column.Table == "DirectoryUser" && column.Column == "Email");
    }

    [Fact]
    public async Task Dossier_listsTheRecordsThatAreThem_andCountsTheMentions()
    {
        await using var context = await SeededAsync();
        var dossier = await new GetPersonDossierHandler(context).HandleAsync(new GetPersonDossier(Email), CancellationToken.None);

        Assert.Contains(dossier.Records, record => record.Kind == "Sales lead" && record.Fields.Any(field => field.Value == "Mary Homeowner"));
        Assert.Contains(dossier.Records, record => record.Kind == "Person at a client or architect");
        Assert.Contains(dossier.Mentions, mention => mention.Table == "AuditEvent" && mention.Column == "ActorEmail" && mention.Count == 2);
        Assert.False(dossier.HasALiveLogin);
    }

    [Fact]
    public async Task Anonymising_erasesTheRecords_rewritesEveryMention_andRedactsTheSentences()
    {
        await using var context = await SeededAsync();
        var outcome = await Handler(context).HandleAsync(new AnonymisePerson(Email, "SAR of 20 Sep 2026"), CancellationToken.None);

        var lead = await context.Leads.SingleAsync();
        Assert.Equal(PersonPseudonym.ErasedName, lead.ContactName);
        Assert.Equal(outcome.Pseudonym, lead.ContactEmail);
        Assert.Equal(PersonPseudonym.ErasedText, lead.SiteAddress);
        Assert.All(await context.AuditEvents.Where(row => row.EventType != (int)AuditEventType.PersonAnonymised).ToListAsync(),
            row => { Assert.Equal(outcome.Pseudonym, row.ActorEmail); Assert.DoesNotContain(Email, row.Detail); Assert.DoesNotContain("Mary Homeowner", row.Detail); });
        Assert.Equal(2, outcome.RecordsAnonymised);
        var after = await new GetPersonDossierHandler(context).HandleAsync(new GetPersonDossier(Email), CancellationToken.None);
        Assert.True(after.IsEmpty);
    }

    [Fact]
    public async Task ALiveLogin_isRefused()
    {
        await using var context = await SeededAsync();
        context.DirectoryUsers.Add(new DirectoryUserEntity { Email = Email, DisplayName = "Mary" });
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(context).HandleAsync(new AnonymisePerson(Email, "asked"), CancellationToken.None));
    }

    private static AnonymisePersonHandler Handler(JpmsContext context) =>
        new(context, new AuditTrail(context, new AuditActor { Email = "admin@jewelbb.co.uk" }, NullLogger<AuditTrail>.Instance));

    private static JpmsContext Context() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"anonymise-{Guid.NewGuid():N}").Options);

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = Context();
        context.Leads.Add(new LeadEntity { LeadId = "lead-1", ContactName = "Mary Homeowner", ContactEmail = Email, ContactPhone = "07700 900000", SiteAddress = "1 Acacia Avenue", OwnerEmail = "sales@jewelbb.co.uk", Number = 12 });
        context.PartyContacts.Add(new PartyContactEntity { PartyContactId = "pc-1", PartyId = "client-1", Name = "Mary Homeowner", Email = Email });
        context.AuditEvents.Add(new AuditEventEntity { AuditEventId = "ae-1", ActorEmail = Email, Detail = $"Mary Homeowner ({Email}) accepted proposal v1.", OccurredAt = DateTimeOffset.UtcNow });
        context.AuditEvents.Add(new AuditEventEntity { AuditEventId = "ae-2", ActorEmail = Email, Detail = "Reacted to a concept.", OccurredAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();
        return context;
    }
}
