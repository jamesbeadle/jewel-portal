namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>The SQL Server error numbers that mean "this value does not fit its column".</summary>
internal static class SqlErrorNumbers
{
    public const int StringTruncatedNamingColumn = 2628;
    public const int StringTruncated = 8152;
    public const int ArithmeticOverflow = 8115;
    public const int NullIntoRequiredColumn = 515;
}
