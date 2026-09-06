using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>The whole lead register, newest-captured first, with each lead's strategy name.</summary>
public sealed record ListLeads : IQuery<IReadOnlyList<Lead>>;

/// <summary>One lead with its timeline (newest activity first).</summary>
public sealed record GetLead(string LeadId) : IQuery<LeadDetail?>;
