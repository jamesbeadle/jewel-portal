using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class SetUsefulInformationSecretHandler : ICommandHandler<SetUsefulInformationSecret, UsefulInformationNote>
{
    private readonly JpmsContext context;
    private readonly SecretProtector protector;

    public SetUsefulInformationSecretHandler(JpmsContext context, SecretProtector protector)
    {
        this.context = context;
        this.protector = protector;
    }

    public async Task<UsefulInformationNote> HandleAsync(SetUsefulInformationSecret command, CancellationToken cancellationToken)
    {
        var entity = await context.UsefulInformationNotes.FindAsync(new object[] { command.UsefulInformationNoteId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Useful Information note {command.UsefulInformationNoteId} not found.");

        entity.SecretCiphertext = CiphertextFor(command.Secret);
        entity.UpdatedByEmail = command.ChangedByEmail;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // A blank secret removes the credential; removing one needs no key, and validation has
    // already refused a value when no key is configured.
    private string? CiphertextFor(string? secret) =>
        string.IsNullOrWhiteSpace(secret) ? null : protector.Protect(secret.Trim());
}
