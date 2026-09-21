using Jewel.JPMS.Contracts.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The subject-access reading over the connector: everything the portal holds against one email
/// (Admin → Data protection). The same query the page reads, behind the same admin gate; the
/// answer is the export a person is sent when they ask, and the reading taken before
/// anonymise_person.
/// </summary>
internal static class AiDataProtectionTools
{
    public const string GetPersonDossierTool = "get_person_dossier";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new AiTool[]
    {
        new(
            GetPersonDossierTool,
            "Everything the portal holds against one email address — the answer to a subject "
            + "access request and the reading before anonymise_person. records[] are the rows that "
            + "ARE the person (a client's primary contact, a person at an architect or a directory "
            + "record, a project contact, a sales lead, imagine rounds, a proposal they accepted, "
            + "messages they wrote, a KPI register entry, a worker), each with its details as "
            + "label/value fields; mentions[] are every column that merely names them — raised-by "
            + "and approved-by stamps, the audit actor — counted per table and column. "
            + "hasALiveLogin says a portal sign-in still carries the address, which anonymising "
            + "waits for. Administrators and the finance director.",
            AiToolSchema.Object(
                ("email", "string", "The person's email address, exactly as the portal holds it.", true)),
            AiToolKind.Read,
            JpmsRoleSets.AdministratorsAndFinanceDirector,
            ReadDossierAsync)
    };

    private static async Task<string> ReadDossierAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var email = AiToolSchema.Text(input, "email")?.Trim();
        if (string.IsNullOrWhiteSpace(email)) return Fail("email is required.");
        var dossier = await context.Services
            .GetRequiredService<IQueryHandler<GetPersonDossier, PersonDossier>>()
            .HandleAsync(new GetPersonDossier(email), ct);
        return Serialise(new
        {
            ok = true,
            dossier.Email,
            dossier.HasALiveLogin,
            recordCount = dossier.Records.Count,
            mentionCount = dossier.MentionCount,
            records = dossier.Records.Select(record => new
            {
                record.Kind, record.RecordId, record.Title,
                fields = record.Fields.Select(field => new { field.Label, field.Value })
            }),
            mentions = dossier.Mentions,
            note = dossier.IsEmpty
                ? "Nothing holds this address."
                : "Erasure is anonymise_person (confirm-first). A live sign-in is removed with "
                  + "remove_directory_user then delete_directory_user; a worker with history is "
                  + "retired with retire_worker first. Route: /admin/data-protection."
        });
    }
}
