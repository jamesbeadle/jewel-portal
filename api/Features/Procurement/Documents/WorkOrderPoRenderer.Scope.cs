using MigraDoc.DocumentObjectModel;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

public static partial class WorkOrderPoRenderer
{
    private static void AddScopeOfWork(Section section, WorkOrderPoDocumentModel model)
    {
        SectionHeading(section, "Scope of Work");
        AddTermsNote(section);
        AddSiteRules(section);
        if (!string.IsNullOrWhiteSpace(model.Order.Scope))
        {
            SubHeading(section, "Works Order info");
            PrewrapText(section, model.Order.Scope);
        }
        AddProgramme(section, model);
        BodyText(section,
            "All changes to the contract sum (variations) must be notified in writing, and written "
            + "approval to be received prior to any additional works being completed. Any works "
            + "completed without written authorisation cannot be charged.");
        AddPaymentRequirements(section, model);
        AddDeposit(section, model);
        BodyText(section, "We thank you for your business and look forward to working with you.");
        BodyText(section,
            "Jewel Bespoke Build Ltd is Incorporated in England with Limited Liability. "
            + "Registered Company Number: 13752749");
    }

    private static void AddTermsNote(Section section)
    {
        var quote = section.AddParagraph(
            "“This Works Order is issued subject to the terms and conditions which are appended hereto”");
        quote.Format.Font.Size = 8.5;
        quote.Format.Font.Italic = true;
        quote.Format.Font.Color = Muted;
        SpaceAfter(quote, 1.5);

        // The terms live on the public site; browser-printed sheets carry a live link, so the PDF
        // prints the address itself for the supplier to follow.
        var terms = section.AddParagraph("Terms of work order: ");
        terms.Format.Font.Size = 8.5;
        terms.AddFormattedText("https://www.jewelbb.co.uk/copy-of-privacy-policy",
            new Font { Color = Gold, Bold = true, Size = 8.5 });
        SpaceAfter(terms, 2);
    }

    private static void AddSiteRules(Section section)
    {
        SubHeading(section, "Special Instructions");
        BodyText(section,
            "Every site requires full PPE, boots and Hi-Vis, please ensure these are worn at all times, "
            + "with all RAMS adhered to.");
        BodyText(section,
            "We expect all of your work areas to be clean and tidy and left safe at the end of each "
            + "working day.");
        BodyText(section,
            "Ensure you are representing Jewel Bespoke Build by being polite and courteous to all "
            + "clients and neighbours at all times.");

        SubHeading(section, "Insurances & RAMS");
        BodyText(section,
            "Contractors to send all insurance documents and RAMS prior to starting works on site — "
            + "projects@jewelbb.co.uk");
    }

    private static void AddProgramme(Section section, WorkOrderPoDocumentModel model)
    {
        var order = model.Order;
        var hasProgramme = order.ProgrammeStart is not null
            || order.ScheduledCompletion is not null
            || !string.IsNullOrWhiteSpace(order.ProgrammeNotes);
        if (!hasProgramme) return;

        SubHeading(section, "Programme");
        if (order.ProgrammeStart is { } start)
            BodyText(section, $"Start Date — {Date(start)}");
        if (order.ScheduledCompletion is { } completion)
            BodyText(section, $"Completion Date — {Date(completion)}");
        if (!string.IsNullOrWhiteSpace(order.ProgrammeNotes))
            PrewrapText(section, order.ProgrammeNotes);
    }

    private static void AddPaymentRequirements(Section section, WorkOrderPoDocumentModel model)
    {
        SubHeading(section, "Invoice and Payment Requirements");
        BodyText(section,
            "Please forward your invoice to accounts@jewelbb.co.uk and projects@jewelbb.co.uk by COB "
            + $"Friday's with a {model.PaymentTermsDays} day terms");
        BodyText(section, "Invoices to include CIS Breakdown (if necessary)");
        BodyText(section, "Correct VAT breakdown (including Reverse Charge if necessary)");
        BodyText(section,
            "In the event that your invoice does not contain the correct info or sent in on time, we "
            + "will be unable to process it in a timely manner.");
    }

    // Only when the order both requires a deposit and carries the percentage; the flag without
    // a figure would print an empty promise (same rule as the sheet).
    private static void AddDeposit(Section section, WorkOrderPoDocumentModel model)
    {
        if (model.Order is not { DepositRequired: true, DepositPercent: { } percent }) return;
        SubHeading(section, "Deposit");
        BodyText(section,
            $"A deposit of {percent.ToString("0.##", Uk)}% of the order value is required on this order.");
    }
}
