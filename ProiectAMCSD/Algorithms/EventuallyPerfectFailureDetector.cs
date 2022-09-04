using Main;
using ProiectAMCSD.Utils;

namespace ProiectAMCSD.Algorithms
{
    public class EventuallyPerfectFailureDetector : Algorithm
    {
        private static int delta = 100;
        private int delay = delta;
        private HashSet<ProcessId> alive, suspected;
        private string m_Locker = "THREAD_LOCKER";

        public EventuallyPerfectFailureDetector() {
            alive = new HashSet<ProcessId>();
            suspected = new HashSet<ProcessId>();
        }

        public void Start()
        {
            Monitor.Enter(m_Locker);
            alive = MySystem.GetInstance().GetProcesses().ToHashSet();
            Monitor.Exit(m_Locker);
            StartTimer();
        }

        public void StartTimer()
        {
            var message = new Message
           {
                Type = Message.Types.Type.EpfdTimeout,
                FromAbstractionId = "epfd",
                ToAbstractionId = "app.uc[" + SystemInfo.TOPIC + "].ec.eld.epfd",
                EpfdTimeout = new EpfdTimeout { }
            };

            while (true)
            {
                Monitor.Enter(m_Locker);

                if (alive.Intersect(suspected).Count() > 0)
                {
                    delay += delta;
                }
                var msg = new Message { };

                foreach (var process in MySystem.GetInstance().GetProcesses())
                {
                    if (!(alive.Contains(process) || suspected.Contains(process)))
                    {
                        suspected.Add(process);
                        msg = new Message
                        {
                            Type = Message.Types.Type.EpfdSuspect,
                            FromAbstractionId = "epfd",
                            ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                            EpfdSuspect = new EpfdSuspect
                            {
                                Process = process,
                            }
                        };
                        MySystem.GetInstance().AddMessageToQueue(msg);
                    }
                    else if (alive.Contains(process) && suspected.Contains(process))
                    {
                        suspected.Remove(process);
                        msg = new Message
                        {
                            Type = Message.Types.Type.EpfdRestore,
                            FromAbstractionId = "epfd",
                            ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                            EpfdRestore = new EpfdRestore
                            {
                                Process = process,
                            }
                        };
                        MySystem.GetInstance().AddMessageToQueue(msg);
                    }
                    msg = new Message
                    {
                        Type = Message.Types.Type.PlSend,
                        FromAbstractionId = "epfd",
                        ToAbstractionId = ParentAbsId(message.ToAbstractionId) + ".pl",
                        PlSend = new PlSend
                        {
                            Destination = process,
                            Message = new Message
                            {
                                Type = Message.Types.Type.EpfdInternalHeartbeatRequest,
                                ToAbstractionId = "epfd",
                                FromAbstractionId = "epfd",
                                EpfdInternalHeartbeatRequest = new EpfdInternalHeartbeatRequest { }
                            }
                        }
                    };
                    MySystem.GetInstance().AddMessageToQueue(msg);
                }
                alive.Clear();

                Monitor.Exit(m_Locker);

                Thread.Sleep(delay);
            }      
        }

        public override bool CanHandle(Message message)
        {
            return (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EpfdInternalHeartbeatReply) ||
                (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.EpfdInternalHeartbeatRequest) ||
                (message.Type == Message.Types.Type.EpfdTimeout);
        }

        public override void DoHandle(Message message)
        {
            var msg = new Message { };
            Monitor.Enter(m_Locker);
            switch (message.Type)
            {
                
                case Message.Types.Type.EpfdTimeout:
                    {
                        break;
                    }
                case Message.Types.Type.PlDeliver:
                    {
                        switch(message.PlDeliver.Message.Type)
                        {
                            case Message.Types.Type.EpfdInternalHeartbeatReply:
                                {
                                    alive.Add(message.PlDeliver.Sender);
                                    break;
                                }
                            case Message.Types.Type.EpfdInternalHeartbeatRequest:
                                {
                                    msg = new Message
                                    {
                                        Type = Message.Types.Type.PlSend,
                                        FromAbstractionId = "epfd",
                                        ToAbstractionId = ParentAbsId(message.ToAbstractionId) + ".pl",
                                        PlSend = new PlSend
                                        {
                                            Destination = message.PlDeliver.Sender,
                                            Message = new Message
                                            {
                                                Type = Message.Types.Type.EpfdInternalHeartbeatReply,
                                                ToAbstractionId = "epfd",
                                                FromAbstractionId = "epfd",
                                                EpfdInternalHeartbeatReply = new EpfdInternalHeartbeatReply { }
                                            }
                                        }
                                    };
                                    MySystem.GetInstance().AddMessageToQueue(msg);
                                    break;
                                }
                        }
                        break;
                    }
            }
            Monitor.Exit(m_Locker);
        }
    }
}
