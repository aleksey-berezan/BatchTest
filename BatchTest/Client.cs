using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BatchTest
{
    class Client : IClient
    {
        public string Send(string request)
        {
            Console.WriteLine("Sending {0} request", request);
            Thread.Sleep(200);
            return $"response_{request}";
        }

        public Dictionary<string, string> SendBatch(List<string> batchRequest)
        {
            Trace.WriteLine($"Sending batch with {batchRequest.Count} requests");
            Console.WriteLine("Sending batch with {0} requests", batchRequest.Count);
            int delay = batchRequest.Count * 10;
            Thread.Sleep(delay);
            return batchRequest.ToDictionary(r => r, p => $"request_{p}");
        }
    }
}