using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

/// <summary>
/// Reads and writes the project's contract. Terms go through the command sender; the document
/// upload is multipart and posts directly, the same split as HttpDrawingStore.
/// </summary>
public sealed class HttpProjectContractStore : IProjectContractStore
{
    // The practical ceiling is the Functions request size; the endpoint enforces 100 MB.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    // Null value = fetched, no contract recorded. Absent key = not fetched. The distinction is the
    // whole point — see the loading-states convention in CLAUDE.md.
    private readonly Dictionary<string, ProjectContract?> byProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> inFlight = new(StringComparer.OrdinalIgnoreCase);

    public HttpProjectContractStore(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public event Action? OnChange;

    public bool LoadedFor(string projectId) => byProject.ContainsKey(projectId);

    public ProjectContract? ForProject(string projectId) =>
        byProject.TryGetValue(projectId, out var contract) ? contract : null;

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return;
        if (!inFlight.Add(projectId)) return;
        try
        {
            var contract = await queries.AskAsync(new GetProjectContract(projectId), cancellationToken);
            byProject[projectId] = contract;
            OnChange?.Invoke();
        }
        finally
        {
            inFlight.Remove(projectId);
        }
    }

    public async Task SetTermsAsync(SetProjectContractTerms terms, CancellationToken cancellationToken)
    {
        var saved = await commands.SendAsync(terms, cancellationToken);
        byProject[terms.ProjectId] = saved;
        OnChange?.Invoke();
    }

    public async Task UploadDocumentAsync(string projectId, IBrowserFile file, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await httpClient.PostAsync(
            $"api/projects/{projectId}/contract/document", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Surface the server's own sentence (e.g. a storage misconfiguration) rather than a
            // bare status code. BadRequestObjectResult returns JSON-encoded strings.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        // The write has been committed. Re-read rather than trusting the response body so the
        // cached row matches whatever the server actually stored.
        byProject.Remove(projectId);
        await RefreshAsync(projectId, cancellationToken);
    }
}
