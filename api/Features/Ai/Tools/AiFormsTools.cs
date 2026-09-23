using System.Text.Json.Serialization;
using Jewel.JPMS.Contracts.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The forms' read surface (ported from Jeremy's forms dashboard, 2026-09-23): each tool wraps the same query
/// handler its /forms endpoint composes and mirrors that endpoint's gate, and every row carries the id
/// its action takes. Two things stay on the page: an emergency form's health answers (revealed there,
/// each look on the audit trail — RevealHealthAnswers has no tool on purpose), and the answers
/// SensitiveAnswers names — NI numbers, UTRs, dates of birth, driving records — which the office's
/// work over the connector never needs.
/// </summary>
internal static partial class AiFormsTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false, Converters = { new JsonStringEnumConverter() } };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);

    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    private static Task<TResult> Query<TQuery, TResult>(AiToolContext context, TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult> =>
        context.Services.GetRequiredService<IQueryHandler<TQuery, TResult>>().HandleAsync(query, ct);

    public static IReadOnlyList<AiTool> Build() => new[]
    {
        ReceivedFormsTool(),
        OneFormTool(),
        PacksTool(),
        InvitesTool(),
        FoldersTool(),
        RightToWorkTool(),
        TrainingTool(),
        WorkstationsTool(),
        LicenceChecksTool(),
        EmergencyContactsTool()
    };
}
