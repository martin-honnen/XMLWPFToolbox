using Saxon.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace XMLWPFToolbox
{
    internal class SimpleErrorCollector
    {
        public List<Error> ErrorList { get; private set; } = new List<Error>();
        public void ErrorReporter(Error xpe)
        {
            ErrorList.Add(xpe);
        }
    }
}
