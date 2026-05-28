using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Mobilisation;

public sealed record GetMobilisationChecklistForProject(string ProjectId) : IQuery<MobilisationChecklist>;
