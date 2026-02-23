using System.Net.Http;
using CommandLine;
using System.Text.Json;

namespace KCKSeFCli;

[Verb("PobierzInfoONip", HelpText = "Retrieve NIP information from the government API.")]
public class PobierzInfoONipCommand : IGlobalCommand
{
    [Value(0, Required = true, HelpText = "NIP number to search for.")]
    public required string Nip { get; set; }

    [Option("data", Required = false, HelpText = "Date for the NIP search (YYYY-MM-DD). Defaults to today.")]
    public string? Date { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        ConfigureLogging();

        var searchDate = Date ?? DateTime.Now.ToString("yyyy-MM-dd");

        try
        {
            var jsonResponse = await FetchNipInfo(Nip, searchDate, cancellationToken).ConfigureAwait(false);
            Console.WriteLine(jsonResponse);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error fetching NIP information: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            return 1;
        }
    }

    public static async Task<string> FetchNipInfo(string nip, string date, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var url = $"https://wl-api.mf.gov.pl/api/search/nip/{nip}?date={date}";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
