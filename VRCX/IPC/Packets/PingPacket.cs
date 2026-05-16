using System.Text.Json.Serialization;

namespace FewTags.VRCX.IPC.Packets
{
    public class PingPacket
    {
        public string Version { get; set; }
        public string Type { get; set; } = "MsgPing";
        public PingPacket() { }
    }

    [JsonSerializable(typeof(PingPacket))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = false)]
    public partial class PingPacketContext : JsonSerializerContext { }
}