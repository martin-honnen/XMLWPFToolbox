using java.util.function;
using net.sf.saxon.s9api;

namespace XMLWPFToolbox
{
    internal class SimpleMessageHandler : Consumer
    {

        public List<Message> Messages = new List<Message>();

        public string GetMessages() => string.Join("\n", Messages.Select(m => $"{m.getContent()} ({m.getLocation().getLineNumber()}:{m.getLocation().getColumnNumber()})"));

        public void accept(object t)
        {
            Messages.Add((Message)t);
        }


        public Consumer andThen(Consumer after)
        {
            throw new NotImplementedException();
        }
    }
}