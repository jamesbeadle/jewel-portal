using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Registers.Policies;
using Jewel.JPMS.Api.Storage;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

/// <summary>The policy a link opens: the revision to show, or why it cannot be signed.</summary>
internal sealed record LinkedPolicy(PolicyDocumentEntity? Policy, PublicPolicy? Shown, FormLinkProblem? Problem)
{
    public static readonly LinkedPolicy None = new(null, null, null);
}

public sealed partial class PublicFormService
{
    /// <summary>
    /// The policy revision a sign-off link was sent for. A link with no policy behind it is not valid;
    /// a revision superseded since the link went says so, because a new revision needs fresh signatures.
    /// </summary>
    private async Task<LinkedPolicy> PolicyOfAsync(FormDefinition form, ResolvedLink link, CancellationToken cancellationToken)
    {
        if (!form.IsSentByLinkOnly) return LinkedPolicy.None;
        var policy = await PolicySignOffSigning.PolicyOfAsync(context, link.Invite, cancellationToken);
        if (policy is null) return new LinkedPolicy(null, null, FormLinkProblem.NotValid);
        if (!policy.IsActive) return new LinkedPolicy(policy, null, FormLinkProblem.PolicySuperseded);
        var facts = PolicySignOffSigning.FactsOf(policy);
        var shown = new PublicPolicy(policy.Title, policy.Revision, facts.Declaration, policy.FileBlobRef.Length > 0);
        return new LinkedPolicy(policy, shown, null);
    }

    /// <summary>A link-only form sent without its link, or for a revision no longer signable, is refused with the page's own words.</summary>
    private async Task<PolicyDocumentEntity?> PolicyOnSendingAsync(FormDefinition form, ResolvedLink? link, CancellationToken cancellationToken)
    {
        if (!form.IsSentByLinkOnly) return null;
        var linked = link is null ? new LinkedPolicy(null, null, FormLinkProblem.NotValid) : await PolicyOfAsync(form, link, cancellationToken);
        if (linked.Problem is not { } problem) return linked.Policy;
        var dead = FormWording.DeadLink(problem);
        throw new PublicFormRefusal($"{dead.Heading}. {dead.Body}");
    }

    /// <summary>
    /// The policy's PDF, to a person holding a live sign-off link for it (or one already used, so the
    /// copy they signed stays readable to them). Anything else is nothing at all.
    /// </summary>
    public async Task<StoredBlob?> OpenPolicyFileAsync(string slug, string? inviteToken, string? packToken, CancellationToken cancellationToken)
    {
        var form = FormCatalogue.For(slug);
        if (form is not { IsSentByLinkOnly: true }) return null;
        var link = await ResolveAsync(form.Slug, inviteToken, packToken, cancellationToken);
        var isReadable = link.IsOpen || FormLinkResolution.IsHonouredOnSending(link) || link.Problem == FormLinkProblem.Used;
        if (!isReadable) return null;
        var policy = await PolicySignOffSigning.PolicyOfAsync(context, link.Invite, cancellationToken);
        return policy is null ? null : await PolicyFiles.OpenAsync(store, policy, cancellationToken);
    }
}
