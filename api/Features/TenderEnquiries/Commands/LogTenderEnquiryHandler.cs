using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>Logs an enquiry by hand on a project that already exists — no email, no files; those
/// are added from the enquiry's page.</summary>
public sealed class LogTenderEnquiryHandler : ICommandHandler<LogTenderEnquiry, TenderEnquiry>
{
    private readonly JpmsContext context;
    private readonly TenderEnquiryRegister register;

    public LogTenderEnquiryHandler(JpmsContext context, TenderEnquiryRegister register)
    {
        this.context = context;
        this.register = register;
    }

    public async Task<TenderEnquiry> HandleAsync(LogTenderEnquiry command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(row => row.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var enquiry = await register.LogAsync(command.ProjectId, command.Details, command.LoggedByEmail, cancellationToken);
        await register.RecordLoggedAsync(enquiry, createdProject: false, cancellationToken);
        return enquiry.ToModel();
    }
}
