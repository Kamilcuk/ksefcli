using KSeF.Client.Api.Services;
using KSeF.Client.Api.Services.Internal;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using System.Reflection;

namespace KSeFCli;

internal class Program
{
    public static int Main(string[] args)
    {
        IServiceCollection services = new ServiceCollection();

        services.AddKSeFClient(options =>
        {
            options.BaseUrl = KsefEnvironmentsUris.TEST;
        });

        // UWAGA! w testach nie używamy AddCryptographyClient tylko rejestrujemy ręcznie, bo on uruchamia HostedService w tle
        services.AddSingleton<ICryptographyClient, CryptographyClient>();
        services.AddSingleton<ICertificateFetcher, DefaultCertificateFetcher>();
        services.AddSingleton<ICryptographyService, CryptographyService>();

        ITypeRegistrar registrar = new DependencyInjectionRegistrar(services);
        CommandApp app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.AddCommand<GetFakturaCommand>("GetFaktura");
            config.AddCommand<SzukajFakturCommand>("SzukajFaktur");
            config.AddCommand<ExportInvoicesCommand>("ExportInvoices");
            config.AddCommand<GetExportStatusCommand>("GetExportStatus");
            config.AddCommand<TokenAuthCommand>("TokenAuth");
            config.AddCommand<TokenRefreshCommand>("TokenRefresh");
            config.AddCommand<CertAuthCommand>("CertAuth");
        });
        return app.Run(args);
    }
}
