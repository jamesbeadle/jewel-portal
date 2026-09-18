using Jewel.JPMS.Contracts.MailboxCompose;
using Xunit;

namespace Jewel.JPMS.Tests;

// The subject-line rule every outbound builder reads (2026-09-18). It exists because a record
// raised from a correspondent's own paperwork already names the site in its title, and appending
// the project to that sent "Work order WO-0055 — Tiling Adhesive Supply, By France — By France"
// to Sussex Tiling and Mastic on every purchase order — the 9 Sept screenshot's third fault.
public sealed class SubjectLineTests
{
    [Fact]
    public void ATitleThatAlreadyNamesTheProject_doesNotNameItAgain()
    {
        Assert.Equal(
            "Tiling Adhesive Supply, By France",
            SubjectLine.NamingTheProjectOnce("Tiling Adhesive Supply, By France", "By France"));
    }

    [Fact]
    public void ATitleThatDoesNot_getsTheProjectAppended()
    {
        Assert.Equal(
            "Shower Trays — By France",
            SubjectLine.NamingTheProjectOnce("Shower Trays", "By France"));
    }

    [Fact]
    public void TheProjectIsRecognisedWhateverItsCase()
    {
        Assert.Equal(
            "Groundworks at BY FRANCE",
            SubjectLine.NamingTheProjectOnce("Groundworks at BY FRANCE", "By France"));
    }

    [Fact]
    public void AProjectWithNoName_leavesTheTitleAlone()
    {
        Assert.Equal("Shower Trays", SubjectLine.NamingTheProjectOnce("Shower Trays", ""));
        Assert.Equal("Shower Trays", SubjectLine.NamingTheProjectOnce("Shower Trays", "   "));
    }
}
