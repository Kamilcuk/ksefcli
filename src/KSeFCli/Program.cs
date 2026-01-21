using CommandLine;

namespace KSeFCli;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parser = new Parser(with => with.HelpWriter = Console.Error);

        var result = parser.ParseArguments<GetFakturaCommand, SzukajFakturCommand, ExportInvoicesCommand, GetExportStatusCommand, TokenAuthCommand, TokenRefreshCommand, CertAuthCommand>(args);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Canceling...");
            cts.Cancel();
            e.Cancel = true;
        };

        return await result.MapResult(
            async (GlobalCommand cmd) => await cmd.ExecuteAsync(cts.Token),
            errs => Task.FromResult(1));
    }
}
