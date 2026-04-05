using System;
using Microsoft.CodeAnalysis;

namespace StructuredLogging.Analyzers.Internal;

internal static class InvocationTargets
{
    public static bool IsSerilogPushProperty(IMethodSymbol method)
    {
        return string.Equals(method.Name, "PushProperty", StringComparison.Ordinal) &&
               string.Equals(method.ContainingType.ToDisplayString(), "Serilog.Context.LogContext", StringComparison.Ordinal);
    }

    public static bool IsSerilogForContextFactory(IMethodSymbol method)
    {
        return string.Equals(method.Name, "ForContext", StringComparison.Ordinal) &&
               method.IsGenericMethod &&
               method.TypeArguments.Length == 1 &&
               method.ContainingType.ToDisplayString().StartsWith("Serilog.", StringComparison.Ordinal);
    }

    public static bool IsMicrosoftLogger(IMethodSymbol method)
    {
        var containingType = method.ContainingType.ToDisplayString();
        return string.Equals(containingType, "Microsoft.Extensions.Logging.LoggerExtensions", StringComparison.Ordinal) ||
               string.Equals(containingType, "Microsoft.Extensions.Logging.ILogger", StringComparison.Ordinal);
    }
}
