using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Ai;

/// <summary>
/// The one form the user currently has open, and the channel the assistant fills it through.
///
/// <para>Scoped, and deliberately single-slot: a user has one dialog open at a time, and a registry
/// that tracked several would immediately raise the question of which one "this form" means. The
/// last form to register wins; closing it clears the slot only if it is still the one registered,
/// so a dialog disposing after another has opened cannot blank the newer one.</para>
///
/// <para>The assistant never submits. It writes values into the boxes and the person presses the
/// button — which is why <see cref="Fill"/> raises an event the form handles rather than invoking a
/// command.</para>
/// </summary>
public sealed class ActiveFormContext
{
    private string? currentKey;
    private Func<AiFormSnapshot>? read;
    private Action<IReadOnlyDictionary<string, string>>? apply;

    /// <summary>Raised after values are applied, so the owning form can re-render.</summary>
    public event Action? OnChange;

    /// <summary>A snapshot of the open form, or null when none is open.</summary>
    public AiFormSnapshot? Current => read?.Invoke();

    /// <summary>
    /// Called by a form when it opens. <paramref name="read"/> is invoked at send time so the
    /// assistant always sees the values as they are now, not as they were when the form opened.
    /// </summary>
    public void Register(
        string formKey,
        Func<AiFormSnapshot> read,
        Action<IReadOnlyDictionary<string, string>> apply)
    {
        currentKey = formKey;
        this.read = read;
        this.apply = apply;
        OnChange?.Invoke();
    }

    /// <summary>Called by a form when it closes. A no-op if another form has since registered.</summary>
    public void Release(string formKey)
    {
        if (!string.Equals(currentKey, formKey, StringComparison.Ordinal)) return;
        currentKey = null;
        read = null;
        apply = null;
        OnChange?.Invoke();
    }

    /// <summary>Applies values the assistant proposed. Unknown field names are the form's to ignore.</summary>
    public void Fill(IReadOnlyDictionary<string, string> values)
    {
        if (apply is null || values.Count == 0) return;
        apply(values);
        OnChange?.Invoke();
    }
}
