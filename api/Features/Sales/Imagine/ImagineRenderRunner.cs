using Microsoft.EntityFrameworkCore;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Research;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// The whole render of one imagine round, consumed from the queue by the worker (api-shared
/// source): mark the row Running; give Claude the photos (and, on a revision, the chosen concept
/// and the notes) and take its concepts; render each concept over the prospect's own photo with
/// Azure image generation and store it; mark Complete; log it on the lead's timeline; email the
/// prospect the link back. Every concept is saved as it lands, so a failure part-way leaves what
/// was made. The runner never rethrows — a render costs real money — it stamps the round Failed
/// with the reason, which both the public page and the lead page show, and the lead page's
/// Retry re-queues it.
/// </summary>
public sealed class ImagineRenderRunner
{
    /// <summary>How many photos go to the models. More costs more and adds little.</summary>
    private const int MaxReferencePhotos = 4;
    private const int RevisionVariants = 2;

    private readonly JpmsContext context;
    private readonly IImagineImageStore store;
    private readonly ImagineConceptWriter writer;
    private readonly IAzureImageClient images;
    private readonly IImagineNotifier notifier;
    private readonly ImagineNotifierOptions notifierOptions;
    private readonly ILogger<ImagineRenderRunner> logger;

    public ImagineRenderRunner(
        JpmsContext context, IImagineImageStore store, ImagineConceptWriter writer, IAzureImageClient images,
        IImagineNotifier notifier, ImagineNotifierOptions notifierOptions, ILogger<ImagineRenderRunner> logger)
    {
        this.context = context;
        this.store = store;
        this.writer = writer;
        this.images = images;
        this.notifier = notifier;
        this.notifierOptions = notifierOptions;
        this.logger = logger;
    }

    public async Task RunAsync(ImagineRenderMessage message, CancellationToken ct)
    {
        var round = await context.ImagineRounds.FirstOrDefaultAsync(row => row.RoundId == message.RoundId, ct);
        if (round is null)
        {
            logger.LogWarning("Imagine render: round {RoundId} not found — message dropped.", message.RoundId);
            return;
        }
        if (round.Status == (int)ImagineRoundStatus.Complete)
        {
            logger.LogInformation("Imagine render: round {RoundId} is already complete — duplicate delivery ignored.", round.RoundId);
            return;
        }
        // host.json's visibilityTimeout re-delivers a message whose first run is still going: a
        // round Running on a recent start is that run — leave it alone rather than pay twice.
        if (round.Status == (int)ImagineRoundStatus.Running
            && round.StartedAt is { } startedAt && startedAt > DateTimeOffset.UtcNow.AddMinutes(-15))
        {
            logger.LogInformation("Imagine render: round {RoundId} is already running — duplicate delivery ignored.", round.RoundId);
            return;
        }

        var lead = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == round.LeadId, ct);
        if (lead is null)
        {
            logger.LogWarning("Imagine render: lead {LeadId} for round {RoundId} not found — message dropped.", round.LeadId, round.RoundId);
            return;
        }

        round.Status = (int)ImagineRoundStatus.Running;
        round.StartedAt = DateTimeOffset.UtcNow;
        round.Error = null;
        await context.SaveChangesAsync(ct);

        try
        {
            var allImages = await context.ImagineImages.Where(row => row.LeadId == lead.LeadId).ToListAsync(ct);
            var isRevision = round.Kind == (int)ImagineRoundKind.Revision;

            // The photos: this round's on a concepts round; on a revision, the lead's photos from
            // the round that started it all (the earliest), so every variant still sees the house.
            var photoRows = (isRevision
                    ? allImages.Where(row => row.Kind == (int)ImagineImageKind.Photo).OrderBy(row => row.CreatedAt).ThenBy(row => row.Order)
                    : allImages.Where(row => row.RoundId == round.RoundId && row.Kind == (int)ImagineImageKind.Photo).OrderBy(row => row.Order))
                .Take(MaxReferencePhotos)
                .ToList();
            var photos = new List<ImageInput>();
            foreach (var row in photoRows)
            {
                var bytes = await store.ReadAllAsync(row.BlobRef, ct);
                if (bytes is null) continue;
                photos.Add(new ImageInput(bytes, row.ContentType, $"photo{photos.Count + 1}.{AzureBlobImagineImageStore.Extension(row.ContentType)}"));
            }
            if (photos.Count == 0)
                throw new InvalidOperationException("No photographs could be read for this round.");

            ImageInput? chosen = null;
            string? chosenTitle = null;
            string? chosenPrompt = null;
            if (isRevision)
            {
                var chosenRow = allImages.FirstOrDefault(row => row.ImageId == round.BasedOnImageId)
                    ?? throw new InvalidOperationException("The chosen concept could not be found.");
                var bytes = await store.ReadAllAsync(chosenRow.BlobRef, ct)
                    ?? throw new InvalidOperationException("The chosen concept's image could not be read.");
                chosen = new ImageInput(bytes, chosenRow.ContentType, $"concept.{AzureBlobImagineImageStore.Extension(chosenRow.ContentType)}");
                chosenTitle = chosenRow.Title;
                chosenPrompt = chosenRow.Prompt;
            }

            var conceptCount = isRevision ? RevisionVariants : ImagineLimits.ConceptsPerRound;
            var propertyLine = string.Join(", ", new[] { lead.SiteAddress, lead.Postcode }.Where(part => !string.IsNullOrWhiteSpace(part)));
            var set = await writer.WriteAsync(photos, chosen, chosenTitle, chosenPrompt, round.Brief, propertyLine, conceptCount, ct);

            round.Observations = set.Observations;
            await context.SaveChangesAsync(ct);

            // What the image model edits: the house photos for a first round; the chosen render
            // (first, so it leads) plus the main photo for a revision, so the variant stays true to
            // both the concept they liked and the house itself.
            var references = isRevision
                ? new List<ImageInput> { chosen!, photos[0] }
                : photos.Take(MaxReferencePhotos).ToList();

            var order = 0;
            var failures = new List<string>();
            foreach (var concept in set.Concepts)
            {
                order++;
                try
                {
                    var rendered = await images.EditAsync(references, concept.ImagePrompt, ct);
                    var imageId = Guid.NewGuid().ToString("N");
                    var blobRef = await store.SaveAsync(lead.LeadId, round.RoundId, imageId, rendered.ContentType, rendered.Bytes, ct);
                    context.ImagineImages.Add(new ImagineImageEntity
                    {
                        ImageId = imageId,
                        LeadId = lead.LeadId,
                        RoundId = round.RoundId,
                        Kind = (int)ImagineImageKind.Concept,
                        Order = order,
                        Title = Clip(concept.Title, 256),
                        Description = Clip(concept.Description, 2000),
                        Prompt = concept.ImagePrompt,
                        BlobRef = blobRef,
                        ContentType = rendered.ContentType,
                        Size = rendered.Bytes.LongLength,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                    await context.SaveChangesAsync(ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Imagine render: concept {Order} of round {RoundId} failed.", order, round.RoundId);
                    failures.Add($"\"{concept.Title}\": {ex.Message}");
                }
            }

            var made = set.Concepts.Count - failures.Count;
            if (made == 0)
                throw new InvalidOperationException("No concept could be rendered. " + string.Join(" ", failures));

            round.Status = (int)ImagineRoundStatus.Complete;
            round.CompletedAt = DateTimeOffset.UtcNow;
            round.Error = failures.Count == 0 ? null : Clip("Some concepts didn't render: " + string.Join(" ", failures), 2000);
            context.LeadActivities.Add(Activity(lead.LeadId,
                isRevision
                    ? $"Imagine round {round.Number}: {made} revised {(made == 1 ? "version" : "versions")} of \"{chosenTitle}\" rendered."
                    : $"Imagine round {round.Number}: {made} {(made == 1 ? "concept" : "concepts")} rendered from {photos.Count} {(photos.Count == 1 ? "photo" : "photos")}."));
            await context.SaveChangesAsync(ct);

            await EmailProspectAsync(lead, round, made, isRevision, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Imagine render failed for round {RoundId}.", round.RoundId);
            round.Status = (int)ImagineRoundStatus.Failed;
            round.CompletedAt = DateTimeOffset.UtcNow;
            round.Error = Clip(ex.Message, 2000);
            context.LeadActivities.Add(Activity(lead.LeadId, $"Imagine round {round.Number} failed: {Clip(ex.Message, 500)}"));
            await context.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task EmailProspectAsync(LeadEntity lead, ImagineRoundEntity round, int made, bool revision, CancellationToken ct)
    {
        var email = string.IsNullOrWhiteSpace(round.ProspectEmail) ? lead.ContactEmail : round.ProspectEmail;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(lead.ImagineToken)) return;
        var name = string.IsNullOrWhiteSpace(round.ProspectName) ? lead.ContactName : round.ProspectName;
        try
        {
            await notifier.SendConceptsReadyAsync(email, name, notifierOptions.ImagineLink(lead.ImagineToken), made, revision, ct);
            context.LeadActivities.Add(Activity(lead.LeadId, $"Emailed {email} — {(revision ? "revised concepts" : "concepts")} ready."));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Imagine render: the concepts-ready email to {Email} failed.", email);
            context.LeadActivities.Add(Activity(lead.LeadId, $"Concepts are ready but the email to {email} failed: {Clip(ex.Message, 300)} — send the link by hand."));
        }
        await context.SaveChangesAsync(CancellationToken.None);
    }

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
