using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.AccessRequests;

internal static class AccessRequestEntityMapping
{
    public static AccessRequest ToModel(this AccessRequestEntity entity) => new(
        Email: entity.Email,
        DisplayName: entity.DisplayName,
        RequestedAt: entity.RequestedAt);
}
