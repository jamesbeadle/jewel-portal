namespace Jewel.JPMS.Models;

public enum Discipline
{
    Architectural,
    Structural,
    Mechanical,
    Electrical,
    Plumbing,
    External,
    Joinery,
    Finishes,
    Other
}

public static class DisciplineExtensions
{
    public static string DisplayName(this Discipline discipline) => discipline switch
    {
        Discipline.Architectural => "Architectural",
        Discipline.Structural    => "Structural",
        Discipline.Mechanical    => "Mechanical (M&E)",
        Discipline.Electrical    => "Electrical (M&E)",
        Discipline.Plumbing      => "Plumbing (M&E)",
        Discipline.External      => "External works",
        Discipline.Joinery       => "Joinery",
        Discipline.Finishes      => "Finishes",
        Discipline.Other         => "Other",
        _ => discipline.ToString()
    };
}
