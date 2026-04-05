using System.Linq;
using System.Text;

namespace StructuredLogging.Analyzers.Internal;

internal static class PropertyNameProvider
{
    public static string GetSuggestedName(string propertyName, PropertyNamingType namingType)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return propertyName;
        }

        var words = SplitWords(propertyName).ToArray();
        if (words.Length == 0)
        {
            return propertyName;
        }

        return namingType switch
        {
            PropertyNamingType.PascalCase => string.Concat(words.Select(Capitalize)),
            PropertyNamingType.CamelCase => LowercaseFirst(string.Concat(words.Select(Capitalize))),
            PropertyNamingType.SnakeCase => string.Join("_", words.Select(w => w.ToLowerInvariant())),
            PropertyNamingType.ElasticNaming => string.Join(".", words.Select(w => w.ToLowerInvariant())),
            _ => propertyName,
        };
    }

    private static string LowercaseFirst(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    private static string Capitalize(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        if (word.Length == 1)
        {
            return word.ToUpperInvariant();
        }

        return char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
    }

    private static System.Collections.Generic.IEnumerable<string> SplitWords(string input)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (!char.IsLetterOrDigit(c))
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }

                continue;
            }

            if (sb.Length > 0 && char.IsUpper(c) && char.IsLower(sb[sb.Length - 1]))
            {
                yield return sb.ToString();
                sb.Clear();
            }

            sb.Append(c);
        }

        if (sb.Length > 0)
        {
            yield return sb.ToString();
        }
    }
}
