using Jewel.JPMS.Components;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Todos;

// The one shape every assignee picker shares: the option pool (roles first, then each role's
// directory holders indented beneath it), the SearchSelect value encoding, and the row/detail
// labels. An assignee is a ROLE, optionally pinned to a named holder (TodoAssignee) — the picker
// offers "Project Manager" (the pool) and "Project Manager — Jane Doe" (pinned) as sibling
// options, so pinning is one pick, not a second control. Built in one place so the project tab,
// the To-dos browser, the dashboard panel, the detail modal and the triage form can never encode
// or label an assignee differently.
public static class TodoAssigneePicker
{
    // SearchSelect option values: the role's int as a string ("3"), or role + pinned person
    // ("3|jane@jewelbb.co.uk"). "" = unassigned (SearchSelect's own blank/placeholder row).
    public const char PersonSeparator = '|';

    public static string Value(Role role, string? personEmail = null) =>
        personEmail is null ? ((int)role).ToString() : $"{(int)role}{PersonSeparator}{personEmail}";

    public static string Value(TodoAssignee? assignee) =>
        assignee is null ? "" : Value(assignee.Role, assignee.PersonEmail);

    public static string ValueFor(TodoItem item) =>
        item.AssigneeRole is Role role ? Value(role, item.AssigneePersonEmail) : "";

    public static TodoAssignee? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var separator = value.IndexOf(PersonSeparator);
        var rolePart = separator < 0 ? value : value[..separator];
        if (!int.TryParse(rolePart, out var parsed)) return null;
        var email = separator < 0 ? null : value[(separator + 1)..];
        return new TodoAssignee((Role)parsed, string.IsNullOrWhiteSpace(email) ? null : email);
    }

    // The combined option pool: each assignable role, followed by its holders ("Role — Person"),
    // so typing either the role or the person's name finds the row. People arrive already grouped
    // by role in picker order (ListTodoAssignablePeople), roles from ListTodoAssignableRoles.
    public static IReadOnlyList<SearchSelect.Option> BuildOptions(
        IReadOnlyList<Role> roles, IReadOnlyList<TodoAssignablePerson> people) =>
        roles.SelectMany(role =>
                new[] { new SearchSelect.Option(Value(role), role.DisplayName()) }
                    .Concat(people
                        .Where(person => person.Role == role)
                        .Select(person => new SearchSelect.Option(
                            Value(role, person.Email),
                            $"{role.DisplayName()} — {person.DisplayName}"))))
            .ToList();

    // The row/detail label for an item's assignee: "Project Manager", or
    // "Project Manager — Jane Doe" when pinned (falling back to the email if the pinned person's
    // directory row has gone before the pin was cleared).
    public static string? Label(TodoItem item) =>
        item.AssigneeRole is not Role role
            ? null
            : item.AssigneePersonEmail is null
                ? role.DisplayName()
                : $"{role.DisplayName()} — {item.AssigneePersonName ?? item.AssigneePersonEmail}";
}
