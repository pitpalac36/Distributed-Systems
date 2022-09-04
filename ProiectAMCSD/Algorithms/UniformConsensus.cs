using Main;

namespace ProiectAMCSD.Algorithms
{
    public class UniformConsensus : Algorithm
    {
        private bool proposed = false;
        private bool decided = false;
        private Value val = new Value { };
        private int ets = 0;
        private ProcessId l = new ProcessId { };
        private int newts = 0;
        private ProcessId newl = new ProcessId { };
        private EpochConsensus epochConsensus;

        public UniformConsensus(EpochConsensus epochConsensus)
        {
            l = MySystem.GetInstance().MaxRank();
            this.epochConsensus = epochConsensus;
        }

        public override bool CanHandle(Message message)
        {
            return message.Type == Message.Types.Type.UcPropose ||
                message.Type == Message.Types.Type.EcStartEpoch ||
                message.Type == Message.Types.Type.EpAborted ||
                message.Type == Message.Types.Type.EpDecide;
        }

        public override void DoHandle(Message message)
        {
            var msg = new Message { };
            switch(message.Type)
            {
                case Message.Types.Type.UcPropose:
                    {
                        val = message.UcPropose.Value;
                        break;
                    }
                case Message.Types.Type.EcStartEpoch:
                    {
                        newts = message.EcStartEpoch.NewTimestamp;
                        newl = message.EcStartEpoch.NewLeader;
                        msg = new Message
                        {
                            Type = Message.Types.Type.EpAbort,
                            FromAbstractionId = "uc",
                            ToAbstractionId = "uc" + ".ep[" + ets + "]",
                            EpAbort = new EpAbort { }
                        };
                        MySystem.GetInstance().AddMessageToQueue(msg);
                        break;
                    }
                case Message.Types.Type.EpAborted:
                    {
                        if (ets == message.EpAborted.Ets)
                        {
                            ets = newts;
                            l = newl;
                            proposed = false;
                            epochConsensus.Reset(ets, new EpInternalState
                            {
                                Value = message.EpAborted.Value,
                                ValueTimestamp = message.EpAborted.ValueTimestamp
                            });
                        }
                        break;
                    }
                case Message.Types.Type.EpDecide:
                    {
                        if (ets == message.EpDecide.Ets)
                        {
                            if (!decided)
                            {
                                decided = true;
                                msg = new Message
                                {
                                    Type = Message.Types.Type.UcDecide,
                                    FromAbstractionId = "uc",
                                    ToAbstractionId = "app",
                                    UcDecide = new UcDecide
                                    {
                                        Value = message.EpDecide.Value
                                    }
                                };
                                MySystem.GetInstance().AddMessageToQueue(msg);
                            }
                        }
                        break;
                    }
            }
            UpdateLeader();
        }

        public void UpdateLeader()
        {
            if (l == MySystem.GetInstance().GetCurrentProcess() && val.Defined && !proposed)
            {
                proposed = true;
                var msg = new Message
                {
                    Type = Message.Types.Type.EpPropose,
                    FromAbstractionId = "uc",
                    ToAbstractionId = "app.uc" + ".ep[" + ets + "]",
                    EpPropose = new EpPropose
                    {
                        Value = val
                    }
                };
                MySystem.GetInstance().AddMessageToQueue(msg);
            }
        }
    }
}
