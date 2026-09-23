using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// The workstation assessment required by the Display Screen Equipment Regulations 1992. Every
/// question is worded so YES is the good answer and NO is the thing to fix: the office lists the
/// noes as the actions, so a reworded question keeps that polarity or it does not go in. Someone
/// who works in more than one place fills it in once for each, and each is its own assessment.
/// </summary>
public static class WorkstationAssessmentForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.WorkstationAssessment,
        "Workstation Assessment",
        "A short check of the desk and screen you work at, whether that is in the office or at home. It "
            + "takes five minutes. Answer honestly rather than helpfully: a No here is something we fix, not "
            + "something anyone is in trouble for.",
        new[]
        {
            Text("full_name", "Your name", Required),
            Text("job_role", "Your role", Optional),
            Choice("where", "Which workstation is this about", Required,
                new[] { "The office", "Home", "A site office", "Somewhere else" },
                "If you regularly work in more than one place, fill this in once for each."),
            Choice("hours", "Roughly how long are you at a screen on a normal day", Required,
                new[] { "Under 1 hour", "1 to 2 hours", "2 to 4 hours", "More than 4 hours" }),
            Choice(
                "kb_separate",
                "Is your keyboard separate from the screen, or on a stand if it is a laptop",
                Required,
                new[] { "Yes", "No" },
                "A laptop on a desk puts the screen too low or the hands too high. One or the other has to give."),
            Choice("kb_comfort", "Can you type with your wrists straight and your forearms roughly level", Required,
                new[] { "Yes", "No" }),
            Choice("mouse_close", "Is your mouse close enough that you are not reaching for it", Required,
                new[] { "Yes", "No" }),
            Choice(
                "screen_clear",
                "Is the text on screen big enough and sharp enough to read without leaning in",
                Required,
                new[] { "Yes", "No" }),
            Choice("screen_height", "Is the top of the screen roughly at eye level", Required, new[] { "Yes", "No" }),
            Choice("screen_glare", "Is the screen free of glare and reflections", Required,
                new[] { "Yes", "No" },
                "Windows behind or in front of you are the usual cause."),
            Choice(
                "chair_adjust",
                "Can you adjust your chair height, and does the back support your lower back",
                Required,
                new[] { "Yes", "No" }),
            Choice("feet_flat", "Can you put your feet flat on the floor, or on a footrest", Required,
                new[] { "Yes", "No" }),
            Choice(
                "leg_room",
                "Is there room for your legs under the desk, with nothing stored where they go",
                Required,
                new[] { "Yes", "No" }),
            Choice("space", "Is there enough desk space for what you need in front of you", Required,
                new[] { "Yes", "No" }),
            Choice("environment", "Is the lighting, temperature and noise reasonable to work in", Required,
                new[] { "Yes", "No" }),
            Choice("breaks", "Are you able to break up screen work with other tasks, or take short breaks", Required,
                new[] { "Yes", "No" }),
            Choice(
                "discomfort",
                "Have you had any aches or discomfort you think is caused by this workstation",
                Required,
                new[] { "No", "Yes" },
                "Neck, shoulders, back, arms, wrists, hands or eyes."),
            LongText("discomfort_detail", "If yes, where and how long has it been going on", Optional),
            Choice("eye_test", "Would you like us to arrange a free eye test", Required,
                new[] { "No", "Yes" },
                "If you use a screen for a significant part of your work you are entitled to one on request, and "
                    + "to basic glasses if you need them specifically for screen work."),
            Choice("training", "Have you been shown how to set your workstation up", Required, new[] { "Yes", "No" }),
            LongText("needs", "Anything you need to make this workstation work better", Optional,
                "Chair, riser, footrest, a second screen, somewhere better to sit at home. Ask."),
            Upload("photo", "A photo of your workstation, if you can", Optional,
                "Taken from the side. It usually shows the problem faster than any answer above.")
        },
        FilingKeys: new[] { "full_name" });
}
