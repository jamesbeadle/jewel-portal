namespace Jewel.JPMS.Contracts.SiteInstructions;

/// <summary>The site instruction register's column limits, stated once: the entity, the doors that
/// write it and the forms that mirror them all read these.</summary>
public static class SiteInstructionLimits
{
    public const int TitleMaxLength = 256;
    public const int LocationMaxLength = 256;
    public const int GivenByMaxLength = 128;
}
