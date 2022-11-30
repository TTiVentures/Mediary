<p align="center">
  <img src="https://i.imgur.com/TvTOd00.png">
</p>

_by [TTI Ventures](https://ttiventures.com/)_

------------------------------------------------------------------------------

> **Warning**
> 
> **IoT Core will be discontinued on Aug. 16, 2023**


Mediary is an MQTT bridge designed to work with Google Cloud IoT Core. 

Sometimes there are IoT devices that are not directly connected to the Internet and need to pass through a gateway or bridge to forward messages to the main broker. 

Although MQTT bridging solutions exist, they often do not meet the complex Google Cloud requirement of generating a JWT token and sending it in the password field. In addition, devices must announce their entry and exit from the bridge, something that many proprietary solutions do not implement.

Mediary enables connecting these IoT devices via the MQTT protocol as well as sending and receiving messages to the IoT Core service that cannot connect directly to the Google Cloud.

## Features

* Easy configuration of client devices.
* Generates the JWT token from the private key.
* Stores and redelivers unsent messages in case of loss of connection.
* Transparent announcement of device connection and disconnection.
* Automatic reconnection upon token expiration.
* Open source!

## Configuration

All settings are set in the `appsettings.json` file located in the root of the project. 

A sample `demo.appsettings.json` file is included for the required parameters:

```json
"Mediary": {
    "Port": 1883,
    "Users": [{
        "ClientId": "juliet",
        "UserName": "juliet",
        "Password": "jul13t"
    }, {
        "ClientId": "romeo",
        "UserName": "romeo",
        "Password": "r0m30"
    }],
    "DelayInMilliSeconds": 30000,
    "TlsPort": 8883,
    "BridgeUrl": "mqtt.googleapis.com",
    "BridgePort": 8883,
    "ProjectId": "<YOUR_PROJECT_ID>",
    "BridgeUser": {
        "ClientId": "<YOUR_CLIENT_ID>",
        "PrivateKey": "<YOUR_BASE64_PRIVATE_KEY>"
    }
}
```

## Technologies

Mediary has been built and developed on **.NET 6.0** using [MQTTnet](https://github.com/dotnet/MQTTnet) as its main library to establish the MQTT client connections to Google Cloud and server to the devices.

### Docker

Mediary can be run as a Docker container, the [Dockerfile](src/Mediary/Dockerfile) is included to build images automatically.

## License

Project licensed under *GNU Lesser General Public License v2.1*

See [LICENSE](LICENSE) for more information.

Other licenses are included in [3RD_PARTY_LICENSES.txt](3RD_PARTY_LICENSES.txt)
