using CORERenderer.Main;

namespace CORERenderer
{
    public class Jobs
    {
        private static int uniqueID = 0;
        internal static int NewID { get { uniqueID++;  return uniqueID - 1; } }
        
        public virtual void Start() { throw new NotImplementedException(); }
        public virtual void Restart() { throw new NotImplementedException(); }
        public virtual void Wait() { throw new NotImplementedException(); }
    }

    public class Job : Jobs
    {
        internal static volatile int threadsUsed = 0;
        public static int usedThreads { get { return threadsUsed; } }

        public Action action;
        public Task task;
        public bool IsFinished { get { return task.IsCompleted; } }
        public readonly int ID;

        public Job(Action action)
        {
            this.action = action;
            ID = NewID;
        }

        public override void Start()
        {
            if (COREMain.console != null)
                COREMain.console.WriteDebug($"Starting job with ID {ID} ...");
            else
                COREMain.consoleCache.Add($"DEBUG Starting new job...");
            task = Task.Run(() => { Interlocked.Increment(ref threadsUsed); action.Invoke(); Interlocked.Decrement(ref threadsUsed); });
        }
        public override void Restart()
        {
            task = Task.Run(() => { Interlocked.Increment(ref threadsUsed); action.Invoke(); Interlocked.Decrement(ref threadsUsed); });
        }

        public override void Wait() => task.Wait();

        public static void ParallelForEach<T>(IEnumerable<T> source, Action<T> body) => Parallel.ForEach(source, s => { Interlocked.Increment(ref threadsUsed); body.Invoke(s); Interlocked.Decrement(ref threadsUsed); });
        public static void ParallelFor(int fromInclusive, int toExclusive, Action<int> action) => Parallel.For(fromInclusive, toExclusive, i => { Interlocked.Increment(ref threadsUsed); action.Invoke(i); Interlocked.Decrement(ref threadsUsed); });
    }
}