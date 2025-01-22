using Bot.Services;
using Bot.Settings;
using Telegram.Bot;

namespace Bot;

public static class DependencyInjection
{
    public static IServiceCollection RegisterBotServices(
        this IServiceCollection services)
    {
        services.AddHttpClient("tgwebhook")
            .RemoveAllLoggers()
            .AddTypedClient(httpClient => new TelegramBotClient(Utils.AccessToken, httpClient));
        
        services
            .AddControllers();

        services
            .AddScoped<UpdateHandler>();
        services
            .ConfigureTelegramBotMvc();

        return services;
    }
}