using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Forms;

/// <summary>Every pack, newest first, each with its forms — the one screen of packs in flight.</summary>
public sealed record ListFormPacks : IQuery<IReadOnlyList<FormPack>>;

/// <summary>Forms sent on their own (not in a pack), newest first.</summary>
public sealed record ListFormInvites : IQuery<IReadOnlyList<FormInvite>>;
