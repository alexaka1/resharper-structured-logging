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

    public static readonly DiagnosticDescriptor ContextualLoggerConstructorMismatch = new(
        id: "SLG101",
        title: "Logger context type should match containing type",
        messageFormat: "ILogger<T> generic type '{0}' does not match containing class '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Injected ILogger<T> should use the containing class type.",
        helpLinkUri: $"{HelpLinkBase}/ContextualLoggerProblem.md");

    public static readonly DiagnosticDescriptor ContextualLoggerFactoryMismatch = new(
        id: "SLG102",
        title: "ForContext type should match containing type",
        messageFormat: "ForContext<T> type '{0}' does not match containing class '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Serilog ForContext<T> should use containing class type.",
        helpLinkUri: $"{HelpLinkBase}/ContextualLoggerProblem.md");

    public static readonly DiagnosticDescriptor ExceptionPassedAsTemplateArgument = new(
        id: "SLG103",
        title: "Exception passed as template argument",
        messageFormat: "Exception argument should be passed in exception parameter overload",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Exception argument is passed as template argument while overload with exception parameter exists.",
        helpLinkUri: $"{HelpLinkBase}/ExceptionPassedAsTemplateArgumentProblem.md");

    public static readonly DiagnosticDescriptor InconsistentPropertyNaming = new(
        id: "SLG104",
        title: "Inconsistent log property naming",
        messageFormat: "Property name '{0}' does not match naming convention; suggested '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Property names should follow configured naming convention.",
        helpLinkUri: $"{HelpLinkBase}/InconsistentLogPropertyNaming.md");

    public static readonly DiagnosticDescriptor ComplexObjectDestructuring = new(
        id: "SLG105",
        title: "Complex object should be destructured",
        messageFormat: "Complex object argument for '{0}' should be destructured with '@'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Complex object likely needs destructuring to log meaningful properties.",
        helpLinkUri: $"{HelpLinkBase}/ComplexObjectDestructuringProblem.md");

    public static readonly DiagnosticDescriptor AnonymousObjectDestructuring = new(
        id: "SLG106",
        title: "Anonymous object should be destructured",
        messageFormat: "Anonymous object argument for '{0}' should be destructured with '@'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Anonymous object arguments should be destructured.",
        helpLinkUri: $"{HelpLinkBase}/AnonymousObjectDestructuringProblem.md");
}
