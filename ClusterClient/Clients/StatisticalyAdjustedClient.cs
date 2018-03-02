using System;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class StatisticalyAdjustedClient : ClusterClientBase
    {

        public StatisticalyAdjustedClient(string[] replicaAddresses)
            : base(replicaAddresses)
        {
        }

        public async override Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            return null;
        }

        protected override ILog Log => LogManager.GetLogger(typeof(StatisticalyAdjustedClient));
    }
}