using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public sealed class CommandRouteTable
{
    private readonly Dictionary<Type, CommandRoute> routesByCommandType = new();

    public void Register<TCommand, TResult>(CommandRoute route)
        where TCommand : ICommand<TResult>
    {
        routesByCommandType[typeof(TCommand)] = route;
    }

    public CommandRoute For(Type commandType)
    {
        if (!routesByCommandType.TryGetValue(commandType, out var route))
            throw new InvalidOperationException($"No command route registered for {commandType.FullName}.");
        return route;
    }
}

public sealed record CommandRoute(string HttpMethod, string PathTemplate, Func<object, string> PathBuilder)
{
    public static CommandRoute Post(string path) => new("POST", path, _ => path);
    public static CommandRoute Put(string path) => new("PUT", path, _ => path);
    public static CommandRoute Delete(string path) => new("DELETE", path, _ => path);

    public string PathFor(object command) => PathBuilder(command);
}
