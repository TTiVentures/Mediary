namespace Mediary;

using System.Reflection;

/// <summary>
/// The main program.
/// </summary>
public class Program
{
    /// <summary>
    /// The configuration.
    /// </summary>
    private static IConfigurationRoot? config;

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    public static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    /// <summary>
    /// Gets or sets the MQTT service configuration.
    /// </summary>
    public static MqttServiceConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// The service name.
    /// </summary>
    public static AssemblyName ServiceName => Assembly.GetExecutingAssembly().GetName();

    /// <summary>
    /// The main method.
    /// </summary>
    /// <param name="args">Some arguments.</param>
    /// <returns>The result code.</returns>
    public static async Task<int> Main(string[] args)
    {

        //Log.Information("Starting {ServiceName}, Version {Version}...", ServiceName.Name, ServiceName.Version);
        var currentLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        await CreateHostBuilder(args, currentLocation!).Build().RunAsync();

        return 0;
    }

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <param name="currentLocation">The current assembly location.</param>
    /// <returns>A new <see cref="IHostBuilder"/>.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args, string currentLocation) =>
        Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseContentRoot(currentLocation);
                    webBuilder.UseStartup<Startup>();
                });
            //.UseWindowsService()
            //.UseSystemd();


}
