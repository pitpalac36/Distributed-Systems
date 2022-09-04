using Main;

namespace ProiectAMCSD
{
    public class PerfectLink : Algorithm
    {
        private MySystem _system;
        private string _host;
        private int _port;

        public PerfectLink(string host, int port)
        {
            _host = host;
            _port = port;
            _system = MySystem.GetInstance();
        }

        public override bool CanHandle(Message message)
        {
            return message.Type == Message.Types.Type.PlSend || message.Type == Message.Types.Type.NetworkMessage;
        }

        public override void DoHandle(Message message)
        {
            var msg = new Message { };
            switch (message.Type)
            {
                case Message.Types.Type.NetworkMessage:
                    {
                        var sender = new ProcessId { };
                        foreach (var p in _system.GetProcesses())
                        {
                            if (p.Host == message.NetworkMessage.SenderHost
                                && p.Port == message.NetworkMessage.SenderListeningPort)
                            {
                                sender = p;
                                break;
                            }
                        }
                        msg = new Message
                        {
                            SystemId = "sys-1",
                            FromAbstractionId = message.ToAbstractionId,
                            ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                            MessageUuid = message.MessageUuid,
                            Type = Message.Types.Type.PlDeliver,
                            PlDeliver = new PlDeliver
                            {
                                Sender = sender,
                                Message = message.NetworkMessage.Message
                            }
                        };

                        _system.AddMessageToQueue(msg);
                        break;
                    }
                case Message.Types.Type.PlSend:
                    {
                        msg = new Message
                        {
                            SystemId = "sys-1",
                            ToAbstractionId = message.ToAbstractionId,
                            Type = Message.Types.Type.NetworkMessage,
                            NetworkMessage = new NetworkMessage
                            {
                                Message = message.PlSend.Message,
                                SenderHost = _host,
                                SenderListeningPort = _port,
                            }
                        };
                        MySystem.GetInstance().SendMessage(msg, message.PlSend.Destination.Host, message.PlSend.Destination.Port);
                        break;
                    }
            }            
        }
    }
}
