using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Models;

/// <summary>
/// One purchase invoice (or credit note) received from the supplier for the project. Labour is
/// the signed net on the CIS labour account, Materials everything else the bill carries for the
/// project, Net their sum — the supplier's own CIS split, exactly as the bill was raised. A bill
/// that also carries lines for other projects (or lines ignored / bucketed on the allocation page)
/// counts only this project's share here and says how much sits elsewhere.
/// </summary>
public sealed record ProjectSupplierAccountInvoice(
    string XeroInvoiceId,
    string InvoiceNumber,
    string? Reference,
    DateTime? Date,
    bool IsCreditNote,
    decimal Labour,
    decimal Materials,
    decimal NetElsewhere,
    // Xero's own status as last synced (DRAFT, SUBMITTED, AUTHORISED, PAID …) and the bill's
    // gross / outstanding, from which State and SettledNet are derived through XeroPaymentMaths.
    string XeroStatus,
    decimal InvoiceTotal,
    decimal AmountDue,
    // The actual payment Xero recorded against the whole bill (under CIS, short of the total by
    // the deduction), the CIS deduction Xero calculated and withheld for HMRC (0 until the bill
    // is approved — Xero calculates it at approval), and the date nothing further was owed.
    // Bill-level, like the total: a bill split across projects shows its whole payment here.
    decimal AmountPaid,
    decimal CisDeduction,
    DateTime? PaidOn,
    ProjectSupplierInvoicePlacement Placement,
    IReadOnlyList<ProjectSupplierAccountOrderShare> Orders)
{
    public decimal Net => Labour + Materials;

    /// <summary>The part of this invoice linked to the supplier's orders on the project.</summary>
    public decimal Matched => Orders.Sum(order => order.Amount);

    /// <summary>The part linked to no order here — an invoice awaiting approval is all unmatched.</summary>
    public decimal Unmatched => Net - Matched;

    public bool IsMatched => Orders.Count > 0;

    public decimal SettledFraction => XeroPaymentMaths.SettledFraction(XeroStatus, InvoiceTotal, AmountDue);

    /// <summary>The settled part of this project's net, to the penny — CIS-safe, off AmountDue.</summary>
    public decimal SettledNet => XeroPaymentMaths.PaidPartOfSlice(Net, XeroStatus, InvoiceTotal, AmountDue);

    public ProjectSupplierInvoiceState State => ProjectSupplierInvoiceStates.For(XeroStatus, SettledFraction);

    /// <summary>Xero has told us about a payment or a deduction on this bill. A settled bill with
    /// neither was synced before the ledger carried them — a dash, never a zero, until the next sync.</summary>
    public bool HasPaymentDetail => AmountPaid != 0m || CisDeduction != 0m;

    public bool IsAwaitingApproval => State == ProjectSupplierInvoiceState.AwaitingApproval;
}
