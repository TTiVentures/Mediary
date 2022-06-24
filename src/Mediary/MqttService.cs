namespace Mediary;

using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

/// <inheritdoc cref="BackgroundService"/>
/// <inheritdoc cref="IMqttServerSubscriptionInterceptor"/>
/// <inheritdoc cref="IMqttServerApplicationMessageInterceptor"/>
/// <inheritdoc cref="IMqttServerConnectionValidator"/>
/// <inheritdoc cref="IMqttServerClientDisconnectedHandler"/>
/// <summary>
///     The main service class of the <see cref="MqttService" />.
/// </summary>
public class MqttService : BackgroundService, IMqttServerSubscriptionInterceptor, IMqttServerApplicationMessageInterceptor,
    IMqttServerConnectionValidator, IMqttServerClientDisconnectedHandler, IMqttServerClientConnectedHandler, IMqttClientConnectedHandler,
    IMqttClientDisconnectedHandler, IMqttApplicationMessageReceivedHandler

{

    private JwtResponse authRequest;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger logger;

    /// <summary>
    /// The MQTT client.
    /// </summary>
    private IMqttClient? mqttClient;

    /// <summary>
    /// The MQTT server.
    /// </summary>
    private IMqttServer? mqttServer;

    /// <summary>
    /// The MQTT client options.
    /// </summary>
    private IMqttClientOptions? clientOptions;

    /// <summary>
    /// The cancellation token.
    /// </summary>
    private CancellationToken cancellationToken;

    /// <summary>
    /// Gets or sets the MQTT service configuration.
    /// </summary>
    public MqttServiceConfiguration MqttServiceConfiguration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttService"/> class.
    /// </summary>
    /// <param name="mqttServiceConfiguration">The MQTT service configuration.</param>
    public MqttService(IOptions<MqttServiceConfiguration> mqttServiceConfiguration, ILogger<MqttService> logger)
    {
        this.MqttServiceConfiguration = mqttServiceConfiguration.Value;
        this.logger = logger;
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {

        if (!this.MqttServiceConfiguration.IsValid())
        {
            throw new Exception("The configuration is invalid");
        }

        this.logger.LogInformation("Starting service");
        this.cancellationToken = cancellationToken;
        await this.StartMqttClient();
        await this.StartMqttServerAsync();
        this.logger.LogInformation("Service started");
        await base.StartAsync(cancellationToken);
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc cref="BackgroundService"/>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(this.MqttServiceConfiguration.DelayInMilliSeconds, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError("An error occurred: {Exception}", ex);
            }
        }
    }

    /// <summary>
    /// Validates the MQTT connection.
    /// </summary>
    /// <param name="context">The context.</param>
    public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
    {
        try
        {
            var currentUser = this.MqttServiceConfiguration.Users.FirstOrDefault(u => u.ClientId == context.ClientId);

            if (currentUser == null)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.logger.LogWarning(
                    "New connection REJECTED: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName},CleanSession = {CleanSession}",
                    context.ClientId,
                    context.Endpoint,
                    context.Username,
                    context.CleanSession);
                return Task.CompletedTask;
            }

            if (context.Username != currentUser.UserName)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.logger.LogWarning(
                    "New connection REJECTED: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName},CleanSession = {CleanSession}",
                    context.ClientId,
                    context.Endpoint,
                    context.Username,
                    context.CleanSession);
                return Task.CompletedTask;
            }

            if (context.Password != currentUser.Password)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.logger.LogWarning(
                    "New connection REJECTED: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName},CleanSession = {CleanSession}",
                    context.ClientId,
                    context.Endpoint,
                    context.Username,
                    context.CleanSession);
                return Task.CompletedTask;
            }

            context.ReasonCode = MqttConnectReasonCode.Success;

            this.logger.LogInformation(
                "New connection GRANTED: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName}, CleanSession = {CleanSession}",
                context.ClientId,
                context.Endpoint,
                context.Username,
                context.CleanSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.LogError("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT subscriptions.
    /// </summary>
    /// <param name="context">The context.</param>
    public async Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
    {
        this.logger.LogDebug(
            "Received subscription from device: ClientId = {ClientId}, Topic = {Topic}, QoS = {QualityOfServiceLevel}",
            context.ClientId,
            context.TopicFilter.Topic,
            context.TopicFilter.QualityOfServiceLevel);
        try
        {
            var status = await this.mqttClient.SubscribeAsync(context.TopicFilter);
            if (status.Items.Count > 0)
            {
                context.AcceptSubscription = true;

            }
            else
            {
                context.AcceptSubscription = false;

            }
        }
        catch (Exception ex)
        {
            this.logger.LogError("An error occurred: {Exception}.", ex);
        }
    }

    /// <summary>
    /// Validates the MQTT application messages.
    /// </summary>
    /// <param name="context">The context.</param>
    public async Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
    {
        string[] topicDecode = context.ApplicationMessage.Topic.Split('/');

        if (topicDecode[2] == null)
        {
            this.logger.LogWarning($"Invalid topic \"{context.ApplicationMessage.Topic}\" from client \"{context.ClientId}\": ClientId is not present");
            context.AcceptPublish = false;
        }
        else if (context.ClientId != topicDecode[2])
        {
            this.logger.LogWarning($"Invalid topic \"{context.ApplicationMessage.Topic}\" from client \"{context.ClientId}\": ClientId missmatch");
            context.AcceptPublish = false;
        }
        else
        {
            this.logger.LogDebug(
                "Received message from device: ClientId = {ClientId}, Topic = {Topic}, QoS = {QoS}, Retain = {Retain}",
                context.ClientId,
                context.ApplicationMessage.Topic,
                context.ApplicationMessage.QualityOfServiceLevel,
                context.ApplicationMessage.Retain);
            try
            {
                var status = await this.mqttClient!.PublishAsync(context.ApplicationMessage, this.cancellationToken);

                if (status.ReasonCode == MqttClientPublishReasonCode.Success)
                {
                    context.AcceptPublish = true;
                }
                else
                {
                    context.AcceptPublish = false;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("An error occurred: {Exception}.", ex);
            }
        }
    }

    /// <summary>
    /// Validates the MQTT client disconnect.
    /// </summary>
    /// <param name="eventArgs">The event args.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    public async Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
    {
        this.logger.LogInformation($"Device {eventArgs.ClientId} disconected");

        var status = await this.mqttClient.PublishAsync($"/devices/{eventArgs.ClientId}/detach", null, MqttQualityOfServiceLevel.AtLeastOnce);
        if (status.ReasonCode == MqttClientPublishReasonCode.Success)
        {
            this.logger.LogInformation($"Device {eventArgs.ClientId} detached");
        }
        else
        {
            this.logger.LogError($"Failed to detach device: {eventArgs.ClientId}");
        }
    }

    /// <summary>
    /// Validates the MQTT client disconnect.
    /// </summary>
    /// <param name="eventArgs">The event args.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    public async Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
    {
        var status = await this.mqttClient.PublishAsync($"/devices/{eventArgs.ClientId}/attach", null, MqttQualityOfServiceLevel.AtLeastOnce);
        if (status.ReasonCode == MqttClientPublishReasonCode.Success)
        {
            this.logger.LogInformation($"Device {eventArgs.ClientId} attached");
        }
        else
        {
            this.logger.LogError($"Failed to attach device: {eventArgs.ClientId}");
        }
    }

    public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        this.logger.LogDebug(
            "Received message from broker: ClientId = {ClientId}, Topic = {Topic}, QoS = {QoS}, Retain = {Retain}",
            eventArgs.ClientId,
            eventArgs.ApplicationMessage.Topic,
            eventArgs.ApplicationMessage.QualityOfServiceLevel,
            eventArgs.ApplicationMessage.Retain);

        await this.mqttServer.PublishAsync(eventArgs.ApplicationMessage);
    }

    public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
    {
        this.logger.LogInformation("OK: Connected to Google Cloud");
        //await this.mqttClient.SubscribeAsync("/devices/airport/config", MqttQualityOfServiceLevel.AtLeastOnce);
        await this.mqttClient.SubscribeAsync("/devices/airport/commands/#", MqttQualityOfServiceLevel.AtMostOnce);
    }

    public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
        this.logger.LogInformation("KO: Disconnected from Google Cloud");

        if (this.authRequest.ExpiresAt > DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            this.logger.LogDebug("Expired token: Renew and reconnect");
        }
        else
        {
            this.logger.LogWarning("EE: Unexpected shutdown, try to reconnect in 15");
            await Task.Delay(TimeSpan.FromSeconds(15));
        }

        try
        {
            await this.mqttClient!.ConnectAsync(this.GenerateClientOptions(), CancellationToken.None);
        }
        catch
        {
            this.logger.LogError("EE: Reconnection failure");
        }
    }

    private IMqttClientOptions GenerateClientOptions()
    {
        this.authRequest = JwtHandler.CreateToken(this.MqttServiceConfiguration.BridgeUser.PrivateKey,
                this.MqttServiceConfiguration.ProjectID);

        return new MqttClientOptionsBuilder()
                .WithCredentials("unused", this.authRequest.Token)
                .WithClientId(this.MqttServiceConfiguration.BridgeUser.ClientId)
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
                    AllowUntrustedCertificates = false,
                    IgnoreCertificateChainErrors = false,
                    IgnoreCertificateRevocationErrors = false,

                })
                .WithKeepAlivePeriod(TimeSpan.FromMinutes(15))
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithTcpServer(this.MqttServiceConfiguration.BridgeUrl, this.MqttServiceConfiguration.BridgePort)
                .Build();
    }

    /// <summary>
    /// Starts the MQTT client.
    /// </summary>
    private async Task StartMqttClient()
    {
        this.clientOptions = this.GenerateClientOptions();

        this.mqttClient = new MqttFactory().CreateMqttClient();
        this.mqttClient.ConnectedHandler = this;
        this.mqttClient.ApplicationMessageReceivedHandler = this;
        this.mqttClient.DisconnectedHandler = this;

        var conStatus = await this.mqttClient!.ConnectAsync(this.clientOptions, this.cancellationToken);
    }

    /// <summary>
    /// Starts the MQTT server.
    /// </summary>
    private async Task StartMqttServerAsync()
    {
        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(this.MqttServiceConfiguration.Port)
            .WithEncryptedEndpointPort(this.MqttServiceConfiguration.TlsPort)
            .WithConnectionValidator(this)
            .WithSubscriptionInterceptor(this)
            .WithApplicationMessageInterceptor(this);

        this.mqttServer = new MqttFactory().CreateMqttServer();
        this.mqttServer.ClientDisconnectedHandler = this;
        this.mqttServer.ClientConnectedHandler = this;
        await this.mqttServer.StartAsync(optionsBuilder.Build());
    }

}
