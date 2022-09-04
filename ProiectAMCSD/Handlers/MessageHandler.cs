using Main;
using System.Net.Sockets;
using System.Text;

namespace ProiectAMCSD.Handlers
{
    public class MessageHandler : BaseThread
    {
        private Socket _socket;

        public MessageHandler(Socket socket)
        {
            _socket = socket;
        }

        public override void RunThread()
        {
            byte[] buffer = new byte[4];
            try
            {
                int receiveCount = 0;

                _socket.Receive(buffer);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }
                int size = BitConverter.ToInt32(buffer.ToArray(), 0);

                byte[] mess = new byte[size];
                receiveCount = _socket.Receive(mess, 0, size, SocketFlags.None);
                var ret = Encoding.ASCII.GetString(mess, 0, receiveCount);

                var receivedOuterMessage = Message.Parser.ParseFrom(mess);

                MySystem.GetInstance().AddMessageToQueue(receivedOuterMessage);;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }
    }
}
