using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

public sealed record SignedInUser(string Email, string DisplayName, IReadOnlyList<Role> Roles);
