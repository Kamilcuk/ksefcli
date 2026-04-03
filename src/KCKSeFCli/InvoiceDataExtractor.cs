using System.Globalization;
using System.Xml.Linq;

namespace KCKSeFCli;

public class InvoiceData {
    public decimal TotalBrutto { get; set; }
    public decimal TotalNetto { get; set; }
    public List<InvoiceItem> Items { get; set; } = new();
}

public class InvoiceItem {
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal NetValue { get; set; }
}

public static class InvoiceDataExtractor {
    public static InvoiceData Extract(string xml) {
        XDocument doc = XDocument.Parse(xml);
        
        InvoiceData data = new();
        XElement? root = doc.Root;
        if (root == null) return data;

        XElement? fa = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Fa");
        if (fa == null) return data;

        // Total Brutto
        XElement? p15El = fa.Elements().FirstOrDefault(e => e.Name.LocalName == "P_15");
        if (p15El != null && decimal.TryParse(p15El.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal brutto)) {
            data.TotalBrutto = brutto;
        }

        // Total Netto - sum of P_13_1, P_13_2, etc.
        decimal totalNetto = 0;
        foreach (XElement el in fa.Elements().Where(e => e.Name.LocalName.StartsWith("P_13_"))) {
            if (decimal.TryParse(el.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal netto)) {
                totalNetto += netto;
            }
        }
        data.TotalNetto = totalNetto;

        // Line Items
        foreach (XElement wiersz in fa.Elements().Where(e => e.Name.LocalName == "FaWiersz")) {
            InvoiceItem item = new();
            item.Name = wiersz.Elements().FirstOrDefault(e => e.Name.LocalName == "P_7")?.Value ?? "";
            
            XElement? p8bEl = wiersz.Elements().FirstOrDefault(e => e.Name.LocalName == "P_8B");
            if (p8bEl != null && decimal.TryParse(p8bEl.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qty)) {
                item.Quantity = qty;
            }

            XElement? p11El = wiersz.Elements().FirstOrDefault(e => e.Name.LocalName == "P_11");
            if (p11El != null && decimal.TryParse(p11El.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val)) {
                item.NetValue = val;
            }
            data.Items.Add(item);
        }

        return data;
    }
}
