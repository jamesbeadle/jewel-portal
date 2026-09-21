namespace Jewel.JPMS.Api.Auth;

/// <summary>The address a request came from, as the Static Web Apps edge reports it — the first
/// forwarded address, else the connection's, with the port the edge appends taken off.</summary>
public static class ClientKey
{
    private const string ForwardedFor = "X-Forwarded-For";
    private const string Unknown = "unknown";

    public static string Of(HttpRequest request)
    {
        var forwarded = request.Headers[ForwardedFor].ToString();
        var address = string.IsNullOrWhiteSpace(forwarded)
            ? request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? Unknown
            : forwarded.Split(',')[0].Trim();
        return WithoutTheEdgesPort(address);
    }

    private static string WithoutTheEdgesPort(string address)
    {
        var colon = address.LastIndexOf(':');
        var isIpv4WithPort = colon > 0 && address.Count(character => character == ':') == 1;
        return isIpv4WithPort ? address[..colon] : address;
    }
}
