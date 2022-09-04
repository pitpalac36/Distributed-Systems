using Google.Protobuf;
using Main;
using ProiectAMCSD.Handlers;
using System.Net;
using System.Net.Sockets;

namespace ProiectAMCSD
{
    public class NetworkHandler : BaseThread
    {
        private int processPort;
        private Socket socket;
        private bool closed = false;

        public NetworkHandler(int processPort)
        {
            this.processPort = processPort;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public override void RunThread()
        {
            try
            {
                OpenSocket();
            }
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
        }

        public void SendMessage(Message message, string host, int port)
        {
            message.SystemId = "sys-1";
            IPAddress ipAddress = IPAddress.Parse(host);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

            byte[] bytes = message.ToByteArray();

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                while (true)
                {
                    try
                    {
                        socket.Connect(endPoint);
                        socket.Send(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length)));
                        socket.Send(bytes);
                        break;
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public void OpenSocket()
        {
            closed = false;
            var ipAddressClient = IPAddress.Parse("127.0.0.1");
            var ipEndpointClient = new IPEndPoint(ipAddressClient, processPort);
            socket.Bind(ipEndpointClient);
            socket.Listen();
            while (!closed)
            {
                Socket handler = socket.Accept();
                var messageHandler = new MessageHandler(handler);
                messageHandler.Start();
            }
        }

        public void CloseSocket()
        {
            socket.Close();
            closed = true;
        }
    }
}
