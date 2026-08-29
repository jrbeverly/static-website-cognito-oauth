namespace SystemTests.Infrastructure;

public record SystemTestConfig
{
    public required string ApiUrl { get; init; }
    public required string CognitoDomain { get; init; }
    public required string UserPoolId { get; init; }
    public required string UserPoolClientId { get; init; }
    public required string User1Email { get; init; }
    public required string User1Password { get; init; }
    public required string User2Email { get; init; }
    public required string User2Password { get; init; }
    public required string AutomationClientId { get; init; }
    public required string AutomationClientSecret { get; init; }

    public static SystemTestConfig? FromEnvironment()
    {
        var apiUrl = Env("SYSTEM_TEST_API_URL");
        var cognitoDomain = Env("SYSTEM_TEST_COGNITO_DOMAIN");
        var userPoolId = Env("SYSTEM_TEST_USER_POOL_ID");
        var userPoolClientId = Env("SYSTEM_TEST_USER_POOL_CLIENT_ID");
        var user1Email = Env("SYSTEM_TEST_USER1_EMAIL");
        var user1Password = Env("SYSTEM_TEST_USER1_PASSWORD");
        var user2Email = Env("SYSTEM_TEST_USER2_EMAIL");
        var user2Password = Env("SYSTEM_TEST_USER2_PASSWORD");
        var automationClientId = Env("SYSTEM_TEST_AUTOMATION_CLIENT_ID");
        var automationClientSecret = Env("SYSTEM_TEST_AUTOMATION_CLIENT_SECRET");

        if (string.IsNullOrEmpty(apiUrl) ||
            string.IsNullOrEmpty(cognitoDomain) ||
            string.IsNullOrEmpty(userPoolId) ||
            string.IsNullOrEmpty(userPoolClientId) ||
            string.IsNullOrEmpty(user1Email) ||
            string.IsNullOrEmpty(user1Password) ||
            string.IsNullOrEmpty(user2Email) ||
            string.IsNullOrEmpty(user2Password) ||
            string.IsNullOrEmpty(automationClientId) ||
            string.IsNullOrEmpty(automationClientSecret))
        {
            return null;
        }

        return new SystemTestConfig
        {
            ApiUrl = apiUrl,
            CognitoDomain = cognitoDomain,
            UserPoolId = userPoolId,
            UserPoolClientId = userPoolClientId,
            User1Email = user1Email,
            User1Password = user1Password,
            User2Email = user2Email,
            User2Password = user2Password,
            AutomationClientId = automationClientId,
            AutomationClientSecret = automationClientSecret,
        };
    }

    private static string? Env(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;
}
