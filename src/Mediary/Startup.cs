namespace Mediary;

/// <summary>
/// The startup class.
/// </summary>
public class Startup
{

    /// <summary>
    /// Gets the MQTT service configuration.
    /// </summary>
    private readonly MqttServiceConfiguration mqttServiceConfiguration = new();
    public IConfiguration Configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public Startup(IConfiguration configuration)
    {
        this.Configuration = configuration;

    }

    /// <summary>
    /// Configures the services.
    /// </summary>
    /// <param name="services">The services.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();

        services.AddSingleton(this.mqttServiceConfiguration);
        services.Configure<MqttServiceConfiguration>(this.Configuration.GetSection("Mediary"));

        services.AddSingleton<MqttService>();
        services.AddHostedService(p => p.GetRequiredService<MqttService>());

        
    }

    /// <summary>
    /// This method gets called by the runtime.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="env">The web hosting environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        _ = app.ApplicationServices.GetService<MqttService>();
    }
}
