using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection.Queries;

/// <summary>
/// The subject-access reading: the rows that are the person, kind by kind, and every column
/// that names them, counted. Read whole before an erasure, and exported whole to answer a
/// request from the person themselves.
/// </summary>
public sealed class GetPersonDossierHandler : IQueryHandler<GetPersonDossier, PersonDossier>
{
    private readonly JpmsContext context;
    public GetPersonDossierHandler(JpmsContext context) { this.context = context; }

    public async Task<PersonDossier> HandleAsync(GetPersonDossier query, CancellationToken cancellationToken)
    {
        var email = query.Email.Trim();
        var records = new List<PersonRecord>();
        foreach (var kind in PersonRecordKinds.All)
            records.AddRange(await kind.FindAsync(context, email, cancellationToken));
        var mentions = await new PersonMentions(context).CountAsync(email, cancellationToken);
        var hasALiveLogin = await context.DirectoryUsers.AnyAsync(user => user.Email == email, cancellationToken);
        return new PersonDossier(email, records, mentions, hasALiveLogin);
    }
}
