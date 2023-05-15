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
        private static int maxThreads = 999;
        public static int ReservedThreads { get => reservedThreads; }
        public static int MaxThreads { get => maxThreads; set { if (value < 0) Console.WriteLine("The application needs one thread to be active"); else maxThreads = value; } }

        public Action action;
        private Thread thread;
        public bool IsFinished { get { return !thread.IsAlive; } }
        public readonly int ID;
        private bool canRunOnDifferentThread = true;

        public Job(Action action)
        {
            this.action = action;
            ID = NewID;

            if (reservedThreads + 1 < maxThreads)
                reservedThreads++;
            else canRunOnDifferentThread = false;
        }

        public override void Start()
        {
            if (canRunOnDifferentThread)
            {
                Console.WriteDebug($"Starting job with ID {ID} ...");
                thread = new(Run);
                thread.Start();
            }
            else
            {
                Console.WriteError("Max threads reached, executing job on main thread...");
                Run();
            }
        }

        public override void Restart()
        {
            thread = new(Run);
            thread.Start();
        }

        public override void Wait()
        {
            if (canRunOnDifferentThread)
                thread.Join();
        }

        private void Run() => action.Invoke();

        public static void ParallelForEach<T>(IEnumerable<T> source, Action<T> body)
        {
            Parallel.ForEach(source, body);
        }
        public static void ParallelFor(int fromInclusive, int toExclusive, Action<int> action)
        {
            Parallel.For(fromInclusive, toExclusive, action);
        }

        ~Job()
        {
            if (canRunOnDifferentThread)
                reservedThreads--;
        }
    }
}