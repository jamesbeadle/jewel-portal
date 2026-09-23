namespace Jewel.JPMS.Api.Features.Forms.Mail;

/// <summary>One email the forms send, in Jewel Bespoke Build's name.</summary>
public sealed record FormEmail(IReadOnlyList<string> To, string Subject, string Html, string Text);

/// <summary>
/// The forms' emails — the link to a named person, the pack, the person's own copy, the office's
/// alert and the renewal chase — through Azure Communication Services like every portal email.
/// </summary>
public interface IFormMailer
{
    bool IsConfigured { get; }
    Task SendAsync(FormEmail email, CancellationToken cancellationToken);
}

/// <summary>No ACS connection: every send is refused with the reason, so the office is told the link was not emailed.</summary>
public sealed class NullFormMailer : IFormMailer
{
    private const string Reason = "Email isn't configured (CommunicationServicesConnectionString).";

    public bool IsConfigured => false;

    public Task SendAsync(FormEmail email, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException(Reason));
}
