using System.Text.Json;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace SystemTests.Infrastructure;

public class CognitoHelper
{
    private readonly SystemTestConfig _config;
    private readonly HttpClient _http;
    private readonly AmazonCognitoIdentityProviderClient _cognito;

    public CognitoHelper(SystemTestConfig config, HttpClient http)
    {
        _config = config;
        _http = http;

        var region = RegionEndpoint.GetBySystemName(
            Environment.GetEnvironmentVariable("AWS_REGION") ?? "ca-central-1");

        _cognito = new AmazonCognitoIdentityProviderClient(region);
    }

    public async Task<string> GetUserTokenAsync(string email, string password)
    {
        var response = await _cognito.AdminInitiateAuthAsync(new AdminInitiateAuthRequest
        {
            UserPoolId = _config.UserPoolId,
            ClientId = _config.UserPoolClientId,
            AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH,
            AuthParameters = new Dictionary<string, string>
            {
                ["USERNAME"] = email,
                ["PASSWORD"] = password,
            },
        });

        return response.AuthenticationResult.AccessToken;
    }

    public async Task<string> GetClientCredentialsTokenAsync()
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _config.AutomationClientId,
            ["client_secret"] = _config.AutomationClientSecret,
            ["scope"] = "sites/read sites/write",
        });

        var response = await _http.PostAsync(
            $"https://{_config.CognitoDomain}/oauth2/token", body);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public void Dispose()
    {
        _cognito.Dispose();
    }
}
