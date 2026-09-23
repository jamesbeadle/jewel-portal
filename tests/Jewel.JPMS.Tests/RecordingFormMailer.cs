using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Features.Forms.Mail;

namespace Jewel.JPMS.Tests;

/// <summary>A forms mailer that keeps every email instead of sending it, and reads a link's secret back out of one.</summary>
internal sealed class RecordingFormMailer : IFormMailer
{
    private static readonly Regex FormLinkSecret = new("[?&]k=([A-Za-z0-9_-]+)");
    private static readonly Regex PackLinkSecret = new("/pack/([A-Za-z0-9_-]+)");

    public List<FormEmail> Sent { get; } = new();

    public bool IsConfigured => true;

    public Task SendAsync(FormEmail email, CancellationToken cancellationToken)
    {
        Sent.Add(email);
        return Task.CompletedTask;
    }

    public IReadOnlyList<FormEmail> To(string address) =>
        Sent.Where(email => email.To.Contains(address, StringComparer.OrdinalIgnoreCase)).ToList();

    public static string FormSecretIn(FormEmail email) => FormLinkSecret.Match(email.Text).Groups[1].Value;

    public static string PackSecretIn(FormEmail email) => PackLinkSecret.Match(email.Text).Groups[1].Value;
}
