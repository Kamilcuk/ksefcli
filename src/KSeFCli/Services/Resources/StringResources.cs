namespace KSeFCli.Services.Resources
{
    public static class StringResources
    {
        public const string CertificatePathError = "[red]Error: CertificatePath is not configured or file does not exist.[/]";
        public const string CertificateLoadError = "[red]Error loading client certificate: {0}[/]";
        public const string GeneratingToken = "[yellow]Attempting to generate new KSeF token...[/]";
        public const string TokenGenerationError = "[red]Failed to get session token from KSeF after submitting signed challenge.[/]";
        public const string TokenGenerated = "[green]Successfully generated and stored a new KSeF token.[/]";
        public const string NoValidTokenFound = "[yellow]No existing token found to refresh. Attempting to generate a new one.[/]";
        public const string TokenStillValid = "[green]Current token is valid and does not need refreshing.[/]";
        public const string RefreshingToken = "[yellow]Attempting to refresh expired KSeF token...[/]";
        public const string TokenRefreshError = "[red]Failed to refresh token from KSeF.[/]";
        public const string TokenRefreshed = "[green]Successfully refreshed and stored KSeF token.[/]";
    }
}
