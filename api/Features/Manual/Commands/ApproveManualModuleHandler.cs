using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>
/// Approval is the one write that puts words in front of the site: the working text becomes the
/// published text at this version, the version is kept whole, and the previous version is closed.
/// </summary>
public sealed class ApproveManualModuleHandler : ICommandHandler<ApproveManualModule, ManualModule>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public ApproveManualModuleHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualModule> HandleAsync(ApproveManualModule command, CancellationToken cancellationToken)
    {
        var module = await modules.RequireAsync(command.ManualModuleId, cancellationToken);
        var isInReview = module.Status == (int)ManualModuleStatus.InReview;
        if (!isInReview) throw new InvalidOperationException("Only a module in review can be approved.");

        var now = DateTimeOffset.UtcNow;
        await CloseCurrentVersionAsync(module, now, cancellationToken);
        context.ManualModuleVersions.Add(new ManualModuleVersionEntity
        {
            ManualModuleVersionId = ManualIdentifierFactory.NextVersionId(),
            ManualModuleId = module.ManualModuleId,
            Version = module.Version,
            Title = module.Title,
            Body = module.Body,
            ChangeSummary = module.ChangeSummary,
            ApprovedByEmail = command.ApprovedByEmail,
            ApprovedAt = now,
        });
        module.PublishedBody = module.Body;
        module.PublishedVersion = module.Version;
        module.Status = (int)ManualModuleStatus.Approved;
        module.ApprovedAt = now;
        module.ApprovedByEmail = command.ApprovedByEmail;
        module.NextReviewAt = command.NextReviewAt ?? module.NextReviewAt;
        module.UpdatedByEmail = command.ApprovedByEmail;
        module.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        return await modules.ReadAsync(module, cancellationToken);
    }

    private async Task CloseCurrentVersionAsync(ManualModuleEntity module, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = await context.ManualModuleVersions
            .Where(version => version.ManualModuleId == module.ManualModuleId && version.SupersededAt == null)
            .ToListAsync(cancellationToken);
        foreach (var version in current) version.SupersededAt = now;
    }
}
