namespace Jewel.JPMS.Api.Data.StoredValues;

/// <summary>
/// One value a save would have put into a column that cannot hold it: the record and field it
/// belongs to, and the sentence a person reads to put it right.
/// </summary>
public sealed record StoredValueProblem(string Record, string Field, string Sentence);
