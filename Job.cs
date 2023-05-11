using CORERenderer.Main;
using Console = CORERenderer.GUI.Console;

namespace CORERenderer
{
    public class Jobs
    {
        private static int uniqueID = 0;
        internal static int NewID { get { uniqueID++; return uniqueID - 1; } }

        public virtual void Start() { throw new NotImplementedException(); }
        public virtual void Restart() { throw new NotImplementedException(); }
        public virtual void Wait() { throw new NotImplementedException(); }
    }

    public class Job : Jobs
    {
        private static int reservedThreads = 0;
        public static int ReservedThreads { get => reservedThreads; }

        public Action action;
        private Thread thread;
        public bool IsFinished { get { return !thread.IsAlive; } }
        public readonly int ID;

        public Job(Action action)
        {
            this.action = action;
            ID = NewID;
            reservedThreads++;
        }

        public override void Start()
        {
           Console.WriteDebug($"Starting job with ID {ID} ...");
            
            thread = new(Run);
            thread.Start();
        }

        public override void Restart()
        {
            thread = new(Run);
            thread.Start();
        }

        public override void Wait()
        {
            thread.Join();
        }

        private void Run() => action.Invoke();

        public static void ParallelForEach<T>(IEnumerable<T> source, Action<T> body)
        {
            Parallel.ForEach(source, body);;
        }
        public static void ParallelFor(int fromInclusive, int toExclusive, Action<int> action)
        {
            Parallel.For(fromInclusive, toExclusive, action);
        }

        ~Job()
        {
            reservedThreads--;
        }
    }
}