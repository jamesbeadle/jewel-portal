using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>The cost code the day's words point to, offered as the worker writes them (Jeremy,
/// 1 Oct 2026: one less thing for the guys to pick, and more often right). Read from the site's
/// own list only: the trade-word rulebook first, Claude for what the rulebook misses. A suggestion
/// pre-fills the picker and is the worker's to change; no match is no suggestion.</summary>
public sealed record SuggestMyDayCostCode(string ProjectId, string Description) : IQuery<MyDayCostCodeSuggestion>;

public enum MyDaySuggestionSource { None, Rule, Claude }

public sealed record MyDayCostCodeSuggestion(
    string CostCode = "",
    string CostCodeName = "",
    MyDaySuggestionSource Source = MyDaySuggestionSource.None)
{
    public bool HasCode => CostCode.Length > 0;
}
