using Bot.Commands;
using Bot.Services;
using Bot.Settings;
using Bot.Storage;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Bot;

public static class DependencyInjection
{
    public static IServiceCollection RegisterBotServices(
        this IServiceCollection services)
    {
        services.AddHttpClient("tgwebhook")
            .RemoveAllLoggers()
            .AddTypedClient(httpClient => new TelegramBotClient(Env.TelegramAccessToken, httpClient));

        foreach (var userName in Env.AllowedUsers)
        {
            services.AddKeyedSingleton<IUserWorkflowManager, UserWorkflowManager>(
                serviceKey: userName, 
                implementationFactory: (s, k) 
                    => new UserWorkflowManager(
                        s.GetRequiredService<ILogger<UserWorkflowManager>>(), 
                        s,
                        k!.ToString()!));
        }

        services.AddDbContextFactory<AccountantDbContext>(options =>
            options
                .UseSqlite("Data Source=accountant.db")
#if DEBUG
                .EnableSensitiveDataLogging()
#endif
        );
        
        services.AddControllers();
        services
            .AddScoped<UpdateHandler>()
            .AddTransient<IPeriodProviderService, PeriodProviderService>()
            .AddTransient<NewCategoryCommand>()
            .AddTransient<RootCommand>()
            .AddTransient<GetStatisticsCommand>()
            .AddTransient<ListRecordsCommand>()
            .AddTransient<NewRecordCommand>();
        services.ConfigureTelegramBotMvc();

        return services;
    }
}