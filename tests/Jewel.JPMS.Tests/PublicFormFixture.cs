using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Forms;
using Jewel.JPMS.Api.Features.Forms.Public;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The public forms as a phone reaches them, over an in-memory database: Sam Smith filling in the
/// emergency contact and right-to-work forms, the drawn signature as a tiny PNG, the office's alert
/// and the person's own copy kept by the recording mailer.
/// </summary>
internal sealed class PublicFormFixture : IAsyncDisposable
{
    public const string FirstSession = "0123456789abcdef01234567";
    public const string SecondSession = "fedcba9876543210fedcba98";
    public const string Address = "client-hash";
    public const string Medical = "Carries an adrenaline pen";
    public const string Mobile = "07700 900000";
    public const string PersonEmail = "sam@example.com";
    private const string DrawingKey = "declaration";
    private static readonly byte[] PngHeader = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public JpmsContext Context { get; } = new(new DbContextOptionsBuilder<JpmsContext>()
        .UseInMemoryDatabase($"public-forms-{Guid.NewGuid():N}").Options);
    public RecordingFormMailer Mailer { get; } = new();
    public MemoryFormEvidenceStore Store { get; } = new();
    public FormSiteOptions Options { get; } = new();
    public PublicFormService Service => new(Context, Store, Mailer, Options, NullLogger<PublicFormService>.Instance);

    public static Dictionary<string, string> EmergencyAnswers() => new()
    {
        ["name"] = "Sam Smith", ["ec_name"] = "Alex Smith", ["ec_email"] = "alex@example.com", ["ec_phone"] = "07700 900001",
        ["ec_address"] = "1 High Street, Surbiton", ["relationship"] = "Brother", ["medical_detail"] = Medical
    };

    public static Dictionary<string, string> RightToWorkAnswers() => new()
    {
        ["full_name"] = "Sam Smith", ["dob"] = "1990-01-01", ["email"] = PersonEmail, ["mobile"] = Mobile,
        ["status"] = RightToWorkForm.BritishOrIrishCitizen, ["engaged_as"] = RightToWorkForm.Employee,
        ["declaration_name"] = "Sam Smith"
    };

    public static PublicFormSubmission Posted(
        string sessionId, IReadOnlyDictionary<string, string> answers, string? inviteToken = null, string? packToken = null,
        string? drawingId = null) =>
        new(sessionId, answers, DrawingUploads(drawingId), inviteToken, packToken);

    public Task<PublicFormUploadReceipt> SignAsync(string sessionId) =>
        Service.UploadAsync(FormSlugs.RightToWork,
            new PublicFormUpload(sessionId, DrawingKey, "signature-declaration.png", Convert.ToBase64String(PngHeader)),
            Address, CancellationToken.None);

    public ValueTask DisposeAsync() => Context.DisposeAsync();

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> DrawingUploads(string? drawingId) =>
        drawingId is null
            ? new Dictionary<string, IReadOnlyList<string>>()
            : new Dictionary<string, IReadOnlyList<string>> { [DrawingKey] = new[] { drawingId } };
}
