using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public sealed class QueryRouteTable
{
    private readonly Dictionary<Type, QueryRoute> routesByQueryType = new();

    public void Register<TQuery, TResult>(QueryRoute route)
        where TQuery : IQuery<TResult>
    {
        routesByQueryType[typeof(TQuery)] = route;
    }

    public QueryRoute For(Type queryType)
    {
        if (!routesByQueryType.TryGetValue(queryType, out var route))
            throw new InvalidOperationException($"No query route registered for {queryType.FullName}.");
        return route;
    }
}

public sealed record QueryRoute(string PathTemplate, Func<object, string> PathBuilder)
{
    public static QueryRoute Static(string path) => new(path, _ => path);

    public string PathFor(object query) => PathBuilder(query);
}
