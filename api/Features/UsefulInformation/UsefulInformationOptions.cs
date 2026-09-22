namespace Jewel.JPMS.Api.Features.UsefulInformation;

/// <summary>
/// Configuration for the credentials held against Useful Information notes: the one key every
/// stored credential is encrypted under. Set as the app setting UsefulInformation__SecretKey —
/// 32 random bytes, base64 (openssl rand -base64 32). Unset means no credential can be held or
/// revealed; the notes themselves are unaffected. Never checked into source control.
/// </summary>
public sealed class UsefulInformationOptions
{
    public const string SectionName = "UsefulInformation";

    public string? SecretKey { get; set; }

    public bool CanHoldSecrets => !string.IsNullOrWhiteSpace(SecretKey);

    public static UsefulInformationOptions FromConfiguration(IConfiguration configuration) =>
        new() { SecretKey = configuration.GetSection(SectionName)["SecretKey"] };
}
