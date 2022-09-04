using Main;
using ProiectAMCSD.Models;

namespace ProiectAMCSD
{
    public class EventLoop : IObserver<Message>
    {
        private MessageQueue messageQueue;
        private List<Algorithm> algorithmsQueue;
        public IDisposable? unsubscriber;

        public EventLoop(MessageQueue messageQueue, List<Algorithm> algorithmsQueue)
        {
            this.messageQueue = messageQueue;
            this.algorithmsQueue = algorithmsQueue;
        }

        public virtual void Subscribe(IObservable<Message> provider)
        {
            if (provider != null)
            {
                unsubscriber = provider.Subscribe(this);
            }
        }
        public virtual void OnCompleted()
        {
            unsubscriber.Dispose();
        }
        public virtual void OnError(Exception e)
        {
            //
        }
        public void OnNext(Message message)
        {
                algorithmsQueue.ForEach(algorithm =>
                {
                    try
                    {
                        if (algorithm.CanHandle(message))
                        {
                            messageQueue.Remove(message);
                            algorithm.DoHandle(message);
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e);
                    }
                });
        }

        public void OnEvent(Message message, int delay)
        {
                algorithmsQueue.ForEach(algorithm =>
                {
                    try
                    {
                        if (algorithm.CanHandle(message))
                        {
                            Thread.Sleep(delay);
                            messageQueue.Remove(message);
                            algorithm.DoHandle(message);
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e);
                    }
                });
        }

        public void Dispose()
        {
            //
        }

    }
}
