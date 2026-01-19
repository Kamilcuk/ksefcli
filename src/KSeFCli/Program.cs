using Spectre.Console.Cli;

namespace KSeFCli;

internal class Program {
    public static int Main(string[] args) {
        CommandApp app = new CommandApp();
        app.Configure(config => {
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
