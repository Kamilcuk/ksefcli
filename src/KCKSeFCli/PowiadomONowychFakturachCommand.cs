using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Invoices;
using KCKSeFCli.Utils;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("PowiadomONowychFakturach", HelpText = "Periodically watch for new invoices and notify the user.")]
public class PowiadomONowychFakturachCommand : PobierzFakturyCommand {
    [Option('i', "interval", Default = 300, HelpText = "Interval between checks in seconds.")]
    public int Interval { get; set; }

    [Option('1', "once", HelpText = "Exit after the first loop iteration.")]
    public bool Once { get; set; }

    [Option('e', "exec", HelpText = "Command to execute after each successful fetch. Mutual exclusive with --email.")]
    public string? Exec { get; set; }

    [Option("email", HelpText = "Email address to send notifications to. Mutual exclusive with --exec.")]
    public string? Email { get; set; }

    [Option("state", HelpText = "Path to state file. Defaults to ~/.cache/kcksefcli/notify_state.json")]
    public string? StatePath { get; set; }

    private string ActualStatePath => StatePath ?? Path.Combine(IGlobalCommand.CacheDir, "notify_state.json");

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        if (!string.IsNullOrEmpty(Exec) && !string.IsNullOrEmpty(Email)) {
            throw new ArgumentException("Options --exec and --email are mutually exclusive.");
        }

        // IMPORTANT: We must initialize the PDF runner before any network operations with KSeF.
        XML2PDFCommand.Runner? pdfRunner = null;
        if (Pdf) {
            pdfRunner = await XML2PDFCommand.GetRunner(cancellationToken).ConfigureAwait(false);
        }

        Log.Information($"Starting PowiadomONowychFakturach loop with interval {Interval}s...");
        NotifyState state = NotifyState.Load(ActualStatePath);
        ProfileConfigWithName profile = Config();
        
        if (!state.Profiles.TryGetValue(profile.Name, out ProfileNotifyState? profileState)) {
            profileState = new ProfileNotifyState();
            state.Profiles[profile.Name] = profileState;
        }

        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();

        while (!cancellationToken.IsCancellationRequested) {
            try {
                Log.Information($"Checking for new invoices at {DateTime.Now}...");

                // Setup search criteria
                if (profileState.LastInvoicingDate.HasValue) {
                    this.From = profileState.LastInvoicingDate.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                } else if (string.IsNullOrEmpty(this.From)) {
                    this.From = "today";
                }
                this.DateType = "Invoicing";

                List<InvoiceSummary> invoices = await base.SzukajFaktury(scope, ksefClient, cancellationToken).ConfigureAwait(false);

                // Filter out already processed invoices
                List<InvoiceSummary> newInvoices = InvoiceFilter.FilterNewInvoices(
                    invoices,
                    profileState.LastInvoicingDate,
                    profileState.LastKsefNumber);

                if (newInvoices.Count > 0) {
                    Log.Information($"Found {newInvoices.Count} new invoices.");
                    await ProcessInvoices(scope, newInvoices, pdfRunner, cancellationToken).ConfigureAwait(false);
                    
                    // Post-process (Mutual Exclusive)
                    if (!string.IsNullOrEmpty(Exec)) {
                        await ExecutePostProcessCommand(newInvoices, cancellationToken).ConfigureAwait(false);
                    } else if (!string.IsNullOrEmpty(Email)) {
                        await FormatAndSendEmail(newInvoices, cancellationToken).ConfigureAwait(false);
                    }

                    // Update state ONLY AFTER SUCCESSFUL POST-PROCESS
                    var lastInvoice = newInvoices.Last();
                    profileState.LastInvoicingDate = lastInvoice.InvoicingDate;
                    profileState.LastKsefNumber = lastInvoice.KsefNumber;
                    state.Save(ActualStatePath);
                } else {
                    Log.Information("No new invoices found.");
                }

                if (Once) {
                    Log.Information("Once option set, exiting...");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(Interval), cancellationToken).ConfigureAwait(false);
            } catch (TaskCanceledException) {
                break;
            } catch (Exception ex) {
                Log.Error($"Error in PowiadomONowychFakturach loop: {ex.Message}");
                if (Verbose) Log.Error(ex.StackTrace ?? "");
                await Task.Delay(TimeSpan.FromSeconds(Interval), cancellationToken).ConfigureAwait(false);
            }
        }
        return 0;
    }

    private async Task ExecutePostProcessCommand(List<InvoiceSummary> newInvoices, CancellationToken cancellationToken) {
        List<string> filePaths = new List<string>();
        string absoluteOutputDir = Path.GetFullPath(OutputDir);
        foreach (var inv in newInvoices) {
            string fileName = UseInvoiceNumber ? inv.InvoiceNumber : inv.KsefNumber;
            filePaths.Add(Path.Combine(absoluteOutputDir, $"{fileName}.xml"));
            if (Pdf) filePaths.Add(Path.Combine(absoluteOutputDir, $"{fileName}.pdf"));
        }

        // Run /bin/sh -c "exec_cmd" -- file1 file2 ...
        // The command in exec_cmd should use "$@" to access file paths
        List<string> commandAndArgs = new List<string> { "/bin/sh", "-c", $"{Exec} \"$@\"", "kcksefcli-postprocess" };
        commandAndArgs.AddRange(filePaths);

        Subprocess sub = new Subprocess(commandAndArgs);
        Log.Information($"Executing post-process command for {newInvoices.Count} invoices...");
        await sub.CheckCallAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task FormatAndSendEmail(List<InvoiceSummary> newInvoices, CancellationToken cancellationToken) {
        decimal totalBrutto = 0;
        decimal totalNetto = 0;
        var productTurnover = new Dictionary<string, decimal>();
        List<string> attachments = new List<string>();

        foreach (var inv in newInvoices) {
            string fileName = UseInvoiceNumber ? inv.InvoiceNumber : inv.KsefNumber;
            string xmlPath = Path.Combine(OutputDir, $"{fileName}.xml");
            string pdfPath = Path.Combine(OutputDir, $"{fileName}.pdf");

            if (File.Exists(xmlPath)) {
                attachments.Add(xmlPath);
                var data = InvoiceDataExtractor.Extract(File.ReadAllText(xmlPath));
                totalBrutto += data.TotalBrutto;
                totalNetto += data.TotalNetto;
                foreach (var item in data.Items) {
                    if (!productTurnover.ContainsKey(item.Name)) productTurnover[item.Name] = 0;
                    productTurnover[item.Name] += item.NetValue;
                }
            }
            if (Pdf && File.Exists(pdfPath)) {
                attachments.Add(pdfPath);
            }
        }

        StringBuilder body = new StringBuilder();
        body.AppendLine($"Pobrano {newInvoices.Count} nowych faktur z KSeF.");
        body.AppendLine();
        body.AppendLine($"Suma Netto: {totalNetto:F2}");
        body.AppendLine($"Suma Brutto: {totalBrutto:F2}");
        body.AppendLine();
        body.AppendLine("Top 10 produktów (według obrotu netto):");
        var topProducts = productTurnover.OrderByDescending(p => p.Value).Take(10);
        foreach (var p in topProducts) {
            body.AppendLine($"- {p.Key}: {p.Value:F2}");
        }

        string subject = $"Nowe faktury KSeF - {newInvoices.Count} sztuk";
        
        SmtpConfig? smtp = FullConfig()?.Smtp;
        if (smtp != null && !string.IsNullOrEmpty(smtp.Host)) {
            Log.Information($"Sending native email notification to {Email} via {smtp.Host}...");
            await SendMail.Send(smtp, Email!, subject, body.ToString(), attachments, cancellationToken).ConfigureAwait(false);
        } else {
            Log.Warning("SMTP not configured in kcksefcli.yaml. Falling back to mailx...");
            await SendEmailMailx(Email!, subject, body.ToString(), attachments, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendEmailMailx(string to, string subject, string body, List<string> attachments, CancellationToken cancellationToken) {
        string attachArgs = string.Join(" ", attachments.Select(a => $"-a \"{a}\""));
        string command = $"echo \"{body.Replace("\"", "\\\"")}\" | mailx -s \"{subject}\" {attachArgs} \"{to}\"";
        Subprocess sub = new Subprocess(new[] { "/bin/sh", "-c", command });
        await sub.CheckCallAsync(cancellationToken).ConfigureAwait(false);
    }
}
