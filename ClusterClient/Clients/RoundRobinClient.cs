using System;
using System.Linq;
using System.Threading.Tasks;
using ClusterClient.Utils;
using log4net;

namespace ClusterClient.Clients
{
    public class RoundRobinClient : ClusterClientBase
    {
        public RoundRobinClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var shuffledReplicas = ReplicaAddresses.ToArray();
            shuffledReplicas.RandomShuffle();

            timeout = TimeSpan.FromTicks(timeout.Ticks / shuffledReplicas.Length);

            foreach (var task in shuffledReplicas
                .Select(uri => CreateRequest(uri + "?query=" + query))
                .Select(request => ProcessRequestAsync(request)))
            {
                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    return task.Result;
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RoundRobinClient));
    }
}