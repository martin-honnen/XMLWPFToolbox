using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using net.sf.saxon.lib;
using net.sf.saxon.s9api;


namespace XMLWPFToolbox
{
    internal class SimpleErrorCollector : ErrorReporter
    {
        public List<XmlProcessingError> ErrorList { get; private set; } = new List<XmlProcessingError>();
        public void report(XmlProcessingError xpe)
        {
            ErrorList.Add(xpe);
        }
    }
}
