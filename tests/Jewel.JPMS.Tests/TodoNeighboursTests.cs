using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed class TodoNeighboursTests
{
    private static readonly IReadOnlyList<TodoItem> Pile = new[] { Open("a"), Open("b"), Open("c") };

    [Fact]
    public void MiddleItemHasBothNeighbours()
    {
        var neighbours = TodoNeighbours.Of(Pile, "b");

        Assert.Equal("a", neighbours.Previous?.TodoItemId);
        Assert.Equal("c", neighbours.Next?.TodoItemId);
    }

    [Fact]
    public void FirstItemHasNoPrevious_andLastHasNoNext()
    {
        Assert.False(TodoNeighbours.Of(Pile, "a").HasPrevious);
        Assert.Equal("b", TodoNeighbours.Of(Pile, "a").Next?.TodoItemId);
        Assert.False(TodoNeighbours.Of(Pile, "c").HasNext);
        Assert.Equal("b", TodoNeighbours.Of(Pile, "c").Previous?.TodoItemId);
    }

    [Fact]
    public void ItemOutsideThePileOffersTheTopOfIt()
    {
        var neighbours = TodoNeighbours.Of(Pile, "done-already");

        Assert.False(neighbours.HasPrevious);
        Assert.Equal("a", neighbours.Next?.TodoItemId);
    }

    [Fact]
    public void EmptyPileHasNowhereToGo()
    {
        var neighbours = TodoNeighbours.Of(Array.Empty<TodoItem>(), "a");

        Assert.Equal(TodoNeighbours.None, neighbours);
    }

    private static TodoItem Open(string id) =>
        new(id, "", $"TODO-{id}", $"Item {id}", "", Role.Accounts, null, null,
            "nigel@jewelenterprises.co.uk", false, DateTimeOffset.UtcNow, null, null);
}
