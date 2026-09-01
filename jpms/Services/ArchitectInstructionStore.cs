using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.ArchitectInstructions;

namespace Jewel.JPMS.Services;

/// <summary>
/// The project's Architect's Instruction register. Uncached: it is one short list read on entry to
/// one page, and an instruction landing while someone is looking at the register is exactly the
/// moment a stale copy would mislead them.
/// </summary>
public interface IArchitectInstructionStore
{
    Task<IReadOnlyList<ArchitectInstruction>> ListAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Files an instruction. The document is optional — a PM who knows an instruction has been
    /// given but has not been sent the paperwork can open the row now and attach it later.
    /// </summary>
    Task<ArchitectInstruction> FileAsync(
        string projectId,
        string instructionRef,
        string title,
        string? notes,
        DateTimeOffset? instructedAt,
        string? issuedByEmail,
        IBrowserFile? file,
        IReadOnlyList<string>? variationOrderIds,
        CancellationToken cancellationToken = default);

    Task<ArchitectInstruction> UpdateAsync(
        string architectInstructionId, string instructionRef, string title, string? notes,
        DateTimeOffset? instructedAt, CancellationToken cancellationToken = default);

    Task<ArchitectInstruction> LinkToVariationAsync(
        string architectInstructionId, string variationOrderId, CancellationToken cancellationToken = default);

    Task<ArchitectInstruction> UnlinkFromVariationAsync(
        string architectInstructionId, string variationOrderId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string architectInstructionId, CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams the stored document.</summary>
    string FileUrl(string architectInstructionId, bool inline = false) =>
        $"api/architect-instructions/{architectInstructionId}/file" + (inline ? "?inline=1" : "");
}

public sealed class HttpArchitectInstructionStore : IArchitectInstructionStore
{
    private const long MaxUploadBytes = 64L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpArchitectInstructionStore(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<ArchitectInstruction>> ListAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListArchitectInstructionsForProject(projectId), cancellationToken);

    public async Task<ArchitectInstruction> FileAsync(
        string projectId,
        string instructionRef,
        string title,
        string? notes,
        DateTimeOffset? instructedAt,
        string? issuedByEmail,
        IBrowserFile? file,
        IReadOnlyList<string>? variationOrderIds,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        if (file is not null)
        {
            var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
        }
        content.Add(new StringContent(instructionRef ?? ""), "instructionRef");
        content.Add(new StringContent(title ?? ""), "title");
        if (!string.IsNullOrWhiteSpace(notes)) content.Add(new StringContent(notes), "notes");
        if (!string.IsNullOrWhiteSpace(issuedByEmail)) content.Add(new StringContent(issuedByEmail), "issuedByEmail");
        if (instructedAt is { } instructed)
            content.Add(new StringContent(instructed.ToString("yyyy-MM-dd")), "instructedAt");
        foreach (var variationOrderId in variationOrderIds ?? Array.Empty<string>())
            content.Add(new StringContent(variationOrderId), "variationOrderId");

        var response = await httpClient.PostAsync(
            $"api/projects/{projectId}/architect-instructions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        var filed = await response.Content.ReadFromJsonAsync<ArchitectInstruction>(cancellationToken: cancellationToken);
        return filed ?? throw new InvalidOperationException("The instruction was filed but couldn't be read back.");
    }

    public Task<ArchitectInstruction> UpdateAsync(
        string architectInstructionId, string instructionRef, string title, string? notes,
        DateTimeOffset? instructedAt, CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new UpdateArchitectInstruction(architectInstructionId, instructionRef, title, notes, instructedAt),
            cancellationToken);

    public Task<ArchitectInstruction> LinkToVariationAsync(
        string architectInstructionId, string variationOrderId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new LinkArchitectInstructionToVariation(architectInstructionId, variationOrderId), cancellationToken);

    public Task<ArchitectInstruction> UnlinkFromVariationAsync(
        string architectInstructionId, string variationOrderId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new UnlinkArchitectInstructionFromVariation(architectInstructionId, variationOrderId), cancellationToken);

    public Task DeleteAsync(string architectInstructionId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DeleteArchitectInstruction(architectInstructionId), cancellationToken);
}
