using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Clients;

namespace Jewel.JPMS.Api.Features.Parties;

/// <summary>
/// What a project's parties — its client and its architect — may read of the RFI and
/// variation views, which are the same pages the delivery team opens, tailored to the role
/// (2026-09-24: one app, never a sub-site per role). The internal team reads every project whole.
/// A party reads only its own projects (ClientProjects, ArchitectProjects); a record outside them
/// reads as not found. A variation still being priced (Quoting) has not reached them. What leaves
/// the server for a party is stripped of what is internal to Jewel: a request's value and internal
/// notes, a project's valuation and Xero facts, a variation's cost code, the subcontractor it will
/// be instructed to, and the tender behind it. Mail and internal notes on the conversations are
/// refused at the data (SignedInCaller).
/// </summary>
internal static class PartyReads
{
    public static bool IsInternal(SignedInUser user) => JpmsRoleSets.AllInternal.IncludesAny(user.Roles);

    public static async Task<bool> MayReadProjectAsync(
        JpmsContext context, SignedInUser user, string projectId, CancellationToken cancellationToken) =>
        IsInternal(user)
        || await ProjectsOf(context, user).AnyAsync(project => project.ProjectId == projectId, cancellationToken);

    public static async Task<bool> MayReadRequestAsync(
        JpmsContext context, SignedInUser user, string requestId, CancellationToken cancellationToken) =>
        IsInternal(user)
        || await context.Requests
            .AsNoTracking()
            .Where(request => request.RequestId == requestId && request.MergedIntoRequestId == null)
            .Join(ProjectsOf(context, user),
                request => request.ProjectId, project => project.ProjectId,
                (request, project) => request.RequestId)
            .AnyAsync(cancellationToken);

    public static async Task<bool> MayReadVariationAsync(
        JpmsContext context, SignedInUser user, string variationOrderId, CancellationToken cancellationToken) =>
        IsInternal(user)
        || await context.VariationOrders
            .AsNoTracking()
            .Where(order => order.VariationOrderId == variationOrderId)
            .Where(order => order.Status != (int)VariationOrderStatus.Quoting)
            .Join(ProjectsOf(context, user),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => order.VariationOrderId)
            .AnyAsync(cancellationToken);

    public static Request AsReadBy(this Request request, SignedInUser user) =>
        IsInternal(user) ? request : request with { Value = null, InternalNotes = null };

    public static VariationOrder AsReadBy(this VariationOrder order, SignedInUser user) =>
        IsInternal(user)
            ? order
            : order with { CostCode = null, SelectedSubcontractorId = null, SelectedBidPackageId = null };

    public static Project AsReadBy(this Project project, SignedInUser user) =>
        IsInternal(user)
            ? project
            : project with
            {
                ExpectedMonthlyValuation = null, NextExpectedValuationDate = null,
                ValuationCycle = ValuationCycle.None, LastValuationLockedAt = null,
                XeroSiteName = null, XeroContactId = null, XeroContactName = null
            };

    public static IReadOnlyList<Request> AsReadBy(this IEnumerable<Request> requests, SignedInUser user) =>
        requests.Where(request => IsInternal(user) || request.MergedIntoRequestId is null)
            .Select(request => request.AsReadBy(user))
            .ToList();

    public static IReadOnlyList<VariationOrder> AsReadBy(this IEnumerable<VariationOrder> orders, SignedInUser user) =>
        orders.Where(order => IsInternal(user) || order.Status != VariationOrderStatus.Quoting)
            .Select(order => order.AsReadBy(user))
            .ToList();

    private static IQueryable<ProjectEntity> ProjectsOf(JpmsContext context, SignedInUser user)
    {
        var clientId = ClientScope.OwnClientId(user);
        if (clientId is not null) return ClientProjects.For(context, clientId);
        var architectLogin = ArchitectScope.OwnArchitectLogin(user);
        if (architectLogin is not null) return ArchitectProjects.For(context, architectLogin);
        return context.Projects.AsNoTracking().Where(_ => false);
    }
}
