using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>GET /api/my/labour/weeks/{weekStart} — one week of the signed-in worker's days, any
/// week up to the current one, as My day swipes through them. Any date in the week names it.</summary>
public sealed record GetMyLabourWeek(DateTimeOffset WeekStart) : IQuery<MyLabourWeek>;

/// <summary>GET /api/my/labour/months/{year}/{month} — one calendar month of the signed-in
/// worker's days with the totals they invoice from.</summary>
public sealed record GetMyLabourMonth(int Year, int Month) : IQuery<MyLabourMonth>;

/// <summary>POST /api/my/labour/weeks/submit — the worker sends a finished week to the office for
/// review in one step. Allowed once the week has elapsed and every working day in it is logged,
/// recorded off, or planned off by the office; a week sent back may be submitted again. Until
/// the office answers, the week's days cannot be amended or added to.</summary>
public sealed record MySubmitWeek(DateTimeOffset WeekStart) : ICommand<MyLabourWeek>;
