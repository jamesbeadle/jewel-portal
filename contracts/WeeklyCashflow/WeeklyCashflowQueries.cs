using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.WeeklyCashflow;

/// <summary>The Weekly Cashflow's own stored state in one read: every live (unarchived) manual
/// item and every placement. Company-wide — there is one weekly plan, shared by everyone who
/// works it.</summary>
public sealed record GetWeeklyCashflowPlan() : IQuery<WeeklyCashflowPlan>;
