using System;
using System.Threading.Tasks;

namespace EmberCrpg.Infrastructure.AiDm
{
    public static class SyncTaskBridge
    {
        public static T Run<T>(Func<Task<T>> taskFactory)
        {
            if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));
            return Task.Run(taskFactory).GetAwaiter().GetResult();
        }
    }
}
