namespace Jewel.JPMS.Models;

// The kind of correspondent a project or request deals with. Jewel either works directly with a
// client (no architect involved) or through an architect acting on the client's behalf — anywhere
// a correspondent is selected, a client account and an architect practice are interchangeable.
// Integer values are pinned because they are stored on ProjectEntity/RequestEntity.PartyKind.
public enum PartyKind
{
    Client = 0,
    Architect = 1
}

public static class PartyKindExtensions
{
    public static string DisplayName(this PartyKind kind) => kind switch
    {
        PartyKind.Client    => "Client",
        PartyKind.Architect => "Architect",
        _ => kind.ToString()
    };
}
