using Google.Protobuf;
using Main;
using ProiectAMCSD;
using ProiectAMCSD.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Project
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
                throw new Exception();

            SystemInfo.SELF_HOST = "127.0.0.1";
            SystemInfo.HUB_HOST = "127.0.0.1";
            SystemInfo.HUB_PORT = 5000;

            SystemInfo.SELF_PORT = int.Parse(args[1]);
            var owner = args[0];
            var index = int.Parse(args[2]);

            using (var sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var ipAddressHub = IPAddress.Parse(SystemInfo.HUB_HOST);
                var ipEndpointHub = new IPEndPoint(ipAddressHub, SystemInfo.HUB_PORT);

                var m = new Message
                {
                    Type = Message.Types.Type.NetworkMessage,
                    NetworkMessage = new NetworkMessage
                    {
                        SenderHost = SystemInfo.SELF_HOST,
                        SenderListeningPort = SystemInfo.SELF_PORT,   // 5004, ...
                        Message = new Message
                        {
                            Type = Message.Types.Type.ProcRegistration,
                            ProcRegistration = new ProcRegistration
                            {
                                Owner = owner,    // abc
                                Index = index  // 1, 2..
                            }
                        }
                    }
                };

                var bytes = m.ToByteArray();
                var length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));

                using (var recvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    var ipAddressClient = IPAddress.Parse(SystemInfo.SELF_HOST);
                    var ipEndpointClient = new IPEndPoint(ipAddressClient, SystemInfo.SELF_PORT);
                    recvSocket.Bind(ipEndpointClient);

                    sendSocket.Connect(ipEndpointHub);
                    sendSocket.Send(length);
                    sendSocket.Send(bytes);

                    recvSocket.Listen();
                    Socket handler = recvSocket.Accept();

                    byte[] buffer = new byte[4];
                    int receiveCount = 0;
                    try
                    {
                        handler.Receive(buffer);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buffer);
                        }
                        int size = BitConverter.ToInt32(buffer.ToArray(), 0);

                        byte[] mess = new byte[size];
                        receiveCount = handler.Receive(mess, 0, size, SocketFlags.None);
                        var ret = Encoding.ASCII.GetString(mess, 0, receiveCount);

                        var message = Message.Parser.ParseFrom(mess);

                        handler.Close();
                        recvSocket.Close();

                        var system = MySystem.GetInstance();
                        system.SetProcesses(message.NetworkMessage.Message.ProcInitializeSystem.Processes.ToHashSet());
                        system.SetProcessPort(SystemInfo.SELF_PORT);
                        system.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        return;
                    }
                }

            }
        }
    }
}