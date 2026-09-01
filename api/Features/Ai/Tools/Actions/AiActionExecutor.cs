using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// Runs one <see cref="AiAction"/> exactly the way its HTTP endpoint would: bind the command with
/// the actor stamped server-side, Authorisation.Allows, Validation.Check, then the registered
/// ICommandHandler — resolved from the same DI scope, so behaviour cannot drift from the portal's.
/// Everything here is reflection over the conventions every endpoint already follows; an action
/// whose classes break the convention fails loudly at boot via <see cref="AiActionRegistry"/>.
/// </summary>
internal static class AiActionExecutor
{
    private const int MaxResultChars = 24_000;

    public static async Task<string> RunAsync(AiAction action, AiToolContext context, JsonElement arguments, CancellationToken cancellationToken)
    {
        var command = AiActionSchema.Bind(action, arguments, context.User.Email, context.User.DisplayName, out var bindProblems);
        if (bindProblems.Count > 0)
            return Serialise(new { ok = false, errors = bindProblems });

        var authorisation = context.Services.GetRequiredService(action.AuthorisationType);
        if (!InvokeAllows(authorisation, context.User, command))
            return Serialise(new { ok = false, error = "Your portal roles do not allow this action." });

        if (action.ValidationType is not null)
        {
            var validation = context.Services.GetRequiredService(action.ValidationType);
            var outcome = await InvokeCheckAsync(validation, command, cancellationToken).ConfigureAwait(false);
            if (outcome.HasFailed)
                return Serialise(new { ok = false, errors = outcome.Errors });
        }

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(action.CommandType, action.ResultType);
        var handler = context.Services.GetRequiredService(handlerType);
        var handle = handlerType.GetMethod("HandleAsync")!;
        object? result;
        try
        {
            var task = (Task)handle.Invoke(handler, new object[] { command, cancellationToken })!;
            await task.ConfigureAwait(false);
            result = task.GetType().GetProperty("Result")?.GetValue(task);
        }
        catch (TargetInvocationException wrapped) when (wrapped.InnerException is InvalidOperationException guard)
        {
            // Handlers signal business guards ("already approved", "missing project") this way and
            // the HTTP endpoints answer them as messages, not 500s — same courtesy for the model.
            return Serialise(new { ok = false, error = guard.Message });
        }
        catch (InvalidOperationException guard)
        {
            return Serialise(new { ok = false, error = guard.Message });
        }

        var payload = Serialise(new { ok = true, result });
        return payload.Length <= MaxResultChars
            ? payload
            : Serialise(new
            {
                ok = true,
                note = "The action completed; its full result was too large to return and was clipped.",
                result = payload[..MaxResultChars]
            });
    }

    /// <summary>Allows(user, command) by convention. Shared gate classes (ValuationReportAuthorisation
    /// and friends) carry MANY typed Allows overloads gating different role sets, so the overload is
    /// selected by the command's own type — never "the first one". Asserted for every action at boot
    /// by <see cref="AiActionRegistry"/> via <see cref="FindAllows"/>.</summary>
    private static bool InvokeAllows(object authorisation, SignedInUser user, object command)
    {
        var method = FindAllows(authorisation.GetType(), command.GetType())
            ?? throw new InvalidOperationException(
                $"{authorisation.GetType().Name} has no Allows overload for {command.GetType().Name}.");
        var arguments = method.GetParameters()
            .Select(parameter => parameter.ParameterType.IsInstanceOfType(user) ? user : command)
            .ToArray();
        return (bool)method.Invoke(authorisation, arguments)!;
    }

    /// <summary>The Allows overload for this command type: one whose non-user parameter is
    /// assignable from the command, else a user-only overload. Null when neither exists.</summary>
    internal static MethodInfo? FindAllows(Type authorisationType, Type commandType)
    {
        var candidates = authorisationType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == "Allows")
            .ToList();
        return candidates.FirstOrDefault(method => method.GetParameters()
                   .Any(parameter => parameter.ParameterType != typeof(SignedInUser)
                                     && parameter.ParameterType.IsAssignableFrom(commandType)))
               ?? candidates.FirstOrDefault(method => method.GetParameters()
                   .All(parameter => parameter.ParameterType == typeof(SignedInUser)));
    }

    /// <summary>Check(command) / CheckAsync(command, ct) — both conventions exist; overloads are
    /// matched to the command type the same way as Allows.</summary>
    internal static MethodInfo? FindCheck(Type validationType, Type commandType)
    {
        var candidates = validationType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name is "Check" or "CheckAsync")
            .ToList();
        return candidates.FirstOrDefault(method => method.GetParameters()
            .Any(parameter => parameter.ParameterType != typeof(CancellationToken)
                              && parameter.ParameterType.IsAssignableFrom(commandType)));
    }

    private static async Task<ValidationOutcome> InvokeCheckAsync(object validation, object command, CancellationToken cancellationToken)
    {
        var method = FindCheck(validation.GetType(), command.GetType())
            ?? throw new InvalidOperationException(
                $"{validation.GetType().Name} has no Check/CheckAsync overload for {command.GetType().Name}.");
        var arguments = method.GetParameters()
            .Select(parameter => parameter.ParameterType == typeof(CancellationToken) ? cancellationToken : command)
            .ToArray();
        var outcome = method.Invoke(validation, arguments)!;
        return outcome is Task<ValidationOutcome> task ? await task.ConfigureAwait(false) : (ValidationOutcome)outcome;
    }

    private static string Serialise(object value) => JsonSerializer.Serialize(value, AiActionSchema.ResultOptions);
}
