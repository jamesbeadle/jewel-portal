namespace Jewel.JPMS.Api.Gates;

public sealed record SignedInUser(string Email, string DisplayName, string Role);
