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

        [JetBrains.Annotations.MessageTemplateFormatMethodAttribute(""messageTemplate"")]
        void Information(System.Exception exception, string messageTemplate, params object[] args);

        [JetBrains.Annotations.MessageTemplateFormatMethodAttribute(""messageTemplate"")]
        void Error(string messageTemplate, params object[] args);

        [JetBrains.Annotations.MessageTemplateFormatMethodAttribute(""messageTemplate"")]
        void Error(System.Exception exception, string messageTemplate, params object[] args);

        ILogger ForContext<T>();
    }

    public static class Log
    {
        public static ILogger Logger => null;
    }
}

namespace Serilog.Context
{
    public static class LogContext
    {
        public static void PushProperty(string name, object value) { }
    }
}

namespace Microsoft.Extensions.Logging
{
    public interface ILogger<TCategoryName> { }

    public static class LoggerExtensions
    {
        public static void LogInformation(this ILogger<object> logger, string message, params object[] args) { }
        public static System.IDisposable BeginScope(this ILogger<object> logger, string messageFormat, params object[] args) => null;
    }
}
";
}
