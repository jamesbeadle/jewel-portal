using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.CashCalls;

public static class CashCallsRouteRegistration
{
    public static void RegisterCashCallsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListCashCallsForProject, IReadOnlyList<CashCall>>(
            new QueryRoute("/api/projects/{projectId}/cash-calls",
                query => $"/api/projects/{((ListCashCallsForProject)query).ProjectId}/cash-calls"));

        queries.Register<GetProjectCashCallSummary, ProjectCashCallSummary>(
            new QueryRoute("/api/projects/{projectId}/cash-calls/summary",
                query => $"/api/projects/{((GetProjectCashCallSummary)query).ProjectId}/cash-calls/summary"));

        commands.Register<CreateCashCall, CashCall>(
            new CommandRoute("POST", "/api/projects/{projectId}/cash-calls",
                command => $"/api/projects/{((CreateCashCall)command).ProjectId}/cash-calls"));

        commands.Register<IssueClientInvoice, CashCall>(
            new CommandRoute("POST", "/api/cash-calls/{cashCallId}/invoice",
                command => $"/api/cash-calls/{((IssueClientInvoice)command).CashCallId}/invoice"));

        commands.Register<RecordCashCallReceipt, CashCall>(
            new CommandRoute("POST", "/api/cash-calls/{cashCallId}/receipt",
                command => $"/api/cash-calls/{((RecordCashCallReceipt)command).CashCallId}/receipt"));
    }
}
