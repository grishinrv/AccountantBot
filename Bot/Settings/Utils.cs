namespace Bot.Settings;

internal static class Utils
{
    private const string TELEGRAM_ACCESS_TOKEN = "TELEGRAM_ACCESS_TOKEN";
    private const string ALLOWED_USERS = "ALLOWED_USERS";
    private const string HOST_DOMAIN = "HOST_DOMAIN";

    private static string? _accessToken;
    public static string AccessToken => _accessToken ??= Environment.GetEnvironmentVariable(TELEGRAM_ACCESS_TOKEN)!;
    public static string WebHookUrl => Environment.ExpandEnvironmentVariables($"https://%{HOST_DOMAIN}%/acc_bot");
}