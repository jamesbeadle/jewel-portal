namespace Jewel.JPMS.Api.Cqrs;

public sealed record ValidationOutcome(IReadOnlyList<string> Errors)
{
    public bool HasFailed => Errors.Count > 0;

    public static ValidationOutcome Passed { get; } = new(Array.Empty<string>());

    public static ValidationOutcome Failed(params string[] errors) => new(errors);
}
