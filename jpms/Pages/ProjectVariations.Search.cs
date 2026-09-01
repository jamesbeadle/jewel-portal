using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariations
{
    // ---- Free-text search -----------------------------------------------------------------------
    // Keywords are ANDed on whitespace, same as every register search. The originating request's
    // own text is included — someone searching "boiler" should find the variation that priced the
    // boiler RFI even when only the RFI says the word.

    private string search = "";

    private bool Searching => !string.IsNullOrWhiteSpace(search);

    private void OnSearchInput(ChangeEventArgs e) => search = e.Value?.ToString() ?? "";

    private void ClearSearch() => search = "";

    private bool MatchesSearch(VariationOrder order)
    {
        if (!Searching) return true;

        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token =>
            SearchableText(order).Any(field => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private IEnumerable<string?> SearchableText(VariationOrder order)
    {
        yield return order.Reference;
        yield return order.DisplayNumber;
        yield return order.VariationRef;
        yield return order.Title;
        yield return order.Description;
        yield return order.CostCode;

        // The Request column's own text, so the search finds a variation by its RFI.
        var source = RequestFor(order);
        if (source is null) yield break;
        yield return source.Reference;
        yield return source.DisplayNumber;
        yield return source.Title;
    }

    private IReadOnlyList<VariationOrder> FilteredRows =>
        Rows.Where(MatchesSearch).ToList();

}
