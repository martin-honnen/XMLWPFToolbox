using Saxon.Api;

namespace XMLWPFToolbox
{
    internal class SimpleMessageHandler
    {

        public List<Message> Messages = new List<Message>();

        public string GetMessages() => string.Join("\n", Messages.Select(m => $"{m.Content} ({m.Location.LineNumber}:{m.Location.ColumnNumber})"));

        public void Handle(Message message)
        {
            Messages.Add(message);
        }
    }
}