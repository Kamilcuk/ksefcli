using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace KCKSeFCli;

public static class XmlValidator {
    public static bool Validate(string xml, out List<string> errors) {
        errors = new List<string>();
        try {
            XmlSchemaSet schemas = new XmlSchemaSet {
                XmlResolver = new XmlUrlResolver()
            };
            Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream("KCKSeFCli.Resources.schemat_FA(3)_v1-0E.xsd")) {
                if (stream == null) {
                    errors.Add("Embedded schema not found.");
                    return false;
                }
                schemas.Add(null, XmlReader.Create(stream));
            }

            XmlReaderSettings settings = new XmlReaderSettings {
                Schemas = schemas,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ProcessSchemaLocation |
                                  XmlSchemaValidationFlags.ReportValidationWarnings,
                XmlResolver = new XmlUrlResolver(),
            };
            List<string> validationEvents = new List<string>();
            settings.ValidationEventHandler += (sender, e) => {
                validationEvents.Add($"{e.Severity}: {e.Message}");
            };

            using (StringReader stringReader = new StringReader(xml))
            using (XmlReader reader = XmlReader.Create(stringReader, settings)) {
                while (reader.Read()) { }
            }

            errors.AddRange(validationEvents);
            return errors.Count == 0;
        } catch (Exception ex) {
            errors.Add($"Validation exception: {ex.Message}");
            return false;
        }
    }
}
