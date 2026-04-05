using System;
using System.Globalization;

namespace StructuredLogging.Core.Templates;

public sealed class PropertyToken : MessageTemplateToken
{
    private readonly string _rawText;
    private readonly int? _position;

    public PropertyToken(
        string propertyName,
        string rawText,
        string? format = null,
        Alignment? alignment = null,
        Destructuring destructuring = Destructuring.Default,
        int startIndex = -1)
        : base(startIndex)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        _rawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
        Format = format;
        Alignment = alignment;
        Destructuring = destructuring;

        if (int.TryParse(PropertyName, NumberStyles.None, CultureInfo.InvariantCulture, out var position) && position >= 0)
        {
            _position = position;
        }
    }

    public string PropertyName { get; }

    public Destructuring Destructuring { get; }

    public string? Format { get; }

    public Alignment? Alignment { get; }

    public bool IsPositional => _position.HasValue;

    public override int Length => _rawText.Length;

    public bool TryGetPositionalValue(out int position)
    {
        if (_position.HasValue)
        {
            position = _position.Value;
            return true;
        }

        position = default;
        return false;
    }

    public override string ToString() => _rawText;
}
