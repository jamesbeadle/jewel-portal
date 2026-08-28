using System.Diagnostics;
using System.Text.Json;
using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Connect;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Mcp;

/// <summary>
/// POST /api/mcp — the portal as an MCP server (Streamable HTTP transport, stateless). Team
/// members connect their own Claude or Perplexity to this URL, sign in as themselves through the
/// Connect feature's OAuth flow, and every tool call runs under their portal identity: the tool
/// list is role-filtered exactly as the HTTP endpoints gate, and every call lands in the agent
/// activity log under their email.
///
/// <para>The transport is deliberately the plain-JSON subset of Streamable HTTP: every POST gets
/// a single <c>application/json</c> response, no SSE and no session ids. Tool calls here are
/// short database reads and writes, and the Static Web Apps gateway (~45s) has no room for
/// long-lived streams anyway. The JSON-RPC surface is small enough that a hand-rolled handler
/// beats a framework dependency.</para>
/// </summary>
public sealed class McpEndpoint
{
    /// <summary>Protocol revisions this server knows. A client asking for anything else is
    /// answered with the newest we speak, per the spec's negotiation rule.</summary>
    private static readonly string[] KnownProtocolVersions = { "2024-11-05", "2025-03-26", "2025-06-18" };

    private const string LatestProtocolVersion = "2025-06-18";

    private readonly OAuthTokenManager tokens;
    private readonly SignedInUserResolver users;
    private readonly SignedInUserCache userCache;
    private readonly JpmsContext db;
    private readonly AuditActor auditActor;
    private readonly AgentActivityLog activityLog;
    private readonly IServiceProvider services;
    private readonly IConfiguration configuration;

    public McpEndpoint(
        OAuthTokenManager tokens,
        SignedInUserResolver users,
        SignedInUserCache userCache,
        JpmsContext db,
        AuditActor auditActor,
        AgentActivityLog activityLog,
        IServiceProvider services,
        IConfiguration configuration)
    {
        this.tokens = tokens;
        this.users = users;
        this.userCache = userCache;
        this.db = db;
        this.auditActor = auditActor;
        this.activityLog = activityLog;
        this.services = services;
        this.configuration = configuration;
    }

    [Function("McpServer")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", "delete", Route = "mcp")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        // The transport's other verbs: GET would open an SSE stream (we don't run one) and DELETE
        // ends a session (we don't issue any). Both answers are the spec's "not offered" shapes.
        if (HttpMethods.IsGet(request.Method)) return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
        if (HttpMethods.IsDelete(request.Method)) return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);

        var user = await AuthenticateAsync(request, cancellationToken);
        if (user is null) return Challenge(request);

        // The identity every audit write inside a tool sees — same wiring as the endpoint gates.
        auditActor.Email = user.Email;

        JsonDocument body;
        try { body = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken); }
        catch (JsonException) { return RpcError(null, -32700, "The request body is not valid JSON."); }

        using (body)
        {
            if (body.RootElement.ValueKind != JsonValueKind.Object)
                return RpcError(null, -32600, "JSON-RPC batching is not supported — send one request per POST.");

            var root = body.RootElement;
            var method = root.TryGetProperty("method", out var methodElement) ? methodElement.GetString() : null;
            var hasId = root.TryGetProperty("id", out var idElement);
            var id = hasId ? idElement.Clone() : (JsonElement?)null;

            // Notifications expect no body at all.
            if (method is not null && !hasId) return new StatusCodeResult(StatusCodes.Status202Accepted);

            return method switch
            {
                "initialize" => Initialize(root, id),
                "ping" => RpcResult(id, new { }),
                "tools/list" => ToolsList(id, user),
                "tools/call" => await ToolsCallAsync(root, id, user, cancellationToken),
                null => RpcError(id, -32600, "The request has no method."),
                _ => RpcError(id, -32601, $"Method '{method}' is not supported.")
            };
        }
    }

    // ---- Auth ----------------------------------------------------------------------------------

    private async Task<SignedInUser?> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var secret = header["Bearer ".Length..].Trim();
        if (secret.Length == 0) return null;

        var resolved = await tokens.ResolveAccessAsync(secret, cancellationToken);
        if (resolved is null) return null;

        // Same short-TTL cache as the cookie path, keyed by the token hash instead of the session
        // id — the directory read and role list survive between calls in a burst.
        var now = DateTimeOffset.UtcNow;
        if (userCache.Get(resolved.TokenHash, now) is { } cached) return cached;

        var user = await users.ResolveByEmailAsync(resolved.UserEmail, cancellationToken);
        if (user is null) return null;
        userCache.Set(resolved.TokenHash, user, now.Add(SignedInUserCache.Ttl), now);
        return user;
    }

    /// <summary>The 401 whose WWW-Authenticate header is what sends Claude and Perplexity into
    /// the OAuth flow (RFC 9728 §5.1).</summary>
    private IActionResult Challenge(HttpRequest request)
    {
        var site = SiteBaseUrl.Resolve(configuration, request);
        request.HttpContext.Response.Headers.WWWAuthenticate =
            $"Bearer resource_metadata=\"{site}/.well-known/oauth-protected-resource\"";
        return new UnauthorizedObjectResult(new
        {
            error = "invalid_token",
            error_description = "Sign in through the portal's OAuth flow to use this MCP server."
        });
    }

    // ---- Methods -------------------------------------------------------------------------------

    private IActionResult Initialize(JsonElement root, JsonElement? id)
    {
        var requested = root.TryGetProperty("params", out var p)
                        && p.TryGetProperty("protocolVersion", out var v)
            ? v.GetString() ?? ""
            : "";
        var negotiated = KnownProtocolVersions.Contains(requested) ? requested : LatestProtocolVersion;

        return RpcResult(id, new
        {
            protocolVersion = negotiated,
            capabilities = new { tools = new { } },
            serverInfo = new { name = "jewel-portal", title = "Jewel Portal", version = "1.0.0" },
            instructions =
                "The Jewel Bespoke Build project-management portal (JPMS). Tools read the live "
                + "portal — projects, requests/RFIs, variations, valuations, work orders, bid "
                + "packages, cost codes, to-dos — and a small set of writes (posting a request "
                + "message, managing to-dos, saving skills). Call list_skills early in real portal "
                + "work: the team teaches the portal its working doctrine there. You are acting as the "
                + "signed-in portal user; everything you can see and do here is what they can "
                + "see and do in the portal, and every call is logged under their name. "
                + "Reference formats: requests REQ-0123, RFIs RFI-049, variations V72. "
                + "Call find_by_reference first when the user names a record."
        });
    }

    private IActionResult ToolsList(JsonElement? id, SignedInUser user)
    {
        var tools = AiToolCatalogue.ForConnector(user)
            .Select(tool => new Dictionary<string, object>
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["inputSchema"] = tool.InputSchema,
                ["annotations"] = new Dictionary<string, object>
                {
                    ["readOnlyHint"] = tool.Kind == AiToolKind.Read,
                    ["destructiveHint"] = false,
                    ["openWorldHint"] = false
                }
            })
            .ToList();
        return RpcResult(id, new { tools });
    }

    private async Task<IActionResult> ToolsCallAsync(
        JsonElement root, JsonElement? id, SignedInUser user, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("params", out var p) || !p.TryGetProperty("name", out var nameElement))
            return RpcError(id, -32602, "tools/call needs params.name.");
        var name = nameElement.GetString() ?? "";

        var tool = AiToolCatalogue.ForConnector(user)
            .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));
        if (tool is null)
            return RpcError(id, -32602, $"Unknown tool '{name}'.");

        using var arguments = p.TryGetProperty("arguments", out var argumentsElement)
            ? JsonDocument.Parse(argumentsElement.GetRawText())
            : JsonDocument.Parse("{}");

        var context = new AiToolContext(db, user, Scope: null, services);
        var stopwatch = Stopwatch.StartNew();
        string output;
        var ok = true;
        try
        {
            output = await tool.ExecuteAsync(context, arguments.RootElement, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            output = JsonSerializer.Serialize(new { ok = false, error = $"The tool failed: {ex.Message}" });
            ok = false;
        }
        stopwatch.Stop();

        await activityLog.WriteAsync(
            agentKey: "mcp",
            trigger: AgentTrigger.Mcp,
            actorEmail: user.Email,
            action: name,
            outcome: ok ? AgentOutcome.Ok : AgentOutcome.Failed,
            summary: Summarise(name, arguments.RootElement),
            cancellationToken,
            toolsUsed: new[] { name },
            durationMs: (int)stopwatch.ElapsedMilliseconds);

        // An image flowing back through a tool (a drawing, a photo, a marked-up plan) rides the
        // AiImageToolResult marker — translated here into MCP's own image content block so the
        // model SEES the picture, exactly as the retired chat replayed it.
        if (AiImageToolResult.IsImage(output)
            && AiImageToolResult.TryParse(output, out var mediaType, out var fileName, out var base64))
        {
            return RpcResult(id, new
            {
                content = new object[]
                {
                    new { type = "text", text = $"\u201c{fileName}\u201d:" },
                    new { type = "image", data = base64, mimeType = mediaType }
                },
                isError = false
            });
        }

        return RpcResult(id, new
        {
            content = new object[] { new { type = "text", text = output } },
            isError = !ok
        });
    }

    /// <summary>"list_requests {"projectId":"…"} " — enough to reconstruct what was asked without
    /// storing whole payloads.</summary>
    private static string Summarise(string toolName, JsonElement arguments)
    {
        var raw = arguments.GetRawText();
        if (raw.Length > 600) raw = raw[..600] + "…";
        return $"MCP tool call {toolName} {raw}";
    }

    // ---- JSON-RPC plumbing ---------------------------------------------------------------------

    private static IActionResult RpcResult(JsonElement? id, object result) =>
        new OkObjectResult(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = result
        });

    /// <summary>JSON-RPC errors ride a 200 — the HTTP layer only speaks for transport and auth.</summary>
    private static IActionResult RpcError(JsonElement? id, int code, string message) =>
        new OkObjectResult(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new { code, message }
        });
}
