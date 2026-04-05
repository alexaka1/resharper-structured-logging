using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StructuredLogging.Analyzers.Internal;

internal enum PropertyNamingType
{
    PascalCase,
    CamelCase,
    SnakeCase,
    ElasticNaming,
}

internal sealed class AnalyzerOptionsProvider
{
    private const string NamingKey = "structured_logging_property_naming_type";
    private const string IgnoredRegexKey = "structured_logging_ignored_properties_regex";

    public AnalyzerOptionsProvider(AnalyzerOptions options)
    {
        var optionsProvider = options.AnalyzerConfigOptionsProvider;
        if (optionsProvider.GlobalOptions.TryGetValue(NamingKey, out var namingValue))
        {
            NamingType = ParseNamingType(namingValue);
        }

        if (optionsProvider.GlobalOptions.TryGetValue(IgnoredRegexKey, out var regexValue) && !string.IsNullOrWhiteSpace(regexValue))
        {
            IgnoredPropertiesRegex = new Regex(regexValue, RegexOptions.Compiled);
        }

        if (options.AdditionalFiles.Length > 0)
        {
            foreach (var additionalFile in options.AdditionalFiles.Where(f => f.Path.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase)))
            {
                var text = additionalFile.GetText();
                if (text is null)
                {
                    continue;
                }

                foreach (var line in text.Lines)
                {
                    var content = line.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(content) || content.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (content.StartsWith(NamingKey, StringComparison.Ordinal))
                    {
                        var separator = content.IndexOf('=');
                        if (separator < 0 || separator == content.Length - 1)
                        {
                            continue;
                        }

                        var value = content.Substring(separator + 1).Trim();
                        NamingType = ParseNamingType(value);
                        continue;
                    }

                    if (content.StartsWith(IgnoredRegexKey, StringComparison.Ordinal))
                    {
                        var separator = content.IndexOf('=');
                        if (separator < 0 || separator == content.Length - 1)
                        {
                            continue;
                        }

                        var value = content.Substring(separator + 1).Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            IgnoredPropertiesRegex = new Regex(value, RegexOptions.Compiled);
                        }
                    }
                }
            }
        }
    }

    public PropertyNamingType NamingType { get; private set; } = PropertyNamingType.PascalCase;

    public Regex? IgnoredPropertiesRegex { get; private set; }

    private static PropertyNamingType ParseNamingType(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "pascal" or "pascalcase" or "pascal_case" => PropertyNamingType.PascalCase,
            "camel" or "camelcase" or "camel_case" => PropertyNamingType.CamelCase,
            "snake" or "snakecase" or "snake_case" => PropertyNamingType.SnakeCase,
            "elastic" or "elasticnaming" or "elastic_naming" => PropertyNamingType.ElasticNaming,
            _ => PropertyNamingType.PascalCase,
        };
    }
}
