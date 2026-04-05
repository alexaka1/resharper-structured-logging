using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace StructuredLogging.Analyzers.Internal;

internal static class TemplateArgumentResolver
{
    public static IArgumentOperation? TryResolveTemplateArgument(IInvocationOperation invocation)
    {
        var templateParameterName = TryResolveTemplateParameterName(invocation.TargetMethod);
        if (templateParameterName is null)
        {
            return null;
        }

        return invocation.Arguments.FirstOrDefault(a =>
            string.Equals(a.Parameter?.Name, templateParameterName, StringComparison.Ordinal));
    }

    private static string? TryResolveTemplateParameterName(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (!string.Equals(attribute.AttributeClass?.Name, "MessageTemplateFormatMethodAttribute", StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string parameterName &&
                !string.IsNullOrWhiteSpace(parameterName))
            {
                return parameterName;
            }
        }

        var containingType = method.ContainingType.ToDisplayString();

        if (string.Equals(containingType, "Microsoft.Extensions.Logging.LoggerExtensions", StringComparison.Ordinal))
        {
            return string.Equals(method.Name, "BeginScope", StringComparison.Ordinal) ? "messageFormat" : "message";
        }

        if (string.Equals(containingType, "ZLogger.ZLoggerExtensions", StringComparison.Ordinal))
        {
            return "format";
        }

        var commonCandidates = new[] { "messageTemplate", "message", "format", "formatString" };
        var byName = method.Parameters.FirstOrDefault(p => commonCandidates.Contains(p.Name, StringComparer.Ordinal));
        return byName?.Name;
    }
}
