namespace Bot.Settings;

internal static class Utils
{
    private const string TELEGRAM_ACCESS_TOKEN = "TELEGRAM_ACCESS_TOKEN";
    private const string ACCOUNTANT_BOT_ACCESS_TOKEN = "ACCOUNTANT_BOT_ACCESS_TOKEN";
    private const string ALLOWED_USERS = "ALLOWED_USERS";
    private const string HOST_DOMAIN = "HOST_DOMAIN";
    
    private static string? _accessToken;
    private static string? _botToken;
    private static long[]? _allowedUserIds;
    public static string TelegramAccessToken => _accessToken ??= Environment.GetEnvironmentVariable(TELEGRAM_ACCESS_TOKEN)!;
    public static string BotToken => _botToken ??= Environment.GetEnvironmentVariable(ACCOUNTANT_BOT_ACCESS_TOKEN)!;
    public static string WebHookUrl => Environment.ExpandEnvironmentVariables($"https://%{HOST_DOMAIN}%/acc_bot");
    public static long[] AllowedUsersIds => _allowedUserIds ??= Environment.GetEnvironmentVariable(ALLOWED_USERS)!.Split(',').Select(long.Parse).ToArray();
}