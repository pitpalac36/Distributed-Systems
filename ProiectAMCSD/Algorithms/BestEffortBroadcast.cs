using Main;

namespace ProiectAMCSD
{
    public class BestEffortBroadcast : Algorithm
    {
        private MySystem _system;

        public BestEffortBroadcast()
        {
            _system = MySystem.GetInstance();
        }

        public override bool CanHandle(Message message)
        {
            return message.Type == Message.Types.Type.BebBroadcast ||
                   (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.AppValue) ||
                   (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.NnarInternalRead) ||
                   (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.NnarInternalWrite) ||
                   (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.NnarInternalValue) ||
                   (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EpInternalRead) ||
                   (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EpInternalWrite) ||
                   (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EpInternalDecided);

        }

        public override void DoHandle(Message message)
        {
            
            var processes = _system.GetProcesses();
            var msg = new Message { };
            switch(message.Type)
            {
                case Message.Types.Type.BebBroadcast:
                    {
                        foreach (var each in processes)
                        {
                            msg = new Message
                            {
                                MessageUuid = Guid.NewGuid().ToString(),
                                Type = Message.Types.Type.PlSend,
                                FromAbstractionId = message.ToAbstractionId,
                                ToAbstractionId = message.ToAbstractionId + ".pl",
                                PlSend = new PlSend
                                {
                                    Destination = each,
                                    Message = message.BebBroadcast.Message
                                }
                            };
                            _system.AddMessageToQueue(msg);
                        }
                        
                        break;
                    }
                case Message.Types.Type.PlDeliver:
                    {
                        msg = new Message
                        {
                            Type = Message.Types.Type.BebDeliver,
                            FromAbstractionId = "app.beb",
                            ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                            BebDeliver = new BebDeliver
                            {
                                Sender = message.PlDeliver.Sender,
                                Message = message.PlDeliver.Message
                            }
                        };
                        _system.AddMessageToQueue(msg);
                        break;
                    }
            }
        }
    }
}
