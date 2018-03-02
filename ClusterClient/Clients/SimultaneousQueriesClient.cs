using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class SimultaneousQueriesClient : ClusterClientBase
    {
        public SimultaneousQueriesClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var queryTasks = ReplicaAddresses
                .Select(uri => CreateRequest(uri + "?query=" + query))
                .Select(ProcessRequestAsync)
                .Concat(new[]{Task.Delay(timeout) })
                .ToArray();

            var task = await Task.WhenAny(queryTasks);

            var result = (task as Task<string>)?.Result;
            if (result == null)
                throw new TimeoutException();
            return result;
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SimultaneousQueriesClient));
    }
}