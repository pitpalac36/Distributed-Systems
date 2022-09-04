using Main;
using ProiectAMCSD.Utils;

namespace ProiectAMCSD.Algorithms
{
    public class EpochChange : Algorithm
    {
        private ProcessId trusted;
        private int lastTimestamp;
        private int timestamp;

        public EpochChange()
        {
            trusted = MySystem.GetInstance().MaxRank();
            lastTimestamp = 0;
            timestamp = MySystem.GetInstance().GetCurrentProcess().Rank;
        }

        public override bool CanHandle(Message message)
        {
            return message.Type == Message.Types.Type.EldTrust ||
                (message.Type == Message.Types.Type.BebDeliver && message.BebDeliver.Message.Type == Message.Types.Type.EcInternalNewEpoch) ||
                (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EcInternalNack);
        }

        public override void DoHandle(Message message)
        {
            Message msg = new Message { };
            switch(message.Type)
            {
                case Message.Types.Type.EldTrust:
                    {
                        trusted = message.EldTrust.Process;
                        if (trusted == MySystem.GetInstance().GetCurrentProcess())
                        {
                            timestamp += MySystem.GetInstance().GetProcesses().Count();
                            msg = new Message
                            {
                                Type = Message.Types.Type.BebBroadcast,
                                FromAbstractionId = "ec",
                                ToAbstractionId = ParentAbsId(message.ToAbstractionId) + ".beb",
                                BebBroadcast = new BebBroadcast
                                {
                                    Message = new Message
                                    {
                                        Type = Message.Types.Type.EcInternalNewEpoch,
                                        FromAbstractionId = "ec",
                                        ToAbstractionId = "app.uc[" + SystemInfo.TOPIC + "].ec",
                                        EcInternalNewEpoch = new EcInternalNewEpoch
                                        {
                                            Timestamp = timestamp
                                        }
                                    }
                                }
                            };
                            MySystem.GetInstance().AddMessageToQueue(msg);
                        }
                        break;
                    }
                case Message.Types.Type.BebDeliver:
                    {
                        var newTimestamp = message.BebDeliver.Message.EcInternalNewEpoch.Timestamp;
                        var l = message.BebDeliver.Sender;
                        if (l == trusted && newTimestamp > lastTimestamp)
                        {
                            lastTimestamp = newTimestamp;
                            msg = new Message
                            {
                                Type = Message.Types.Type.EcStartEpoch,
                                FromAbstractionId = "ec",
                                ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                                EcStartEpoch = new EcStartEpoch
                                {
                                    NewTimestamp = newTimestamp,
                                    NewLeader = message.BebDeliver.Sender
                                }
                            };
                        }
                        else
                        {
                            msg = new Message
                            {
                                Type = Message.Types.Type.PlSend,
                                FromAbstractionId = "ec",
                                ToAbstractionId = ParentAbsId(message.ToAbstractionId) + ".pl",
                                PlSend = new PlSend
                                {
                                    Destination = message.BebDeliver.Sender,
                                    Message = new Message
                                    {
                                        Type = Message.Types.Type.EcInternalNack,
                                        FromAbstractionId = "ec",
                                        ToAbstractionId = "app.uc[" + SystemInfo.TOPIC + "].ec",
                                        EcInternalNack = new EcInternalNack { }
                                    }
                                }
                            };
                        }
                        MySystem.GetInstance().AddMessageToQueue(msg);
                        break;
                    }
                case Message.Types.Type.PlDeliver:
                    {
                        msg = new Message
                        {
                            Type = Message.Types.Type.BebBroadcast,
                            FromAbstractionId = "ec",
                            ToAbstractionId = ParentAbsId(message.ToAbstractionId) + ".beb",
                            BebBroadcast = new BebBroadcast
                            {
                                Message = new Message
                                {
                                    Type = Message.Types.Type.EcInternalNewEpoch,
                                    FromAbstractionId = "ec",
                                    ToAbstractionId = "app.uc[" + SystemInfo.TOPIC + "].ec",
                                    EcInternalNewEpoch = new EcInternalNewEpoch
                                    {
                                        Timestamp = timestamp
                                    }
                                }
                            }
                        };
                        MySystem.GetInstance().AddMessageToQueue(msg);
                        break;
                    }
            }
        }
    }
}
