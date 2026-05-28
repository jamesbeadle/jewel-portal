namespace Jewel.JPMS.Services;

public static class FormNumber
{
    public static decimal ParseAmount(string value) =>
        decimal.TryParse(value, out var amount) ? amount : 0m;

    public static int ParseCount(string value) =>
        int.TryParse(value, out var count) ? count : 0;
}
