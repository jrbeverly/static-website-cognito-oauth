using Xunit;

namespace SystemTests.Infrastructure;

public class SystemTestFixture : IAsyncLifetime
{
    public SystemTestConfig? Config { get; private set; }
    public CognitoHelper? CognitoHelper { get; private set; }
    public HttpClient HttpClient { get; } = new();
    public SystemTestClient? Client { get; private set; }

    private readonly List<(string SiteId, string Token)> _cleanupSites = new();

    public async Task InitializeAsync()
    {
        Config = SystemTestConfig.FromEnvironment();
        if (Config is null) return;

        Client = new SystemTestClient(HttpClient, Config.ApiUrl);
        CognitoHelper = new CognitoHelper(Config, HttpClient);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up any remaining sites in reverse order
        if (Client is not null)
        {
            foreach (var (siteId, token) in _cleanupSites.AsEnumerable().Reverse())
            {
                try
                {
                    await Client.DeleteAsync($"/sites/{siteId}", token);
                }
                catch
                {
                    // Best-effort cleanup
                }
            }
        }

        CognitoHelper?.Dispose();
        HttpClient.Dispose();
    }

    public void TrackForCleanup(string siteId, string token)
    {
        _cleanupSites.Add((siteId, token));
    }

    public bool IsConfigured => Config is not null;
}
