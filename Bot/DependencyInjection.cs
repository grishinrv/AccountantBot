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
            .AddTypedClient(httpClient => new TelegramBotClient(Utils.TelegramAccessToken, httpClient));

        foreach (var userName in Utils.AllowedUsers)
        {
            services.AddKeyedSingleton<IUserWorkflowManager, UserWorkflowManager>(
                serviceKey: userName, 
                implementationFactory: (s, k) => new UserWorkflowManager(k!.ToString()!, s));
        }
        
        services
            .AddControllers();

        services
            .AddScoped<UpdateHandler>();
        services
            .ConfigureTelegramBotMvc();

        return services;
    }
}