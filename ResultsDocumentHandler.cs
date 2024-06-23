using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.sun.xml.@internal.messaging.saaj.util;
using net.liberty_development.SaxonHE12s9apiExtensions;
using net.sf.saxon.s9api;

using java.util.function;

using JURI = java.net.URI;
using javax.swing;


namespace XMLWPFToolbox
{
    public class MyResultDocumentsHandler : Function
    {
        private Processor processor;
        public Dictionary<string, MySerializer> ResultDocuments { get; set; }

        public MyResultDocumentsHandler(Processor proc, Dictionary<string, MySerializer> resultDocuments)
        {
            processor = proc;
            ResultDocuments = resultDocuments;
        }


        public object apply(object uri)
        {
            JURI juri = (JURI)uri;
            var mySerializer = new MySerializer(processor);
            ResultDocuments[juri.toASCIIString()] = mySerializer;
            return mySerializer.serializer;
        }

        public Dictionary<string, string> GetSerializedResultDocuments()
        {
            return
                ResultDocuments.ToDictionary(dict => dict.Key, dict =>
                {
                    var mySerializer = dict.Value;
                    var resultWriter = mySerializer.stringWriter;
                    var result = resultWriter.ToString();
                    resultWriter.Close();
                    return result;
                });
        }

        public Function andThen(Function after)
        {
            throw new NotImplementedException();
        }

        public Function compose(Function before)
        {
            throw new NotImplementedException();
        }
    }

    public class MySerializer
    {
        Processor processor { get; set; }
        public Serializer serializer { get; set; }
        public StringWriter stringWriter { get; set; }

        public MySerializer(Processor proc)
        {
            processor = proc;
            stringWriter = new StringWriter();
            serializer = processor.NewSerializer(stringWriter);
        }
    }
}
