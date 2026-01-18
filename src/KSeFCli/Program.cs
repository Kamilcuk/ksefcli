using Spectre.Console;
using Spectre.Console.Cli;
using KSeFCli.Commands.Auth;
using KSeFCli.Commands.Faktura;
using KSeFCli.Config;
using KSeFCli.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using KSeF.Client.DI;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Clients; // For OnlineSessionClient, etc.
using System;

namespace KSeFCli
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var appConfig = ConfigLoader.LoadConfig(args);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(appConfig);
            serviceCollection.AddSingleton<TokenStore>();
            
            // Add KSeF Client services
            serviceCollection.AddKSeFClient(options =>
            {
                options.BaseUrl = appConfig.KsefApi.BaseUrl;
                options.BaseQRUrl = appConfig.KsefApi.BaseUrl; // Assuming QR base URL is same as API base URL for now
            });
            serviceCollection.AddCryptographyClient(); // Required for signing operations

            // Register application services
            serviceCollection.AddTransient<AuthService>();
            serviceCollection.AddTransient<InvoiceService>(); // Add InvoiceService

            var app = new CommandApp(new DependencyInjectionRegistrar(serviceCollection));
            app.Configure(config =>
            {
                config.AddBranch("auth", auth =>
                {
                    auth.SetDescription("Manage KSeF authorization and tokens.");
                    auth.AddCommand<TokenRefreshCommand>("token").WithDescription("Refresh authentication token.");
                });
                config.AddBranch("faktura", faktura =>
                {
                    faktura.SetDescription("Manage KSeF invoices (upload, download, search).");
                    // Add wyslij and ls commands here later
                });
            });
            return await app.RunAsync(args);
        }
    }

    // Implementing Spectre.Console.Cli's ITypeRegistrar for Microsoft.Extensions.DependencyInjection
    public sealed class DependencyInjectionRegistrar : ITypeRegistrar
    {
        private readonly IServiceCollection _serviceCollection;

        public DependencyInjectionRegistrar(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public ITypeResolver Build()
        {
            return new DependencyInjectionResolver(_serviceCollection.BuildServiceProvider());
        }

        public void Register(Type service, Type implementation)
        {
            _serviceCollection.AddTransient(service, implementation);
        }

        public void RegisterInstance(Type service, object implementation)
        {
            _serviceCollection.AddSingleton(service, implementation);
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            _serviceCollection.AddSingleton(service, (provider) => factory());
        }
    }

    // Implementing Spectre.Console.Cli's ITypeResolver for Microsoft.Extensions.DependencyInjection
    public sealed class DependencyInjectionResolver : ITypeResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyInjectionResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object? Resolve(Type? type)
        {
            if (type == null) return null;
            return _serviceProvider.GetService(type);
        }
    }
}
