namespace StructuredLogging.Analyzers.Tests;

public class StructuredLoggingAnalyzerTests
{
    [Fact]
    public async Task Reports_template_not_compile_time_constant()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger, string name)
    {
        logger.Information("Hello " + name);
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("SLG001", diagnostics[0].Id);
    }

    [Fact]
    public async Task Reports_duplicate_properties()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("Start {Name} End {Name}", 1, 2);
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("SLG002", diagnostics[0].Id);
    }

    [Fact]
    public async Task Reports_positional_properties()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("{0} {1}", 1, 2);
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("SLG003", diagnostics[0].Id);
    }

    [Fact]
    public async Task Reports_sentence_message()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("Operation completed.");
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("SLG004", diagnostics[0].Id);
    }

    [Fact]
    public async Task Does_not_report_for_valid_named_fragment()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("Processing {Name}", "n");
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Empty(diagnostics);
    }
}
