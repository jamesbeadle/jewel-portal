
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class ReturnManualModuleToDraftHandler : ICommandHandler<ReturnManualModuleToDraft, ManualModule>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public ReturnManualModuleToDraftHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualModule> HandleAsync(ReturnManualModuleToDraft command, CancellationToken cancellationToken)
    {
        var module = await modules.RequireAsync(command.ManualModuleId, cancellationToken);
        var isInReview = module.Status == (int)ManualModuleStatus.InReview;
        if (!isInReview) throw new InvalidOperationException("Only a module in review can be returned to draft.");

        module.Status = (int)ManualModuleStatus.Draft;
        module.ChangeSummary = ReturnNote.Append(module.ChangeSummary, command.Reason);
        module.UpdatedByEmail = command.ReturnedByEmail;
        module.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return await modules.ReadAsync(module, cancellationToken);
    }
}

/// <summary>The reviewer's reason rides on the change summary so the owner sees it where they edit.</summary>
internal static class ReturnNote
{
    private const string Prefix = "Returned to draft: ";

    public static string Append(string changeSummary, string reason)
    {
        var note = Prefix + reason.Trim();
        var combined = string.IsNullOrWhiteSpace(changeSummary) ? note : changeSummary.Trim() + Environment.NewLine + note;
        if (combined.Length > ManualLimits.ChangeSummary)
            throw new InvalidOperationException($"The change summary with this reason would exceed {ManualLimits.ChangeSummary} characters — shorten the reason.");
        return combined;
    }
}
