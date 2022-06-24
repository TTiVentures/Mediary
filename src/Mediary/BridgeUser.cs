namespace Mediary;

/// <summary>
///     The <see cref="BridgeUser" /> read from the configuration file.
/// </summary>
public class BridgeUser
{
    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the private key in Base64.
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;
}
