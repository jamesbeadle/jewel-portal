using System.Reflection;

namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// Runs a generic per-entity method for an entity type known only at run time: the data
/// protection readers and writers are written once over TEntity and closed over each column's
/// entity here. The method's first two parameters are always the context and the column name.
/// </summary>
internal static class ClosedOverEntity
{
    public static Task<T> Invoke<T>(MethodInfo method, Type entityType, JpmsContext context, string column, params object[] arguments)
    {
        var closed = method.MakeGenericMethod(entityType);
        var allArguments = new object[] { context, column }.Concat(arguments).ToArray();
        return (Task<T>)closed.Invoke(null, allArguments)!;
    }
}
