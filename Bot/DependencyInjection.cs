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
            .AddTypedClient(httpClient => new TelegramBotClient(Utils.TelegramAccessToken, httpClient));

        foreach (var userName in Utils.AllowedUsers)
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
                .EnableSensitiveDataLogging());
        
        services.AddControllers();
        services
            .AddScoped<UpdateHandler>()
            .AddTransient<NewCategoryCommand>()
            .AddTransient<RootCommand>()
            .AddTransient<GetStatisticsCommand>()
            .AddTransient<NewRecordCommand>();
        services.ConfigureTelegramBotMvc();

        return services;
    }
}