using Jewel.JPMS.Contracts.Ai;
using Xunit;

namespace Jewel.JPMS.Tests;

// The two bounds that keep a long conversation affordable (docs/ai/04-orchestration.md §6). The
// whole transcript is re-sent to the model on every turn, so these are the difference between a
// ten-turn drafting conversation costing one email-thread fetch and costing ten. The predecessor
// of this logic (AiTranscript) shipped and was never wired in — this file exists so that cannot
// happen silently again.
public sealed class AiTranscriptBudgetTests
{
    [Fact]
    public void ARepeatedIdenticalCall_stubsTheOlderCopy()
    {
        var bodies = new[] { "user asks", "FIRST BIG RESULT", "reply", "SECOND BIG RESULT" };
        var toolRows = new[]
        {
            new TranscriptToolRow(1, "get_request_context", "{\"requestId\":\"r1\"}", Sequence: 2),
            new TranscriptToolRow(3, "get_request_context", "{\"requestId\":\"r1\"}", Sequence: 4)
        };

        AiTranscriptBudget.Apply(bodies, toolRows);

        Assert.Contains("superseded", bodies[1]);
        Assert.Equal("SECOND BIG RESULT", bodies[3]);
        // The conversation itself is never touched.
        Assert.Equal("user asks", bodies[0]);
        Assert.Equal("reply", bodies[2]);
    }

    [Fact]
    public void TheSameTool_withDifferentArguments_keepsBothResults()
    {
        // Two different requests' working papers are both live context — keying the supersede rule
        // on the tool name alone would throw away exactly the evidence the model is comparing.
        var bodies = new[] { "RESULT FOR R1", "RESULT FOR R2" };
        var toolRows = new[]
        {
            new TranscriptToolRow(0, "get_request_context", "{\"requestId\":\"r1\"}", Sequence: 1),
            new TranscriptToolRow(1, "get_request_context", "{\"requestId\":\"r2\"}", Sequence: 2)
        };

        AiTranscriptBudget.Apply(bodies, toolRows);

        Assert.Equal("RESULT FOR R1", bodies[0]);
        Assert.Equal("RESULT FOR R2", bodies[1]);
    }

    [Fact]
    public void EveryBigReader_isInTheSupersedeSet()
    {
        // The record-context readers were missing from the set until 2026-08-22: a ten-turn
        // drafting conversation re-paid every earlier copy of a 25k-character read_record_emails
        // on every hop. This pins the whole family so the next big reader added to the catalogue
        // fails a test instead of silently re-billing.
        foreach (var reader in new[]
        {
            "get_request_context", "read_selected_email", "read_record_emails",
            "read_email_attachment", "list_request_correspondence",
            "get_bid_package_context", "get_work_order_context",
            "load_skill", "load_skill_reference"
        })
        {
            var bodies = new[] { "OLD COPY", "NEW COPY" };
            var toolRows = new[]
            {
                new TranscriptToolRow(0, reader, "{\"id\":\"x\"}", Sequence: 1),
                new TranscriptToolRow(1, reader, "{\"id\":\"x\"}", Sequence: 2)
            };

            AiTranscriptBudget.Apply(bodies, toolRows);

            Assert.Contains("superseded", bodies[0]);
            Assert.Equal("NEW COPY", bodies[1]);
        }
    }

    [Fact]
    public void AnOrdinaryTool_isNeverSuperseded()
    {
        var bodies = new[] { "V70 V71 V72", "V70 V71 V72" };
        var toolRows = new[]
        {
            new TranscriptToolRow(0, "list_variations", "{}", Sequence: 1),
            new TranscriptToolRow(1, "list_variations", "{}", Sequence: 2)
        };

        AiTranscriptBudget.Apply(bodies, toolRows);

        Assert.Equal("V70 V71 V72", bodies[0]);
        Assert.Equal("V70 V71 V72", bodies[1]);
    }

    [Fact]
    public void OverBudget_stubsOldestToolRowsFirst_andOnlyToolRows()
    {
        var huge = new string('x', AiTranscriptBudget.MaxTranscriptChars);
        var bodies = new[] { "the user's own words", huge, "recent tool result" };
        var toolRows = new[]
        {
            new TranscriptToolRow(1, "list_requests", "{}", Sequence: 2),
            new TranscriptToolRow(2, "list_variations", "{}", Sequence: 4)
        };

        AiTranscriptBudget.Apply(bodies, toolRows);

        // The oldest tool row went; the total is back under budget before the newer one is touched.
        Assert.Contains("dropped to keep this conversation affordable", bodies[1]);
        Assert.Equal("recent tool result", bodies[2]);
        Assert.Equal("the user's own words", bodies[0]);
    }

    [Fact]
    public void UnderBudget_nothingIsTouched()
    {
        var bodies = new[] { "a", "b", "c" };
        var toolRows = new[] { new TranscriptToolRow(1, "list_requests", "{}", Sequence: 2) };

        AiTranscriptBudget.Apply(bodies, toolRows);

        Assert.Equal(new[] { "a", "b", "c" }, bodies);
    }
}
