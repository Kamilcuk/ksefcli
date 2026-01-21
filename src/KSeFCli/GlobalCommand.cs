using CommandLine;
using KSeF.Client.Api.Services;
using KSeF.Client.Api.Services.Internal;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KSeFCli;

public class GlobalCommand
{
    [Option('t', "token", HelpText = "Session token")]
    public string Token { get; set; }

    [Option('s', "server", HelpText = "KSeF server address")]
    public string Server { get; set; }

    [Option('n', "nip", HelpText = "Tax Identification Number (NIP)")]
    public string Nip { get; set; }

    public virtual Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
    public ServiceProvider GetServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddFilter("KSeFCli", LogLevel.Information)
                   .AddFilter("Microsoft", LogLevel.Warning)
                   .AddFilter("System", LogLevel.Warning)
                   .AddConsole(options =>
                   {
                       options.LogToStandardErrorThreshold = LogLevel.Trace;
                   })
                   .AddSimpleConsole(options =>
                   {
                       options.SingleLine = true;
                       options.TimestampFormat = "HH:mm:ss ";
                   });
        });
        services.AddKSeFClient(options =>
        {
            options.BaseUrl = Server ?? KsefEnvironmentsUris.TEST;
        });
        services.AddSingleton<ICryptographyClient, CryptographyClient>();
        services.AddSingleton<ICertificateFetcher, DefaultCertificateFetcher>();
        services.AddSingleton<ICryptographyService>(sp =>
        {
            ICertificateFetcher fetcher = sp.GetRequiredService<ICertificateFetcher>();
            CryptographyService service = new CryptographyService(fetcher);
            service.WarmupAsync().GetAwaiter().GetResult();
            return service;
        });
        return services.BuildServiceProvider();
    }
}
