using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>The quiz's 25 questions in the form's order, one point each: the first ten here, the last fifteen in the other half.</summary>
public static partial class CyberQuizQuestions
{
    public static IReadOnlyList<FormQuestion> All { get; } = Opening().Concat(Closing()).ToList();

    private static FormQuestion[] Opening() => new[]
    {
        Choice("installing_software",
            "Installing software - You find a free tool online that claims to \"optimise\" Windows. What should you do?", Required,
            new[] { "Install it yourself as long as it's from a site that \"looks OK\"",
                "Install it, but only on your home PC, then copy work files there",
                "Ask IT or a Director to approve it before installing on a JBB device",
                "Install it and tell IT afterwards if there's a problem" }),
        Choice("passwords_and_mfa",
            "Password manager and MFA - Which combination best matches Jewel Bespoke Build's requirements for access security?", Required,
            new[] { "Keep passwords in a notebook and MFA is optional",
                "Use 1Password or the approved password manager and enable MFA where required",
                "Reuse the same strong password everywhere and MFA is not needed",
                "Store passwords in your browser only and share with colleagues who need them" }),
        Choice("suspicious_request",
            "Suspicious email or payment request - You receive an email that appears to be from Nigel, Jeremy, a supplier or any other "
                + "contact, asking you to make a payment, change bank details, buy gift cards, open an attachment or click a link. "
                + "What should you do?", Required,
            new[] { "Do it quickly if the email looks urgent",
                "Reply to the email and ask if it is genuine",
                "Stop and verify the request using a known, trusted phone number or separate contact method, and call Nigel or Jeremy if you are unsure",
                "Forward it to a colleague and assume they will deal with it" }),
        Choice("fake_payment_link",
            "Fake invoice or payment link - You receive an email with a link to pay an invoice or re-enter payment details, but "
                + "something feels off. What is the correct action?", Required,
            new[] { "Click the link and check whether the page looks genuine",
                "Use the link only if the logo and branding look right",
                "Forward your card details by email instead",
                "Do not click the link; verify the request separately and call Nigel or Jeremy if you are unsure" }),
        Choice("acceptable_use",
            "Acceptable use of IT systems - You are using your JBB laptop during a break. Which of the following uses is acceptable?", Required,
            new[] { "Streaming explicit content as long as the sound is off",
                "Downloading unlicensed software to speed up your work",
                "Briefly checking your personal email, as long as it does not affect your work and complies with Jewel Bespoke Build's policies",
                "Sharing a client's confidential report with a friend for advice" }),
        Choice("company_card",
            "Company card usage - When using a JBB company payment card (e.g. Capital on Tap / Pleo), which of the following is correct?", Required,
            new[] { "You may use it on any website as long as you are in a hurry",
                "You should only use it on trusted, secure websites and avoid public/unsecured Wi-Fi",
                "It is fine to share card details via email with suppliers for convenience",
                "You may store the card in any mobile app that asks for it" }),
        Choice("personal_card",
            "Personal card usage - When is it acceptable to use your personal payment card for JBB purchases?", Required,
            new[] { "Whenever it is quicker and you will sort it out later",
                "Only with prior approval from a Director, and then claim it back through the JBB expense process (Dext) with a valid receipt",
                "Never, under any circumstances",
                "Only for small purchases under £50" }),
        Choice("card_problem",
            "Reporting card or payment issues - You notice an unexpected transaction on a JBB company card statement that you don't "
                + "recognise. What should you do?", Required,
            new[] { "Ignore it if it is a small amount",
                "Wait until month-end and mention it then",
                "Immediately report it to Jeremy (or the named contact) and do not make any further payments until advised",
                "Try to refund it yourself via the website" }),
        Choice("approved_ai_tools",
            "Approved AI tools - Which statement best reflects Jewel Bespoke Build's rules on AI tools?", Required,
            new[] { "You may use any AI tool you like, as long as it helps your work",
                "You must only use enterprise AI tools (e.g. Perplexity Corporate, Microsoft 365 Copilot) and other AI services that have been formally approved, via your JBB corporate account",
                "You may use your personal ChatGPT account for JBB work if you do not mention client names",
                "AI tools are banned for all JBB work" }),
        Choice("software_updates",
            "Software updates - Your laptop or phone shows a notification that Windows, macOS or an app update is ready. What should you do?", Required,
            new[] { "Ignore it - updates slow your machine down",
                "Install the update promptly (or as soon as you finish your current task) so the latest security patches are applied",
                "Wait until IT tells you specifically to install each update",
                "Disable Windows/macOS update so you don't get bothered" })
    };
}
