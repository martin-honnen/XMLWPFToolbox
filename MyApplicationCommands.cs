using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XMLWPFToolbox.Commands;

public static class MyApplicationCommands
{
    public static RoutedUICommand LoadXmlInput = new RoutedUICommand("Load XML input file", "LoadXmlInput", typeof(MyApplicationCommands));

    public static RoutedUICommand LoadJsonInput = new RoutedUICommand("Load JSON input file", "LoadJsonInput", typeof(MyApplicationCommands));

    public static RoutedUICommand LoadXsltCode = new RoutedUICommand("Load XSLT file", "LoadXsltCode", typeof(MyApplicationCommands));

    public static RoutedUICommand LoadXQueryCode = new RoutedUICommand("Load XQuery file", "LoadXQueryCode", typeof(MyApplicationCommands));
    public static RoutedUICommand LoadXPathCode = new RoutedUICommand("Load XPath file", "LoadXPathCode", typeof(MyApplicationCommands));

    public static RoutedUICommand LoadXsdCode = new RoutedUICommand("Load XSD file", "LoadXsdCode", typeof(MyApplicationCommands));

    public static RoutedUICommand NewXsltCode = new RoutedUICommand("New XSLT file", "NewXsltCode", typeof(MyApplicationCommands));
    public static RoutedUICommand NewXQueryCode = new RoutedUICommand("New XQuery file", "NewXQueryCode", typeof(MyApplicationCommands)); 
    
    public static RoutedUICommand NewXsdCode = new RoutedUICommand("New XSD file", "NewXsdCode", typeof(MyApplicationCommands));
    public static RoutedUICommand NewXPathCode = new RoutedUICommand("New XPath file", "NewXPathCode", typeof(MyApplicationCommands));
    
    public static RoutedUICommand NewXmlInput = new RoutedUICommand("New XML file", "NewXmlInput", typeof(MyApplicationCommands));
    public static RoutedUICommand NewJsonInput = new RoutedUICommand("New JSON file", "NewJsonInput", typeof(MyApplicationCommands));

    public static RoutedUICommand SaveInputDocument = new RoutedUICommand("Save input document", "SaveInputDocument", typeof(MyApplicationCommands));

    public static RoutedUICommand SaveXsltCode = new RoutedUICommand("Save XSLT code", "SaveXsltCode", typeof(MyApplicationCommands));

    public static RoutedUICommand SaveXQueryCode = new RoutedUICommand("Save XQuery code", "SaveXQueryCode", typeof(MyApplicationCommands));

    public static RoutedUICommand SaveXPathCode = new RoutedUICommand("Save XPath code", "SaveXPathCode", typeof(MyApplicationCommands));

    public static RoutedUICommand SaveXsdCode = new RoutedUICommand("Save XSD code", "SaveXsdCode", typeof(MyApplicationCommands));


    public static RoutedUICommand SaveResultDocument = new RoutedUICommand("Save result document", "SaveResultDocument", typeof(MyApplicationCommands));

    public static RoutedUICommand SaveAll = new RoutedUICommand("Save all", "SaveAll", typeof(MyApplicationCommands));


    public static RoutedUICommand NewPadWindow = new RoutedUICommand("New XSLT/XQuery/XPath Notepad window", "NewPadWindow", typeof(MyApplicationCommands));

    public static RoutedUICommand AboutXMLToolbox = new RoutedUICommand("About XSLT/XQuery/XPath/XSD Toolbox", "AboutDialogue", typeof(MyApplicationCommands));

}
