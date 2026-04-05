namespace StructuredLogging.Core.Templates;

public interface IMessageTemplateParser
{
    MessageTemplate Parse(string messageTemplate);
}
