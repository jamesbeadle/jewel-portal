namespace Jewel.JPMS.Services;

public static class FormNumber
{
    public static decimal ParseAmount(string value) =>
        decimal.TryParse(value, out var amount) ? amount : 0m;

    /// <summary>ParseAmount that says whether the text WAS a number — for serialising a form's
    /// live state, where a blank must go out as null rather than a 0 someone appears to have typed.</summary>
    public static bool TryParseAmount(string? value, out decimal amount)
    {
        if (decimal.TryParse(value, out amount)) return true;
        amount = 0m;
        return false;
    }

    public static int ParseCount(string value) =>
        int.TryParse(value, out var count) ? count : 0;
}
