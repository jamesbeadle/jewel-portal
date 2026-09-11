using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices.XeroPayments;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The MD's ask (11 Sep 2026): "the portal says it can't recognise when a sales invoice is paid".
/// Xero is the home of what has been paid; the portal READS it. A linked Issued invoice that Xero
/// holds PAID is recorded Paid here on Xero's date through the one payment handler; an unlinked one
/// is matched the way a person would — the reference naming the valuation, else the same net — and
/// linked when unique; two candidates are never guessed between.
/// </summary>
public sealed class SyncValuationInvoicePaymentsTests
{
    private const string ProjectId = "P-RAVENSWOOD";
    private const string Contact = "xero-contact-ravenswood";

    [Fact]
    public async Task LinkedInvoicePaidInXero_isRecordedPaidOnXerosDate_throughThePaymentHandler()
    {
        var fixture = Fixture.Create();
        fixture.Portal(5, 12000m, ValuationInvoiceStatus.Issued, xeroNumber: "INV-0227");
        fixture.InXero("x-227", "INV-0227", "Valuation 05", "PAID", net: 12000m, paid: 14400m, due: 0m, fullyPaidOn: new DateTime(2026, 9, 5));

        var preview = await fixture.PreviewAsync();
        var row = Assert.Single(preview.Rows);
        Assert.Equal(ValuationInvoicePaymentSyncAction.RecordPayment, row.Action);
        Assert.Equal(12000m, row.AmountToRecord);
        Assert.Equal(new DateTime(2026, 9, 5), row.PaidOn);
        Assert.Equal(1, preview.PaymentsPlanned);

        var outcome = await fixture.SyncAsync();
        Assert.Equal(1, outcome.PaymentsRecorded);
        var invoice = await fixture.Context.ValuationInvoices.SingleAsync(candidate => candidate.Number == 5);
        Assert.Equal((int)ValuationInvoiceStatus.Paid, invoice.Status);
        Assert.Equal(12000m, invoice.AmountPaid);
        Assert.Equal(new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero), invoice.PaidAt);
        var project = await fixture.Context.Projects.SingleAsync();
        Assert.Equal(12000m, project.ValuationInvoicePaidTotal);
        var paymentEvent = Assert.Single(fixture.Context.ValuationInvoiceEvents, e => e.EventType == (int)ValuationInvoiceEventType.PaymentRecorded);
        Assert.Contains("Paid in Xero (INV-0227 on 05 Sep 2026)", paymentEvent.Note);
        Assert.Contains("jeremy@jewelbb.co.uk", paymentEvent.Note);
    }

    [Fact]
    public async Task UnlinkedInvoice_isLinkedByReference_andRecordedWhenPaid_withTheNetCrossChecked()
    {
        var fixture = Fixture.Create();
        fixture.Portal(5, 12000m, ValuationInvoiceStatus.Issued);
        // Named for the valuation but keyed at a different net — linked and recorded, with the note.
        fixture.InXero("x-227", "INV-0227", "Valuation 05 - September", "PAID", net: 11950m, paid: 14340m, due: 0m, fullyPaidOn: new DateTime(2026, 9, 5), date: new DateTime(2026, 9, 1));

        var preview = await fixture.PreviewAsync();
        var row = Assert.Single(preview.Rows);
        Assert.Equal(ValuationInvoicePaymentSyncAction.LinkAndRecordPayment, row.Action);
        Assert.Equal("INV-0227", row.XeroInvoiceNumber);
        Assert.Contains("differs from the portal's £12,000.00", row.Note);
        Assert.Equal(12000m, row.AmountToRecord);

        var outcome = await fixture.SyncAsync();
        Assert.Equal(1, outcome.Linked);
        Assert.Equal(1, outcome.PaymentsRecorded);
        var invoice = await fixture.Context.ValuationInvoices.SingleAsync();
        Assert.Equal("x-227", invoice.XeroInvoiceId);
        Assert.Equal("INV-0227", invoice.XeroInvoiceNumber);
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), invoice.XeroRaisedAt);
        Assert.Equal((int)ValuationInvoiceStatus.Paid, invoice.Status);
        Assert.Contains(fixture.Context.ValuationInvoiceEvents, e => e.EventType == (int)ValuationInvoiceEventType.RaisedInXero && e.Note.Contains("Linked to Xero invoice INV-0227"));
    }

    [Fact]
    public async Task UnlinkedUnpaidInvoice_isLinkedOnly_andPartPaidRecordsNothing()
    {
        var fixture = Fixture.Create();
        fixture.Portal(6, 8000m, ValuationInvoiceStatus.Issued);
        fixture.Portal(7, 9000m, ValuationInvoiceStatus.Issued, xeroNumber: "INV-0231");
        fixture.InXero("x-230", "INV-0230", "Valuation 06", "AUTHORISED", net: 8000m, paid: 0m, due: 9600m);
        fixture.InXero("x-231", "INV-0231", "Valuation 07", "AUTHORISED", net: 9000m, paid: 5000m, due: 5800m);

        var preview = await fixture.PreviewAsync();
        Assert.Equal(ValuationInvoicePaymentSyncAction.Link, preview.Rows[0].Action);
        Assert.Contains("Unpaid in Xero", preview.Rows[0].Note);
        Assert.Equal(ValuationInvoicePaymentSyncAction.None, preview.Rows[1].Action);
        Assert.Contains("Part paid in Xero: £5,000.00 of £10,800.00", preview.Rows[1].Note);
        Assert.Equal(0, preview.PaymentsPlanned);
        Assert.Equal(1, preview.LinksPlanned);

        var outcome = await fixture.SyncAsync();
        Assert.Equal(1, outcome.Linked);
        Assert.Equal(0, outcome.PaymentsRecorded);
        Assert.Equal(1, outcome.NoChange);
        var six = await fixture.Context.ValuationInvoices.SingleAsync(candidate => candidate.Number == 6);
        Assert.Equal("INV-0230", six.XeroInvoiceNumber);
        Assert.Equal((int)ValuationInvoiceStatus.Issued, six.Status);
    }

    [Fact]
    public async Task TwoInvoicesAtTheSameNet_withNothingNamed_areAmbiguous_andNothingChanges()
    {
        var fixture = Fixture.Create();
        fixture.Portal(3, 10000m, ValuationInvoiceStatus.Issued);
        fixture.Portal(4, 10000m, ValuationInvoiceStatus.Issued);
        fixture.InXero("x-a", "INV-0201", "", "PAID", net: 10000m, paid: 12000m, due: 0m);
        fixture.InXero("x-b", "INV-0202", "", "PAID", net: 10000m, paid: 12000m, due: 0m);

        var preview = await fixture.PreviewAsync();
        Assert.All(preview.Rows, row => Assert.Equal(ValuationInvoicePaymentSyncAction.None, row.Action));
        Assert.All(preview.Rows, row => Assert.Contains("Ambiguous: 2 Xero invoices match", row.Note));
        Assert.All(preview.Rows, row => Assert.Equal(2, row.Candidates.Count));
        Assert.False(preview.HasWork);

        var outcome = await fixture.SyncAsync();
        Assert.Equal(0, outcome.Linked + outcome.PaymentsRecorded);
        Assert.All(await fixture.Context.ValuationInvoices.ToListAsync(), invoice => Assert.Null(invoice.XeroInvoiceNumber));
    }

    [Fact]
    public async Task TwoInvoicesAtTheSameNet_eachNamedInXero_pairOffAsAPersonWouldReadThem()
    {
        var fixture = Fixture.Create();
        fixture.Portal(3, 10000m, ValuationInvoiceStatus.Issued);
        fixture.Portal(4, 10000m, ValuationInvoiceStatus.Issued);
        fixture.InXero("x-a", "INV-0201", "Valuation 03", "PAID", net: 10000m, paid: 12000m, due: 0m);
        fixture.InXero("x-b", "INV-0202", "Valuation 4", "AUTHORISED", net: 10000m, paid: 0m, due: 12000m);

        var preview = await fixture.PreviewAsync();
        Assert.Equal(ValuationInvoicePaymentSyncAction.LinkAndRecordPayment, preview.Rows[0].Action);
        Assert.Equal("INV-0201", preview.Rows[0].XeroInvoiceNumber);
        Assert.Equal(ValuationInvoicePaymentSyncAction.Link, preview.Rows[1].Action);
        Assert.Equal("INV-0202", preview.Rows[1].XeroInvoiceNumber);
    }

    [Fact]
    public async Task OneXeroInvoice_thatFitsTwoPortalInvoices_isHandedToNeither()
    {
        var fixture = Fixture.Create();
        fixture.Portal(3, 10000m, ValuationInvoiceStatus.Issued);
        fixture.Portal(4, 10000m, ValuationInvoiceStatus.Issued);
        fixture.InXero("x-a", "INV-0201", "", "PAID", net: 10000m, paid: 12000m, due: 0m);

        var preview = await fixture.PreviewAsync();
        Assert.All(preview.Rows, row => Assert.Equal(ValuationInvoicePaymentSyncAction.None, row.Action));
        Assert.Contains("also matches VI-0004", preview.Rows[0].Note);
        Assert.Contains("also matches VI-0003", preview.Rows[1].Note);
    }

    [Fact]
    public async Task OnlyIssuedInvoicesAreConsidered_andAClaimedXeroRowIsNeverOfferedAgain()
    {
        var fixture = Fixture.Create();
        fixture.Portal(1, 5000m, ValuationInvoiceStatus.Paid, xeroNumber: "INV-0100");
        fixture.Portal(2, 5000m, ValuationInvoiceStatus.Approved);
        fixture.Portal(3, 5000m, ValuationInvoiceStatus.Issued);
        fixture.InXero("x-100", "INV-0100", "Valuation 01", "PAID", net: 5000m, paid: 6000m, due: 0m);

        var preview = await fixture.PreviewAsync();
        var row = Assert.Single(preview.Rows);
        Assert.Equal("VI-0003", row.Reference);
        Assert.Equal(ValuationInvoicePaymentSyncAction.None, row.Action);
        Assert.Contains("No Xero invoice matches", row.Note);
    }

    [Fact]
    public async Task LinkedInvoiceXeroNoLongerHolds_isReported_andAMissingContactMappingBlocks()
    {
        var fixture = Fixture.Create();
        fixture.Portal(5, 12000m, ValuationInvoiceStatus.Issued, xeroNumber: "INV-0999");

        var preview = await fixture.PreviewAsync();
        Assert.Contains("Xero no longer holds INV-0999", Assert.Single(preview.Rows).Note);

        var unmapped = Fixture.Create(xeroContactId: null);
        unmapped.Portal(5, 12000m, ValuationInvoiceStatus.Issued);
        var blocked = await unmapped.PreviewAsync();
        Assert.Contains(blocked.Blockers, blocker => blocker.Contains("No Xero contact is mapped on Ravenswood"));
        Assert.Empty(blocked.Rows);
        Assert.DoesNotContain(unmapped.Xero.Calls, call => call.StartsWith("GetSalesInvoices"));
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => unmapped.SyncAsync());
        Assert.Contains("No Xero contact is mapped", refused.Message);
    }

    private sealed class Fixture
    {
        public JpmsContext Context { get; }
        public RecordingXero Xero { get; } = new();
        private readonly string? contactId;

        private Fixture(JpmsContext context, string? contactId) { Context = context; this.contactId = contactId; }

        public static Fixture Create(string? xeroContactId = Contact)
        {
            var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"payment-sync-{Guid.NewGuid():N}").Options);
            context.Projects.Add(new ProjectEntity
            {
                ProjectId = ProjectId, Reference = "2001", Name = "Ravenswood", ClientName = "Ravenswood Homes Ltd",
                XeroContactId = xeroContactId, XeroContactName = xeroContactId is null ? null : "Ravenswood Homes Ltd"
            });
            context.SaveChanges();
            return new Fixture(context, xeroContactId);
        }

        public void Portal(int number, decimal amount, ValuationInvoiceStatus status, string? xeroNumber = null, string? xeroId = null)
        {
            Context.ValuationInvoices.Add(new ValuationInvoiceEntity
            {
                ValuationInvoiceId = $"VI-{number}", ProjectId = ProjectId, Number = number, Reference = $"VI-{number:0000}",
                PeriodMonth = new DateTimeOffset(2026, number, 1, 0, 0, 0, TimeSpan.Zero), Amount = amount, Status = (int)status,
                RaisedAt = DateTimeOffset.UtcNow, XeroInvoiceNumber = xeroNumber, XeroInvoiceId = xeroId,
                AmountPaid = status == ValuationInvoiceStatus.Paid ? amount : 0m
            });
            Context.SaveChanges();
            Context.ChangeTracker.Clear();
        }

        public void InXero(string id, string number, string reference, string status, decimal net, decimal paid, decimal due, DateTime? fullyPaidOn = null, DateTime? date = null)
        {
            if (contactId is null) return;
            if (!Xero.SalesInvoicesByContact.TryGetValue(contactId, out var list)) Xero.SalesInvoicesByContact[contactId] = list = new();
            list.Add(new XeroSalesInvoiceSummary(id, number, reference, status, date ?? new DateTime(2026, 9, 1), null,
                net, net * 0.2m, net * 1.2m, paid, due, fullyPaidOn, "GBP"));
        }

        public Task<ValuationInvoicePaymentSyncPreview> PreviewAsync() =>
            new PreviewValuationInvoicePaymentSyncHandler(Context, Xero).HandleAsync(new PreviewValuationInvoicePaymentSync(ProjectId), CancellationToken.None);

        public Task<ValuationInvoicePaymentSyncOutcome> SyncAsync() =>
            new SyncValuationInvoicePaymentsFromXeroHandler(Context, Xero, new RecordValuationInvoicePaymentHandler(Context))
                .HandleAsync(new SyncValuationInvoicePaymentsFromXero(ProjectId, "jeremy@jewelbb.co.uk"), CancellationToken.None);
    }
}
