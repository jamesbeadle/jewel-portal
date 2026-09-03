using Jewel.JPMS.Api.Features.Kpi;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The KPI register over the connector (2026-09-03): emails an administrator has marked as a KPI
/// against a person at Jewel — a portal user, or someone added by name who has no login.
/// ADMINISTRATORS ONLY — both tools are filtered out of every other caller's catalogue (ADR-002:
/// never described to someone it would refuse), and each re-checks the role because the
/// catalogue filter is a courtesy, not the gate. The writes are actions: mark_email_as_kpi,
/// update_kpi_email, remove_kpi_email, add_kpi_person (KpiActions).
/// </summary>
internal static class AiKpiTools
{
    public const string ListKpiEmails = "list_kpi_emails";
    public const string ListKpiPeople = "list_kpi_people";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new AiTool[]
    {
        new(
            ListKpiPeople,
            "The people KPIs are filed under (administrators only): everyone at Jewel who has "
            + "been filed against or added by name — portal users (with their sign-in email) and "
            + "people without a login alike — each with their KPI count and the personId the "
            + "mark/update actions and list_kpi_emails take. Someone not on the list yet can "
            + "still be filed against: mark_email_as_kpi with personEmail (a portal user from "
            + "list_portal_users) or personName (anyone else) adds them on the spot, or "
            + "add_kpi_person adds them first.",
            AiToolSchema.Empty(),
            AiToolKind.Read,
            KpiRoles.Administrators,
            async (context, _, ct) =>
            {
                if (!KpiRoles.IsAdministrator(context.User))
                    return Fail("The KPI register is for administrators only.");
                var counts = (await context.Db.KpiEmails.AsNoTracking()
                        .GroupBy(row => row.PersonId)
                        .Select(group => new { PersonId = group.Key, Count = group.Count() })
                        .ToListAsync(ct))
                    .ToDictionary(row => row.PersonId, row => row.Count);
                var people = await context.Db.KpiPeople.AsNoTracking().ToListAsync(ct);
                return Serialise(new
                {
                    ok = true,
                    count = people.Count,
                    people = people
                        .OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(person => new
                        {
                            personId = person.KpiPersonId,
                            name = person.Name,
                            email = person.Email,
                            portalUser = person.Email is not null,
                            kpiCount = counts.GetValueOrDefault(person.KpiPersonId)
                        })
                });
            }),

        new(
            ListKpiEmails,
            "The KPI register (administrators only): every email marked as a KPI against a "
            + "person at Jewel, newest-marked first — who it is filed under, the email's subject, "
            + "sender and received date, the administrator's note, who marked it and when, and "
            + "its KPI-#### reference. Filter to one person with personId (list_kpi_people) or "
            + "personEmail (a portal sign-in address). Nothing here is visible to anyone but "
            + "administrators — never repeat its contents to, or in a draft for, anyone else. To "
            + "read the email itself, call get_mailbox_message with the row's messageId "
            + "(internetMessageId as the fallback). To mark, re-file or unmark, use the "
            + "mark_email_as_kpi / update_kpi_email / remove_kpi_email actions.",
            AiToolSchema.Object(
                ("personId", "string", "Only this person's KPIs — a personId from list_kpi_people.", false),
                ("personEmail", "string", "Only this portal user's KPIs — their sign-in email.", false),
                ("search", "string", "Case-insensitive match on subject, sender, person or note.", false)),
            AiToolKind.Read,
            KpiRoles.Administrators,
            async (context, input, ct) =>
            {
                if (!KpiRoles.IsAdministrator(context.User))
                    return Fail("The KPI register is for administrators only.");

                var personId = AiToolSchema.Text(input, "personId")?.Trim();
                var personEmail = AiToolSchema.Text(input, "personEmail")?.Trim();
                var search = AiToolSchema.Text(input, "search")?.Trim();

                var people = await context.Db.KpiPeople.AsNoTracking().ToDictionaryAsync(row => row.KpiPersonId, ct);
                if (string.IsNullOrWhiteSpace(personId) && !string.IsNullOrWhiteSpace(personEmail))
                {
                    personId = people.Values
                        .FirstOrDefault(person => string.Equals(person.Email, personEmail, StringComparison.OrdinalIgnoreCase))
                        ?.KpiPersonId;
                    if (personId is null)
                        return Serialise(new { ok = true, count = 0, totalMatching = 0, rows = Array.Empty<object>(), note = $"No KPIs are filed under {personEmail} yet." });
                }

                var rows = context.Db.KpiEmails.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(personId))
                    rows = rows.Where(row => row.PersonId == personId);
                var items = await rows.OrderByDescending(row => row.MarkedAt).ToListAsync(ct);
                var total = items.Count;

                string NameOf(string id) => people.TryGetValue(id, out var person) ? person.Name : "(person removed)";
                string? EmailOf(string id) => people.TryGetValue(id, out var person) ? person.Email : null;

                if (!string.IsNullOrWhiteSpace(search))
                    items = items.Where(row =>
                            row.Subject.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || row.FromEmail.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || row.FromName.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || row.Note.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || NameOf(row.PersonId).Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                var byPerson = items.GroupBy(row => row.PersonId)
                    .Select(group => new { personId = group.Key, person = NameOf(group.Key), email = EmailOf(group.Key), count = group.Count() })
                    .OrderByDescending(group => group.count)
                    .ToList();

                return Serialise(new
                {
                    ok = true,
                    count = items.Count,
                    totalMatching = total,
                    byPerson,
                    rows = items.Select(row => new
                    {
                        reference = row.Reference,
                        kpiEmailId = row.KpiEmailId,
                        personId = row.PersonId,
                        person = NameOf(row.PersonId),
                        personEmail = EmailOf(row.PersonId),
                        subject = row.Subject,
                        from = string.IsNullOrWhiteSpace(row.FromName) ? row.FromEmail : $"{row.FromName} <{row.FromEmail}>",
                        receivedAt = row.ReceivedAt,
                        note = string.IsNullOrWhiteSpace(row.Note) ? null : row.Note,
                        markedBy = row.MarkedByEmail,
                        markedAt = row.MarkedAt,
                        messageId = row.MessageId,
                        internetMessageId = row.InternetMessageId,
                        route = "/admin/kpis"
                    }),
                    note = "Administrators only. Read an email with get_mailbox_message(messageId, "
                        + "internetMessageId). Re-file or annotate with update_kpi_email; unmark "
                        + "with remove_kpi_email (confirm-first)."
                });
            })
    };
}
