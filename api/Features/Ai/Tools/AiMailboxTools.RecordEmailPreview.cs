using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Contracts.MailboxCompose;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiMailboxTools
{
    /// <summary>Reading a record's email before it goes. VisibleTo is the union of the four send
    /// gates; the per-record gate is checked inside, against the composer's own set, so previewing
    /// is allowed exactly where sending is.</summary>
    public static IReadOnlyList<AiTool> RecordEmailPreviewTools() => new List<AiTool>
    {
        new(
            "preview_record_email",
            "READS, SENDS NOTHING: what a record's email will say before anyone sends it — the "
            + "subject, the body the recipient will read, who it is addressed to and copied, and "
            + "what rides with it by name and size. Covers the four records whose email the portal "
            + "writes for them: a request document (RFI/NOD/EOT), a valuation report statement, a "
            + "subcontractor statement of account and a variation order. It is the SAME composition "
            + "the matching send door uses, so what you read here is what would go out. Read this "
            + "before offering to send one, and quote the subject and recipients back to the user.",
            AiToolSchema.Object(
                ("record", "string",
                    "request, valuationReportSnapshot, subcontractorComms (a statement of account) "
                    + "or variation.", true),
                ("recordId", "string",
                    "The record's id — from find_by_reference, or the subcontractorId for a statement.", true),
                ("recipientOverride", "string",
                    "Preview it addressed to this one address instead of the resolved contacts.", false),
                ("subject", "string",
                    "A subject you are proposing. Leave out to see the one the portal writes.", false),
                ("bodyHtml", "string",
                    "A body you are proposing. Leave out to see the one the portal writes.", false)),
            AiToolKind.Read,
            JpmsRoleSets.InternalAndArchitect,
            async (context, input, ct) =>
            {
                var recordName = AiToolSchema.Text(input, "record");
                if (!Enum.TryParse<RecordType>(recordName, ignoreCase: true, out var record))
                    return Fail($"'{recordName}' is not a kind of record.");

                var recordId = AiToolSchema.Text(input, "recordId");
                if (string.IsNullOrWhiteSpace(recordId)) return Fail("A recordId is required.");

                var composers = context.Services.GetRequiredService<RecordEmailComposers>();
                var composer = composers.Find(record);
                if (composer is null)
                    return Fail(
                        $"The portal doesn't compose an email for a {record}, so there is nothing to "
                        + $"preview. It writes one for: {string.Join(", ", composers.Records)}.");
                if (!composer.RolesThatMaySend.IncludesAny(context.User.Roles))
                    return Fail($"Your role cannot send a {record} email, so it cannot read one either.");

                var handler = context.Services
                    .GetRequiredService<IQueryHandler<PreviewRecordEmail, RecordEmailPreview>>();
                var preview = await handler.HandleAsync(
                    new PreviewRecordEmail(
                        record, recordId,
                        AiToolSchema.Text(input, "recipientOverride"),
                        AiToolSchema.Text(input, "subject"),
                        AiToolSchema.Text(input, "bodyHtml")),
                    ct);

                return Serialise(new { ok = true, preview });
            })
    };
}
