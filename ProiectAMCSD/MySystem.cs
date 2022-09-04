using Main;
using ProiectAMCSD.Algorithms;
using ProiectAMCSD.Models;

namespace ProiectAMCSD
{
    public class MySystem
    {
        private static MySystem? instance;
        private MessageQueue messageQueue;
        private List<Algorithm> algorithms;

        private ISet<ProcessId> _processes;

        internal ProcessId MaxRank()
        {
            ProcessId max = _processes.ElementAt(0);
            foreach(var process in _processes)
            {
                if (process.Rank > max.Rank)
                {
                    max = process;
                }
            }
            return max;
        }

        private int processPort;

        private EventLoop? eventLoop;

        private NetworkHandler? networkHandler;

        public static MySystem GetInstance()
        {
            if (instance == null)
                instance = new MySystem();

            return instance;
        }

        private MySystem()
        {
            messageQueue = new MessageQueue();
            algorithms = new List<Algorithm>();
            _processes = new HashSet<ProcessId>();
        }

        internal void AddEventToQueue(Message msg, int delay)
        {
            messageQueue.Add(msg, delay);
        }

        public ProcessId GetCurrentProcess()
        {
            return GetProcessIdByPort(processPort);
        }

        public void Start()
        {
            InitializeAlgorithms();

            eventLoop = new EventLoop(messageQueue, algorithms);
            messageQueue.Subscribe(eventLoop!);

            networkHandler = new NetworkHandler(processPort);
            networkHandler.Start();
        }

        public void InitializeAlgorithms()
        {
            algorithms.Add(new Application("127.0.0.1", processPort));
            algorithms.Add(new PerfectLink("127.0.0.1", processPort));
            algorithms.Add(new BestEffortBroadcast());
            algorithms.Add(new Nnar());

            algorithms.Add(new EventuallyPerfectFailureDetector());
            algorithms.Add(new EventualLeaderDetector());
            algorithms.Add(new EpochChange());
            var epochConsensus = new EpochConsensus();
            algorithms.Add(epochConsensus);
            algorithms.Add(new UniformConsensus(epochConsensus));
        }

        public void RegisterConsensus()
        {
            try
            {
                var epfd = (EventuallyPerfectFailureDetector)algorithms.FirstOrDefault(x => x is EventuallyPerfectFailureDetector)!;
                new Thread(() => epfd.Start()).Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        internal void AddMessageToQueue(Message wrapperMessage)
        {
            messageQueue.Add(wrapperMessage);
        }

        internal void SetProcessPort(int clientPort)
        {
            processPort = clientPort;
        }

        public void SendMessage(Message message, string host, int port)
        {
            networkHandler!.SendMessage(message, host, port);
        }

        public ISet<ProcessId> GetProcesses()
        {
            return _processes;
        }

        public void SetProcesses(ISet<ProcessId> processes)
        {
            _processes = processes;
        }

        public ProcessId GetProcessIdByPort(int processPort)
        {
            return _processes.FirstOrDefault(id => id.Port == processPort)!;
        }

    }

}
