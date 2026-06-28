using Jewel.JPMS.Api.Features.Requests.Documents;

// A representative RFI with recipients and a short activity trail, mirroring what the SQL builder
// produces. Rendering this proves the renderer + font resolver work end-to-end on this machine.
var model = new RequestDocumentModel(
    RequestId: "smoke-0001",
    DisplayNumber: "REQ-0001",
    TypeShort: "RFI",
    TypeLong: "Request for Information",
    Title: "Clarification of structural steel connection at grid B/4",
    Description:
        "The structural drawings (S-204 rev C) show a moment connection at grid B/4, but the architectural "
        + "details (A-330) indicate a pinned connection. Please confirm which is correct so fabrication can "
        + "proceed. This is on the critical path for the steel package.",
    StatusLabel: "Awaiting response",
    ProjectName: "Coombe Hill House",
    ProjectReference: "JBB-2026-014",
    ClientName: "Mr & Mrs Harrington",
    RaisedByEmail: "site.manager@jewelbb.co.uk",
    RaisedAt: DateTimeOffset.Now.AddDays(-3),
    ResponseDue: DateTimeOffset.Now.AddDays(2),
    RaisedTo: "Foster Studio Architects",
    DrawingRef: "S-204 rev C; A-330",
    RelatedDrawingSpec: "Section 05 12 00 — Structural Steel Framing",
    Value: 4250.00m,
    ImpliesVariation: true,
    ClientNotes: "Fabricator on standby; a 48-hour turnaround would avoid a programme slip.",
    ResponseText: null,
    RespondedByEmail: null,
    RespondedAt: null,
    Recipients: new[]
    {
        new RequestDocumentRecipient("Jane Foster", "jane@fosterstudio.co.uk", "Architect", "Foster Studio Architects"),
        new RequestDocumentRecipient("Mr & Mrs Harrington", "harrington@example.com", "Client", null)
    },
    Activity: new[]
    {
        new RequestDocumentActivity("Tom Reilly (Jewel)", "RFI raised and issued to the design team.", DateTimeOffset.Now.AddDays(-3), false),
        new RequestDocumentActivity("Jane Foster", "Acknowledged — checking with the structural engineer.", DateTimeOffset.Now.AddDays(-2), true)
    },
    GeneratedAt: DateTimeOffset.Now);

var outPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "REQ-0001-smoke.pdf");
outPath = Path.GetFullPath(outPath);

try
{
    var pdf = RequestDocumentRenderer.Render(model);
    File.WriteAllBytes(outPath, pdf);

    var ok = pdf.Length > 1000
        && pdf[0] == (byte)'%' && pdf[1] == (byte)'P' && pdf[2] == (byte)'D' && pdf[3] == (byte)'F';

    Console.WriteLine($"Rendered {pdf.Length:N0} bytes -> {outPath}");
    Console.WriteLine($"File name from model: {model.FileName}");
    Console.WriteLine($"Email subject from model: {model.EmailSubject}");
    Console.WriteLine(ok ? "PASS: output is a non-trivial PDF." : "FAIL: output does not look like a PDF.");
    return ok ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine("FAIL: render threw an exception.");
    Console.Error.WriteLine(ex);
    // A font-resolution failure here is the most likely cause — see DocumentFontResolver for the
    // RequestDocuments:FontPath app setting that points the renderer at a TrueType font directory.
    return 1;
}
