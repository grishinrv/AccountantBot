using Bot;
using Bot.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterBotServices();
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(80));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AccountantDbContext>>().CreateDbContextAsync();
    dbContext.Database.EnsureCreated();
}

app.Use(async (context, next) =>
{
    if (context.Request.Method is "POST" or "PUT")
    {
        // this is early enough in the pipeline
        context.Request.EnableBuffering();

        // var bodyString = string.Empty;
        // using var reader = new StreamReader(context.Request.Body,
        //     Encoding.UTF8,
        //     detectEncodingFromByteOrderMarks: false,
        //     leaveOpen: true /* important! */);
        //
        // try
        // {
        //     bodyString = await reader.ReadToEndAsync();
        //     if (context.Request.Body.CanSeek)
        //         context.Request.Body.Position = 0;
        // }
        // catch (Exception)
        // {
        //     bodyString = string.Empty;
        // }
        // Console.WriteLine(bodyString);
    }

    await next();
});

app.MapControllers();

app.Run();
