using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace KCKSeFCli;

public static class XmlValidator {
    private static XmlSchemaSet? _schema;

    private static XmlSchemaSet GetSchema() {
        if (_schema != null)
            return _schema;

        _schema = new XmlSchemaSet();
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = "KCKSeFCli.Resources.schemat.xsd";
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName)) {
            if (stream == null)
                throw new Exception($"Embedded resource not found: {resourceName}");
            using (XmlReader reader = XmlReader.Create(stream)) {
                _schema.Add("http://crd.gov.pl/wzor/2025/06/25/13775/", reader);
            }
        }
        return _schema;
    }

    public static bool Validate(string xml, out List<string> errors) {
        List<string> localErrors = new List<string>();
        XmlReaderSettings settings = new XmlReaderSettings {
            Schemas = GetSchema(),
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
        };
        settings.ValidationEventHandler += (sender, args) => localErrors.Add(args.Message);

        try {
            using (XmlReader reader = XmlReader.Create(new StringReader(xml), settings)) {
                while (reader.Read()) { }
            }
        } catch (XmlException ex) {
            localErrors.Add(ex.Message);
        }

        errors = localErrors;
        return errors.Count == 0;
    }

    public static bool ValidateLog(string xml, out List<string> errors) {
        bool isValid = Validate(xml, out errors);
        if (!isValid) {
            Log.LogError("XML validation failed:");
            foreach (string error in errors) {
                Log.LogError(error);
            }
        }
        return isValid;
    }
}
