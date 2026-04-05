namespace StructuredLogging.Core.Templates;

public abstract class MessageTemplateToken
{
    protected MessageTemplateToken(int startIndex)
    {
        StartIndex = startIndex;
    }

    public int StartIndex { get; }

    public abstract int Length { get; }
}
