using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;
/// <summary>Internal signal that a Xero call failed with a message safe to show in the snapshot.</summary>
internal sealed class XeroCallFailedException : Exception
{
    public XeroCallFailedException(string message) : base(message) { }
}
