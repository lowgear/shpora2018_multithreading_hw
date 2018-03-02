using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterClient.Utils;
using log4net;

namespace ClusterClient.Clients
{
    class SmartClient : ClusterClientBase
    {
        public SmartClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var shuffledReplicas = ReplicaAddresses.ToArray();
            shuffledReplicas.RandomShuffle();

            timeout = TimeSpan.FromTicks(timeout.Ticks / shuffledReplicas.Length);

            var queries = new List<RequestDescriptor>();
            foreach (var uri in shuffledReplicas)
            {
                var guid = Guid.NewGuid();
                var request = CreateRequest(uri + "?query=" + query + "&guid=" + guid);
                var task = ProcessRequestAsync(request);
                queries.Add(new RequestDescriptor(uri, guid, task));
                var finishedTask = await Task.WhenAny(queries
                    .Select(d => d.Task)
                    .Concat(new[] {Task.Delay(timeout)}));
                if (!(finishedTask is Task<string>)) continue;
                foreach ( var _ in queries
                    .Where(d => d.Task != finishedTask)
                    .Select(d => CreateRequest(d.Uri + "?abort=" + "&guid=" + d.Guid))
                    .Select(r => ProcessRequestAsync(r)));

                return (finishedTask as Task<string>).Result;
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClient));
    }
}