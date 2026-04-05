using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StructuredLogging.Analyzers.Tests;

internal static class AnalyzerVerifier
{
    internal static async Task<IReadOnlyList<Diagnostic>> AnalyzeAsync(string source, params string[] editorConfigLines)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source + "\n" + CommonStubs.Source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new StructuredLoggingAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);

        var options = editorConfigLines.Length == 0
            ? new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty)
            : new AnalyzerOptions(
                ImmutableArray.Create<AdditionalText>(
                    new InMemoryAdditionalText("/workspace/.editorconfig", SourceText.From(string.Join("\n", editorConfigLines)))));

        var diagnostics = await compilation.WithAnalyzers(analyzers, options).GetAnalyzerDiagnosticsAsync();
        return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
    }

    private sealed class InMemoryAdditionalText(string path, SourceText text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            return text;
        }
    }
}
