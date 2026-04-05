using System;
using System.Collections.Generic;
using System.Linq;

namespace StructuredLogging.Core.Templates;

public sealed class MessageTemplate
{
    public MessageTemplate(string text, IEnumerable<MessageTemplateToken> tokens)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        Text = text;
        Tokens = tokens.ToArray();

        var propertyTokens = Tokens.OfType<PropertyToken>().ToArray();
        if (propertyTokens.Length == 0)
        {
            return;
        }

        var allPositional = true;
        var anyPositional = false;

        foreach (var propertyToken in propertyTokens)
        {
            if (propertyToken.IsPositional)
            {
                anyPositional = true;
            }
            else
            {
                allPositional = false;
            }
        }

        if (allPositional)
        {
            PositionalProperties = propertyTokens;
            return;
        }

        if (anyPositional)
        {
            IsMixedTemplate = true;
        }

        NamedProperties = propertyTokens;
    }

    public string Text { get; }

    public IReadOnlyList<MessageTemplateToken> Tokens { get; }

    public IReadOnlyList<PropertyToken>? NamedProperties { get; }

    public IReadOnlyList<PropertyToken>? PositionalProperties { get; }

    public bool IsMixedTemplate { get; }
}
