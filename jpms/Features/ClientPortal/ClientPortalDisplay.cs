using System.Globalization;
using Jewel.JPMS.Contracts.ClientPortal;

using static Jewel.JPMS.MoneyFormats;

namespace Jewel.JPMS.Features.ClientPortal;

/// <summary>Display helpers shared by the client portal's variation views.</summary>
public static class ClientPortalDisplay
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    public static string Money(decimal value) => value.ToString("C0", Gb);

    /// <summary>The agreed value once approved; the estimate while the order is still with the
    /// client; null when the order is unpriced.</summary>
    public static string? ValueLabel(ClientPortalVariationOrder order)
    {
        if (order.Status == VariationOrderStatus.Approved) return Money(order.Value);
        return order.EstimatedValue is { } estimate ? Money(estimate) : null;
    }
}
