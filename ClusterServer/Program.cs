using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace ClusterServer
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			XmlConfigurator.Configure();

			try
			{
				ServerArguments parsedArguments;
				if(!ServerArguments.TryGetArguments(args, out parsedArguments))
					return;

				var listener = new HttpListener
				{
					Prefixes =
								{
									string.Format("http://+:{0}/{1}/",
										parsedArguments.Port,
										parsedArguments.MethodName)
								}
				};

				log.InfoFormat("Server is starting listening prefixes: {0}", string.Join(";", listener.Prefixes));

				if(parsedArguments.Async)
				{
					log.InfoFormat("Press ENTER to stop listening");
					listener.StartProcessingRequestsAsync(CreateAsyncCallback(parsedArguments));

					Console.ReadLine();
					log.InfoFormat("Server stopped!");
				}
				else
					listener.StartProcessingRequestsSync(CreateSyncCallback(parsedArguments));
			}
			catch(Exception e)
			{
				Log.Fatal(e);
			}
		}

		private static Func<HttpListenerContext, Task> CreateAsyncCallback(ServerArguments parsedArguments)
		{
		    var tokenSources = new ConcurrentDictionary<string, CancellationTokenSource>();
			return async context =>
			{
				var currentRequestNum = Interlocked.Increment(ref RequestsCount);
				log.InfoFormat("Thread #{0} received request #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestNum, DateTime.Now.TimeOfDay);

			    CancellationTokenSource ts;
			    if (context.Request.QueryString.AllKeys.Contains("abort"))
			    {
			        if (tokenSources.TryRemove(context.Request.UserHostAddress + ":" + context.Request.QueryString["guid"], out ts))
                        ts.Cancel();
			        log.InfoFormat("Thread #{0} canceled request.",
			            Thread.CurrentThread.ManagedThreadId);
			        await context.Response.OutputStream.WriteAsync(new byte[]{}, 0, 0);
                    return;
			    }

			    ts = new CancellationTokenSource();
			    var token = ts.Token;
			    tokenSources[context.Request.UserHostAddress + ":" + context.Request.QueryString["guid"]] = ts;

		        await Task.Delay(parsedArguments.MethodDuration, ts.Token);
                
//				Thread.Sleep(parsedArguments.MethodDuration);

				var encryptedBytes = GetBase64HashBytes(context.Request.QueryString["query"], Encoding.UTF8);
				await context.Response.OutputStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);

				log.InfoFormat("Thread #{0} sent response #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestNum,
					DateTime.Now.TimeOfDay);
			};
		}

	    private static Action<HttpListenerContext> CreateSyncCallback(ServerArguments parsedArguments)
		{
			return context =>
			{
				var currentRequestId = Interlocked.Increment(ref RequestsCount);
				log.InfoFormat("Thread #{0} received request #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestId, DateTime.Now.TimeOfDay);

				Thread.Sleep(parsedArguments.MethodDuration);

				var encryptedBytes = GetBase64HashBytes(context.Request.QueryString["query"], Encoding.UTF8);
				context.Response.OutputStream.Write(encryptedBytes, 0, encryptedBytes.Length);

				log.InfoFormat("Thread #{0} sent response #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestId,
					DateTime.Now.TimeOfDay);
			};
		}

		private static readonly ILog log = LogManager.GetLogger(typeof(Program));

		private static byte[] GetBase64HashBytes(string query, Encoding encoding)
		{
			using(var hasher = new HMACMD5(Key))
			{
				var hash = Convert.ToBase64String(hasher.ComputeHash(encoding.GetBytes(query ?? "")));
				return encoding.GetBytes(hash);
			}
		}

		private static readonly byte[] Key = Encoding.UTF8.GetBytes("Контур.Шпора");
		private static int RequestsCount;

		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
	}
}
