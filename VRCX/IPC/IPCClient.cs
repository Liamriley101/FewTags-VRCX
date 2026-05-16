using System.Text;
using System.IO.Pipes;
using System.Text.Json;
using FewTags.VRCX.IPC.Packets;

namespace FewTags.VRCX.IPC
{
    internal class IPCClient
    {
        private Thread Thread = null;
        private NamedPipeClientStream IpcClient = null;
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
            if (Thread != null)
            {
                return;
            }

            if (IpcClient != null)
            {
                IpcClient.Dispose();
            }
            IpcClient = new NamedPipeClientStream(".", $"vrcx-ipc-{Extensions.Hash()}", PipeDirection.InOut);

            Thread = new Thread(ConnectThread);
            Thread.IsBackground = true;
            Thread.Start();
            Console.WriteLine("[VRCX] IPC Server: Connected Awaiting Reciever");
        }
        public void ReConnect()
        {
            if (IpcClient != null)
            {
                IpcClient.Close();
            }
            Connect();
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
            while (IpcClient != null)
            {
                try
                {
                    IpcClient.Connect(1000);
                    Thread = null;
                    break;
                }
                catch { }
                Thread.Sleep(1000);
            }
        }

        private void Write(string Message)
        {
            if (IpcClient == null || IpcClient.IsConnected == false)
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

            IpcClient.BeginWrite(PacketBuffer, 0, Length, OnWrite, null);
        }
        private void Write(VrcxMessagePacket IpcPacket) => Write(JsonSerializer.Serialize(IpcPacket, VrcxMessagePacketContext.Default.VrcxMessagePacket));
        // private void Write(PingPacket IpcPacket) => Write(JsonSerializer.Serialize(IpcPacket, PingPacketContext.Default.PingPacket));

        private void OnWrite(IAsyncResult AsyncResult)
        {
            try
            {
                IpcClient.EndWrite(AsyncResult);
            }
            catch
            {
                Console.WriteLine("[VRCX] IPC Server: Reconnecting");
                ReConnect();
            }
        }
    }
}