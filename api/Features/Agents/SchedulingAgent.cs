using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// Programme. When implemented it will schedule all work and issue EoT and NoD notices. Ships as a stub.
public sealed class SchedulingAgent : StubAgent
{
    public const string AgentKey = "scheduling";

    public override string Key => AgentKey;
    public override string DisplayName => "Scheduling Agent";
    public override AgentDiscipline Discipline => AgentDiscipline.Programme;
    public override string Summary =>
        "Schedules all work and issues extension-of-time and notice-of-delay notices.";
}
