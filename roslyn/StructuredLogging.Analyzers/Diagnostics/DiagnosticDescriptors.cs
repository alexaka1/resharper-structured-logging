using Microsoft.CodeAnalysis;

namespace StructuredLogging.Analyzers.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string Category = "StructuredLogging";
    private const string HelpLinkBase = "https://github.com/alexaka1/resharper-structured-logging/tree/master/rules";

    public static readonly DiagnosticDescriptor TemplateIsNotCompileTimeConstant = new(
        id: "SLG001",
        title: "Template should be a compile-time constant",
        messageFormat: "Template should be a compile-time constant",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Structured logging templates should be compile-time constants.",
        helpLinkUri: $"{HelpLinkBase}/TemplateIsNotCompileTimeConstantProblem.md");

    public static readonly DiagnosticDescriptor DuplicateTemplateProperty = new(
        id: "SLG002",
        title: "Duplicate properties in a template",
        messageFormat: "Template contains duplicate property names",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Template contains duplicate property names.",
        helpLinkUri: $"{HelpLinkBase}/TemplateDuplicatePropertyProblem.md");

    public static readonly DiagnosticDescriptor PositionalPropertyUsed = new(
        id: "SLG003",
        title: "Prefer named properties instead of positional ones",
        messageFormat: "Template contains positional property placeholders",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Prefer named properties in message templates.",
        helpLinkUri: $"{HelpLinkBase}/PositionalPropertyUsedProblem.md");

    public static readonly DiagnosticDescriptor LogMessageIsSentence = new(
        id: "SLG004",
        title: "Log event messages should be fragments, not sentences",
        messageFormat: "Log event messages should be fragments, not sentences",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Log message template ends with period.",
        helpLinkUri: $"{HelpLinkBase}/LogMessageIsSentenceProblem.md");
}
