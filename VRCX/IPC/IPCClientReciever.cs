using System.Text;
using System.IO.Pipes;
using System.Text.Json;
using FewTags.VRCX.IPC.Packets;

namespace FewTags.VRCX.IPC
{
    public class IPCClientReceive
    {
        private Thread Thread = null;
        private string CurrentPacket = null;
        private NamedPipeClientStream IpcClient = null;
        private readonly byte[] RecieverBuffer = new byte[1024 * 8];

        public void Connect()
        {
            if (Thread == null)
            {
                IpcClient?.Dispose();
                IpcClient = new NamedPipeClientStream(".", $"vrcx-ipc-{Extensions.Hash()}", PipeDirection.InOut);

                Thread = new Thread(ConnectThread);
                Thread.IsBackground = true;
                Thread.Start();
            }
        }
        public void ReConnect()
        {
            if (IpcClient != null)
            {
                IpcClient.Close();
            }
            Connect();
        }

        public void ConnectThread()
        {
            while (IpcClient != null)
            {
                try
                {
                    IpcClient.Connect(1000);
                    IpcClient.BeginRead(RecieverBuffer, 0, RecieverBuffer.Length, OnRead, IpcClient);
                    Thread = null;
                    Console.WriteLine("[VRCX] IPC Server: Receiver Connected");
                    break;
                }
                catch { }
                Thread.Sleep(1000);
            }
            if (IpcClient != null)
            {
                while (IpcClient != null)
                {
                    if (IpcClient.IsConnected == false)
                    {
                        ReConnect();
                        Thread = null;
                        Console.WriteLine("[VRCX] IPC Server: Disconnected, ReConnecting");
                        break;
                    }
                }
            }
        }

        private void OnRead(IAsyncResult AsyncResult)
        {
            try
            {
                var Bytes = IpcClient.EndRead(AsyncResult);
                if (Bytes <= 0)
                {
                    IpcClient.Close();
                    return;
                }
                CurrentPacket += Encoding.UTF8.GetString(RecieverBuffer, 0, Bytes);
                if (CurrentPacket[CurrentPacket.Length - 1] == (char)0x00)
                {
                    var Packets = CurrentPacket.Split((char)0x00);
                    foreach (var Packet in Packets)
                    {
                        if (string.IsNullOrEmpty(Packet) == false)
                        {
                            try
                            {
                                RecPackage RecPackage = JsonSerializer.Deserialize(Packet, RecPackageContext.Default.RecPackage);
                                if (RecPackage != null)
                                {
                                    if (RecPackage.Type == "VrcxMessage")
                                    {
                                        if (RecPackage.MsgType == "ShowUserDialog")
                                        {
                                            Program.HandleIPC(RecPackage.Data);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    CurrentPacket = string.Empty;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            IpcClient.BeginRead(RecieverBuffer, 0, RecieverBuffer.Length, OnRead, IpcClient);
        }
    }
}