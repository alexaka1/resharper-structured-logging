using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using StructuredLogging.Analyzers.Diagnostics;
using StructuredLogging.Analyzers.Internal;
using StructuredLogging.Core.Templates;

namespace StructuredLogging.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructuredLoggingAnalyzer : DiagnosticAnalyzer
{
    private static readonly Regex DotAtTheEnd = new(@"(?<!\.)\.$", RegexOptions.Compiled);

    private readonly IMessageTemplateParser _parser = new MessageTemplateParser();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.TemplateIsNotCompileTimeConstant,
            DiagnosticDescriptors.DuplicateTemplateProperty,
            DiagnosticDescriptors.PositionalPropertyUsed,
            DiagnosticDescriptors.LogMessageIsSentence,
            DiagnosticDescriptors.ContextualLoggerConstructorMismatch,
            DiagnosticDescriptors.ContextualLoggerFactoryMismatch,
            DiagnosticDescriptors.ExceptionPassedAsTemplateArgument,
            DiagnosticDescriptors.InconsistentPropertyNaming,
            DiagnosticDescriptors.ComplexObjectDestructuring,
            DiagnosticDescriptors.AnonymousObjectDestructuring);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterSymbolAction(AnalyzeConstructorSymbol, SymbolKind.Method);
    }

    private static void AnalyzeConstructorSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method || method.MethodKind != MethodKind.Constructor)
        {
            return;
        }

        var containingType = method.ContainingType;
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type is not INamedTypeSymbol namedType ||
                namedType.TypeArguments.Length != 1 ||
                !string.Equals(namedType.ConstructedFrom.ToDisplayString(), "Microsoft.Extensions.Logging.ILogger<TCategoryName>", StringComparison.Ordinal))
            {
                continue;
            }

            var contextType = namedType.TypeArguments[0];
            if (SymbolEqualityComparer.Default.Equals(contextType, containingType))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ContextualLoggerConstructorMismatch,
                parameter.Locations.FirstOrDefault() ?? method.Locations.First(),
                contextType.ToDisplayString(),
                containingType.ToDisplayString()));
        }
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        AnalyzeContextualFactory(context, invocation);
        AnalyzeContextPushPropertyNaming(context, invocation);

        var templateArgument = TemplateArgumentResolver.TryResolveTemplateArgument(invocation);
        if (templateArgument is null)
        {
            return;
        }

        if (!TryGetConstantString(templateArgument.Value, out var templateText))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TemplateIsNotCompileTimeConstant,
                templateArgument.Syntax.GetLocation()));
            return;
        }

        var options = new AnalyzerOptionsProvider(context.Options);
        var messageTemplate = _parser.Parse(templateText);

        if (messageTemplate.PositionalProperties is { Count: > 0 })
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PositionalPropertyUsed,
                templateArgument.Syntax.GetLocation()));
        }

        if (messageTemplate.NamedProperties is { Count: > 0 } &&
            messageTemplate.NamedProperties.GroupBy(t => t.PropertyName).Any(g => g.Count() > 1))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DuplicateTemplateProperty,
                templateArgument.Syntax.GetLocation()));
        }

        if (DotAtTheEnd.IsMatch(templateText))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LogMessageIsSentence,
                templateArgument.Syntax.GetLocation()));
        }

        AnalyzeExceptionPassing(context, invocation, templateArgument);
        AnalyzePropertyNaming(context, templateArgument, messageTemplate, options);
        AnalyzeDestructuring(context, invocation, templateArgument, messageTemplate);
    }

    private static void AnalyzeContextualFactory(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;
        if (!InvocationTargets.IsSerilogForContextFactory(method) || method.TypeArguments.Length == 0)
        {
            return;
        }

        var containingType = context.ContainingSymbol?.ContainingType;
        if (containingType is null)
        {
            return;
        }

        var invocationType = method.TypeArguments[0];
        if (SymbolEqualityComparer.Default.Equals(invocationType, containingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ContextualLoggerFactoryMismatch,
            invocation.Syntax.GetLocation(),
            invocationType.ToDisplayString(),
            containingType.ToDisplayString()));
    }

    private static void AnalyzeExceptionPassing(OperationAnalysisContext context, IInvocationOperation invocation, IArgumentOperation templateArgument)
    {
        var logicalArguments = GetLogicalArgumentsAfterTemplate(invocation, templateArgument);
        var invalidExceptionArgument = logicalArguments
            .Select(GetUnderlyingOperation)
            .FirstOrDefault(op => TypeClassification.IsException(op.Type, context.Compilation));

        if (invalidExceptionArgument is null || templateArgument.Parameter is null)
        {
            return;
        }

        var overloadAvailable = invocation.TargetMethod.ContainingType
            .GetMembers(invocation.TargetMethod.Name)
            .OfType<IMethodSymbol>()
            .Any(overload =>
                overload.Parameters.Any(p => TypeClassification.IsException(p.Type, context.Compilation)) &&
                overload.Parameters.Any(p => string.Equals(p.Name, templateArgument.Parameter.Name, StringComparison.Ordinal)));

        if (!overloadAvailable)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ExceptionPassedAsTemplateArgument,
            invalidExceptionArgument.Syntax.GetLocation()));
    }

    private static void AnalyzePropertyNaming(
        OperationAnalysisContext context,
        IArgumentOperation templateArgument,
        MessageTemplate messageTemplate,
        AnalyzerOptionsProvider options)
    {
        if (messageTemplate.NamedProperties is null || messageTemplate.NamedProperties.Count == 0)
        {
            return;
        }

        foreach (var property in messageTemplate.NamedProperties)
        {
            if (string.IsNullOrWhiteSpace(property.PropertyName))
            {
                continue;
            }

            if (options.IgnoredPropertiesRegex?.IsMatch(property.PropertyName) == true)
            {
                continue;
            }

            var suggestedName = PropertyNameProvider.GetSuggestedName(property.PropertyName, options.NamingType);
            if (string.Equals(property.PropertyName, suggestedName, StringComparison.Ordinal))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InconsistentPropertyNaming,
                templateArgument.Syntax.GetLocation(),
                property.PropertyName,
                suggestedName));
        }
    }

    private static void AnalyzeDestructuring(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        IArgumentOperation templateArgument,
        MessageTemplate messageTemplate)
    {
        if (messageTemplate.NamedProperties is null || messageTemplate.NamedProperties.Count == 0)
        {
            return;
        }

        var logicalArguments = GetLogicalArgumentsAfterTemplate(invocation, templateArgument);
        for (var index = 0; index < logicalArguments.Length && index < messageTemplate.NamedProperties.Count; index++)
        {
            var property = (PropertyToken)messageTemplate.NamedProperties[index];
            if (property.Destructuring != Destructuring.Default)
            {
                continue;
            }

            var underlyingOperation = GetUnderlyingOperation(logicalArguments[index]);
            if (underlyingOperation is IAnonymousObjectCreationOperation || underlyingOperation.Type?.IsAnonymousType == true)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AnonymousObjectDestructuring,
                    underlyingOperation.Syntax.GetLocation(),
                    property.PropertyName));
                continue;
            }

            if (TypeClassification.NeedsDestructuring(underlyingOperation.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ComplexObjectDestructuring,
                    underlyingOperation.Syntax.GetLocation(),
                    property.PropertyName));
            }
        }
    }

    private static void AnalyzeContextPushPropertyNaming(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!InvocationTargets.IsSerilogPushProperty(invocation.TargetMethod) || invocation.Arguments.Length == 0)
        {
            return;
        }

        var propertyNameArgument = invocation.Arguments[0];
        if (!TryGetConstantString(propertyNameArgument.Value, out var propertyName) || string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        var options = new AnalyzerOptionsProvider(context.Options);
        if (options.IgnoredPropertiesRegex?.IsMatch(propertyName) == true)
        {
            return;
        }

        var suggestedName = PropertyNameProvider.GetSuggestedName(propertyName, options.NamingType);
        if (string.Equals(propertyName, suggestedName, StringComparison.Ordinal))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InconsistentPropertyNaming,
            propertyNameArgument.Syntax.GetLocation(),
            propertyName,
            suggestedName));
    }

    private static bool TryGetConstantString(IOperation operation, out string value)
    {
        if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is string stringValue)
        {
            value = stringValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static IOperation GetUnderlyingOperation(IOperation operation)
    {
        var current = operation;
        while (current is IConversionOperation conversion)
        {
            current = conversion.Operand;
        }

        return current;
    }

    private static ImmutableArray<IOperation> GetLogicalArgumentsAfterTemplate(IInvocationOperation invocation, IArgumentOperation templateArgument)
    {
        if (invocation.Syntax is not InvocationExpressionSyntax invocationSyntax ||
            invocationSyntax.ArgumentList is null ||
            invocation.SemanticModel is null)
        {
            return ImmutableArray<IOperation>.Empty;
        }

        var argumentSyntaxes = invocationSyntax.ArgumentList.Arguments;
        var templateIndex = -1;
        for (var i = 0; i < argumentSyntaxes.Count; i++)
        {
            if (argumentSyntaxes[i].Expression.Span.Equals(templateArgument.Syntax.Span))
            {
                templateIndex = i;
                break;
            }
        }

        if (templateIndex < 0)
        {
            return ImmutableArray<IOperation>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<IOperation>();
        for (var i = templateIndex + 1; i < argumentSyntaxes.Count; i++)
        {
            var operation = invocation.SemanticModel.GetOperation(argumentSyntaxes[i].Expression);
            if (operation is not null)
            {
                builder.Add(operation);
            }
        }

        return builder.ToImmutable();
    }
}
