using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using KSeF.Client.Validation;

namespace KCKSeFCli;

public static class KsefUtils {
    public static bool IsValidNrKSeF(string? nrKSeF) {
        return !string.IsNullOrEmpty(nrKSeF) && RegexPatterns.KsefNumber.IsMatch(nrKSeF);
    }

    public static string? ExtractNrKSeFFromFileName(string fileName) {
        var match = RegexPatterns.KsefNumber.Match(fileName);
        return match.Success ? match.Value : null;
    }

    public static (string Nip, DateTime Date, string TechnicalPart, string Checksum)? ParseNrKSeF(string nrKSeF) {
        if (string.IsNullOrEmpty(nrKSeF)) return null;
        
        var match = RegexPatterns.KsefNumber.Match(nrKSeF);
        if (!match.Success) return null;

        var nip = match.Groups[1].Value;
        var dateStr = match.Groups[2].Value + match.Groups[3].Value + match.Groups[4].Value;
        var technicalPart = match.Groups[5].Value + match.Groups[6].Value;
        var checksum = match.Groups[7].Value;

        if (!DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date)) {
            return null;
        }

        if (!IdentifierValidators.IsValidNip(nip)) {
            return null;
        }

        return (nip, date, technicalPart, checksum);
    }

    public static bool ValidateNrKSeF(string nrKSeF) {
        var parsed = ParseNrKSeF(nrKSeF);
        return parsed.HasValue;
    }

    public static string BuildInvoiceVerificationUrl(string xmlContent, string? baseUrl = null) {
        var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
        if (doc.Root is null) throw new InvalidDataException("Invoice XML is missing root element.");

        var ns = doc.Root.Name.Namespace;
        var podmiot1 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Podmiot1") 
            ?? throw new InvalidDataException("Could not find Podmiot1 in invoice XML.");

        string sellerNip = podmiot1.Element(ns + "DaneIdentyfikacyjne")?.Element(ns + "NIP")?.Value 
            ?? throw new InvalidDataException("Could not find seller NIP in invoice XML.");

        string issueDateValue = doc.Root.Element(ns + "Fa")?.Element(ns + "P_1")?.Value 
            ?? throw new InvalidDataException("Could not find issue date in invoice XML.");

        DateTime issueDate = DateTime.Parse(issueDateValue);

        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(xmlContent));
        string invoiceHash = Convert.ToBase64String(hashBytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        string date = issueDate.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
        string url = baseUrl ?? "https://ksef.mf.gov.pl";
        return $"{url}/invoice/{sellerNip}/{date}/{invoiceHash}";
    }
}