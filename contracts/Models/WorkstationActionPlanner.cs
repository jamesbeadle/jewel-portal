namespace Jewel.JPMS.Models;

/// <summary>The dashboard's action for each NO, word for word, plus the discomfort, the eye test and the ask.</summary>
public static class WorkstationActionPlanner
{
    private static readonly (string Key, string Action)[] ActionForEachNo =
    {
        ("kb_separate", "Laptop needs a stand and a separate keyboard"),
        ("kb_comfort", "Cannot type with wrists straight - check desk and chair height"),
        ("mouse_close", "Reaching for the mouse - move it in"),
        ("screen_clear", "Text too small or blurry - check the display settings"),
        ("screen_height", "Screen not at eye level - riser needed"),
        ("screen_glare", "Glare on the screen - move the desk or fit a blind"),
        ("chair_adjust", "Chair does not adjust, or gives no back support"),
        ("feet_flat", "Feet not flat on the floor - footrest needed"),
        ("leg_room", "No room for legs under the desk - clear it out"),
        ("space", "Not enough desk space"),
        ("environment", "Lighting, noise or temperature is a problem"),
        ("breaks", "Cannot break up screen work - look at how the work is planned"),
        ("training", "Never been shown how to set a workstation up")
    };

    public static IReadOnlyList<WorkstationActionNeeded> ActionsFor(IReadOnlyDictionary<string, string> answers)
    {
        var actions = ActionForEachNo
            .Where(question => Says(answers, question.Key, FormWording.No))
            .Select(question => new WorkstationActionNeeded(question.Key, question.Action))
            .ToList();
        if (Says(answers, "discomfort", FormWording.Yes))
            actions.Add(Discomfort(answers));
        if (Says(answers, "eye_test", FormWording.Yes))
            actions.Add(new("eye_test", "Wants an eye test - they are entitled to one, arrange it"));
        var needs = answers.GetValueOrDefault("needs", "").Trim();
        if (needs.Length > 0)
            actions.Add(new("needs", "Asked for: " + needs));
        return actions;
    }

    private static WorkstationActionNeeded Discomfort(IReadOnlyDictionary<string, string> answers)
    {
        var detail = answers.GetValueOrDefault("discomfort_detail", "").Trim();
        var where = detail.Length > 0 ? ": " + detail : "";
        return new("discomfort", "Reports aches or discomfort" + where + " - follow this one up first");
    }

    private static bool Says(IReadOnlyDictionary<string, string> answers, string key, string answer) =>
        string.Equals(answers.GetValueOrDefault(key, "").Trim(), answer, StringComparison.OrdinalIgnoreCase);
}
