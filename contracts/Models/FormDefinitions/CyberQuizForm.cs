using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// The IT, Cyber, AI &amp; Monitoring Quiz, ported from the Microsoft Form JPS used (export kept at
/// docs/03-workflows/forms/jps-it-cyber-ai-monitoring-quiz.html) and written for Jewel Bespoke Build:
/// the details it asks, then its 25 questions of one point each. A company takes it for due
/// diligence, so it is filed under the company. Which answer is right is never sent to the browser:
/// the answer key and the pass mark live in the api, which marks the quiz when it is sent.
/// </summary>
public static class CyberQuizForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.CyberQuiz,
        "IT, Cyber, AI & Monitoring Quiz",
        "This quiz is part of Jewel Bespoke Build's cyber, IT and AI due diligence. Please answer all questions "
            + "honestly. There are 25 scored questions - pass mark is 23/25. Allow approximately 15 minutes. Jewel "
            + "Bespoke Build uses workplace monitoring on its systems and devices - by completing this quiz you "
            + "acknowledge our IT, Cyber and AI Acceptable Use Policy. Fields marked * are required.",
        Details().Concat(CyberQuizQuestions.All).ToList(),
        FilingKind: FormFilingKind.Company,
        FilingKeys: new[] { "company" });

    private static FormQuestion[] Details() => new[]
    {
        Text("name", "Full name", Required),
        Text("email", "Email address", Required),
        Text("company", "Company name", Required, "The company you are taking the quiz for."),
        Date("completed", "Date completed", Required)
    };
}
