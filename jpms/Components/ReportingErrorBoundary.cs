using Jewel.JPMS.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Jewel.JPMS.Components;

/// <summary>
/// Catches exceptions thrown inside a page and turns them into a report rather than a dead app.
///
/// Blazor's stock ErrorBoundary renders a message and stops; the useful part — what actually went
/// wrong — never reaches anybody who could act on it. This one hands the exception to
/// <see cref="ErrorReporter"/>, so the full-width toast carries the reference and the stack, and
/// offers the user a way back onto the page without losing their session.
/// </summary>
public sealed class ReportingErrorBoundary : ErrorBoundaryBase
{
    [Inject] private ErrorReporter Reporter { get; set; } = default!;

    /// <summary>
    /// Clear the error and re-render the page. ErrorBoundaryBase.Recover() is protected, so this is
    /// the way in for the router, which resets the boundary on every navigation — otherwise one
    /// broken page would keep showing its error screen on every page the user visited afterwards.
    /// </summary>
    public void Reset() => Recover();

    protected override Task OnErrorAsync(Exception exception)
    {
        Reporter.ReportUnhandled(exception, "Page render");
        return Task.CompletedTask;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CurrentException is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        if (ErrorContent is not null)
        {
            builder.AddContent(1, ErrorContent(CurrentException));
            return;
        }

        // The toast has already said what went wrong and holds the detail; this is only the way
        // back. Recover() re-renders the page from scratch, which is almost always enough.
        builder.OpenElement(2, "section");
        builder.AddAttribute(3, "class", "px-6 md:px-8 py-16 text-center");

        builder.OpenElement(4, "h1");
        builder.AddAttribute(5, "class", "text-2xl font-semibold text-content mb-2");
        builder.AddContent(6, "This page couldn't finish loading");
        builder.CloseElement();

        builder.OpenElement(7, "p");
        builder.AddAttribute(8, "class", "text-content-muted mb-6 max-w-md mx-auto leading-relaxed");
        builder.AddContent(9, "The details are in the red bar at the top of the screen — copy them and send them on, "
                             + "then try again. Nothing you had already saved is affected.");
        builder.CloseElement();

        builder.OpenElement(10, "button");
        builder.AddAttribute(11, "type", "button");
        builder.AddAttribute(12, "class",
            "rounded-lg bg-accent text-accent-ink font-medium px-4 py-2.5 hover:bg-accent-hover transition");
        builder.AddAttribute(13, "onclick", EventCallback.Factory.Create(this, Recover));
        builder.AddContent(14, "Try again");
        builder.CloseElement();

        builder.CloseElement();
    }
}
