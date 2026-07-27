using System.Text.Json;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Api.Features.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// Tools that reach into a single record's substance — its correspondence and its attachments — and
/// the one tool that writes back into a form the user has open.
///
/// <para>The attachment pair is deliberately a negotiation rather than a dump. Listing is cheap and
/// returns names only; the model decides what it actually needs and asks for that. Pushing every
/// email body and every attachment into the prompt would cost a fortune and bury the answer.</para>
/// </summary>
internal static class AiRecordTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "list_request_correspondence",
                "The emails and attachments on a request, as a list of headlines — sender, subject, date, "
                + "and the names of anything attached. Cheap. Call this FIRST when you need to know what "
                + "exists, then call get_request_context only if you actually need the wording.",
                AiToolSchema.Object(("requestId", "string", "The request's id, from list_requests or find_by_reference.", true)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var requestId = AiToolSchema.Text(input, "requestId");
                    if (string.IsNullOrWhiteSpace(requestId)) return Fail("A requestId is required.");

                    var request = await context.Db.Requests.AsNoTracking()
                        .FirstOrDefaultAsync(row => row.RequestId == requestId, ct);
                    if (request is null) return Fail($"No request found with id {requestId}.");

                    // Files and drawing links held against the record itself.
                    var attachments = await context.Db.RequestAttachments.AsNoTracking()
                        .Where(row => row.RequestId == requestId)
                        .Select(row => new
                        {
                            row.RequestAttachmentId,
                            kind = row.Kind == (int)RequestAttachmentKind.Drawing ? "drawing" : "file",
                            name = row.Kind == (int)RequestAttachmentKind.Drawing
                                ? (row.DrawingCode ?? "") + " " + (row.RevisionLabel ?? "")
                                : (row.FileName ?? ""),
                            row.ContentType,
                            row.Caption
                        })
                        .ToListAsync(ct);

                    // The live mailbox thread. Headlines only — HasAttachments is a flag, not names,
                    // because names cost a per-message Graph round trip.
                    var emails = Array.Empty<object>() as IReadOnlyList<object>;
                    try
                    {
                        var reader = context.Services.GetRequiredService<RequestEmailReader>();
                        var messages = await reader.ForRequestAsync(requestId, ct);
                        emails = messages
                            .Select(message => (object)new
                            {
                                from = string.IsNullOrWhiteSpace(message.FromName) ? message.FromEmail : message.FromName,
                                message.Subject,
                                received = message.ReceivedAt,
                                message.HasAttachments,
                                preview = message.BodyPreview
                            })
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        return Serialise(new
                        {
                            ok = true,
                            request = request.Reference,
                            attachments,
                            emails = Array.Empty<object>(),
                            note = $"The mailbox could not be read ({ex.Message}). The attachments listed are the "
                                   + "ones held on the record itself."
                        });
                    }

                    return Serialise(new
                    {
                        ok = true,
                        request = request.Reference,
                        request.Title,
                        attachments,
                        emails,
                        note = "Previews are truncated. Call get_request_context for the full wording."
                    });
                }),


        };
    }
}
