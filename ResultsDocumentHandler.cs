using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Saxon.Api;


namespace XMLWPFToolbox
{
    public class MyResultDocumentsHandler
    {
        private Processor processor;
        public Dictionary<string, MySerializer> ResultDocuments { get; set; }

        public MyResultDocumentsHandler(Processor proc, Dictionary<string, MySerializer> resultDocuments)
        {
            processor = proc;
            ResultDocuments = resultDocuments;
        }


        public IDestination ResultDocumentHandler(string href, Uri baseUri)
        {
            var mySerializer = new MySerializer(processor);
            ResultDocuments[new Uri(baseUri, href).AbsoluteUri] = mySerializer;
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
