namespace Jewel.JPMS.Models;

/// <summary>
/// The standard form a project is let under. Pinned values — never renumber, and never insert a
/// member mid-list: these persist as ints. The edition (2016, 2024, …) is carried separately in
/// <see cref="ProjectContract.FormEdition"/> so a new edition of an existing form does not need a
/// new enum member.
/// </summary>
public enum ContractForm
{
    Unspecified = 0,
    JctMinorWorks = 1,                  // MW
    JctMinorWorksWithDesign = 2,        // MWD — Contractor's Design Portion
    JctIntermediate = 3,                // IC
    JctIntermediateWithDesign = 4,      // ICD
    JctDesignAndBuild = 5,              // DB
    JctStandardBuildingContract = 6,    // SBC
    Nec4Ecc = 7,
    Bespoke = 8
}

public static class ContractFormExtensions
{
    /// <summary>How the form is written in correspondence, e.g. "JCT ICD 2024".</summary>
    public static string LongName(this ContractForm form, string? edition) => form switch
    {
        ContractForm.JctMinorWorks => Join("JCT MW", edition),
        ContractForm.JctMinorWorksWithDesign => Join("JCT MWD", edition),
        ContractForm.JctIntermediate => Join("JCT IC", edition),
        ContractForm.JctIntermediateWithDesign => Join("JCT ICD", edition),
        ContractForm.JctDesignAndBuild => Join("JCT DB", edition),
        ContractForm.JctStandardBuildingContract => Join("JCT SBC", edition),
        ContractForm.Nec4Ecc => "NEC4 ECC",
        ContractForm.Bespoke => "Bespoke",
        _ => "Not specified"
    };

    private static string Join(string stem, string? edition) =>
        string.IsNullOrWhiteSpace(edition) ? stem : $"{stem} {edition.Trim()}";
}

/// <summary>
/// The contract for a project — the single place a contract term is a fact about the project rather
/// than a frozen copy on an operational record.
///
/// Before this existed, "the contract sum" was answerable only from
/// <c>ValuationClaimEntity.ContractSum</c> (frozen per claim, so two claims could disagree), the LAD
/// rate only from <c>LadClaimEntity.RatePerWeek</c> (per claim, same problem), and the completion
/// date not at all. Those columns stay — they are deliberate snapshots — but this row is the truth
/// they should be taken from.
///
/// The OH&amp;P and notice-period fields are here rather than in configuration because they are
/// contract terms: they vary per project and are argued from the Contract Particulars.
/// </summary>
public sealed record ProjectContract(
    string ProjectContractId,
    string ProjectId,

    // ---- The form ----
    ContractForm Form,
    string? FormEdition,                 // "2016", "2024" — free text on purpose
    string? BespokeDeviations,           // Where the form has been amended. Read this before citing a clause.

    // ---- The parties ----
    string? EmployerName,
    string? ContractAdministratorName,
    string? ContractAdministratorEmail,
    string? ArchitectName,
    string? ArchitectEmail,
    string? ContractorName,

    // ---- The money ----
    decimal ContractSum,
    decimal LiquidatedDamagesPerWeek,

    // ---- The dates ----
    DateTimeOffset? ContractDate,
    DateTimeOffset? PossessionDate,
    DateTimeOffset? CompletionDate,

    // ---- Retention and defects ----
    decimal RetentionPercent,                    // JCT default 5.0 pre-practical completion
    decimal RetentionPercentAfterCompletion,     // JCT default 2.5 post-practical completion
    int DefectsLiabilityPeriodMonths,

    // ---- The payment mechanism ----
    int? ApplicationCutOffDayOfMonth,   // The same date every cycle. Missing it once sets a precedent.
    int PaymentNoticeDays,              // Days after application for the Payment Notice. JCT default 5.
    int PayLessNoticeDays,              // Days before the Final Date for the Pay-Less Notice. JCT default 5.
    int FinalDateForPaymentDays,        // Days after the due date.

    // ---- Overheads, profit and attendance ----
    decimal OhpDirectWorksPercent,          // Main contractor executing varied works directly. Typically 10.
    decimal OhpSubcontractorPercent,        // On a subcontractor's nett variation value. Typically 10.
    decimal AttendanceOnClientDirectPercent,// Client-direct or free-issue supply. Typically 5.
    decimal DayworkLabourPercent,           // Typically 15.
    decimal DayworkMaterialsPercent,        // Typically 10.
    decimal DayworkPlantPercent,            // Typically 10.

    // ---- The executed document ----
    string? DocumentFileName,
    string? DocumentContentType,
    long? DocumentFileSizeBytes,
    DateTimeOffset? DocumentUploadedAt,
    string? DocumentUploadedByEmail,

    string? UpdatedByEmail,
    DateTimeOffset UpdatedAt)
{
    /// <summary>True once the executed contract document has been uploaded.</summary>
    public bool HasDocument => !string.IsNullOrWhiteSpace(DocumentFileName);

    /// <summary>How the form is cited in correspondence, e.g. "JCT ICD 2024".</summary>
    public string FormDisplayName => Form.LongName(FormEdition);

    /// <summary>
    /// True when the form has been amended. Anything citing a clause number on this project must
    /// check <see cref="BespokeDeviations"/> first — an amended form does not map to the standard.
    /// </summary>
    public bool IsAmended =>
        Form == ContractForm.Bespoke || !string.IsNullOrWhiteSpace(BespokeDeviations);
}
