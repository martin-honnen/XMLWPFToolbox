using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Schema;
using Microsoft.Win32;

using ICSharpCode.AvalonEdit.Highlighting;
using Path = System.IO.Path;


using System.IO;
using PhoenixmlDb.XQuery.Functions;
using PhoenixmlDb.Xslt;
using PhoenixmlDb.Xslt.Engine;
using PhoenixmlDb.XQuery.Execution;
using PhoenixmlDb.XQuery.Parser;
using System.Xml;
using PhoenixmlDb.Xdm;

using Saxon.Api;

namespace XMLWPFToolbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private static SchemaFactory xsd11SchemaFactory = new org.apache.xerces.jaxp.validation.XMLSchema11Factory();
        //private static SchemaFactory xsd10SchemaFactory = new org.apache.xerces.jaxp.validation.XMLSchemaFactory();

        static MainWindow()
        {
            //ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("xercesImpl"));

            //ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("icu4j"));

            //ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("java-cup"));

            //ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("xpath2"));
            
            //Assembly.Load("xpath2");
        }

        private static readonly string defaultBaseInputURI = "urn:from-string";

        private bool useSaxonEngine = true;

        private string baseResultURI = defaultBaseInputURI;

        private string baseXsltCodeURI = defaultBaseInputURI;

        private string baseXQueryCodeURI = defaultBaseInputURI;

        private string baseXPathCodeURI = defaultBaseInputURI;

        private string baseXsdCodeURI = defaultBaseInputURI;

        private string baseInputCodeURI = defaultBaseInputURI;

        private static string parseJsonFromXslt = @"<xsl:stylesheet version=""3.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
  <xsl:output method=""adaptive""/>
  <xsl:param name=""json"" as=""xs:string""/>
  <xsl:template name=""xsl:initial-template"">
    <xsl:sequence select=""parse-json($json)""/>
  </xsl:template>
</xsl:stylesheet>";

        private static Processor processor = new Processor();

        private XPathCompiler xpathCompiler;

        private XQueryCompiler xqueryCompiler;

        private XsltCompiler xsltCompiler;

        private DocumentBuilder docBuilder;

        private JsonBuilder jsonBuilder;

        //private XPathSelector jsonBuilder;

        private Serializer serializer;

        private XPathSelector xpathResultSerializer;

        private PhoenixmlDb.Xslt.XsltTransformer transformer;

        private PhoenixmlDb.Xslt.XsltTransformer parseJsonTransformer;

        private PhoenixmlDb.XQuery.XQueryFacade xqueryFacade;

        private SelectionChangedEventHandler selectionChangedEventHandler;

        private DispatcherTimer typingTimer;
        public MainWindow()
        {
            InitializeComponent();

            xpathCompiler = processor.NewXPathCompiler();

            xqueryCompiler = processor.NewXQueryCompiler();

            xsltCompiler = processor.NewXsltCompiler();

            serializer = processor.NewSerializer();

            docBuilder = processor.NewDocumentBuilder();

            //XPathCompiler jsonDocCompiler = processor.NewXPathCompiler();
            //jsonDocCompiler.DeclareVariable(new QName("input"));

            jsonBuilder = processor.NewJsonBuilder();

            XPathCompiler xpathResultCompiler = processor.NewXPathCompiler();
            xpathResultCompiler.DeclareVariable(new QName("value"));
            xpathResultCompiler.DeclareVariable(new QName("serialization-parameters"));

            xpathResultSerializer = xpathResultCompiler.Compile("serialize($value, $serialization-parameters)").Load();

            transformer = new PhoenixmlDb.Xslt.XsltTransformer();

            parseJsonTransformer = new PhoenixmlDb.Xslt.XsltTransformer();

            xqueryFacade = new PhoenixmlDb.XQuery.XQueryFacade();

            typingTimer = new DispatcherTimer(DispatcherPriority.ContextIdle);
            typingTimer.Interval = TimeSpan.FromSeconds(1.2);
            typingTimer.Tick += TypingTimer_Tick;

            typingTimer.IsEnabled = (bool)autoEvaluateCbx.IsChecked;

        }

        private async void TypingTimer_Tick(object sender, EventArgs e)
        {
            if ((bool)autoEvaluateCbx.IsChecked)
            {
                typingTimer.IsEnabled = false;
                typingTimer.Stop();
                await evaluateCurrentCodeType();
            }
        }

        private XdmItem ParseJson(string json)
        {
            //jsonBuilder.SetVariable(new QName("input"), new XdmAtomicValue(json));
            return (XdmItem)jsonBuilder.parseJson(json);
        }

        private async Task<XdmSequence> ParseJsonPhoenixml(string json)
        {
            await parseJsonTransformer.LoadStylesheetAsync(parseJsonFromXslt);

            parseJsonTransformer.SetInitialTemplate("initial-template", "http://www.w3.org/1999/XSL/Transform");

            parseJsonTransformer.SetParameter("json", json);

            return await parseJsonTransformer.TransformToSequenceAsync((XdmSequence?)null);
        }
        private void xpathEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            runXPathEvaluation();
        }

        private async void xqueryEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            await runXQueryEvaluation();
        }

        private async void xsltTransformationButton_Click(object sender, RoutedEventArgs e)
        {
            await runXsltTransformation();
        }

        //private void xsdValidationButton_Click(object sender, RoutedEventArgs e)
        //{
        //    runXsdValidation();
        //}

        private void DisplayResultDocuments(Dictionary<string, string> serializedDocuments)
        {
            resultDocumentList.ItemsSource = serializedDocuments.Keys;
            resultDocumentList.SelectionChanged += selectionChangedEventHandler = (s, e) => DisplayResultDocuments(serializedDocuments[((ComboBox)s).SelectedItem as string]);
            resultDocumentList.SelectedIndex = 0;
        }

        private void DisplayResultDocuments(string result)
        {
            resultEditor.Text = result;

            resultWebView.NavigateToString(result);

            resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
        }

        private void ClearResultDocumentList()
        {
            if (selectionChangedEventHandler != null)
            {
                resultDocumentList.SelectionChanged -= selectionChangedEventHandler;
            }
            resultDocumentList.ItemsSource = null;
        }

        private void ShowResultDocumentList()
        {
            resultPanel.Visibility = Visibility.Visible;
        }

        private void HideResultDocumentList()
        {
            resultPanel.Visibility = Visibility.Collapsed;
        }
        private void ShowGridRow(int rowIndex)
        {
            mainGrid.RowDefinitions[rowIndex].Height = new GridLength(1, GridUnitType.Star);
        }
        private void HideGridRow(int rowIndex)
        {
            mainGrid.RowDefinitions[rowIndex].Height = new GridLength(0);
        }
        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ToggleEngine_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            useSaxonEngine = !useSaxonEngine;
            if (useSaxonEngine)
            {
                statusText.Text = "Switched to SaxonCS-HE engine.";
            }
            else
            {
                statusText.Text = "Switched to Phoenixml engine.";
            }
        }

        private void NewPadWindow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var secondaryWindow = new MainWindow();
            secondaryWindow.Show();
        }
        private void NewXsltCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            codeEditor.Text = @"<xsl:stylesheet xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" version=""3.0""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema""
  exclude-result-prefixes=""#all""
  expand-text=""yes"">

  <xsl:mode on-no-match=""shallow-copy""/>

  <xsl:output indent=""yes""/>

  <xsl:template match=""/"" name=""xsl:initial-template"">
    <xsl:copy>
      <xsl:apply-templates/>
      <xsl:comment>Run with {system-property('xsl:product-name')} {system-property('xsl:product-version')} at {current-dateTime()}</xsl:comment>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>";

            baseXsltCodeURI = defaultBaseInputURI;

            codeTypeXslt.IsChecked = true;

        }

        private void NewXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            codeEditor.Text = @"declare namespace map = ""http://www.w3.org/2005/xpath-functions/map"";
declare namespace array = ""http://www.w3.org/2005/xpath-functions/array"";

declare namespace output = ""http://www.w3.org/2010/xslt-xquery-serialization"";

declare option output:method ""xml"";
declare option output:indent ""yes"";

.";

            baseXQueryCodeURI = defaultBaseInputURI;

            codeTypeXQuery.IsChecked = true;
        }

        private void NewXsdCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            codeEditor.Text = @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">

</xs:schema>";

            baseXsdCodeURI = defaultBaseInputURI;

            //codeTypeXsd.IsChecked = true;
        }

        private void NewXPathCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            codeEditor.Text = @"";

            baseXPathCodeURI = defaultBaseInputURI;

            codeTypeXPath.IsChecked = true;
        }

        private void NewXmlInput_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            inputEditor.Text = @"<?xml version=""1.0"" encoding=""UTF-8""?>";

            baseInputCodeURI = defaultBaseInputURI;
        }

        private void NewJsonInput_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            inputEditor.Text = @"{
  
}";

            baseInputCodeURI = defaultBaseInputURI;
        }
        private void LoadXmlInput_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseInputCodeURI = LoadFileIntoEditor(inputEditor, "XML files|*.xml|XHTML files|*.xhtml|All files|*.*", xmlInputType);
        }

        private void LoadJsonInput_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseInputCodeURI = LoadFileIntoEditor(inputEditor, "JSON files|*.json|All files|*.*", jsonInputType);
        }

        private void LoadXsltCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseXsltCodeURI = LoadFileIntoEditor(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*", codeTypeXslt) ?? defaultBaseInputURI;
        }

        private void LoadXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseXQueryCodeURI = LoadFileIntoEditor(codeEditor, "XQuery files|*.xq;*.xqy;*.xqu;*.xqm;*.xql;*.xquery|All files|*.*", codeTypeXQuery);
        }
        private void LoadXPathCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseXPathCodeURI = LoadFileIntoEditor(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*", codeTypeXPath);
        }

        //private void LoadXsdCode_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    baseXsdCodeURI = LoadFileIntoEditor(codeEditor, "XSD schema files|*.xsd|XML files|*.xml|All files|*.*", codeTypeXsd);
        //}

        private void SaveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveInputDocument_Executed(sender, e);
            if ((bool)codeTypeXslt.IsChecked)
            {
                SaveXsltCode_Executed(sender, e);
            }
            else if ((bool)codeTypeXQuery.IsChecked)
            {
                SaveXQueryCode_Executed(sender, e);
            }
            else if ((bool)codeTypeXPath.IsChecked)
            {
                SaveXPathCode_Executed(sender, e);
            }
            //else if ((bool)codeTypeXsd.IsChecked)
            //{
            //    SaveXsdCode_Executed(sender, e);
            //}
        }
        private void SaveXsltCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*", baseXsltCodeURI);
            if (result != null) {
                baseXsltCodeURI = result;
            }
        }

        private void SaveXsltCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*", defaultBaseInputURI);
            if (result != null)
            {
                baseXsltCodeURI = result;
            }
        }

        private void SaveXQueryCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XQuery files|*.xq;*.xqy;*.xqu;*.xqm;*.xql;*.xquery|All files|*.*", defaultBaseInputURI);
            if (result != null)
            {
                baseXQueryCodeURI = result;
            }
        }

        private void SaveXPathCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*", defaultBaseInputURI);
            if (result != null)
            {
                baseXPathCodeURI = result;
            }
        }

        private void SaveXsdCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XSD schema files|*.xsd|XML files|*.xml|All files|*.*", defaultBaseInputURI);
            if (result != null)
            {
                baseXsdCodeURI = result;
            }
        }

        private void SaveInputDocumentAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(inputEditor, "XML files|*.xml|JSON files|*.json|All files|*.*", defaultBaseInputURI);
            if (result != null)
            {
                baseInputCodeURI = result;
            }
        }

        private void SaveXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XQuery files|*.xq;*.xqy;*.xqu;*.xqm;*.xql;*.xquery|All files|*.*", baseXQueryCodeURI);
            if (result != null)
            {
                baseXQueryCodeURI = result;
            }
        }
        private void SaveXPathCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*", baseXPathCodeURI);
            if (result != null)
            {
                baseXPathCodeURI = result;
            }
        }

        private void SaveXsdCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(codeEditor, "XSD schema files|*.xsd|XML files|*.xml|All files|*.*", baseXsdCodeURI);
            if (result != null)
            {
                baseXsdCodeURI = result;
            }
        }
        private void SaveResultDocument_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(resultEditor, "HTML files|*.html;*.html|XML files|*.xml|Text files|*.txt;*.text|JSON files|*.json|All files|*.*", baseResultURI);
            if (result != null)
            {
                baseResultURI = result;
            }
        }

        private void SaveInputDocument_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = SaveEditorToFile(inputEditor, "XML files|*.xml|JSON files|*.json|All files|*.*", baseInputCodeURI);
            if (result != null)
            {
                baseInputCodeURI = result;
            }
        }

        private void AboutXMLToolbox_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("XSLT 3.0, XQuery 3.1, XPath 3.1 XML Toolbox using Saxon " + processor.ProductVersion + " or PhoenixmlDb.Xslt 1.6.10/PhoenixmlDb.XQuery 1.6.9" +  $" run under {Environment.OSVersion} .NET {Environment.Version}", "About XSLT 3.0/XQuery 3.1/XPath 3.1 Toolbox");
        }

        private string LoadFileIntoEditor(ICSharpCode.AvalonEdit.TextEditor editor, string filter, RadioButton type)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.CheckFileExists = true;

            openFileDialog.Filter = filter;

            if (openFileDialog.ShowDialog() ?? false)
            {
                //editor.Text = File.ReadAllText(openFileDialog.FileName);
                editor.Load(openFileDialog.FileName);
                type.IsChecked = true;
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(openFileDialog.FileName));
                return new Uri(openFileDialog.FileName).AbsoluteUri;
            }

            return null;
        }

        private string SaveEditorToFile(ICSharpCode.AvalonEdit.TextEditor editor, string filter, string baseURI)
        {
            string currentFileName = baseURI == defaultBaseInputURI ? null : new Uri(baseURI).AbsolutePath;

            if (currentFileName == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.Filter = filter;

                if (saveFileDialog.ShowDialog() ?? false)
                {
                    currentFileName = saveFileDialog.FileName;
                }
                else
                {
                    return null;
                }
            }

            editor.Save(currentFileName);

            return new Uri(currentFileName).AbsoluteUri;
        }

        private void XmlInputType_Click(object sender, RoutedEventArgs e)
        {
            inputEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
        }

        private void JsonInputType_Click(object sender, RoutedEventArgs e)
        {
            inputEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Json");
        }

        private async Task runXsltTransformation()
        {
            statusText.Text = "";
            ClearResultDocumentList();
            ShowResultDocumentList();
            resultEditor.Clear();

            if (useSaxonEngine)
            {
                runXsltTransformationSaxon();
            }
            else
            {
                await runXsltTransformationPhoenixml();
            }

        }

        private void runXsltTransformationSaxon()
        {
            var errorCollector = new SimpleErrorCollector();
            xsltCompiler.ErrorReporter = errorCollector.ErrorReporter;

            try
            {
                statusText.Text = "Compiling XSLT code...";

                xsltCompiler.BaseUri = new Uri(baseXsltCodeURI);

                Xslt30Transformer transformer = xsltCompiler.Compile(new StringReader(codeEditor.Text)).Load30();

                transformer.ErrorReporter = errorCollector.ErrorReporter;

                //var loggerWriter = new JStringWriter();

                //var traceLogger = new StandardLogger(loggerWriter);

                //transformer.SetTraceFunctionDestination(traceLogger);

                var messageHandler = new SimpleMessageHandler();
                transformer.MessageListener = messageHandler.Handle;

                transformer.BaseOutputURI = "urn:to-string";

                var mainSerializer = new MySerializer(processor);
                Dictionary<string, MySerializer> resultDocuments = new Dictionary<string, MySerializer>();
                resultDocuments["*** principal result ***"] = mainSerializer;

                var resultDocumentsHandler = new MyResultDocumentsHandler(processor, resultDocuments);

                transformer.ResultDocumentHandler = resultDocumentsHandler.ResultDocumentHandler;

                XdmItem inputItem = null;

                if ((bool)xmlInputType.IsChecked)
                {
                    statusText.Text = "Parsing XML input document...";

                    docBuilder.BaseUri = new Uri(baseInputCodeURI);
                    inputItem = docBuilder.Build(new StringReader(inputEditor.Text));
                }
                else if ((bool)jsonInputType.IsChecked)
                {
                    statusText.Text = "Parsing JSON input...";

                    inputItem = (XdmItem)jsonBuilder.ParseJson(inputEditor.Text);
                }


                if (inputItem == null)
                {
                    statusText.Text = "Running xsl:initialTemplate...";

                    transformer.CallTemplate(null, mainSerializer.serializer);

                    statusText.Text = "";

                    var serializedResultDocuments = resultDocumentsHandler.GetSerializedResultDocuments();

                    if (messageHandler.Messages.Any())
                    {
                        serializedResultDocuments.Add("*** messages ***", messageHandler.GetMessages());

                    }

                    //var traces = loggerWriter.ToString();

                    //if (traces != string.Empty)
                    //{
                    //    serializedResultDocuments.Add("*** trace ***", loggerWriter.ToString());
                    //}

                    //traceLogger.close();

                    DisplayResultDocuments(serializedResultDocuments);
                }
                else
                {
                    transformer.GlobalContextItem = inputItem;

                    statusText.Text = "Applying templates processing...";

                    transformer.ApplyTemplates(inputItem, mainSerializer.serializer);

                    statusText.Text = "";

                    var serializedResultDocuments = resultDocumentsHandler.GetSerializedResultDocuments();

                    if (messageHandler.Messages.Any())
                    {
                        serializedResultDocuments.Add("*** messages ***", messageHandler.GetMessages());

                    }

                    //var traces = loggerWriter.ToString();

                    //if (traces != string.Empty)
                    //{
                    //    serializedResultDocuments.Add("*** trace ***", loggerWriter.ToString());
                    //}

                    //traceLogger.close();

                    DisplayResultDocuments(serializedResultDocuments);
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
                //throw ex;

                var errors = errorCollector.ErrorList;
                if (errors.Any())
                {
                    statusText.Text += string.Format(": {0}: {1}:{2}", errors.First().Message, errors.First().Location.LineNumber, errors.First().Location.ColumnNumber);
                    resultEditor.Text = string.Join("\n", errors.Select(error => string.Format("{0}: {1}:{2}", error.Message, error.Location.LineNumber, error.Location.ColumnNumber)));
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                }
            }
        }

        private async Task runXsltTransformationPhoenixml()
        {

            //xsltCompiler.setErrorReporter(new SimpleErrorCollector());

            try
            {
                statusText.Text = "Compiling XSLT code...";

                transformer = new PhoenixmlDb.Xslt.XsltTransformer();

                using var traceWriter = new StringWriter();
                transformer.TraceListener = (depth, evt, details) => traceWriter.WriteLine($"{new string(' ', depth * 2)}{evt}: {details}");

                using var messageWriter = new StringWriter();
                transformer.MessageListenerWithLocation = (message, terminate, line, col) => messageWriter.WriteLine($"{message} ({line}:{col})");

                await transformer.LoadStylesheetAsync(codeEditor.Text, new Uri(baseXsltCodeURI));

                //Xslt30Transformer transformer = xsltCompiler.compile(new StreamSource(new JStringReader(codeEditor.Text), baseXsltCodeURI)).load30();

                //var loggerWriter = new JStringWriter();

                //var traceLogger = new StandardLogger(loggerWriter);

                //transformer.setTraceFunctionDestination(traceLogger);

                //var messageHandler = new SimpleMessageHandler();
                //transformer.setMessageHandler(messageHandler);

                //transformer.setBaseOutputURI("urn:to-string"); //.BaseOutputURI = "urn:to-string";

                //var mainSerializer = new MySerializer(processor);
                //Dictionary<string, MySerializer> resultDocuments = new Dictionary<string, MySerializer>();
                //resultDocuments["*** principal result ***"] = mainSerializer;

                //var resultDocumentsHandler = new MyResultDocumentsHandler(processor, resultDocuments);

                //transformer.setResultDocumentHandler(resultDocumentsHandler); //ResultDocumentHandler = new MyResultDocumentsHandler(processor, resultDocuments);

                //XdmItem inputItem = null;

                //if ((bool)xmlInputType.IsChecked)
                //{
                //    statusText.Text = "Parsing XML input document...";

                //    docBuilder.setBaseURI(new JURI(baseInputCodeURI));  //.BaseUri = new Uri(baseInputCodeURI);
                //    inputItem = docBuilder.build(new StreamSource(new JStringReader(inputEditor.Text)));
                //}
                //else if ((bool)jsonInputType.IsChecked)
                //{
                //    statusText.Text = "Parsing JSON input...";

                //    inputItem = (XdmItem)jsonBuilder.parseJson(inputEditor.Text);
                //}


                if (!(bool)xmlInputType.IsChecked && !(bool)jsonInputType.IsChecked)
                {
                    statusText.Text = "Running xsl:initialTemplate...";

                    transformer.SetInitialTemplate("initial-template", "http://www.w3.org/1999/XSL/Transform");

                    var result = await transformer.TransformAsync((string?)null);

                    var serializedResultDocuments = new Dictionary<string, string>() { { "*** principal result ***", result } };

                    statusText.Text = "";

                    foreach (var kvp in transformer.SecondaryResultDocuments)
                    {
                        serializedResultDocuments[kvp.Key] = kvp.Value;
                    }

                    var traces = traceWriter.ToString();

                    if (traces != string.Empty)
                    {
                        serializedResultDocuments.Add("*** trace ***", traces);
                    }

                    var messages = messageWriter.ToString();

                    if (messages != string.Empty)
                    {
                        serializedResultDocuments.Add("*** messages ***", messages);
                    }
                    //if (messageHandler.Messages.Any())
                    //{
                    //    serializedResultDocuments.Add("*** messages ***", messageHandler.GetMessages());

                    //}

                    //var traces = loggerWriter.ToString();

                    //if (traces != string.Empty)
                    //{
                    //    serializedResultDocuments.Add("*** trace ***", loggerWriter.ToString());
                    //}

                    //traceLogger.close();

                    DisplayResultDocuments(serializedResultDocuments);
                }
                else if ((bool)jsonInputType.IsChecked)
                {

                    statusText.Text = "Parsing JSON...";

                    var inputJson = await ParseJsonPhoenixml(inputEditor.Text);

                    statusText.Text = "Applying templates processing...";

                    if (baseInputCodeURI != null)
                    {
                        transformer.SetSourceDocumentUri(new Uri(baseInputCodeURI));
                    }

                    var result = await transformer.TransformAsync(inputJson);

                    var serializedResultDocuments = new Dictionary<string, string>() { { "*** principal result ***", result } };

                    statusText.Text = "";

                    foreach (var kvp in transformer.SecondaryResultDocuments)
                    {
                        serializedResultDocuments[kvp.Key] = kvp.Value;
                    }

                    var traces = traceWriter.ToString();

                    if (traces != string.Empty)
                    {
                        serializedResultDocuments.Add("*** trace ***", traces);
                    }

                    var messages = messageWriter.ToString();

                    if (messages != string.Empty)
                    {
                        serializedResultDocuments.Add("*** messages ***", messages);
                    }
                    

                    DisplayResultDocuments(serializedResultDocuments);

                }
                else
                {

                    statusText.Text = "Applying templates processing...";

                    if (baseInputCodeURI != null)
                    {
                        transformer.SetSourceDocumentUri(new Uri(baseInputCodeURI));
                    }

                    var result = await transformer.TransformAsync(inputEditor.Text);

                    var serializedResultDocuments = new Dictionary<string, string>() { { "*** principal result ***", result } };

                    statusText.Text = "";

                    foreach (var kvp in transformer.SecondaryResultDocuments)
                    {
                        serializedResultDocuments[kvp.Key] = kvp.Value;
                    }

                    var traces = traceWriter.ToString();

                    if (traces != string.Empty)
                    {
                        serializedResultDocuments.Add("*** trace ***", traces);
                    }

                    var messages = messageWriter.ToString();

                    if (messages != string.Empty)
                    {
                        serializedResultDocuments.Add("*** messages ***", messages);
                    }
                    //if (messageHandler.Messages.Any())
                    //{
                    //    serializedResultDocuments.Add("*** messages ***", messageHandler.GetMessages());

                    //}

                    //var traces = loggerWriter.ToString();

                    //if (traces != string.Empty)
                    //{
                    //    serializedResultDocuments.Add("*** trace ***", loggerWriter.ToString());
                    //}

                    //traceLogger.close();

                    DisplayResultDocuments(serializedResultDocuments);
                }
            }
            catch (XmlException ex)
            {
                statusText.Text = ex.Message;
                resultEditor.Text = $"{ex.Message} in {ex.SourceUri}{ex.LineNumber}:{ex.LinePosition}";
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
            }
            catch (XsltException ex)
            {
                statusText.Text = ex.Message;
                resultEditor.Text = ex.Location is { } loc
                    ? $"{ex.Message} in {loc.Module} at {loc.Line}:{loc.Column}"
                    : ex.Message;
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                //throw ex;

                //var errors = (xsltCompiler.getErrorReporter() as SimpleErrorCollector).ErrorList;
                //if (errors.Any())
                //{
                //    statusText.Text += string.Format(": {0}: {1}:{2}", errors.First().getMessage(), errors.First().getLocation().getLineNumber(), errors.First().getLocation().getColumnNumber());
                //    resultEditor.Text = string.Join("\n", errors.Select(error => string.Format("{0}: {1}:{2}", error.getMessage(), error.getLocation().getLineNumber(), error.getLocation().getColumnNumber())));
                //    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                //}
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
                resultEditor.Text = ex.Message;
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
            }
        }

        private void codeEditor_TextChanged(object sender, EventArgs e)
        {
            if ((bool)autoEvaluateCbx.IsChecked)
            {
                typingTimer.Start();
            }
        }

        private void autoEvaluateCbx_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)autoEvaluateCbx.IsChecked)
            {
                typingTimer.IsEnabled = true;
                typingTimer.Start();
            }
            else
            {
                typingTimer.IsEnabled = false;
                typingTimer.Stop();
            }
        }

        private async void evaluateCode_Click(object sender, RoutedEventArgs e)
        {
            await evaluateCurrentCodeType();
        }

        private async Task evaluateCurrentCodeType()
        {
            if ((bool)codeTypeXslt.IsChecked)
            {
                await runXsltTransformation();
            }
            else if ((bool)codeTypeXQuery.IsChecked)
            {
                await runXQueryEvaluation();
            }
            else if ((bool)codeTypeXPath.IsChecked)
            {
                runXPathEvaluation();
            }
            //else if ((bool)codeTypeXsd.IsChecked)
            //{
            //    runXsdValidation();
            //}
        }

        //private void runXsdValidation()
        //{
        //    statusText.Text = "";
        //    HideResultDocumentList();
        //    ClearResultDocumentList();
        //    renderResultCbx.IsChecked = false;
        //    resultEditor.Clear();

        //    Schema schema;

        //    if (codeEditor.Text == string.Empty)
        //    {
        //        schema = xsd11SchemaFactory.newSchema();
        //    }
        //    else
        //    {
        //        statusText.Text = "Parsing/compiling your schema...";
        //        try
        //        {
        //            schema = xsd11SchemaFactory.newSchema(new StreamSource(new JStringReader(codeEditor.Text), baseXsdCodeURI));
        //        }
        //        catch (SAXParseException ex)
        //        {
        //            statusText.Text += "Schema parsing failed: " + ex.Message;
        //            return;
        //        }
        //    }

        //    Validator validator = schema.newValidator();

        //    var myErrorHandler = new MyErrorHandler();

        //    validator.setErrorHandler(myErrorHandler);

        //    statusText.Text = "Parsing/validating your XML input...";

        //    string resultString;

        //    try
        //    {
        //        validator.validate(new StreamSource(new JStringReader(inputEditor.Text), baseInputCodeURI));
        //    }
        //    catch (SAXParseException ex)
        //    {
        //        statusText.Text = "Parsing your XML input failed:" + ex.Message + " (" + ex.getLineNumber() + ":" + ex.getColumnNumber() + ")";
        //        resultString = "Parsing your XML input failed:\r\n" + ex.Message + " (" + ex.getLineNumber() + ":" + ex.getColumnNumber() + ")\r\n";
        //    }


        //    if (myErrorHandler.Valid)
        //    {
        //        statusText.Text = resultString = "XML input is valid against schema.";
        //    }
        //    else
        //    {
        //        statusText.Text = $"Validation failed: {myErrorHandler.ErrorList.Count} errors.";
        //        resultString = "Validation failed:\r\n" + string.Join("\r\n", myErrorHandler.ErrorList);
        //    }

        //    resultEditor.Text = resultString;
        //    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
        //}

        private void runXPathEvaluation()
        {
            statusText.Text = "";
            HideResultDocumentList();
            ClearResultDocumentList();
            resultEditor.Clear();

            try
            {
                var xpathCode = codeEditor.Text;

                var serializationParamsCode = Regex.Replace(xpathCode, @"(^\(:(?!:\))(.+?):\))?(.*)", "$2", RegexOptions.Singleline);

                if (serializationParamsCode == "")
                {
                    serializationParamsCode = "map{}";
                }

                var serializationParams = xpathCompiler.EvaluateSingle(serializationParamsCode, null);

                //serializer.SetOutputWriter(sw);

                docBuilder.BaseUri = new Uri(baseInputCodeURI);

                xpathCompiler.BaseUri = new Uri(baseXPathCodeURI);

                var result = xpathCompiler.Evaluate(
                    codeEditor.Text,
                    (bool)xmlInputType.IsChecked ?
                    docBuilder.Build(new StringReader(inputEditor.Text))
                    : (bool)jsonInputType.IsChecked ?
                    ParseJson(inputEditor.Text) : null);

                //serializer.SerializeXdmValue(result);

                xpathResultSerializer.SetVariable(new QName("value"), result);
                xpathResultSerializer.SetVariable(new QName("serialization-parameters"), serializationParams);

                var resultString = xpathResultSerializer.EvaluateSingle().StringValue;

                resultEditor.Text = resultString;
                resultWebView.NavigateToString(resultString);
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
            }
        }

        private async Task runXQueryEvaluation()
        {
            statusText.Text = "";
            ClearResultDocumentList();
            HideResultDocumentList();
            resultEditor.Clear();

            if (useSaxonEngine)
            {
                runXQueryEvaluationSaxon();
            }
            else
            {
                await runXQueryEvaluationPhoenixml();
            }
            
        }

        private void runXQueryEvaluationSaxon()
        {
 
            var errorCollector = new SimpleErrorCollector();
                
            xqueryCompiler.ErrorReporter = errorCollector.ErrorReporter;

            xqueryCompiler.BaseUri = new Uri(baseXQueryCodeURI);

            try
            {
                using (var sw = new StringWriter())
                {
                    serializer.OutputWriter = sw;

                    statusText.Text = "Compiling XQuery...";

                    var xqueryEvaluator = xqueryCompiler.Compile(codeEditor.Text).Load();

                    xqueryEvaluator.ErrorReporter = errorCollector.ErrorReporter;

                    //var loggerWriter = new StringWriter();

                    //var traceLogger = new StandardLogger(loggerWriter);

                    //xqueryEvaluator.SetTraceFunctionDestination(traceLogger);

                    if ((bool)xmlInputType.IsChecked)
                    {
                        statusText.Text = "Parsing XML input document...";

                        docBuilder.BaseUri = new Uri(baseInputCodeURI);
                        xqueryEvaluator.ContextItem = docBuilder.Build(new StringReader(inputEditor.Text));
                    }
                    else if ((bool)jsonInputType.IsChecked)
                    {
                        statusText.Text = "Parsing JSON input";

                        xqueryEvaluator.ContextItem = ParseJson(inputEditor.Text);
                    }

                    statusText.Text = "Running XQuery...";

                    xqueryEvaluator.Run(serializer);

                    statusText.Text = "";

                    var result = sw.ToString();

                    //var traces = loggerWriter.ToString();

                    //if (traces != string.Empty)
                    //{
                    //    var results = new Dictionary<String, string>();
                    //    results.Add("*** XQuery results ***", result);
                    //    results.Add("*** Trace ***", traces);

                    //    ShowResultDocumentList();

                    //    DisplayResultDocuments(results);
                    //}
                    //else
                    //{
                        resultEditor.Text = result;
                        resultWebView.NavigateToString(result);
                        resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                    //}
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;

                var errors = errorCollector.ErrorList;
                if (errors.Any())
                {

                    statusText.Text += string.Format(": {0}: {1}:{2}", errors.First().Message, errors.First().Location.LineNumber, errors.First().Location.ColumnNumber);
                    resultEditor.Text = string.Join("\n", errors.Select(error => string.Format("{0}: {1}:{2}", error.Message, error.Location.LineNumber, error.Location.ColumnNumber)));
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                }
            }
        }

        private async Task runXQueryEvaluationPhoenixml()
        {
            TransformFunction.Provider = new XsltTransformProvider();

            xqueryFacade = new PhoenixmlDb.XQuery.XQueryFacade();

            //xqueryCompiler.setBaseURI(new JURI(baseXQueryCodeURI));//= new Uri(baseXQueryCodeURI).AbsoluteUri;

            try
            {
                var result = await xqueryFacade.EvaluateAsync(codeEditor.Text, (bool)xmlInputType.IsChecked ? inputEditor.Text : null, (bool)xmlInputType.IsChecked ? new Uri(baseInputCodeURI) : null, new Uri(baseXQueryCodeURI));
    
                resultEditor.Text = result;
                resultWebView.NavigateToString(result);
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");

            }
            catch (XmlException ex)
            {
                statusText.Text = ex.Message;
                resultEditor.Text = $"{ex.Message} in {ex.SourceUri}{ex.LineNumber}:{ex.LinePosition}";
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
            }
            catch (XQueryParseException ex)
            {
                statusText.Text = ex.Message;
                resultEditor.Text = ex.Message;
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
            }
            catch (XQueryRuntimeException ex)
            {
                statusText.Text = ex.Message;
                resultEditor.Text = ex.Message;
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
                resultEditor.Text = ex.Message;
                resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
            }
        }

        private void codeTypeXslt_Click(object sender, RoutedEventArgs e)
        {

        }

        private void codeTypeXQuery_Click(object sender, RoutedEventArgs e)
        {

        }

        private void codeTypeXPath_Click(object sender, RoutedEventArgs e)
        {

        }

        private void codeTypeXsd_Click(object sender, RoutedEventArgs e)
        {
        }

        private void renderResultCbx_Checked(object sender, RoutedEventArgs e)
        {
            if (resultWebView != null)
            {
                resultWebView.Visibility = (bool)renderResultCbx.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            }
        }

    }
}