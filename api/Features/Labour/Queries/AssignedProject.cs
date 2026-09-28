namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>A project a worker is on the list for, as the day's cards name it.</summary>
public sealed record AssignedProject(string ProjectId, string ProjectName);
