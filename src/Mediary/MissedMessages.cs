namespace Mediary
{
    using MQTTnet.Protocol;

    public class MissedMessages
    {
        public int Id { get; set; }

        public string? Topic { get; set; }

        public byte[]? Payload { get; set; }

        public MqttQualityOfServiceLevel? QualityOfServiceLevel { get; set; }

        public bool Retain { get; set; }
        public bool Dup { get; set; }
        public string? ContentType { get; set; }
    }
}
