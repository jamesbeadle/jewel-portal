using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Jewel.JPMS.Layout;

// RouteView that keys the page component by its type plus route parameter values.
//
// Blazor's default RouteView reuses the live page instance when only a route *value* changes —
// e.g. the project header's prev/next arrows going /projects/A/financials → /projects/B/financials.
// OnInitializedAsync then never re-runs, so the front-end data-loading convention (every tab page
// calls its stores' Refresh(projectId) once from OnInitializedAsync — see CLAUDE.md) silently
// skips, leaving the tab rendering project A's cached figures under project B's header until a
// manual reload.
//
// Keying the page fragment forces a fresh component per (page, route values): the loading state
// shows and OnInitializedAsync runs again. Only the page is keyed — the LayoutView wrapper is
// untouched, so MainLayout (and its side-nav expanded/collapsed state) survives navigation.
public sealed class KeyedPageRouteView : RouteView
{
    protected override void Render(RenderTreeBuilder builder)
    {
        // Mirrors RouteView.Render (layout attribute on the page wins over the router default),
        // swapping in our keyed page fragment.
        var pageLayoutType = RouteData.PageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType ?? DefaultLayout;
        builder.OpenComponent<LayoutView>(0);
        builder.AddComponentParameter(1, nameof(LayoutView.Layout), pageLayoutType);
        builder.AddComponentParameter(2, nameof(LayoutView.ChildContent), (RenderFragment)RenderPageWithParameters);
        builder.CloseComponent();
    }

    private void RenderPageWithParameters(RenderTreeBuilder builder)
    {
        builder.OpenComponent(0, RouteData.PageType);
        builder.SetKey(PageKey());
        foreach (var pair in RouteData.RouteValues)
        {
            builder.AddComponentParameter(1, pair.Key, pair.Value);
        }
        builder.CloseComponent();
    }

    private string PageKey() =>
        RouteData.PageType.FullName + "|" + string.Join(
            "|",
            RouteData.RouteValues
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}"));
}
