using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Projects.Commands;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class MarkLeadAsWonHandler
    : ICommandHandler<MarkLeadAsWon, LeadOutcome>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<CreateProjectShell, Project> projectShellCreator;

    public MarkLeadAsWonHandler(
        JpmsContext context,
        ICommandHandler<CreateProjectShell, Project> projectShellCreator)
    {
        this.context = context;
        this.projectShellCreator = projectShellCreator;
    }

    public async Task<LeadOutcome> HandleAsync(MarkLeadAsWon command, CancellationToken cancellationToken)
    {
        var lead = await context.Leads.FindAsync(new object[] { command.LeadId }, cancellationToken);
        if (lead is null) throw new InvalidOperationException($"Lead {command.LeadId} not found.");

        var newProject = await projectShellCreator.HandleAsync(
            new CreateProjectShell(
                Reference: lead.Reference,
                Name: lead.ContactName,
                ClientName: lead.CompanyName,
                Organisation: Organisation.JewelBespokeBuild,
                ProjectManagerEmail: lead.OwnerEmail),
            cancellationToken);

        var outcome = new LeadOutcomeEntity
        {
            LeadId = command.LeadId,
            IsWon = true,
            Reason = "Won",
            DecidedByEmail = command.DecidedByEmail,
            DecidedAt = DateTimeOffset.UtcNow,
            CreatedProjectId = newProject.ProjectId
        };
        context.LeadOutcomes.Add(outcome);
        lead.Stage = (int)LeadStage.Won;
        await context.SaveChangesAsync(cancellationToken);
        return outcome.ToModel();
    }
}
