namespace Jewel.JPMS.Api.Data.StoredValues;

/// <summary>
/// Thrown by <see cref="JpmsContext"/> before a save reaches SQL Server when a value does not fit
/// its column. The message is the sentences a person reads; the API answers it as a 400 through
/// <c>StoredValueRejectionMiddleware</c>, never as a server error.
/// </summary>
public sealed class StoredValuesRejectedException : Exception
{
    public StoredValuesRejectedException(IReadOnlyList<StoredValueProblem> problems)
        : base(string.Join(" ", problems.Select(problem => problem.Sentence)))
    {
        Problems = problems;
    }

    public IReadOnlyList<StoredValueProblem> Problems { get; }

    public IReadOnlyList<string> Sentences => Problems.Select(problem => problem.Sentence).ToList();
}
