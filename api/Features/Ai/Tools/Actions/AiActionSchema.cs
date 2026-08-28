using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// Generates the model-facing JSON schema for an action from its command contract, and binds a
/// model's arguments back onto that contract. Contracts are positional records with one public
/// constructor, so the constructor IS the schema: parameter name → camelCase property, parameter
/// type → JSON type, non-nullable-without-default → required. Stamped parameters (the actor) are
/// invisible in both directions — never described, never bindable.
/// </summary>
internal static class AiActionSchema
{
    private static readonly NullabilityInfoContext Nullability = new();

    internal static readonly JsonSerializerOptions BindOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    internal static readonly JsonSerializerOptions ResultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ConstructorInfo Constructor(Type commandType) =>
        commandType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();

    private static bool IsStamped(AiAction action, string parameterName) =>
        action.EmailStamps.Contains(parameterName, StringComparer.OrdinalIgnoreCase)
        || action.NameStamps.Contains(parameterName, StringComparer.OrdinalIgnoreCase);

    /// <summary>The JSON schema for the action's arguments object.</summary>
    public static object InputSchema(AiAction action)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        foreach (var parameter in Constructor(action.CommandType).GetParameters())
        {
            if (IsStamped(action, parameter.Name!)) continue;
            var name = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
            properties[name] = TypeSchema(parameter.ParameterType, depth: 0);
            if (IsRequired(parameter)) required.Add(name);
        }
        return new { type = "object", properties, required = required.ToArray() };
    }

    private static bool IsRequired(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue) return false;
        if (Nullable.GetUnderlyingType(parameter.ParameterType) is not null) return false;
        if (!parameter.ParameterType.IsValueType
            && Nullability.Create(parameter).WriteState == NullabilityState.Nullable) return false;
        return true;
    }

    private static object TypeSchema(Type type, int depth)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string)) return new Dictionary<string, object> { ["type"] = "string" };
        if (type == typeof(bool)) return new Dictionary<string, object> { ["type"] = "boolean" };
        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return new Dictionary<string, object> { ["type"] = "integer" };
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return new Dictionary<string, object> { ["type"] = "number" };
        if (type.IsEnum)
            return new Dictionary<string, object> { ["type"] = "string", ["enum"] = Enum.GetNames(type) };
        if (type == typeof(DateTimeOffset) || type == typeof(DateTime))
            return new Dictionary<string, object> { ["type"] = "string", ["description"] = "Date or date-time, ISO 8601 (yyyy-MM-dd accepted)." };
        if (type == typeof(DateOnly))
            return new Dictionary<string, object> { ["type"] = "string", ["description"] = "Date, yyyy-MM-dd." };
        if (type == typeof(TimeOnly))
            return new Dictionary<string, object> { ["type"] = "string", ["description"] = "Time of day, HH:mm." };
        if (type == typeof(Guid))
            return new Dictionary<string, object> { ["type"] = "string", ["description"] = "Id (GUID)." };

        var enumerable = AsEnumerable(type);
        if (enumerable is not null)
            return new Dictionary<string, object> { ["type"] = "array", ["items"] = TypeSchema(enumerable, depth + 1) };

        if (depth >= 3) return new Dictionary<string, object> { ["type"] = "object" };

        // A nested contract record: describe its constructor the same way.
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        var constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        if (constructor is null) return new Dictionary<string, object> { ["type"] = "object" };
        foreach (var parameter in constructor.GetParameters())
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
            properties[name] = TypeSchema(parameter.ParameterType, depth + 1);
            if (IsRequired(parameter)) required.Add(name);
        }
        return new Dictionary<string, object> { ["type"] = "object", ["properties"] = properties, ["required"] = required.ToArray() };
    }

    private static Type? AsEnumerable(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray) return type.GetElementType();
        if (!type.IsGenericType) return null;
        return typeof(IEnumerable).IsAssignableFrom(type) ? type.GetGenericArguments().FirstOrDefault() : null;
    }

    /// <summary>Builds the command instance: stamped parameters from the authenticated user,
    /// everything else bound from the model's arguments JSON (camelCase or exact, case-insensitive).
    /// A missing optional becomes its default; a missing required non-nullable becomes null/default
    /// and is left for the command's own Validation to refuse with a proper message.</summary>
    public static object Bind(AiAction action, JsonElement arguments, string userEmail, string userDisplayName, out List<string> problems)
    {
        problems = new List<string>();
        var constructor = Constructor(action.CommandType);
        var parameters = constructor.GetParameters();
        var values = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (action.EmailStamps.Contains(parameter.Name!, StringComparer.OrdinalIgnoreCase))
            {
                values[i] = userEmail;
                continue;
            }
            if (action.NameStamps.Contains(parameter.Name!, StringComparer.OrdinalIgnoreCase))
            {
                values[i] = userDisplayName;
                continue;
            }

            var found = TryGetProperty(arguments, parameter.Name!, out var element);
            if (!found || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                values[i] = parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) is null
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                continue;
            }

            try
            {
                values[i] = element.Deserialize(parameter.ParameterType, BindOptions);
            }
            catch (JsonException)
            {
                problems.Add($"'{JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!)}' could not be read as {FriendlyType(parameter.ParameterType)}.");
                values[i] = parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }
        }

        return problems.Count > 0 ? null! : constructor.Invoke(values);
    }

    private static bool TryGetProperty(JsonElement arguments, string parameterName, out JsonElement element)
    {
        element = default;
        if (arguments.ValueKind != JsonValueKind.Object) return false;
        foreach (var property in arguments.EnumerateObject())
        {
            if (string.Equals(property.Name, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                element = property.Value;
                return true;
            }
        }
        return false;
    }

    private static string FriendlyType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsEnum) return $"one of: {string.Join(", ", Enum.GetNames(type))}";
        if (type == typeof(DateTimeOffset) || type == typeof(DateOnly)) return "a date (yyyy-MM-dd)";
        return type.Name;
    }
}
