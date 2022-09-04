using Main;

namespace ProiectAMCSD.Algorithms
{
    public class EpochConsensus : Algorithm
    {
        private EpInternalState state = new EpInternalState();
        private int accepted = 0;
        private Value tmpval;
        private int epochTimestamp;
        private Dictionary<ProcessId, EpInternalState> states = new Dictionary<ProcessId, EpInternalState>();

        public EpochConsensus()
        {
            tmpval = new Value();
        }

        public void Reset(int ets, EpInternalState state)
        {
            epochTimestamp = ets;
            this.state = state;
            accepted = 0;
            tmpval = new Value();
            states = new Dictionary<ProcessId, EpInternalState>();
        }

        public override bool CanHandle(Message message)
        {
            return message.Type == Message.Types.Type.EpPropose ||
                (message.Type == Message.Types.Type.BebDeliver && message.BebDeliver.Message.Type == Message.Types.Type.EpInternalRead) ||
                (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EpInternalState) ||
                (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EpInternalAccept) ||
                (message.Type == Message.Types.Type.BebDeliver && message.BebDeliver.Message.Type == Message.Types.Type.EpInternalWrite) ||
                (message.Type == Message.Types.Type.BebDeliver && message.BebDeliver.Message.Type == Message.Types.Type.EpInternalDecided) ||
                (message.Type == Message.Types.Type.EpAbort);
        }

        public override void DoHandle(Message message)
        {
            var msg = new Message { };
            switch(message.Type)
            {
                case Message.Types.Type.EpPropose:
                    {
                        tmpval = message.EpPropose.Value;
                        msg = new Message
                        {
                            Type = Message.Types.Type.BebBroadcast,
                            FromAbstractionId = "ep",
                            ToAbstractionId = ParentAbsId(message.ToAbstractionId) + ".beb",
                            BebBroadcast = new BebBroadcast
                            {
                                Message = new Message
                                {
                                    Type = Message.Types.Type.EpInternalRead,
                                    FromAbstractionId = "ep",
                                    ToAbstractionId = "ep",
                                    EpInternalRead = new EpInternalRead { }
                                }
                            }
                        };
                        MySystem.GetInstance().AddMessageToQueue(msg);
                        break;
                    }
                case Message.Types.Type.BebDeliver:
                    {
                        switch (message.BebDeliver.Message.Type)
                        {
                            case Message.Types.Type.EpInternalRead:
                                {
                                    msg = new Message
                                    {
                                        Type = Message.Types.Type.PlSend,
                                        FromAbstractionId = message.ToAbstractionId,
                                        ToAbstractionId = message.ToAbstractionId + ".pl",
                                        PlSend = new PlSend
                                        {
                                            Destination = message.BebDeliver.Sender,
                                            Message = new Message
                                            {
                                                Type = Message.Types.Type.EpInternalState,
                                                FromAbstractionId = message.ToAbstractionId,
                                                ToAbstractionId = message.ToAbstractionId,
                                                EpInternalState = new EpInternalState
                                                {
                                                    Value = state.Value,
                                                    ValueTimestamp = state.ValueTimestamp
                                                }
                                            }
                                        }
                                    };
                                    MySystem.GetInstance().AddMessageToQueue(msg);
                                    break;
                                }
                            case Message.Types.Type.EpInternalWrite:
                                {
                                    state.ValueTimestamp = epochTimestamp;
                                    state.Value = message.BebDeliver.Message.EpInternalWrite.Value;
                                    msg = new Message
                                    {
                                        Type = Message.Types.Type.PlSend,
                                        FromAbstractionId = message.ToAbstractionId,
                                        ToAbstractionId = message.ToAbstractionId + ".pl",
                                        PlSend = new PlSend
                                        {
                                            Destination = message.BebDeliver.Sender,
                                            Message = new Message
                                            {
                                                Type = Message.Types.Type.EpInternalAccept,
                                                FromAbstractionId = message.ToAbstractionId,
                                                ToAbstractionId = message.ToAbstractionId,
                                                EpInternalAccept = new EpInternalAccept { }
                                            }
                                        }
                                    };
                                    MySystem.GetInstance().AddMessageToQueue(msg);
                                    break;
                                }
                            case Message.Types.Type.EpInternalDecided:
                                {
                                    msg = new Message
                                    {
                                        Type = Message.Types.Type.EpDecide,
                                        FromAbstractionId = "ep",
                                        ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                                        EpDecide = new EpDecide
                                        {
                                            Ets = epochTimestamp,
                                            Value = message.BebDeliver.Message.EpInternalDecided.Value
                                        }
                                    };
                                    MySystem.GetInstance().AddMessageToQueue(msg);
                                    break;
                                }
                        }
                        break;
                    }
                case Message.Types.Type.PlDeliver:
                    {
                        switch (message.PlDeliver.Message.Type)
                        {
                            case Message.Types.Type.EpInternalState:
                                {
                                    states[message.PlDeliver.Sender] = message.PlDeliver.Message.EpInternalState;
                                    if (states.Count > MySystem.GetInstance().GetProcesses().Count / 2)
                                    {
                                        var highest = Highest(states.Values);
                                        if (highest.Value != null && highest.Value.Defined)
                                        {
                                            tmpval = highest.Value;
                                        }
                                        states.Clear();
                                        msg = new Message
                                        {
                                            Type = Message.Types.Type.BebBroadcast,
                                            ToAbstractionId = "beb",
                                            BebBroadcast = new BebBroadcast
                                            {
                                                Message = new Message
                                                {
                                                    Type = Message.Types.Type.EpInternalWrite,
                                                    EpInternalWrite = new EpInternalWrite
                                                    {
                                                        Value = tmpval
                                                    }
                                                }
                                            }
                                        };
                                        MySystem.GetInstance().AddMessageToQueue(msg);
                                    }
                                    break;
                                }
                            case Message.Types.Type.EpInternalAccept:
                                {
                                    accepted += 1;
                                    if (accepted > MySystem.GetInstance().GetProcesses().Count / 2)
                                    {
                                        msg = new Message
                                        {
                                            Type = Message.Types.Type.BebBroadcast,
                                            ToAbstractionId = "beb",
                                            BebBroadcast = new BebBroadcast
                                            {
                                                Message = new Message
                                                {
                                                    Type = Message.Types.Type.EpInternalDecided,
                                                    EpInternalDecided = new EpInternalDecided
                                                    {
                                                        Value = tmpval
                                                    }
                                                }
                                            }
                                        };
                                        MySystem.GetInstance().AddMessageToQueue(msg);
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case Message.Types.Type.EpAbort:
                    {
                        msg = new Message
                        {
                            Type = Message.Types.Type.EpAborted,
                            FromAbstractionId = "ep",
                            ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                            EpAborted = new EpAborted
                            {
                                Ets = epochTimestamp,
                                Value =state.Value,
                                ValueTimestamp = state.ValueTimestamp
                            }
                        };
                        MySystem.GetInstance().AddMessageToQueue(msg);
                        break;
                    }
            }
        }

        private EpInternalState Highest(IEnumerable<EpInternalState> states)
        {
            return states.Aggregate((maxstate, state) => state.ValueTimestamp > maxstate.ValueTimestamp ? state : maxstate);
        }
    }
}
