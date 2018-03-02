using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using log4net;

namespace NMAP
{
	public class SequentialScanner : IPScanner
	{
		protected virtual ILog log => LogManager.GetLogger(typeof(SequentialScanner));

		public virtual Task Scan(IPAddress[] ipAddrs, int[] ports)
		{
			return Task.Run(() =>
			{
			    Task.WaitAll(ipAddrs.Select(async ipAddr =>
			    {
			        if (await PingAddrAsync(ipAddr) != IPStatus.Success)
			            return;


			        Task.WaitAll(ports
			            .Select(port => CheckPortAsync(ipAddr, port))
			            .ToArray());
			    }).ToArray());
			});
		}

		protected async Task<IPStatus> PingAddrAsync(IPAddress ipAddr, int timeout = 3000)
		{
			log.Info($"Pinging {ipAddr}");
			using(var ping = new Ping())
			{
				var task = await ping.SendPingAsync(ipAddr, timeout);
				log.Info($"Pinged {ipAddr}: {task.Status}");
				return task.Status;
			}
		}

		protected async Task CheckPortAsync(IPAddress ipAddr, int port, int timeout = 3000)
		{
			using(var tcpClient = new TcpClient())
			{
				log.Info($"Checking {ipAddr}:{port}");

				var connectTask = tcpClient.Connect(ipAddr, port, timeout);
				PortStatus portStatus;
				switch(connectTask.Status)
				{
					case TaskStatus.RanToCompletion:
						portStatus = PortStatus.OPEN;
						break;
					case TaskStatus.Faulted:
						portStatus = PortStatus.CLOSED;
						break;
					default:
						portStatus = PortStatus.FILTERED;
						break;
				}
				log.Info($"Checked {ipAddr}:{port} - {portStatus}");
			}
		}
	}
}