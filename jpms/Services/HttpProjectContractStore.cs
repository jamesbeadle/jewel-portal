using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

/// <summary>
/// Reads and writes the project's contract and its amendments. Terms, amendment details and
/// removals go through the command sender; the document uploads are multipart and post directly,
/// the same split as HttpDrawingStore.
/// </summary>
public sealed class HttpProjectContractStore : IProjectContractStore
{
    // The practical ceiling is the Functions request size; the endpoints enforce 100 MB.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    // Null value = fetched, no contract recorded. Absent key = not fetched. The distinction is the
    // whole point — see the loading-states convention in CLAUDE.md. The amendments dictionary is
    // keyed identically and written in the same fetch, so LoadedFor answers for both.
    private readonly Dictionary<string, ProjectContract?> byProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<ProjectContractAmendment>> amendmentsByProject = new(StringComparer.OrdinalIgnoreCase);
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

    public IReadOnlyList<ProjectContractAmendment>? AmendmentsFor(string projectId) =>
        amendmentsByProject.TryGetValue(projectId, out var amendments) ? amendments : null;

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return;
        if (!inFlight.Add(projectId)) return;
        try
        {
            var contract = await queries.AskAsync(new GetProjectContract(projectId), cancellationToken);
            var amendments = await queries.AskAsync(new ListProjectContractAmendments(projectId), cancellationToken);
            // Written together after both awaits so a failure part-way leaves the store unloaded
            // rather than half-loaded — LoadedFor must never say yes for a project whose
            // amendments were not fetched.
            byProject[projectId] = contract;
            amendmentsByProject[projectId] = amendments ?? Array.Empty<ProjectContractAmendment>();
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
        //
        // Deliberately NOT clearing the cached row first: RefreshAsync overwrites it on success, so
        // the removal bought nothing, and if the re-read then failed the store was left with no key
        // at all — LoadedFor(projectId) went back to false and every panel gated on it pulsed
        // forever, hiding the very error message the caller had just set.
        await RefreshAsync(projectId, cancellationToken);
    }

    public async Task UploadAmendmentAsync(
        string projectId, IBrowserFile file, string title, DateTimeOffset? amendmentDate, string? notes,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);

        if (!string.IsNullOrWhiteSpace(title))
            content.Add(new StringContent(title.Trim()), "title");
        if (amendmentDate is { } date)
            content.Add(new StringContent(date.ToString("yyyy-MM-dd")), "amendmentDate");
        if (!string.IsNullOrWhiteSpace(notes))
            content.Add(new StringContent(notes.Trim()), "notes");

        var response = await httpClient.PostAsync(
            $"api/projects/{projectId}/contract/amendments", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        // Same re-read rationale as UploadDocumentAsync — and the same reason not to clear first.
        await RefreshAsync(projectId, cancellationToken);
    }

    public async Task SetAmendmentDetailsAsync(
        SetProjectContractAmendmentDetails details, CancellationToken cancellationToken)
    {
        var saved = await commands.SendAsync(details, cancellationToken);

        // Splice the saved row into the cached list rather than re-fetching: the server returned
        // the row it stored, and the edit cannot have changed any other row.
        if (amendmentsByProject.TryGetValue(details.ProjectId, out var amendments))
        {
            amendmentsByProject[details.ProjectId] = amendments
                .Select(a => a.ProjectContractAmendmentId == saved.ProjectContractAmendmentId ? saved : a)
                .OrderBy(a => a.AmendmentDate ?? a.DocumentUploadedAt)
                .ThenBy(a => a.DocumentUploadedAt)
                .ToList();
        }
        OnChange?.Invoke();
    }

    public async Task RemoveAmendmentAsync(string projectId, string amendmentId, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new RemoveProjectContractAmendment(projectId, amendmentId), cancellationToken);

        if (amendmentsByProject.TryGetValue(projectId, out var amendments))
        {
            amendmentsByProject[projectId] = amendments
                .Where(a => a.ProjectContractAmendmentId != amendmentId)
                .ToList();
        }
        OnChange?.Invoke();
    }
}
