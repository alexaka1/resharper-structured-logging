using System;

namespace StructuredLogging.Core.Templates;

public sealed class TextToken : MessageTemplateToken
{
    public TextToken(string text, int startIndex = -1)
        : base(startIndex)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public string Text { get; }

    public override int Length => Text.Length;

    public override string ToString() => Text;
}
