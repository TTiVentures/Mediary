using Mediary;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddOptions();

        services.AddDbContext<MessageContext>(options =>
            options.UseSqlite(@"DataSource=missedMessages.db;"),ServiceLifetime.Singleton,ServiceLifetime.Singleton);

        services.AddSingleton(new MqttServiceConfiguration());
        services.Configure<MqttServiceConfiguration>(context.Configuration.GetSection("Mediary"));

        services.AddSingleton<MqttService>();
        services.AddHostedService(p => p.GetRequiredService<MqttService>());
    })
    .Build();

using (var serviceScope = host.Services.CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<MessageContext>();
    context.Database.Migrate();
}

await host.RunAsync();

