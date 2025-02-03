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

using net.liberty_development.SaxonHE12s9apiExtensions;
using net.sf.saxon.s9api;

using JStringReader = java.io.StringReader;
using JStringWriter = java.io.StringWriter;

using JURI = java.net.URI;

using JList = java.util.List;
using javax.xml.transform.stream;
using net.sf.saxon.lib;

using javax.xml;
using javax.xml.validation;
using org.xml.sax;
using JFile = java.io.File;
using System.Reflection;

namespace XMLWPFToolbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static SchemaFactory xsd11SchemaFactory = new org.apache.xerces.jaxp.validation.XMLSchema11Factory();
        private static SchemaFactory xsd10SchemaFactory = new org.apache.xerces.jaxp.validation.XMLSchemaFactory();

        static MainWindow()
        {
            ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("xercesImpl"));

            ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("icu4j"));

            ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("java-cup"));

            ikvm.runtime.Startup.addBootClassPathAssembly(Assembly.Load("xpath2"));
        }

        private static readonly string defaultBaseInputURI = "urn:from-string";

        private string baseResultURI = defaultBaseInputURI;

        private string baseXsltCodeURI = defaultBaseInputURI;

        private string baseXQueryCodeURI = defaultBaseInputURI;

        private string baseXPathCodeURI = defaultBaseInputURI;

        private string baseXsdCodeURI = defaultBaseInputURI;

        private string baseInputCodeURI = defaultBaseInputURI;

        private static Processor processor = new Processor();

        private XPathCompiler xpathCompiler;

        private XQueryCompiler xqueryCompiler;

        private XsltCompiler xsltCompiler;

        private DocumentBuilder docBuilder;

        private JsonBuilder jsonBuilder;

        //private XPathSelector jsonBuilder;

        private Serializer serializer;

        private XPathSelector xpathResultSerializer;

        private SelectionChangedEventHandler selectionChangedEventHandler;

        private DispatcherTimer typingTimer;
        public MainWindow()
        {
            InitializeComponent();

            xpathCompiler = processor.newXPathCompiler();

            xqueryCompiler = processor.newXQueryCompiler();

            xsltCompiler = processor.newXsltCompiler();

            serializer = processor.newSerializer();

            docBuilder = processor.newDocumentBuilder();

            //XPathCompiler jsonDocCompiler = processor.NewXPathCompiler();
            //jsonDocCompiler.DeclareVariable(new QName("input"));

            jsonBuilder = processor.newJsonBuilder();

            XPathCompiler xpathResultCompiler = processor.newXPathCompiler();
            xpathResultCompiler.declareVariable(new QName("value"));
            xpathResultCompiler.declareVariable(new QName("serialization-parameters"));

            xpathResultSerializer = xpathResultCompiler.compile("serialize($value, $serialization-parameters)").load();

            typingTimer = new DispatcherTimer(DispatcherPriority.ContextIdle);
            typingTimer.Interval = TimeSpan.FromSeconds(1.2);
            typingTimer.Tick += TypingTimer_Tick;

            typingTimer.IsEnabled = (bool)autoEvaluateCbx.IsChecked;

        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            if ((bool)autoEvaluateCbx.IsChecked)
            {
                typingTimer.IsEnabled = false;
                typingTimer.Stop();
                evaluateCurrentCodeType();
            }
        }

        private XdmItem ParseJson(string json)
        {
            //jsonBuilder.SetVariable(new QName("input"), new XdmAtomicValue(json));
            return (XdmItem)jsonBuilder.parseJson(json);
        }
        private void xpathEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            runXPathEvaluation();
        }

        private void xqueryEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            runXQueryEvaluation();
        }

        private void xsltTransformationButton_Click(object sender, RoutedEventArgs e)
        {
            runXsltTransformation();
        }

        private void xsdValidationButton_Click(object sender, RoutedEventArgs e)
        {
            runXsdValidation();
        }

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

            codeTypeXsd.IsChecked = true;
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

        private void LoadXsdCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseXsdCodeURI = LoadFileIntoEditor(codeEditor, "XSD schema files|*.xsd|XML files|*.xml|All files|*.*", codeTypeXsd);
        }

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
            else if ((bool)codeTypeXsd.IsChecked)
            {
                SaveXsdCode_Executed(sender, e);
            }
        }
        private void SaveXsltCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*", baseXsltCodeURI);
        }

        private void SaveXsltCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*", defaultBaseInputURI);
        }

        private void SaveXQueryCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XQuery files|*.xq;*.xqy;*.xqu;*.xqm;*.xql;*.xquery|All files|*.*", defaultBaseInputURI);
        }

        private void SaveXPathCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*", defaultBaseInputURI);
        }

        private void SaveXsdCodeAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XSD schema files|*.xsd|XML files|*.xml|All files|*.*", defaultBaseInputURI);
        }

        private void SaveInputDocumentAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(inputEditor, "XML files|*.xml|JSON files|*.json|All files|*.*", defaultBaseInputURI);
        }

        private void SaveXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XQuery files|*.xq;*.xqy;*.xqu;*.xqm;*.xql;*.xquery|All files|*.*", baseXQueryCodeURI);
        }
        private void SaveXPathCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*", baseXPathCodeURI);
        }

        private void SaveXsdCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XSD schema files|*.xsd|XML files|*.xml|All files|*.*", baseXsdCodeURI);
        }
        private void SaveResultDocument_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(resultEditor, "HTML files|*.html;*.html|XML files|*.xml|Text files|*.txt;*.text|JSON files|*.json|All files|*.*", baseResultURI);
        }

        private void SaveInputDocument_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(inputEditor, "XML files|*.xml|JSON files|*.json|All files|*.*", baseInputCodeURI);
        }

        private void AboutXMLToolbox_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("XSLT 3.0, XQuery 3.1, XPath 3.1, XSD 1.1 XML Toolbox using Saxon " + processor.getSaxonEdition() + " " + processor.getSaxonProductVersion() + $" and {org.apache.xerces.impl.Version.getVersion()} run under {Environment.OSVersion} .NET {Environment.Version}", "About XSLT 3.0/XQuery 3.1/XPath 3.1/XSD 1.1 Toolbox");
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

        private void SaveEditorToFile(ICSharpCode.AvalonEdit.TextEditor editor, string filter, string baseURI)
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
                    return;
                }
            }

            editor.Save(currentFileName);
        }

        private void XmlInputType_Click(object sender, RoutedEventArgs e)
        {
            inputEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
        }

        private void JsonInputType_Click(object sender, RoutedEventArgs e)
        {
            inputEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Json");
        }

        private void runXsltTransformation()
        {
            statusText.Text = "";
            ClearResultDocumentList();
            ShowResultDocumentList();
            resultEditor.Clear();

            xsltCompiler.setErrorReporter(new SimpleErrorCollector());

            try
            {
                statusText.Text = "Compiling XSLT code...";

                Xslt30Transformer transformer = xsltCompiler.compile(new StreamSource(new JStringReader(codeEditor.Text), baseXsltCodeURI)).load30();

                var loggerWriter = new JStringWriter();

                var traceLogger = new StandardLogger(loggerWriter);

                transformer.setTraceFunctionDestination(traceLogger);

                var messageHandler = new SimpleMessageHandler();
                transformer.setMessageHandler(messageHandler);

                transformer.setBaseOutputURI("urn:to-string"); //.BaseOutputURI = "urn:to-string";

                var mainSerializer = new MySerializer(processor);
                Dictionary<string, MySerializer> resultDocuments = new Dictionary<string, MySerializer>();
                resultDocuments["*** principal result ***"] = mainSerializer;

                var resultDocumentsHandler = new MyResultDocumentsHandler(processor, resultDocuments);

                transformer.setResultDocumentHandler(resultDocumentsHandler); //ResultDocumentHandler = new MyResultDocumentsHandler(processor, resultDocuments);

                XdmItem inputItem = null;

                if ((bool)xmlInputType.IsChecked)
                {
                    statusText.Text = "Parsing XML input document...";

                    docBuilder.setBaseURI(new JURI(baseInputCodeURI));  //.BaseUri = new Uri(baseInputCodeURI);
                    inputItem = docBuilder.build(new StreamSource(new JStringReader(inputEditor.Text)));
                }
                else if ((bool)jsonInputType.IsChecked)
                {
                    statusText.Text = "Parsing JSON input...";

                    inputItem = (XdmItem)jsonBuilder.parseJson(inputEditor.Text);
                }


                if (inputItem == null)
                {
                    statusText.Text = "Running xsl:initialTemplate...";

                    transformer.callTemplate(null, mainSerializer.serializer);

                    statusText.Text = "";

                    var serializedResultDocuments = resultDocumentsHandler.GetSerializedResultDocuments();

                    if (messageHandler.Messages.Any())
                    {
                        serializedResultDocuments.Add("*** messages ***", messageHandler.GetMessages());

                    }

                    var traces = loggerWriter.ToString();

                    if (traces != string.Empty)
                    {
                        serializedResultDocuments.Add("*** trace ***", loggerWriter.ToString());
                    }

                    traceLogger.close();

                    DisplayResultDocuments(serializedResultDocuments);
                }
                else
                {
                    transformer.setGlobalContextItem(inputItem);  //.GlobalContextItem = inputItem;

                    statusText.Text = "Applying templates processing...";

                    transformer.applyTemplates(inputItem, mainSerializer.serializer);

                    statusText.Text = "";

                    var serializedResultDocuments = resultDocumentsHandler.GetSerializedResultDocuments();

                    if (messageHandler.Messages.Any())
                    {
                        serializedResultDocuments.Add("*** messages ***", messageHandler.GetMessages());

                    }

                    var traces = loggerWriter.ToString();

                    if (traces != string.Empty)
                    {
                        serializedResultDocuments.Add("*** trace ***", loggerWriter.ToString());
                    }

                    traceLogger.close();

                    DisplayResultDocuments(serializedResultDocuments);
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
                //throw ex;

                var errors = (xsltCompiler.getErrorReporter() as SimpleErrorCollector).ErrorList;
                if (errors.Any())
                {
                    statusText.Text += string.Format(": {0}: {1}:{2}", errors.First().getMessage(), errors.First().getLocation().getLineNumber(), errors.First().getLocation().getColumnNumber());
                    resultEditor.Text = string.Join("\n", errors.Select(error => string.Format("{0}: {1}:{2}", error.getMessage(), error.getLocation().getLineNumber(), error.getLocation().getColumnNumber())));
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                }
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

        private void evaluateCode_Click(object sender, RoutedEventArgs e)
        {
            evaluateCurrentCodeType();
        }

        private void evaluateCurrentCodeType()
        {
            if ((bool)codeTypeXslt.IsChecked)
            {
                runXsltTransformation();
            }
            else if ((bool)codeTypeXQuery.IsChecked)
            {
                runXQueryEvaluation();
            }
            else if ((bool)codeTypeXPath.IsChecked)
            {
                runXPathEvaluation();
            }
            else if ((bool)codeTypeXsd.IsChecked)
            {
                runXsdValidation();
            }
        }

        private void runXsdValidation()
        {
            statusText.Text = "";
            HideResultDocumentList();
            ClearResultDocumentList();
            renderResultCbx.IsChecked = false;
            resultEditor.Clear();

            Schema schema;

            if (codeEditor.Text == string.Empty)
            {
                schema = xsd11SchemaFactory.newSchema();
            }
            else
            {
                statusText.Text = "Parsing/compiling your schema...";
                try
                {
                    schema = xsd11SchemaFactory.newSchema(new StreamSource(new JStringReader(codeEditor.Text), baseXsdCodeURI));
                }
                catch (SAXParseException ex)
                {
                    statusText.Text += "Schema parsing failed: " + ex.Message;
                    return;
                }
            }

            Validator validator = schema.newValidator();

            var myErrorHandler = new MyErrorHandler();

            validator.setErrorHandler(myErrorHandler);

            statusText.Text = "Parsing/validating your XML input...";

            string resultString;

            try
            {
                validator.validate(new StreamSource(new JStringReader(inputEditor.Text), baseInputCodeURI));
            }
            catch (SAXParseException ex)
            {
                statusText.Text = "Parsing your XML input failed:" + ex.Message + " (" + ex.getLineNumber() + ":" + ex.getColumnNumber() + ")";
                resultString = "Parsing your XML input failed:\r\n" + ex.Message + " (" + ex.getLineNumber() + ":" + ex.getColumnNumber() + ")\r\n";
            }


            if (myErrorHandler.Valid)
            {
                statusText.Text = resultString = "XML input is valid against schema.";
            }
            else
            {
                statusText.Text = $"Validation failed: {myErrorHandler.ErrorList.Count} errors.";
                resultString = "Validation failed:\r\n" + string.Join("\r\n", myErrorHandler.ErrorList);
            }

            resultEditor.Text = resultString;
            resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
        }

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

                var serializationParams = xpathCompiler.evaluateSingle(serializationParamsCode, null);

                //serializer.SetOutputWriter(sw);

                docBuilder.setBaseURI(new JURI(baseInputCodeURI));

                xpathCompiler.setBaseURI(new JURI(baseXPathCodeURI)); //.BaseUri = new Uri(baseXPathCodeURI).AbsoluteUri;

                var result = xpathCompiler.evaluate(
                    codeEditor.Text,
                    (bool)xmlInputType.IsChecked ?
                    docBuilder.build(new StreamSource(new JStringReader(inputEditor.Text)))
                    : (bool)jsonInputType.IsChecked ?
                    ParseJson(inputEditor.Text) : null);

                //serializer.SerializeXdmValue(result);

                xpathResultSerializer.setVariable(new QName("value"), result);
                xpathResultSerializer.setVariable(new QName("serialization-parameters"), serializationParams);

                var resultString = xpathResultSerializer.evaluateSingle().getStringValue();

                resultEditor.Text = resultString;
                resultWebView.NavigateToString(resultString);
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
            }
        }

        private void runXQueryEvaluation()
        {
            statusText.Text = "";
            ClearResultDocumentList();
            HideResultDocumentList();
            resultEditor.Clear();

            xqueryCompiler.setErrorReporter(new SimpleErrorCollector());

            xqueryCompiler.setBaseURI(new JURI(baseXQueryCodeURI));//= new Uri(baseXQueryCodeURI).AbsoluteUri;

            try
            {
                using (JStringWriter sw = new JStringWriter())
                {
                    serializer.setOutputWriter(sw);

                    statusText.Text = "Compiling XQuery...";

                    var xqueryEvaluator = xqueryCompiler.compile(codeEditor.Text).load();

                    var loggerWriter = new JStringWriter();

                    var traceLogger = new StandardLogger(loggerWriter);

                    xqueryEvaluator.setTraceFunctionDestination(traceLogger);

                    if ((bool)xmlInputType.IsChecked)
                    {
                        statusText.Text = "Parsing XML input document...";

                        docBuilder.setBaseURI(new JURI(baseInputCodeURI));//.BaseUri = new Uri(baseInputCodeURI);
                        xqueryEvaluator.setContextItem(docBuilder.build(new StreamSource(new JStringReader(inputEditor.Text))));//.ContextItem = docBuilder.Build(new StringReader(inputEditor.Text));
                    }
                    else if ((bool)jsonInputType.IsChecked)
                    {
                        statusText.Text = "Parsing JSON input";

                        xqueryEvaluator.setContextItem((XdmItem)jsonBuilder.parseJson(inputEditor.Text)); //.ContextItem = ParseJson(inputEditor.Text);
                    }

                    statusText.Text = "Running XQuery...";

                    xqueryEvaluator.run(serializer);

                    statusText.Text = "";

                    var result = sw.toString();

                    var traces = loggerWriter.toString();

                    if (traces != string.Empty)
                    {
                        var results = new Dictionary<String, string>();
                        results.Add("*** XQuery results ***", result);
                        results.Add("*** Trace ***", traces);

                        ShowResultDocumentList();

                        DisplayResultDocuments(results);
                    }
                    else
                    {
                        resultEditor.Text = result;
                        resultWebView.NavigateToString(result);
                        resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                    }
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;

                var errors = (xqueryCompiler.getErrorReporter() as SimpleErrorCollector).ErrorList;
                if (errors.Any())
                {
 
                    statusText.Text += string.Format(": {0}: {1}:{2}", errors.First().getMessage(), errors.First().getLocation().getLineNumber(), errors.First().getLocation().getColumnNumber());
                    resultEditor.Text = string.Join("\n", errors.Select(error => string.Format("{0}: {1}:{2}", error.getMessage(), error.getLocation().getLineNumber(), error.getLocation().getColumnNumber())));
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                }
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