
namespace Jewel.JPMS.Features.Todos;

// The one shape every assignee picker shares: the option pool (roles first, then each role's
// directory holders indented beneath it), the SearchSelect value encoding, and the row/detail
// labels. An assignee is a ROLE, optionally pinned to a named holder (TodoAssignee) — the picker
// pool carries "Project Manager" and "Project Manager — Jane Doe" as sibling rows, and
// TodoAssigneeSelect shows that pool as TWO controls — pick the role, then optionally the person
// (2026-09-03: one flat list of "Role" / "Role — Name" rows read terribly). Built in one place so the project tab,
// the To-dos browser, the dashboard panel, the item's page and the triage form can never encode
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

    // The pool split the way TodoAssigneeSelect shows it: the ROLE rows (values without a pinned
    // person) and, for one role, its named holders with the "Role — " prefix stripped so the
    // person control reads "Nigel Reilly", not "Director / MD — Nigel Reilly" a second time.
    public static IReadOnlyList<SearchSelect.Option> RoleOptions(IReadOnlyList<SearchSelect.Option> pool) =>
        pool.Where(option => !option.Value.Contains(PersonSeparator)).ToList();

    public static IReadOnlyList<SearchSelect.Option> PeopleOptions(IReadOnlyList<SearchSelect.Option> pool, Role role)
    {
        var prefix = $"{(int)role}{PersonSeparator}";
        return pool
            .Where(option => option.Value.StartsWith(prefix, StringComparison.Ordinal))
            .Select(option => new SearchSelect.Option(option.Value, PersonLabel(option.Label, role)))
            .ToList();
    }

    private static string PersonLabel(string label, Role role)
    {
        var prefix = $"{role.DisplayName()} — ";
        return label.StartsWith(prefix, StringComparison.Ordinal) ? label[prefix.Length..] : label;
    }

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
