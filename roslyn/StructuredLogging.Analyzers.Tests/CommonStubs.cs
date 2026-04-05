namespace StructuredLogging.Analyzers.Tests;

internal static class CommonStubs
{
    internal const string Source = @"
namespace JetBrains.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class MessageTemplateFormatMethodAttribute : System.Attribute
    {
        public MessageTemplateFormatMethodAttribute(string templateParameterName) { }
    }
}

namespace Serilog
{
    public interface ILogger
    {
        [JetBrains.Annotations.MessageTemplateFormatMethodAttribute(""messageTemplate"")]
        void Information(string messageTemplate, params object[] args);
    }
}

namespace Microsoft.Extensions.Logging
{
    public interface ILogger { }

    public static class LoggerExtensions
    {
        public static void LogInformation(this ILogger logger, string message, params object[] args) { }
        public static System.IDisposable BeginScope(this ILogger logger, string messageFormat, params object[] args) => null;
    }
}
";
}
