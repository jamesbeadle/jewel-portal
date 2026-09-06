using System.Net;
using System.Security.Cryptography;
using System.Text;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using Jewel.JPMS.Api.Features.Sales.Research;
using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// Everything the public imagine page can do, keyed by the lead's token and nothing else. The
/// token IS the authorisation: it was printed on one letter to one household, so whoever holds
/// it is that prospect (or someone they handed it to). Refusals throw InvalidOperationException
/// with a sentence the page shows as-is. Abuse limits are deliberately simple and all here: so
/// many rounds per lead, one render at a time per lead, so many submissions per connection per
/// hour, so many across the site per hour — a public upload endpoint with no login needs a
/// ceiling on what it can cost.
/// </summary>
public sealed class ImaginePublicService
{
    private const int MaxRoundsPerClientPerHour = 6;
    private const int MaxRoundsSiteWidePerHour = 60;

    private readonly JpmsContext context;
    private readonly IImagineImageStore store;
    private readonly IImagineRenderQueue queue;
    private readonly IImagineNotifier notifier;
    private readonly ILogger<ImaginePublicService> logger;

    public ImaginePublicService(
        JpmsContext context, IImagineImageStore store, IImagineRenderQueue queue,
        IImagineNotifier notifier, ILogger<ImaginePublicService> logger)
    {
        this.context = context;
        this.store = store;
        this.queue = queue;
        this.notifier = notifier;
        this.logger = logger;
    }

    /// <summary>SHA-256 of the caller's address — the per-connection throttle key.</summary>
    public static string ClientHash(HttpRequest request)
    {
        var forwarded = request.Headers["X-Forwarded-For"].ToString();
        var address = string.IsNullOrWhiteSpace(forwarded)
            ? request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : forwarded.Split(',')[0].Trim();
        // The SWA edge appends the port to the forwarded address.
        var colon = address.LastIndexOf(':');
        if (colon > 0 && address.Count(c => c == ':') == 1) address = address[..colon];
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address))).ToLowerInvariant();
    }

    public async Task<ImagineView?> GetAsync(string token, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct);
        return lead is null ? null : await ViewAsync(lead, ct);
    }

    public async Task<StoredBlob?> OpenImageAsync(string token, string imageId, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct);
        if (lead is null) return null;
        var image = await context.ImagineImages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ImageId == imageId && row.LeadId == lead.LeadId, ct);
        return image is null ? null : await store.OpenAsync(image.BlobRef, ct);
    }

    public async Task<ImagineView> SubmitAsync(string token, ImagineSubmission submission, string clientHash, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var brief = (submission.Brief ?? "").Trim();
        var email = (submission.Email ?? "").Trim();
        var name = (submission.Name ?? "").Trim();
        if (!submission.Consent) throw new InvalidOperationException("Please tick the box so we can email you your concepts.");
        if (!LooksLikeEmail(email)) throw new InvalidOperationException("Please give an email address we can send the concepts to.");
        if (brief.Length > ImagineLimits.MaxBriefLength) throw new InvalidOperationException($"Please keep the description under {ImagineLimits.MaxBriefLength} characters.");
        var photos = submission.Photos ?? Array.Empty<ImaginePhotoUpload>();
        if (photos.Count == 0) throw new InvalidOperationException("Add at least one photo of your house or plot.");
        if (photos.Count > ImagineLimits.MaxPhotosPerRound) throw new InvalidOperationException($"Up to {ImagineLimits.MaxPhotosPerRound} photos per round, please.");
        if (!store.IsConfigured) throw new InvalidOperationException("We can't take uploads just now — please try again later.");

        var decoded = photos.Select(Decode).ToList();
        var rounds = await context.ImagineRounds.Where(row => row.LeadId == lead.LeadId).ToListAsync(ct);
        await CheckLimitsAsync(rounds, clientHash, ct);

        var now = DateTimeOffset.UtcNow;
        var round = new ImagineRoundEntity
        {
            RoundId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Number = rounds.Count + 1,
            Kind = (int)ImagineRoundKind.Concepts,
            Brief = brief,
            Status = (int)ImagineRoundStatus.Queued,
            RequestedAt = now,
            ProspectName = Clip(name, 256),
            ProspectEmail = Clip(email, 256),
            ClientHash = clientHash
        };
        context.ImagineRounds.Add(round);

        var order = 0;
        foreach (var (bytes, contentType) in decoded)
        {
            order++;
            var imageId = Guid.NewGuid().ToString("N");
            var blobRef = await store.SaveAsync(lead.LeadId, round.RoundId, imageId, contentType, bytes, ct);
            context.ImagineImages.Add(new ImagineImageEntity
            {
                ImageId = imageId,
                LeadId = lead.LeadId,
                RoundId = round.RoundId,
                Kind = (int)ImagineImageKind.Photo,
                Order = order,
                Title = $"Photo {order}",
                BlobRef = blobRef,
                ContentType = contentType,
                Size = bytes.LongLength,
                CreatedAt = now
            });
        }

        // What they typed fills the gaps on the lead — a letter-found lead often has an address and
        // no name; now it has both, and an email to reply to.
        if (string.IsNullOrWhiteSpace(lead.ContactName) && name.Length > 0) lead.ContactName = Clip(name, 256);
        if (string.IsNullOrWhiteSpace(lead.ContactEmail)) lead.ContactEmail = Clip(email, 256);

        context.LeadActivities.Add(Activity(lead.LeadId,
            $"Imagine round {round.Number}: {(name.Length > 0 ? name : email)} uploaded {decoded.Count} {(decoded.Count == 1 ? "photo" : "photos")}"
            + (brief.Length > 0 ? $" — \"{Clip(brief, 300)}\"" : "") + "."));
        MoveToEngaged(lead, now);
        await context.SaveChangesAsync(ct);

        await EnqueueAsync(round, ct);
        await NotifySalesAsync(lead,
            $"Imagine: {(name.Length > 0 ? name : email)} uploaded photos ({lead.DisplayReference})",
            $"<p><strong>{WebUtility.HtmlEncode(name.Length > 0 ? name : email)}</strong> ({WebUtility.HtmlEncode(email)}) has uploaded {decoded.Count} photo(s) on the imagine page for {WebUtility.HtmlEncode(lead.DisplayReference)}"
            + (string.IsNullOrWhiteSpace(lead.SiteAddress) ? "" : $" — {WebUtility.HtmlEncode(lead.SiteAddress)}") + ".</p>"
            + (brief.Length > 0 ? $"<p>They wrote: <em>{WebUtility.HtmlEncode(brief)}</em></p>" : "")
            + "<p>The concepts are rendering; they'll be emailed the link when they're ready.</p>",
            $"{(name.Length > 0 ? name : email)} ({email}) uploaded {decoded.Count} photo(s) on the imagine page for {lead.DisplayReference}.\n\n{brief}", ct);

        return await ViewAsync(lead, ct);
    }

    public async Task<ImagineView> ReviseAsync(string token, ImagineRevisionRequest request, string clientHash, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var feedback = (request.Feedback ?? "").Trim();
        if (feedback.Length == 0) throw new InvalidOperationException("Tell us what you'd change — a sentence is plenty.");
        if (feedback.Length > ImagineLimits.MaxBriefLength) throw new InvalidOperationException($"Please keep it under {ImagineLimits.MaxBriefLength} characters.");
        var chosen = await context.ImagineImages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ImageId == request.ImageId && row.LeadId == lead.LeadId && row.Kind == (int)ImagineImageKind.Concept, ct)
            ?? throw new InvalidOperationException("Pick one of the concepts to build on.");

        var rounds = await context.ImagineRounds.Where(row => row.LeadId == lead.LeadId).ToListAsync(ct);
        await CheckLimitsAsync(rounds, clientHash, ct);
        var first = rounds.OrderBy(row => row.Number).First();

        var now = DateTimeOffset.UtcNow;
        var round = new ImagineRoundEntity
        {
            RoundId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Number = rounds.Count + 1,
            Kind = (int)ImagineRoundKind.Revision,
            Brief = feedback,
            BasedOnImageId = chosen.ImageId,
            Status = (int)ImagineRoundStatus.Queued,
            RequestedAt = now,
            ProspectName = first.ProspectName,
            ProspectEmail = first.ProspectEmail,
            ClientHash = clientHash
        };
        context.ImagineRounds.Add(round);
        context.LeadActivities.Add(Activity(lead.LeadId,
            $"Imagine round {round.Number}: asked for a revision of \"{chosen.Title}\" — \"{Clip(feedback, 300)}\"."));
        await context.SaveChangesAsync(ct);

        await EnqueueAsync(round, ct);
        await NotifySalesAsync(lead,
            $"Imagine: revision requested on \"{chosen.Title}\" ({lead.DisplayReference})",
            $"<p>The prospect on {WebUtility.HtmlEncode(lead.DisplayReference)} chose <strong>{WebUtility.HtmlEncode(chosen.Title)}</strong> and asked for a revision:</p><p><em>{WebUtility.HtmlEncode(feedback)}</em></p>",
            $"Revision requested on \"{chosen.Title}\" for {lead.DisplayReference}:\n\n{feedback}", ct);

        return await ViewAsync(lead, ct);
    }

    public async Task<ImagineView> ReactAsync(string token, ImagineReaction reaction, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var image = await context.ImagineImages
            .FirstOrDefaultAsync(row => row.ImageId == reaction.ImageId && row.LeadId == lead.LeadId && row.Kind == (int)ImagineImageKind.Concept, ct)
            ?? throw new InvalidOperationException("That concept isn't on this page.");
        var comment = (reaction.Comment ?? "").Trim();
        if (comment.Length > 2000) throw new InvalidOperationException("Please keep the comment under 2000 characters.");
        var changed = image.Liked != reaction.Liked || image.Comment != comment;
        image.Liked = reaction.Liked;
        image.Comment = comment;
        if (changed)
        {
            context.LeadActivities.Add(Activity(lead.LeadId,
                (reaction.Liked ? $"Liked \"{image.Title}\"" : $"Un-liked \"{image.Title}\"")
                + (comment.Length > 0 ? $" — \"{Clip(comment, 300)}\"" : "") + "."));
        }
        await context.SaveChangesAsync(ct);
        return await ViewAsync(lead, ct);
    }

    public async Task<ImagineView> AcceptProposalAsync(string token, ProposalAcceptance acceptance, string clientHash, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var proposal = await context.SalesProposals
            .FirstOrDefaultAsync(row => row.ProposalId == acceptance.ProposalId && row.LeadId == lead.LeadId, ct)
            ?? throw new InvalidOperationException("That proposal isn't on this page.");
        if (proposal.Status == (int)SalesProposalStatus.Accepted) return await ViewAsync(lead, ct);
        if (proposal.Status != (int)SalesProposalStatus.Sent)
            throw new InvalidOperationException("This proposal has been replaced — please use the latest one, or reply to our email.");
        if (!acceptance.AgreedToTerms) throw new InvalidOperationException("Please confirm you've read and agree to the terms.");
        var name = (acceptance.Name ?? "").Trim();
        var email = (acceptance.Email ?? "").Trim();
        if (name.Length == 0) throw new InvalidOperationException("Please give your full name — it goes on the contract.");
        if (!LooksLikeEmail(email)) throw new InvalidOperationException("Please give a valid email address.");

        var valid = new HashSet<string>(proposal.Options().Select(option => option.OptionId), StringComparer.Ordinal);
        var chosen = (acceptance.OptionIds ?? Array.Empty<string>()).Where(valid.Contains).Distinct(StringComparer.Ordinal).ToList();
        var now = DateTimeOffset.UtcNow;
        proposal.Status = (int)SalesProposalStatus.Accepted;
        proposal.AcceptedAt = now;
        proposal.AcceptedByName = Clip(name, 256);
        proposal.AcceptedByEmail = Clip(email, 256);
        proposal.AcceptedOptionIdsJson = ProposalMapping.ToJson(chosen);
        proposal.AcceptedPrice = ProposalMapping.PriceFor(proposal, chosen);
        proposal.AcceptedClientHash = clientHash;
        proposal.UpdatedAt = now;

        var optionNames = proposal.Options().Where(option => chosen.Contains(option.OptionId)).Select(option => option.Name).ToList();
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Kind = (int)LeadActivityKind.ProposalAccepted,
            Summary = $"Proposal v{proposal.Version} \"{proposal.Title}\" accepted by {name} ({email}) at £{proposal.AcceptedPrice:N0}"
                + (optionNames.Count > 0 ? $" with {string.Join(", ", optionNames)}" : " with no options") + ". Ready to mark Won.",
            OccurredAt = now,
            RecordedByEmail = email
        });
        await context.SaveChangesAsync(ct);

        await NotifySalesAsync(lead,
            $"ACCEPTED: {proposal.Title} — {lead.DisplayReference}",
            $"<p><strong>{WebUtility.HtmlEncode(name)}</strong> ({WebUtility.HtmlEncode(email)}) accepted proposal v{proposal.Version} <strong>{WebUtility.HtmlEncode(proposal.Title)}</strong> at <strong>£{proposal.AcceptedPrice:N0}</strong>"
            + (optionNames.Count > 0 ? $" with {WebUtility.HtmlEncode(string.Join(", ", optionNames))}" : " with no options")
            + $".</p><p>Open {WebUtility.HtmlEncode(lead.DisplayReference)} in the portal and mark it Won to create the client and the project.</p>",
            $"{name} ({email}) accepted proposal v{proposal.Version} \"{proposal.Title}\" at £{proposal.AcceptedPrice:N0}. Open {lead.DisplayReference} and mark it Won.", ct);

        return await ViewAsync(lead, ct);
    }

    public async Task<ImagineView> DeclineProposalAsync(string token, ProposalDecline decline, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var proposal = await context.SalesProposals
            .FirstOrDefaultAsync(row => row.ProposalId == decline.ProposalId && row.LeadId == lead.LeadId, ct)
            ?? throw new InvalidOperationException("That proposal isn't on this page.");
        if (proposal.Status != (int)SalesProposalStatus.Sent) return await ViewAsync(lead, ct);
        var reason = Clip((decline.Reason ?? "").Trim(), 1024);
        var now = DateTimeOffset.UtcNow;
        proposal.Status = (int)SalesProposalStatus.Declined;
        proposal.DeclinedAt = now;
        proposal.DeclineReason = reason.Length == 0 ? null : reason;
        proposal.UpdatedAt = now;
        context.LeadActivities.Add(Activity(lead.LeadId,
            $"Proposal v{proposal.Version} \"{proposal.Title}\" declined" + (reason.Length > 0 ? $" — \"{reason}\"" : "") + "."));
        await context.SaveChangesAsync(ct);
        await NotifySalesAsync(lead,
            $"Declined: {proposal.Title} — {lead.DisplayReference}",
            $"<p>The prospect on {WebUtility.HtmlEncode(lead.DisplayReference)} declined proposal v{proposal.Version}.</p>" + (reason.Length > 0 ? $"<p><em>{WebUtility.HtmlEncode(reason)}</em></p>" : ""),
            $"Proposal v{proposal.Version} declined on {lead.DisplayReference}.\n\n{reason}", ct);
        return await ViewAsync(lead, ct);
    }

    // ---- helpers ----------------------------------------------------------------------------

    private Task<LeadEntity?> FindLeadAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64) return Task.FromResult<LeadEntity?>(null);
        return context.Leads.FirstOrDefaultAsync(row => row.ImagineToken == token, ct);
    }

    private async Task<ImagineView> ViewAsync(LeadEntity lead, CancellationToken ct)
    {
        var rounds = await ImagineMapping.RoundsForLeadAsync(context, lead.LeadId, ct);
        var proposals = await context.SalesProposals.AsNoTracking().Where(row => row.LeadId == lead.LeadId).ToListAsync(ct);
        var live = ProposalMapping.Live(proposals);
        var firstName = FirstName(lead.ContactName);
        return new ImagineView(
            firstName,
            lead.ContactEmail,
            rounds,
            Math.Max(0, ImagineLimits.MaxRoundsPerLead - rounds.Count),
            ImagineLimits.MaxPhotosPerRound,
            live?.ToView());
    }

    private async Task CheckLimitsAsync(List<ImagineRoundEntity> rounds, string clientHash, CancellationToken ct)
    {
        if (rounds.Count >= ImagineLimits.MaxRoundsPerLead)
            throw new InvalidOperationException("You've used all the rounds on this page — reply to our email and we'll carry on together.");
        if (rounds.Any(row => ((ImagineRoundStatus)row.Status).IsInProgress()))
            throw new InvalidOperationException("Your last round is still rendering — give it a few minutes.");
        if (!queue.IsConfigured)
            throw new InvalidOperationException("We can't render just now — please try again later.");
        var hourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var fromClient = await context.ImagineRounds.CountAsync(row => row.ClientHash == clientHash && row.RequestedAt > hourAgo, ct);
        if (fromClient >= MaxRoundsPerClientPerHour)
            throw new InvalidOperationException("That's a lot of uploads in an hour — please try again a little later.");
        var siteWide = await context.ImagineRounds.CountAsync(row => row.RequestedAt > hourAgo, ct);
        if (siteWide >= MaxRoundsSiteWidePerHour)
            throw new InvalidOperationException("We're busy rendering just now — please try again in an hour.");
    }

    private async Task EnqueueAsync(ImagineRoundEntity round, CancellationToken ct)
    {
        try
        {
            await queue.EnqueueAsync(new ImagineRenderMessage(round.RoundId), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The submission is saved and the lead page can retry it; the prospect sees "failed".
            logger.LogError(ex, "Imagine: could not queue round {RoundId}.", round.RoundId);
            round.Status = (int)ImagineRoundStatus.Failed;
            round.Error = Clip("Couldn't queue the render: " + ex.Message, 2000);
            await context.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task NotifySalesAsync(LeadEntity lead, string subject, string html, string text, CancellationToken ct)
    {
        if (!notifier.IsConfigured) return;
        try { await notifier.SendToSalesAsync(subject, html, text, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Imagine: the note to the sales mailbox about {Lead} failed.", lead.DisplayReference);
        }
    }

    private static void MoveToEngaged(LeadEntity lead, DateTimeOffset now)
    {
        var stage = (LeadStage)lead.Stage;
        if (!stage.IsOpen() || stage >= LeadStage.Engaged) return;
        lead.Stage = (int)LeadStage.Engaged;
        lead.StageChangedAt = now;
        lead.LostReason = null;
    }

    private static (byte[] Bytes, string ContentType) Decode(ImaginePhotoUpload upload)
    {
        var data = upload.Base64 ?? "";
        var comma = data.IndexOf(',');
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0) data = data[(comma + 1)..];
        byte[] bytes;
        try { bytes = Convert.FromBase64String(data); }
        catch (FormatException) { throw new InvalidOperationException($"{upload.FileName} couldn't be read — please try that photo again."); }
        if (bytes.Length < 64) throw new InvalidOperationException($"{upload.FileName} is empty.");
        if (bytes.Length > ImagineLimits.MaxPhotoBytes)
            throw new InvalidOperationException($"{upload.FileName} is too large after resizing — please try a smaller photo.");
        var contentType = Sniff(bytes) ?? throw new InvalidOperationException($"{upload.FileName} isn't a JPEG, PNG or WebP photo.");
        return (bytes, contentType);
    }

    /// <summary>The type from the bytes, never from the client's word for it.</summary>
    private static string? Sniff(byte[] bytes)
    {
        if (bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
        if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
        if (bytes.Length > 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return "image/webp";
        return null;
    }

    private static bool LooksLikeEmail(string value) =>
        value.Length is > 5 and <= 256 && value.IndexOf('@') > 0 && value.LastIndexOf('.') > value.IndexOf('@') && !value.Any(char.IsWhiteSpace);

    private static string FirstName(string contactName)
    {
        var name = (contactName ?? "").Trim();
        if (name.Length == 0) return "";
        // "Mr & Mrs Harding" → "Mr & Mrs Harding"; "Sarah Harding" → "Sarah".
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && !name.Contains('&') && !IsTitle(parts[0]) ? parts[0] : name;
    }

    private static bool IsTitle(string word) =>
        word.TrimEnd('.').ToLowerInvariant() is "mr" or "mrs" or "ms" or "miss" or "dr" or "sir" or "lady" or "lord";

    private static LeadActivityEntity Activity(string leadId, string summary) => new()
    {
        LeadActivityId = Guid.NewGuid().ToString("N"),
        LeadId = leadId,
        Kind = (int)LeadActivityKind.Imagine,
        Summary = Clip(summary, 4000),
        OccurredAt = DateTimeOffset.UtcNow,
        RecordedByEmail = "imagine@jpms"
    };

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}
