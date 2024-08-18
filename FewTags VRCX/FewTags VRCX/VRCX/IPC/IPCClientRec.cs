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
        private readonly byte[] RecBuffer = new byte[1024 * 8];

        public void BeginRead()
        {
            IpcClient.BeginRead(RecBuffer, 0, RecBuffer.Length, OnRead, IpcClient);
        }

        public void Disconnect()
        {
            IpcClient.Close();
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

        public void ConnectThread()
        {
            if (IpcClient != null)
            {
                while (true)
                {
                    try
                    {
                        IpcClient.Connect(30000);
                        BeginRead();

                        Thread = null;
                        Console.WriteLine("Receiver Connected To VRCX IPC Server");
                        break;
                    }
                    catch { }
                    Thread.Sleep(30000);
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
                CurrentPacket += Encoding.UTF8.GetString(RecBuffer, 0, Bytes);
                if (CurrentPacket[CurrentPacket.Length - 1] == (char)0x00)
                {
                    var Packets = CurrentPacket.Split((char)0x00);
                    foreach (var Packet in Packets)
                    {
                        if (!string.IsNullOrEmpty(Packet))
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
                                            Program.HandleJoin(RecPackage.Data);
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

            BeginRead();
        }
    }
}