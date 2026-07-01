using System.Text.Json.Serialization;

namespace FewTags.VRCX.IPC.Packets
{
    public class RecPackage
    {
        public string Data { get; set; }
        public string Type { get; set; }
        public string MsgType { get; set; }

        public RecPackage()
        {
        }
    }

    [JsonSerializable(typeof(RecPackage))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = false)]
    public partial class RecPackageContext : JsonSerializerContext { }
}