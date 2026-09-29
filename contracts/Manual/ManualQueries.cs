using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Manual;

/// <summary>The office master: every module, retired ones included, in family order.</summary>
public sealed record ListManualModules : IQuery<IReadOnlyList<ManualModule>>;

/// <summary>One module with the text the site sees, its versions and its acknowledgements.</summary>
public sealed record GetManualModule(string ManualModuleId) : IQuery<ManualModuleDetail?>;

/// <summary>A published view: the approved modules for that audience, with the reader's own acknowledgements.</summary>
public sealed record GetManualView(ManualView View) : IQuery<ManualViewReading>;
