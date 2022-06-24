using Mediary;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddOptions();

        services.AddSingleton(new MqttServiceConfiguration());
        services.Configure<MqttServiceConfiguration>(context.Configuration.GetSection("Mediary"));

        services.AddSingleton<MqttService>();
        services.AddHostedService(p => p.GetRequiredService<MqttService>());


    })
    .Build();

await host.RunAsync();

