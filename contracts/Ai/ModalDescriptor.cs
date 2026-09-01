using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// One field of a registered dialog, described for the MODEL rather than for a developer. The
/// description is prompt text: it is the only place the model learns the house rules for that field,
/// so state the constraint and the reason, and say what NOT to do.
/// </summary>
public sealed record ModalField(
    string Name,
    /// <summary>JSON Schema type: "string", "number", "boolean" or "array".</summary>
    string Type,
    string Description,
    bool Required = false,
    /// <summary>For "array" fields: the shape of one item. Null for a bare list of scalars.</summary>
    IReadOnlyList<ModalField>? ItemFields = null);

/// <summary>
/// A dialog the assistant is allowed to open and fill in — docs/ai/00-agent-architecture.md §5
/// (ADR-003). Registering one here is the explicit opt-in that makes it reachable; the registry is
/// never derived from the component tree, because "every dialog in the app" is not a capability
/// anybody chose to grant.
///
/// <para>Filling a dialog writes NOTHING. It puts values on the user's own screen, in the form they
/// already know, and they press the button. The dialog is the proposal card §4 asks for and its
/// confirm button is the approval step, which is why these are <c>Ui</c> tools and not writes.</para>
/// </summary>
public sealed record ModalDescriptor(
    /// <summary>snake_case, what the model sees and what the client switches on.</summary>
    string ModalKey,
    /// <summary>The dialog's own title, exactly as the user reads it on screen.</summary>
    string DisplayName,
    /// <summary>One or two clauses telling the model what this dialog is for.</summary>
    string Purpose,
    /// <summary>Where it can be opened. <c>{project}</c> and <c>{record}</c> are substituted.</summary>
    string RouteTemplate,
    IReadOnlyList<Role> OpenableBy,
    IReadOnlyList<ModalField> Fields);

