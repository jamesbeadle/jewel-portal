using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The wording of the email a defect's supplier gets (2026-09-16, shared by the defect page and the
// connector's send_defect_to_supplier): the reference leads the subject, the first send asks for
// attendance, the chase names the date it was sent, and a defect with no supplier address has
// nowhere to go.
public sealed class DefectSupplierEmailTests
{
    private static readonly Defect Defect = new(
        "def-1", "P-BF", "Cracked tiles delivered to site", "Kitchen", "", DefectStatus.Open,
        new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero), null, "DEF-0012",
        SubcontractorId: "sup-1", SubcontractorName: "Sussex Tiling and Mastic Ltd", SupplierContactEmail: "kim@sussextiling.co.uk");

    [Fact]
    public void TheFirstSend_leadsWithTheReference_andAsksForAttendance()
    {
        var email = DefectSupplierEmails.Next(Defect, "By France", "JBB-2026-001", "Kim Barnes");

        Assert.Equal("kim@sussextiling.co.uk", email.To);
        Assert.Equal("DEF-0012 · Kitchen · By France", email.Subject);
        Assert.StartsWith("Hello Kim,", email.Body);
        Assert.Contains("at By France (JBB-2026-001)", email.Body);
        Assert.Contains("Location: Kitchen", email.Body);
        Assert.Contains("Please confirm when you can attend", email.Body);
    }

    [Fact]
    public void OnceSent_theNextEmailIsTheChase_namingTheDate()
    {
        var sent = Defect with { SentToSupplierAt = new DateTimeOffset(2026, 9, 12, 8, 30, 0, TimeSpan.Zero), Status = DefectStatus.InProgress };

        var email = DefectSupplierEmails.Next(sent, "By France", "JBB-2026-001", null);

        Assert.Equal("Chasing: DEF-0012 · Kitchen · By France", email.Subject);
        Assert.StartsWith("Hello,", email.Body);
        Assert.Contains("sent to you on 12 Sep 2026", email.Body);
        Assert.Contains("Please let us know when this will be attended to.", email.Body);
    }

    [Fact]
    public void ADefectWithNoSupplierAddress_hasNowhereToGo()
    {
        var unaddressed = Defect with { SubcontractorId = null, SubcontractorName = null, SupplierContactEmail = "", AssignedToEmail = "" };
        Assert.Equal("", DefectSupplierEmails.Next(unaddressed, null, null, null).To);
    }
}
