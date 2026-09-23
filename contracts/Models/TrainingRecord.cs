namespace Jewel.JPMS.Models;

/// <summary>Where a certificate stands today. A certificate with no expiry is never chased and never expires.</summary>
public enum TrainingStanding
{
    Valid = 0,
    ExpiringSoon = 1,
    Expired = 2,
    NoExpiry = 3,
    Ended = 4
}

/// <summary>
/// One ticket or certificate on the training register, accepted from a Training Certificate form.
/// The value is the expiry date, not the upload: a card sitting in a folder tells nobody it dies in
/// three weeks, so the date is held here and the holder is asked for the renewal before it lapses.
/// </summary>
public sealed record TrainingRecord(
    string TrainingRecordId,
    JewelCompany Company,
    string PersonName,
    string Email,
    string Course,
    string Provider,
    string CertificateNumber,
    DateOnly CompletedOn,
    DateOnly? ExpiresOn,
    string? FormSubmissionId,
    string? CertificateUploadId,
    DateTimeOffset? LastChasedAt,
    int ChaseCount,
    DateOnly? EndedOn)
{
    public const int ExpiringSoonDays = 30;

    public TrainingStanding StandingOn(DateOnly today) => this switch
    {
        { EndedOn: not null } => TrainingStanding.Ended,
        { ExpiresOn: null } => TrainingStanding.NoExpiry,
        _ when ExpiresOn < today => TrainingStanding.Expired,
        _ when ExpiresOn <= today.AddDays(ExpiringSoonDays) => TrainingStanding.ExpiringSoon,
        _ => TrainingStanding.Valid
    };
}
