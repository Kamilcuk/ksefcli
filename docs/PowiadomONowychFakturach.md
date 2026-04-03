# PowiadomONowychFakturach

Subcommand `PowiadomONowychFakturach` is used to periodically check for new invoices in KSeF, download them, and notify the user via email or by executing a custom command.

## Usage

```bash
kcksefcli PowiadomONowychFakturach --outputdir ./invoices --email user@example.com --interval 300
```

## Options

- `-i, --interval`: Interval between checks in seconds (default: 300).
- `-1, --once`: Exit after the first loop iteration.
- `-e, --exec`: Command to execute after each successful fetch. Mutual exclusive with `--email`. The command will receive the list of downloaded file paths (XML and PDF) as arguments.
- `--email`: Email address to send notifications to. Mutual exclusive with `--exec`.
- `--state`: Path to state file (default: `~/.cache/kcksefcli/notify_state.json`).
- `-o, --outputdir`: Required. Output directory to save downloaded files.
- `-p, --pdf`: Also save PDF files.
- `--from`: Start date for the first run (e.g., `2023-01-01`, `today`, `-7days`).

## Configuration (SMTP)

To use direct email sending, please see [Email Configuration](Configuration_Email.md).

## How it works

1.  On the first run, it fetches invoices from the date specified in `--from` (defaulting to the beginning of the current day).
2.  It saves the state (last `InvoicingDate` and `KsefNumber`) in a JSON file.
3.  On subsequent runs, it only fetches invoices newer than the last processed one.
4.  Downloaded invoices are saved to `--outputdir`.
5.  If `--email` is provided, it sends a summary email with:
    -   Total Netto and Brutto sums of the new batch.
    -   Top 10 products by net turnover.
    -   XML and PDF files as attachments.
6.  If `--exec` is provided, it runs the specified shell command, passing the paths to all downloaded files as arguments. If the command exits with a non-zero code, the tool terminates.
