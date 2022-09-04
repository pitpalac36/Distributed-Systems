using Main;

namespace ProiectAMCSD
{
    public abstract class Algorithm 
    {
        bool Handle(Message message)
        {
            if (CanHandle(message)) 
            {
                DoHandle(message);
                return true;
            }
            return false;
        }

        public abstract bool CanHandle(Message message);

        public abstract void DoHandle(Message message);

        public string ParentAbsId(string aId)
        {
            var ids = aId.Split('.').ToList();
            ids.RemoveAt(ids.Count - 1);
            return string.Join(".", ids);
        }
    }
}
