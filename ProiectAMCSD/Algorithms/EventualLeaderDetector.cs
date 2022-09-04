using Main;

namespace ProiectAMCSD.Algorithms
{
    public class EventualLeaderDetector : Algorithm
    {
        private HashSet<ProcessId> alive;
        private HashSet<ProcessId> suspected;
        private ProcessId leader;

        public EventualLeaderDetector()
        {
            suspected = new HashSet<ProcessId>();
            alive = new HashSet<ProcessId>();
            leader = new ProcessId();
        }

        public override bool CanHandle(Message message)
        {
            return message.Type == Message.Types.Type.EpfdSuspect || message.Type == Message.Types.Type.EpfdRestore;
        }

        public override void DoHandle(Message message)
        {
            switch (message.Type)
            {
                case Message.Types.Type.EpfdSuspect:
                    {
                        var p = message.EpfdSuspect.Process;
                        suspected.Add(p);
                        break;
                    }
                case Message.Types.Type.EpfdRestore:
                    {
                        var p = message.EpfdRestore.Process;
                        suspected.Remove(p);
                        break;
                    }
            }
            UpdateLeader(message);
        }

        public void UpdateLeader(Message message)
        {
            alive = MySystem.GetInstance().GetProcesses().Where(x => !suspected.Contains(x)).ToHashSet();
            if (alive.Count == 0) return;

            var max = MaxRank();

            if (leader == null || leader != max)
            {
                leader = max;
                var msg = new Message
                {
                    Type = Message.Types.Type.EldTrust,
                    FromAbstractionId = "eld",
                    ToAbstractionId = ParentAbsId(message.ToAbstractionId),
                    EldTrust = new EldTrust
                    {
                        Process = leader
                    }
                };
                MySystem.GetInstance().AddMessageToQueue(msg);
            }
        }

        internal ProcessId MaxRank()
        {
            ProcessId max = alive.ElementAt(0);
            foreach (var process in alive)
            {
                if (process.Rank > max.Rank)
                {
                    max = process;
                }
            }
            return max;
        }
    }
}
