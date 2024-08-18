using System.Text;
using System.IO.Pipes;
using System.Text.Json;
using FewTags.VRCX.IPC.Packets;

namespace FewTags.VRCX.IPC
{
    internal class IPCClient
    {
        private Thread Thread;
        private NamedPipeClientStream IpcClient;
        private readonly byte[] PacketBuffer = new byte[1024 * 1024];
        public bool Connected => IpcClient != null && IpcClient.IsConnected;
        private static readonly UTF8Encoding NoBomEncoding = new(false, false);

        public void SetCustomTag(string UserID, string Tag, string Color)
        {
            Write(new VrcxMessagePacket(VrcxMessagePacket.MessageType.CustomTag)
            {
                UserId = UserID,
                Tag = Tag,
                TagColour = Color
            });
        }

        public void Connect()
        {
            if (Thread == null)
            {
                IpcClient?.Dispose();
                IpcClient = new NamedPipeClientStream(".", "vrcx-ipc", PipeDirection.InOut);

                Thread = new Thread(ConnectThread);
                Thread.IsBackground = true;
                Thread.Start();
            }
        }

        public void SendMessage(string Message, string UserID, string DisplayName)
        {
            Write(new VrcxMessagePacket(VrcxMessagePacket.MessageType.External)
            {
                Data = Message,
                DisplayName = DisplayName,
                UserId = UserID
            });
            Write(new VrcxMessagePacket(VrcxMessagePacket.MessageType.Noty)
            {
                Data = Message
            });
        }

        private void ConnectThread()
        {
            if (IpcClient == null)
            {
                return;
            }
            while (true)
            {
                try
                {
                    IpcClient.Connect(30000);
                    Thread = null;
                    Console.WriteLine("Connected To VRCX IPC Server Notifications Will Be Sent Via VRCX");
                    break;
                }
                catch { }
                Thread.Sleep(30000);
            }
        }

        private void Write(string Message)
        {
            if (IpcClient == null || !IpcClient.IsConnected)
            {
                return;
            }

            using var MemoryStream = new MemoryStream(PacketBuffer);
            MemoryStream.Seek(0, SeekOrigin.Begin);
            using var StreamWriter = new StreamWriter(MemoryStream, NoBomEncoding, 65535, true);

            StreamWriter.Write(Message);
            StreamWriter.Write((char)0x00);
            StreamWriter.Flush();

            var Length = (int)MemoryStream.Position;

            IpcClient?.BeginWrite(PacketBuffer, 0, Length, OnWrite, null);
        }
        private void Write(VrcxMessagePacket IpcPacket) => Write(JsonSerializer.Serialize(IpcPacket, VrcxMessagePacketContext.Default.VrcxMessagePacket));

        private void Write(PingPacket IpcPacket) => Write(JsonSerializer.Serialize(IpcPacket, PingPacketContext.Default.PingPacket));

        private void OnWrite(IAsyncResult AsyncResult)
        {
            try
            {
                IpcClient?.EndWrite(AsyncResult);
            }
            catch
            {
                Console.WriteLine("Lost connection to the VRCX IPC Server. Reconnecting...");
                Connect();
            }
        }
    }
}