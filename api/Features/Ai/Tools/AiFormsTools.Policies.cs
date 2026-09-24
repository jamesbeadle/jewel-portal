using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiFormsTools
{
    private static AiTool PolicySignOffsTool() => new(
        "list_policy_sign_offs",
        "Who has and hasn't signed each published policy revision (Internal > Policies): policyDocumentId, the title, the "
        + "revision, whether it is the current one, its declaration and whether a PDF is attached; then every person asked, "
        + "signed first or outstanding — policySignOffId, name, email, company, position, when and as whom they signed, and "
        + "whether they were asked by Policy sign-off link (formInviteId) or on their portal login. A new revision needs fresh "
        + "signatures, so only the current revision's outstanding people are worth chasing. send_form_invite with formSlug "
        + "policy-sign-off takes the policyDocumentId; chase_policy_sign_off takes the policySignOffId.",
        AiToolSchema.Object(
            ("policyDocumentId", "string", "One revision only. Left out, every revision.", false),
            ("currentOnly", "boolean", "true (the default) lists only each policy's current revision.", false)),
        AiToolKind.Read,
        RegisterRoleSets.PolicyReaders,
        PolicySignOffsAsync);

    private static async Task<string> PolicySignOffsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var policyDocumentId = AiToolSchema.Text(input, "policyDocumentId");
        var isCurrentOnly = AiToolSchema.Flag(input, "currentOnly") != false;
        var documents = await Query<ListPolicyDocuments, IReadOnlyList<PolicyDocument>>(context, new ListPolicyDocuments(), ct);
        var chosen = documents
            .Where(document => string.IsNullOrEmpty(policyDocumentId) || document.PolicyDocumentId == policyDocumentId)
            .Where(document => !string.IsNullOrEmpty(policyDocumentId) || !isCurrentOnly || document.IsActive)
            .ToList();
        var policies = new List<object>();
        foreach (var document in chosen) policies.Add(await PolicyRowAsync(context, document, ct));
        return Serialise(new { ok = true, policies });
    }

    private static async Task<object> PolicyRowAsync(AiToolContext context, PolicyDocument document, CancellationToken ct)
    {
        var signOffs = await Query<ListPolicySignOffs, IReadOnlyList<PolicySignOff>>(context, new ListPolicySignOffs(document.PolicyDocumentId), ct);
        return new
        {
            document.PolicyDocumentId, document.Title, document.Revision, isCurrent = document.IsActive,
            document.Declaration, document.HasFile, document.PublishedAt,
            signed = signOffs.Where(row => row.IsSigned).Select(SignOffRow),
            outstanding = signOffs.Where(row => !row.IsSigned).Select(SignOffRow)
        };
    }

    private static object SignOffRow(PolicySignOff row) => new
    {
        row.PolicySignOffId, name = row.RecipientName, email = row.RecipientEmail, company = row.CompanyName, row.Position,
        row.RequestedAt, row.SignedAt, row.SignedName, askedBy = row.IsByLink ? "link" : "portal login",
        row.FormInviteId, row.FormSubmissionId
    };
}
