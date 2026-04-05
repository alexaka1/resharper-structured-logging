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
        Assert.Single(diagnostics, d => d.Id == "SLG001");
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
        Assert.Single(diagnostics, d => d.Id == "SLG002");
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
        Assert.Single(diagnostics, d => d.Id == "SLG003");
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
        Assert.Single(diagnostics, d => d.Id == "SLG004");
    }

    [Fact]
    public async Task Reports_contextual_logger_constructor_mismatch()
    {
        const string source = """
class MyService
{
    public MyService(Microsoft.Extensions.Logging.ILogger<object> logger)
    {
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics, d => d.Id == "SLG101");
    }

    [Fact]
    public async Task Reports_contextual_logger_factory_mismatch()
    {
        const string source = """
class MyService
{
    void M(Serilog.ILogger logger)
    {
        var contextual = logger.ForContext<object>();
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics, d => d.Id == "SLG102");
    }

    [Fact]
    public async Task Reports_exception_passed_as_template_argument()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger, System.Exception ex)
    {
        logger.Error("Failed {Reason}", ex);
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics, d => d.Id == "SLG103");
    }

    [Fact]
    public async Task Reports_property_naming_mismatch_with_editorconfig()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("Processing {order_id}", 1);
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(
            source,
            "root = true",
            "[*.cs]",
            "structured_logging_property_naming_type = pascal_case");

        Assert.Single(diagnostics, d => d.Id == "SLG104");
    }

    [Fact]
    public async Task Reports_complex_object_without_destructuring()
    {
        const string source = """
class Data { }

class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("Data {Value}", new Data());
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics, d => d.Id == "SLG105");
    }

    [Fact]
    public async Task Reports_anonymous_object_without_destructuring()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("Data {Value}", new { A = 1 });
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Single(diagnostics, d => d.Id == "SLG106");
    }

    [Fact]
    public async Task Reports_context_push_property_naming_mismatch()
    {
        const string source = """
class C
{
    void M()
    {
        Serilog.Context.LogContext.PushProperty("order_id", 1);
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(
            source,
            "root = true",
            "[*.cs]",
            "structured_logging_property_naming_type = pascal_case");

        Assert.Single(diagnostics, d => d.Id == "SLG104");
    }

    [Fact]
    public async Task Does_not_report_for_valid_named_fragment()
    {
        const string source = """
class C
{
    void M(Serilog.ILogger logger)
    {
        logger.Information("Processing {OrderId}", "n");
    }
}
""";

        var diagnostics = await AnalyzerVerifier.AnalyzeAsync(source);
        Assert.Empty(diagnostics);
    }
}
