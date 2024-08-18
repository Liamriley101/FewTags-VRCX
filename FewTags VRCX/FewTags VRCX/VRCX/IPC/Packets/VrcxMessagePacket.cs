using System.Text.Json.Serialization;

namespace FewTags.VRCX.IPC.Packets
{
    public class VrcxMessagePacket
    {
        public enum MessageType
        {
            VrcxMessage,
            Noty,
            CustomTag,
            External
        }

        public string Tag { get; set; }
        public string Data { get; set; }
        public string UserId { get; set; }
        public string MsgType { get; set; }
        public string TagColour { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; } = "VrcxMessage";

        public VrcxMessagePacket(MessageType messageType)
        {
            MsgType = messageType.ToString();
        }
    }

    [JsonSerializable(typeof(VrcxMessagePacket))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = false)]
    public partial class VrcxMessagePacketContext : JsonSerializerContext { }
}