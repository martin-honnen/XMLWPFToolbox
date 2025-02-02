using org.xml.sax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLWPFToolbox
{
    public class MyErrorHandler : ErrorHandler
    {
        public MyErrorHandler()
        {
        }
        public bool Valid
        {
            get;
            private set;
        } = true;

        public List<string> ErrorList
        {
            get;
            private set;

        } = new List<string>();

        public void error(SAXParseException exception)
        {
            Valid = false;
            ErrorList.Add(FormatSAXParseException(exception));
        }
        public void fatalError(SAXParseException exception)
        {
            Valid = false;
            ErrorList.Add(FormatSAXParseException(exception));

        }
        public void warning(SAXParseException exception)
        {
            ErrorList.Add(FormatSAXParseException(exception));
        }

        private static string FormatSAXParseException(SAXParseException exception)
        {
            return string.Format("{0} ({1}:{2})", exception.Message, exception.getLineNumber(), exception.getColumnNumber());
        }
    }
}
