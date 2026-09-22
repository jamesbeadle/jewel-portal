using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Queries;

// The one read that answers a credential's value. The reveal is written to the audit trail
// AFTER the value is in hand (best effort, as every audit write is): the row names the note and
// the person, never the value, so a misused code can be traced to who had read it.
public sealed class RevealUsefulInformationSecretHandler : IQueryHandler<RevealUsefulInformationSecret, UsefulInformationSecret>
{
    private readonly JpmsContext context;
    private readonly SecretProtector protector;
    private readonly AuditTrail audit;

    public RevealUsefulInformationSecretHandler(JpmsContext context, SecretProtector protector, AuditTrail audit)
    {
        this.context = context;
        this.protector = protector;
        this.audit = audit;
    }

    public async Task<UsefulInformationSecret> HandleAsync(RevealUsefulInformationSecret query, CancellationToken cancellationToken)
    {
        var entity = await context.UsefulInformationNotes.AsNoTracking()
            .FirstOrDefaultAsync(note => note.UsefulInformationNoteId == query.UsefulInformationNoteId, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Useful Information note {query.UsefulInformationNoteId} not found.");
        if (entity.SecretCiphertext is null) throw new InvalidOperationException($"'{entity.Title}' holds no credential.");

        var secret = protector.Unprotect(entity.SecretCiphertext);

        await audit.WriteAsync(
            AuditEventType.SiteCredentialRevealed,
            detail: $"Revealed the credential held on '{entity.Title}'",
            projectId: entity.ProjectId,
            cancellationToken: cancellationToken);

        return new UsefulInformationSecret(entity.UsefulInformationNoteId, secret);
    }
}
