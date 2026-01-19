using System.ComponentModel;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Http;
using Spectre.Console.Cli;

namespace KSeFCli {
    public class GlobalSettings : CommandSettings {
        [CommandOption("--token")]
        [Description("KSeF API token")]
        public string Token { get; set; } = Environment.GetEnvironmentVariable("KSEF_TOKEN") ?? string.Empty;

        [CommandOption("--base-url")]
        [Description("KSeF base URL")]
        public string BaseUrl { get; set; } = Environment.GetEnvironmentVariable("KSEF_URL") ?? "https://api-test.ksef.mf.gov.pl/v2";
    }

    public static class KSeFClientFactory {
        public static IKSeFClient CreateKSeFClient(GlobalSettings settings) {
            HttpClient httpClient = new HttpClient { BaseAddress = new Uri(settings.BaseUrl) };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    settings.Token
                );
            RestClient restClient = new RestClient(httpClient);
            return new KSeFClient(restClient);
        }
    }
}
