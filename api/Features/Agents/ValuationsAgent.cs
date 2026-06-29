using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// Commercial. When implemented it will create and confirm variation-order quotes and pull the
// latest financials. Ships as a stub.
public sealed class ValuationsAgent : StubAgent
{
    public const string AgentKey = "valuations";

    public override string Key => AgentKey;
    public override string DisplayName => "Valuations Agent";
    public override AgentDiscipline Discipline => AgentDiscipline.Commercial;
    public override string Summary =>
        "Creates and confirms variation-order quotes and pulls the latest project financials.";
}
