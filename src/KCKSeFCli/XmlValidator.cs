using System.Xml;
using System.Xml.Schema;

namespace KCKSeFCli;

public static class XmlValidator
{
    public static bool Validate(string xml, out List<string> errors)
    {
        errors = new List<string>();
        try
        {
            var schemas = new XmlSchemaSet
            {
                XmlResolver = new XmlUrlResolver()
            };
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("KCKSeFCli.Resources.schemat_FA(3)_v1-0E.xsd"))
            {
                if (stream == null)
                {
                    errors.Add("Embedded schema not found.");
                    return false;
                }
                schemas.Add(null, XmlReader.Create(stream));
            }

            var settings = new XmlReaderSettings
            {
                Schemas = schemas,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ProcessSchemaLocation |
                                  XmlSchemaValidationFlags.ReportValidationWarnings,
                XmlResolver = new XmlUrlResolver(),
            };
            var validationEvents = new List<string>();
            settings.ValidationEventHandler += (sender, e) => {
                validationEvents.Add($"{e.Severity}: {e.Message}");
            };
            
            using (var stringReader = new StringReader(xml))
            using (var reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.Read()) { }
            }

            errors.AddRange(validationEvents);
            return errors.Count == 0;
        }
        catch (Exception ex)
        {
            errors.Add($"Validation exception: {ex.Message}");
            return false;
        }
    }
}
