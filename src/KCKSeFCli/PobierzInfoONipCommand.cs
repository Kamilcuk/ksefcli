using System.Text.Json;

using CommandLine;

namespace KCKSeFCli;

public record NipInfo(
    string Nip,
    string? Regon,
    string? Address,
    string? Name
);

[Verb("PobierzInfoONip", HelpText = "Retrieve NIP information from the government API.")]
public class PobierzInfoONipCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "NIP number to search for.")]
    public required string Nip { get; set; }

    [Option("data", Required = false, HelpText = "Date for the NIP search (YYYY-MM-DD). Defaults to today.")]
    public string? Date { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();

        string searchDate = Date ?? DateTime.Now.ToString("yyyy-MM-dd");

        try {
            string jsonResponse = await FetchNipInfo(Nip, searchDate, cancellationToken).ConfigureAwait(false);
            Console.WriteLine(jsonResponse);
            return 0;
        } catch (HttpRequestException ex) {
            Console.Error.WriteLine($"Error fetching NIP information: {ex.Message}");
            return 1;
        } catch (Exception ex) {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            return 1;
        }
    }

    public static async Task<string> FetchNipInfo(string nip, string date, CancellationToken cancellationToken) {
        using HttpClient client = new HttpClient();
        string url = $"https://wl-api.mf.gov.pl/api/search/nip/{nip}?date={date}";
        HttpResponseMessage response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<NipInfo?> GetNipDetailsAsync(string nip, string date, CancellationToken cancellationToken) {
        try {
            string jsonResponse = await FetchNipInfo(nip, date, cancellationToken).ConfigureAwait(false);
            using (JsonDocument document = JsonDocument.Parse(jsonResponse)) {
                if (document.RootElement.TryGetProperty("result", out JsonElement resultElement) && resultElement.ValueKind == JsonValueKind.Object) {
                    if (resultElement.TryGetProperty("subject", out JsonElement subjectElement)) {
                        string? name = null;
                        string? regon = null;
                        string? address = null;

                        if (subjectElement.TryGetProperty("name", out JsonElement nameElement)) {
                            name = nameElement.GetString();
                        }
                        if (subjectElement.TryGetProperty("regon", out JsonElement regonElement)) {
                            regon = regonElement.GetString();
                        }
                        if (subjectElement.TryGetProperty("residenceAddress", out JsonElement residenceAddressElement) && residenceAddressElement.ValueKind != JsonValueKind.Null) {
                            address = residenceAddressElement.GetString();
                        } else if (subjectElement.TryGetProperty("workingAddress", out JsonElement workingAddressElement) && workingAddressElement.ValueKind != JsonValueKind.Null) {
                            address = workingAddressElement.GetString();
                        }

                        return new NipInfo(nip, regon, address, name);
                    }
                }
            }
        } catch (Exception ex) {
            Console.Error.WriteLine($"Error parsing NIP info for {nip}: {ex.Message}");
        }
        return null;
    }
}
