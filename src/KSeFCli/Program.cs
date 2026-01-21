using KSeF.Client.ClientFactory.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;

namespace KSeFCli;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddFilter("KSeFCli", LogLevel.Information)
                   .AddFilter("Default", LogLevel.Warning) // Suppress logs from other libraries
                   .AddFilter("System", LogLevel.Warning)
                   .AddSimpleConsole(options =>
                   {
                       options.SingleLine = true;
                       options.TimestampFormat = "HH:mm:ss ";
                   });
        });

        services.RegisterKSeFClientFactory();


        var registrar = new DependencyInjectionRegistrar(services);
        var app = new CommandApp(registrar);

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

        return await app.RunAsync(args).ConfigureAwait(false);
    }
}
