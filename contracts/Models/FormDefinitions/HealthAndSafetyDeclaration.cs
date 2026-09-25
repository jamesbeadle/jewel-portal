namespace Jewel.JPMS.Models;

/// <summary>
/// Appendix B of the Jewel Bespoke Build Ltd Health and Safety Policy, Rev 15 (June 2026), page 55: the
/// employee and sub-contractor declaration a person signs to when the H&amp;S Policy is published for
/// signing. Kept here word for word from the policy (Jeremy, 25 Sept 2026) so the office publishes each
/// revision with the same declaration rather than retyping it; the sheet's own Print Name, Signed,
/// Position in Company and Date lines are the Policy sign-off form's fields.
/// </summary>
public static class HealthAndSafetyDeclaration
{
    public const string Title = "Health and Safety Policy, Organisation and Arrangements";

    public const string Source = "Appendix B of the H&S Policy Rev 15 (Jun 2026), page 55";

    private static readonly string[] Paragraphs =
    {
        "The relevant pages from the Company Safety Policy document have been explained to me by the JEWELBB "
            + "Operations Director or other person nominated by the company.",
        "It is my intention to carry out my duties, as far as is reasonably practicable, in a safe and proper manner, "
            + "without causing unnecessary risk to the health and safety of other persons, who may be affected by my "
            + "acts or omissions whilst at work. I will co-operate with any instructions given to me by Jewel Bespoke "
            + "Build Ltd and follow the procedures set out in the Arrangements Section of the Document.",
        "I will co-operate with any instructions given to me by Jewel Bespoke Build Ltd or any passed on to me by "
            + "Jewel Bespoke Build Ltd whether imposed by them or other persons with the authority to request certain "
            + "safe working procedures, to ensure so far as reasonably practicable, the safety and absence of risk to "
            + "myself or others affected by my work activities.",
        "I undertake not to interfere with or misuse anything provided in my interests of health, safety or welfare "
            + "and to wear any personal protective equipment as instructed to do so.",
        "I will carry out my duties when using any work equipment in accordance with the training I have received, "
            + "whether by the Company, a previous employer or training establishment.",
        "I will report any hazards to Jewel Bespoke Build Ltd if seen by me and where necessary, will bring to Jewel "
            + "Bespoke Build Ltd's notice any matter signalling a shortcoming in their arrangements for my Health, "
            + "Safety or Welfare at work.",
        "Where required to do so, I will comply with any permit to work system, risk assessment or method statement "
            + "to the best of my ability in accordance with any training received or instructions given.",
        "I am prepared to sign this declaration on the understanding that Jewel Bespoke Build Ltd will, so far as "
            + "reasonably practicable, provide me with a safe place of work, with a safe access and egress, safe and "
            + "properly maintained plant and equipment to comply with the provision and use of work equipment and safe "
            + "working arrangements for me to carry out the duties I am being paid to carry out, and on the "
            + "understanding that Jewel Bespoke Build Ltd will do all that is reasonably practicable to ensure his part "
            + "as stated in the current legislative framework governing the safety and absence of risk to my place of "
            + "work."
    };

    public static readonly string Wording = Title + "\n\n" + string.Join("\n\n", Paragraphs);
}
