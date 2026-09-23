using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>
/// Correcting a certificate: the email its renewal is chased to, its expiry, or the day it stopped
/// mattering because the person left — an ended record is never chased, and its date starts the clock
/// on the certificate's retention.
/// </summary>
public sealed class SetTrainingRecordDetailsHandler : ICommandHandler<SetTrainingRecordDetails, TrainingRecord>
{
    private readonly JpmsContext context;

    public SetTrainingRecordDetailsHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<TrainingRecord> HandleAsync(SetTrainingRecordDetails command, CancellationToken cancellationToken)
    {
        var record = await context.TrainingRecords.FirstOrDefaultAsync(row => row.TrainingRecordId == command.TrainingRecordId, cancellationToken)
            ?? throw new InvalidOperationException("That certificate is no longer on the register.");
        var hasANewExpiry = record.ExpiresOn != command.ExpiresOn;
        record.Email = command.Email?.Trim() ?? "";
        record.ExpiresOn = command.ExpiresOn;
        record.EndedOn = command.EndedOn;
        if (hasANewExpiry) record.ChaseCount = 0;
        await context.SaveChangesAsync(cancellationToken);
        return record.ToModel();
    }
}

public sealed class SetTrainingRecordDetailsAuthorisation
{
    public bool Allows(SignedInUser user, SetTrainingRecordDetails command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class SetTrainingRecordDetailsValidation
{
    public ValidationOutcome Check(SetTrainingRecordDetails command)
    {
        var errors = new List<string>();
        var email = command.Email?.Trim() ?? "";
        var isABadAddress = email.Length > 0 && !FormInviteRows.IsAnEmailAddress(email);
        if (string.IsNullOrWhiteSpace(command.TrainingRecordId)) errors.Add("Which certificate?");
        if (isABadAddress) errors.Add("That email address does not look right.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
