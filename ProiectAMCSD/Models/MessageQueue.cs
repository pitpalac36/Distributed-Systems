using Main;

namespace ProiectAMCSD.Models
{
    public class MessageQueue : IObservable<Message>
    {
        private List<Message> _messages;
        private EventLoop? _observer;
        private readonly string m_Locker = "THREAD_LOCKER";

        public MessageQueue()
        {
            _messages = new List<Message>();
            
        }

        internal void Add(Message message)
        {
            Monitor.Enter(m_Locker);
            _messages.Add(message);
            Monitor.Exit(m_Locker);
            if (message == null)
            {
                _observer!.OnError(new ArgumentNullException());
            }
            _observer!.OnNext(message!);
        }

        internal void Add(Message message, int delay)
        {
            Monitor.Enter(m_Locker);
            _messages.Add(message);
            Monitor.Exit(m_Locker);
            if (message == null)
            {
                _observer!.OnError(new ArgumentNullException());
            }
            _observer!.OnEvent(message!, delay);
        }

        internal void Remove(Message message)
        {
            Monitor.Enter(m_Locker);
            _messages.Remove(message);
            Monitor.Exit(m_Locker);
        }

        public IDisposable Subscribe(IObserver<Message> eventLoop)
        {
            _observer = (EventLoop) eventLoop;
            return new Unsubscriber(_observer);
        }
        

        public class Unsubscriber : IDisposable
        {
            private IObserver<Message>? _observer;

            public Unsubscriber(IObserver<Message> observer)
            {
                _observer = observer;
            }

            public void Dispose()
            {
                Dispose(true);
            }
            private bool _disposed = false;
            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }
                if (disposing)
                {
                    _observer = null;
                }
                _disposed = true;
            }
        }

        public void End()
        {
            _observer!.OnCompleted();
            _observer = null;
        }
    }
}
