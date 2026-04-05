using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
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
            DiagnosticDescriptors.LogMessageIsSentence);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
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
}
