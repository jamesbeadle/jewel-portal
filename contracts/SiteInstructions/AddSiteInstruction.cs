using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.SiteInstructions;

public sealed record AddSiteInstruction(
    string ProjectId,
    string Title,
    string Instruction,
    string Location) : ICommand<SiteInstruction>;
