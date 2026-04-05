using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogging.Core.Templates;

public sealed class MessageTemplateParser : IMessageTemplateParser
{
    public MessageTemplate Parse(string messageTemplate)
    {
        if (messageTemplate is null)
        {
            throw new ArgumentNullException(nameof(messageTemplate));
        }

        return new MessageTemplate(messageTemplate, Tokenize(messageTemplate));
    }

    private static IEnumerable<MessageTemplateToken> Tokenize(string messageTemplate)
    {
        if (messageTemplate.Length == 0)
        {
            yield return new TextToken(string.Empty, 0);
            yield break;
        }

        var nextIndex = 0;
        while (true)
        {
            var beforeText = nextIndex;
            var textToken = ParseTextToken(nextIndex, messageTemplate, out nextIndex);
            if (nextIndex > beforeText)
            {
                yield return textToken;
            }

            if (nextIndex == messageTemplate.Length)
            {
                yield break;
            }

            var beforeProp = nextIndex;
            var propToken = ParsePropertyToken(nextIndex, messageTemplate, out nextIndex);
            if (beforeProp < nextIndex)
            {
                yield return propToken;
            }

            if (nextIndex == messageTemplate.Length)
            {
                yield break;
            }
        }
    }

    private static MessageTemplateToken ParsePropertyToken(int startAt, string messageTemplate, out int next)
    {
        var first = startAt;
        startAt++;

        while (startAt < messageTemplate.Length && IsValidInPropertyTag(messageTemplate[startAt]))
        {
            startAt++;
        }

        if (startAt == messageTemplate.Length || messageTemplate[startAt] != '}')
        {
            next = startAt;
            return new TextToken(messageTemplate.Substring(first, next - first), first);
        }

        next = startAt + 1;

        var rawText = messageTemplate.Substring(first, next - first);
        var tagContent = rawText.Substring(1, next - (first + 2));
        if (tagContent.Length == 0)
        {
            return new TextToken(rawText, first);
        }

        if (!TrySplitTagContent(tagContent, out var propertyNameAndDestructuring, out var format, out var alignment))
        {
            return new TextToken(rawText, first);
        }

        var propertyName = propertyNameAndDestructuring;
        var destructuring = Destructuring.Default;
        if (propertyName.Length != 0 && TryGetDestructuringHint(propertyName[0], out destructuring))
        {
            propertyName = propertyName.Substring(1);
        }

        if (propertyName.Length == 0)
        {
            return new TextToken(rawText, first);
        }

        for (var i = 0; i < propertyName.Length; ++i)
        {
            if (!IsValidInPropertyName(propertyName[i]))
            {
                return new TextToken(rawText, first);
            }
        }

        if (format != null)
        {
            for (var i = 0; i < format.Length; ++i)
            {
                if (!IsValidInFormat(format[i]))
                {
                    return new TextToken(rawText, first);
                }
            }
        }

        Alignment? alignmentValue = null;
        if (alignment != null)
        {
            for (var i = 0; i < alignment.Length; ++i)
            {
                if (!IsValidInAlignment(alignment[i]))
                {
                    return new TextToken(rawText, first);
                }
            }

            var lastDash = alignment.LastIndexOf('-');
            if (lastDash > 0)
            {
                return new TextToken(rawText, first);
            }

            if (!int.TryParse(lastDash == -1 ? alignment : alignment.Substring(1), out var width) || width == 0)
            {
                return new TextToken(rawText, first);
            }

            var direction = lastDash == -1 ? AlignmentDirection.Right : AlignmentDirection.Left;
            alignmentValue = new Alignment(direction, width);
        }

        return new PropertyToken(propertyName, rawText, format, alignmentValue, destructuring, first);
    }

    private static bool TrySplitTagContent(string tagContent, out string propertyNameAndDestructuring, out string? format, out string? alignment)
    {
        var formatDelim = tagContent.IndexOf(':');
        var alignmentDelim = tagContent.IndexOf(',');

        if (formatDelim == -1 && alignmentDelim == -1)
        {
            propertyNameAndDestructuring = tagContent;
            format = null;
            alignment = null;
            return true;
        }

        if (alignmentDelim == -1 || (formatDelim != -1 && alignmentDelim > formatDelim))
        {
            propertyNameAndDestructuring = tagContent.Substring(0, formatDelim);
            format = formatDelim == tagContent.Length - 1 ? null : tagContent.Substring(formatDelim + 1);
            alignment = null;
            return true;
        }

        propertyNameAndDestructuring = tagContent.Substring(0, alignmentDelim);
        if (formatDelim == -1)
        {
            if (alignmentDelim == tagContent.Length - 1)
            {
                format = null;
                alignment = null;
                return false;
            }

            format = null;
            alignment = tagContent.Substring(alignmentDelim + 1);
            return true;
        }

        if (alignmentDelim == formatDelim - 1)
        {
            format = null;
            alignment = null;
            return false;
        }

        alignment = tagContent.Substring(alignmentDelim + 1, formatDelim - alignmentDelim - 1);
        format = formatDelim == tagContent.Length - 1 ? null : tagContent.Substring(formatDelim + 1);
        return true;
    }

    private static bool IsValidInPropertyTag(char c) =>
        IsValidInDestructuringHint(c) ||
        IsValidInPropertyName(c) ||
        IsValidInFormat(c) ||
        c == ':';

    private static bool IsValidInPropertyName(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == ' ';

    private static bool TryGetDestructuringHint(char c, out Destructuring destructuring)
    {
        switch (c)
        {
            case '@':
                destructuring = Destructuring.Destructure;
                return true;
            case '$':
                destructuring = Destructuring.Stringify;
                return true;
            default:
                destructuring = Destructuring.Default;
                return false;
        }
    }

    private static bool IsValidInDestructuringHint(char c) => c == '@' || c == '$';

    private static bool IsValidInAlignment(char c) => char.IsDigit(c) || c == '-';

    private static bool IsValidInFormat(char c) => c != '}' && (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ' || c == '+');

    private static TextToken ParseTextToken(int startAt, string messageTemplate, out int next)
    {
        var first = startAt;
        var accum = new StringBuilder();

        do
        {
            var current = messageTemplate[startAt];
            if (current == '{')
            {
                if (startAt + 1 < messageTemplate.Length && messageTemplate[startAt + 1] == '{')
                {
                    accum.Append(current);
                    startAt++;
                }
                else
                {
                    break;
                }
            }
            else
            {
                accum.Append(current);
                if (current == '}' && startAt + 1 < messageTemplate.Length && messageTemplate[startAt + 1] == '}')
                {
                    startAt++;
                }
            }

            startAt++;
        } while (startAt < messageTemplate.Length);

        next = startAt;
        return new TextToken(accum.ToString(), first);
    }
}
