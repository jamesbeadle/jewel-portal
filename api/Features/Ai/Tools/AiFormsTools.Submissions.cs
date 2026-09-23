using Jewel.JPMS.Api.Features.Forms.Office.Submissions;
using Jewel.JPMS.Contracts.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiFormsTools
{
    private static AiTool ReceivedFormsTool() => new(
        "list_form_submissions",
        "Every form that came in, newest first — the office's Received list: formSubmissionId, "
        + "formSlug and title, who sent it (submitterName, from the one-time link when there was one), the person or "
        + "company it is filed under (filingName, formFolderId), whether it came by a verified link, the pack it belongs to, "
        + "and status New / InProgress / Handled / Destroyed. No answers: get_form_submission reads one. A site "
        + "manager or the H&S officer sees the health and safety forms alone — the site checks and incident reports.",
        AiToolSchema.Object(
            ("status", "string", "Narrow to New, InProgress, Handled or Destroyed.", false),
            ("formSlug", "string", "Narrow to one form: starter, emergency, rtw, dse, vehicle, training, accident, subcontractor, insurance, "
                + "toolbox-talk, ladder-inspection, equipment-schedule, puwer-inspection, site-incident, personnel-incident, first-aid-kit, fire-extinguishers.", false)),
        AiToolKind.Read,
        FormRoleSets.AnyReader,
        ReceivedFormsAsync);

    private static async Task<string> ReceivedFormsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var status = AiToolSchema.Text(input, "status");
        var formSlug = AiToolSchema.Text(input, "formSlug");
        var submissions = await Query<ListFormSubmissions, IReadOnlyList<FormSubmission>>(context, new ListFormSubmissions(), ct);
        var shown = FormSubmissionReading.VisibleTo(context.User, submissions)
            .Where(submission => status is null || string.Equals(submission.Status.ToString(), status, StringComparison.OrdinalIgnoreCase))
            .Where(submission => formSlug is null || submission.FormSlug == formSlug)
            .Select(AiFormReading.SubmissionRow);
        return Serialise(new { ok = true, submissions = shown });
    }

    private static AiTool OneFormTool() => new(
        "get_form_submission",
        "One form that came in, read in the form's own order: every question with its answer, the files with the "
        + "formUploadId file_form_to_directory takes, and what the office does next with this kind of form — "
        + "filingSuggestions for a questionnaire or insurance update, trainingSuggestion for a training certificate, "
        + "checkCodeDaysLeft for a company vehicle form. Health answers and the answers the portal treats as sensitive "
        + "(NI number, UTR, date of birth, driving record) are withheld here and read on the page.",
        AiToolSchema.Object(
            ("formSubmissionId", "string", "From list_form_submissions.", true)),
        AiToolKind.Read,
        FormRoleSets.AnyReader,
        OneFormAsync);

    private static async Task<string> OneFormAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var formSubmissionId = AiToolSchema.Text(input, "formSubmissionId");
        if (string.IsNullOrWhiteSpace(formSubmissionId)) return Fail("Pass formSubmissionId (list_form_submissions returns ids).");
        var access = context.Services.GetRequiredService<FormSubmissionAccess>();
        var mayRead = await access.MayReadAsync(context.User, formSubmissionId, ct);
        if (!mayRead) return Fail("That form is kept in a store your role does not read, or it no longer exists.");
        var view = await Query<OpenFormSubmission, FormSubmissionView>(context, new OpenFormSubmission(formSubmissionId), ct);
        return Serialise(AiFormReading.Of(view, DateTimeOffset.UtcNow));
    }
}
