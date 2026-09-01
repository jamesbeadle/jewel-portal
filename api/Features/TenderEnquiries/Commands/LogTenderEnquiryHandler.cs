using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>Logs an enquiry by hand — no email, no files; those are added from the enquiry's
/// page. Creates the Lead project when the job is new, exactly as the email route does.</summary>
public sealed class LogTenderEnquiryHandler : ICommandHandler<LogTenderEnquiry, TenderEnquiry>
{
    private readonly TenderEnquiryProjectResolver projectResolver;
    private readonly TenderEnquiryRegister register;

    public LogTenderEnquiryHandler(TenderEnquiryProjectResolver projectResolver, TenderEnquiryRegister register)
    {
        this.projectResolver = projectResolver;
        this.register = register;
    }

    public async Task<TenderEnquiry> HandleAsync(LogTenderEnquiry command, CancellationToken cancellationToken)
    {
        var (projectId, createdProject) = await projectResolver.ResolveAsync(
            command.ProjectId, command.NewProject, command.Details, command.LoggedByEmail, cancellationToken);
        var enquiry = await register.LogAsync(projectId, command.Details, command.LoggedByEmail, cancellationToken);
        await register.RecordLoggedAsync(enquiry, createdProject, cancellationToken);
        return enquiry.ToModel();
    }
}
