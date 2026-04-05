using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using StructuredLogging.Core.Templates;

namespace StructuredLogging.Analyzers.Internal;

internal static class TemplatePropertyMap
{
    public static PropertyToken? TryMapPropertyToken(IInvocationOperation invocation, IArgumentOperation templateArgument, MessageTemplate messageTemplate, IArgumentOperation argument)
    {
        if (messageTemplate.NamedProperties is null || messageTemplate.NamedProperties.Count == 0)
        {
            return null;
        }

        var invocationArguments = invocation.Arguments.ToArray();
        var templateArgumentIndex = Array.IndexOf(invocationArguments, templateArgument);
        if (templateArgumentIndex < 0)
        {
            return null;
        }

        var argumentIndex = Array.IndexOf(invocationArguments, argument);
        if (argumentIndex <= templateArgumentIndex)
        {
            return null;
        }

        var index = argumentIndex - templateArgumentIndex - 1;
        if (index < 0 || index >= messageTemplate.NamedProperties.Count)
        {
            return null;
        }

        return (PropertyToken)messageTemplate.NamedProperties[index];
    }

    public static Location GetPropertyLocation(IArgumentOperation templateArgument)
    {
        return templateArgument.Syntax.GetLocation();
    }
}
