using Main;
using System.Text.RegularExpressions;

namespace ProiectAMCSD.Algorithms
{
    public class Nnar : Algorithm
    {
        private int N;
        private int Timestamp = 0;
        private int WriterRank = 0;
        private Value value = new Value();
        private int Acks = 0;
        private Value WriteValue = new Value();
        private Value ReadValue = new Value();
        private int ReadId = 0;
        private IDictionary<int, NnarInternalValue> ReadList = new Dictionary<int, NnarInternalValue>();
        private bool Reading = false;
        private readonly string m_Locker = "THREAD_LOCKER";

        public Nnar()
        {
            N = MySystem.GetInstance().GetProcesses().Count();
        }

        public override bool CanHandle(Message message)
        {
            return message.Type == Message.Types.Type.NnarRead ||
                message.Type == Message.Types.Type.NnarWrite ||
                (message.Type == Message.Types.Type.BebDeliver && message.BebDeliver.Message.Type == Message.Types.Type.NnarInternalRead) ||
                (message.Type == Message.Types.Type.BebDeliver && message.BebDeliver.Message.Type == Message.Types.Type.NnarInternalWrite) ||
                (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.NnarInternalValue) ||
                (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.NnarInternalAck);
        }

        public override void DoHandle(Message message)
        {
            var aId = "app.nnar[" + GetRegisterId(message.ToAbstractionId) + "]";
            Message msg = null;
            switch (message.Type)
            {
                case Message.Types.Type.NnarRead:
                    {
                        Monitor.Enter(m_Locker);
                        ReadId += 1;
                        Acks = 0;
                        ReadList.Clear();
                        Reading = true;
                        msg = new Message
                        {
                            Type = Message.Types.Type.BebBroadcast,
                            ToAbstractionId = aId + ".beb",
                            BebBroadcast = new BebBroadcast
                            {
                                Message = new Message
                                {
                                    Type = Message.Types.Type.NnarInternalRead,
                                    ToAbstractionId = aId,
                                    NnarInternalRead = new NnarInternalRead
                                    {
                                        ReadId = ReadId
                                    }
                                }
                            }
                        };
                        Monitor.Exit(m_Locker);
                        break;
                    }
                case Message.Types.Type.NnarWrite:
                    {
                        Monitor.Enter(m_Locker);
                        ReadId += 1;
                        WriteValue = message.NnarWrite.Value;
                        Acks = 0;
                        ReadList.Clear();
                        msg = new Message
                        {
                            Type = Message.Types.Type.BebBroadcast,
                            FromAbstractionId = aId,
                            ToAbstractionId = aId + ".beb",
                            BebBroadcast = new BebBroadcast
                            {
                                Message = new Message
                                {
                                    Type = Message.Types.Type.NnarInternalRead,
                                    ToAbstractionId = aId,
                                    NnarInternalRead = new NnarInternalRead
                                    {
                                        ReadId = ReadId
                                    }
                                }
                            }
                        };
                        Monitor.Exit(m_Locker);
                        break;
                    }
                case Message.Types.Type.BebDeliver:
                    {
                        switch (message.BebDeliver.Message.Type)
                        {
                            case Message.Types.Type.NnarInternalRead:
                                {
                                    Monitor.Enter(m_Locker);
                                    msg = new Message
                                    {
                                        Type = Message.Types.Type.PlSend,
                                        ToAbstractionId = aId + ".pl",
                                        PlSend = new PlSend
                                        {
                                            Destination = message.BebDeliver.Sender,
                                            Message = new Message
                                            {
                                                Type = Message.Types.Type.NnarInternalValue,
                                                ToAbstractionId = aId,
                                                NnarInternalValue = new NnarInternalValue
                                                {
                                                    ReadId = message.BebDeliver.Message.NnarInternalRead.ReadId,
                                                    Timestamp = Timestamp,
                                                    WriterRank = WriterRank,
                                                    Value = value
                                                }
                                            }
                                        }
                                    };
                                    Monitor.Exit(m_Locker);
                                    break;
                                }
                            case Message.Types.Type.NnarInternalWrite:
                                {
                                    Monitor.Enter(m_Locker);
                                    var nnarInternalWrite = message.BebDeliver.Message.NnarInternalWrite;
                                    if (nnarInternalWrite.Timestamp > Timestamp ||
                                        (nnarInternalWrite.Timestamp == Timestamp && nnarInternalWrite.WriterRank > WriterRank))
                                    {
                                        Timestamp = nnarInternalWrite.Timestamp;
                                        WriterRank = nnarInternalWrite.WriterRank;
                                        value = nnarInternalWrite.Value;
                                    }

                                    var ack = new Message
                                    {
                                        Type = Message.Types.Type.NnarInternalAck,
                                        ToAbstractionId = aId,
                                        NnarInternalAck = new NnarInternalAck
                                        {
                                            ReadId = nnarInternalWrite.ReadId
                                        }
                                    };

                                    msg = new Message
                                    {
                                        Type = Message.Types.Type.PlSend,
                                        ToAbstractionId = aId + ".pl",
                                        PlSend = new PlSend
                                        {
                                            Destination = message.BebDeliver.Sender,
                                            Message = ack
                                        }
                                    };
                                    Monitor.Exit(m_Locker);
                                    break;
                                }
                        }
                        break;
                    }
                case Message.Types.Type.PlDeliver:
                    {
                        switch (message.PlDeliver.Message.Type)
                        {
                            case Message.Types.Type.NnarInternalValue:
                                {
                                    Monitor.Enter(m_Locker);
                                    var m = message.PlDeliver.Message.NnarInternalValue;
                                    if (m.ReadId == ReadId)
                                    {
                                        ReadList[message.PlDeliver.Sender.Port] = m;
                                        ReadList[message.PlDeliver.Sender.Port].WriterRank = message.PlDeliver.Message.NnarInternalValue.WriterRank;
                                        ReadList[message.PlDeliver.Sender.Port].Timestamp = message.PlDeliver.Message.NnarInternalValue.Timestamp;
                                        if (ReadList.Count() > N / 2)
                                        {
                                            var highest = Highest();
                                            ReadValue = highest.Value;
                                            ReadList.Clear();

                                            Message internalWrite;
                                            if (Reading)
                                            {
                                                internalWrite = new Message
                                                {
                                                    Type = Message.Types.Type.NnarInternalWrite,
                                                    ToAbstractionId = aId,
                                                    NnarInternalWrite = new NnarInternalWrite
                                                    {
                                                        ReadId = ReadId,
                                                        Timestamp = highest.Timestamp,
                                                        WriterRank = highest.WriterRank,
                                                        Value = ReadValue
                                                    }
                                                };
                                            }
                                            else
                                            {
                                                internalWrite = new Message
                                                {
                                                    Type = Message.Types.Type.NnarInternalWrite,
                                                    ToAbstractionId = aId,
                                                    NnarInternalWrite = new NnarInternalWrite
                                                    {
                                                        ReadId = ReadId,
                                                        Timestamp = highest.Timestamp + 1,
                                                        WriterRank = MySystem.GetInstance().GetCurrentProcess().Rank,
                                                        Value = WriteValue
                                                    }
                                                };
                                            }

                                            msg = new Message
                                            {
                                                Type = Message.Types.Type.BebBroadcast,
                                                ToAbstractionId = aId + ".beb",
                                                BebBroadcast = new BebBroadcast
                                                {
                                                    Message = internalWrite
                                                }
                                            };
                                        }
                                    }

                                    Monitor.Exit(m_Locker);
                                    break;
                                }
                            case Message.Types.Type.NnarInternalAck:
                                {
                                    Monitor.Enter(m_Locker);
                                    var vMessage = message.PlDeliver.Message.NnarInternalAck;
                                    if (vMessage.ReadId == ReadId)
                                    {
                                        Acks += 1;
                                        if (Acks > N / 2)
                                        {
                                            Acks = 0;
                                            if (Reading)
                                            {
                                                Reading = false;
                                                msg = new Message
                                                {
                                                    Type = Message.Types.Type.NnarReadReturn,
                                                    FromAbstractionId = aId,
                                                    ToAbstractionId = "app",
                                                    NnarReadReturn = new NnarReadReturn
                                                    {
                                                        Value = ReadValue
                                                    }
                                                };
                                            }
                                            else
                                            {
                                                msg = new Message
                                                {
                                                    Type = Message.Types.Type.NnarWriteReturn,
                                                    FromAbstractionId = aId,
                                                    ToAbstractionId = "app",
                                                    NnarWriteReturn = new NnarWriteReturn { }
                                                };
                                            }
                                        }
                                    }
                                    Monitor.Exit(m_Locker);
                                    break;
                                }
                        }
                        break;
                    }
            }
            if (msg != null)
            {
                MySystem.GetInstance().AddMessageToQueue(msg);
            }
        }

        public NnarInternalValue Highest()
        {
            NnarInternalValue highest = ReadList.Values.First();
            foreach (var value in ReadList.Values)
            {
                if (Compare(value, highest) == 1)
                {
                    highest = value;
                }
            }
            return highest;
        }

        public int Compare(NnarInternalValue v1, NnarInternalValue v2)
        {
            if (v1.Timestamp > v2.Timestamp) return 1;
            if (v2.Timestamp > v1.Timestamp) return -1;
            if (v1.WriterRank > v2.WriterRank) return 1;
            if (v2.WriterRank > v1.WriterRank) return -1;
            return 0;
        }

        public string GetRegisterId(string abstractionId)
        {
            var pattern = @"([^\[]*)(\[([^\]]*)\])?";
            var match = Regex.Match(abstractionId, pattern);
            return match.Groups[3].Value;
        }
    }
}