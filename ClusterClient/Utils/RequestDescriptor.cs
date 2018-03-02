using System;
using System.Threading.Tasks;

namespace ClusterClient.Utils
{
    public class RequestDescriptor
    {
        public RequestDescriptor(string uri, Guid guid, Task<string> task)
        {
            Uri = uri;
            Guid = guid;
            Task = task;
        }

        public string Uri { get; }
        public Guid Guid { get; }

        public Task<string> Task { get; }

    }
}