using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The H&S Policy's own declaration (Appendix B, Rev 15, page 55) is what the office publishes the
// policy with, so a person sent the Policy sign-off link signs to the policy's words, not the standard line.
public sealed class HealthAndSafetyDeclarationTests
{
    [Fact]
    public void TheWording_isThePolicysOwn_andIsWhatSigningAgreesTo()
    {
        var wording = HealthAndSafetyDeclaration.Wording;

        Assert.StartsWith(HealthAndSafetyDeclaration.Title, wording);
        Assert.Contains("The relevant pages from the Company Safety Policy document have been explained to me", wording);
        Assert.EndsWith("absence of risk to my place of work.", wording);
        Assert.Equal(wording, PolicyDeclarations.Of(wording));
        Assert.NotEqual(PolicyDeclarations.Standard, wording);
    }
}
