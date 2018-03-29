using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BatchTest
{
    class BatchClientWrapper : IClientWrapper
    {
        private sealed class RequestInfo
        {
            public string Request { get; }
            public TaskCompletionSource<string> TaskCompletion { get; }
            public Task<string> Task { get; }

            public RequestInfo(string request)
            {
                Request = request;
                TaskCompletion = new TaskCompletionSource<string>();
                Task = TaskCompletion.Task;
            }
        }

        private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(200);
        private static readonly int MaxBatchSize = 30;

        private readonly IClient _client;
        private readonly ConcurrentQueue<RequestInfo> _queue = new ConcurrentQueue<RequestInfo>();
        private readonly Thread _workerThread;
        private readonly ManualResetEventSlim _manualResetEvent = new ManualResetEventSlim(false);

        private bool IsStopped => _manualResetEvent.IsSet;

        public BatchClientWrapper(IClient client)
        {
            _client = client;
            _workerThread = new Thread(WorkerLoop)
            {
                Name = $"{GetType().Name}_Worker",
                IsBackground = true
            };
            _workerThread.Start();
        }

        public string Send(string request)
        {
            var requestInfo = new RequestInfo(request);
            _queue.Enqueue(requestInfo);
            return requestInfo.Task.Result;
        }

        private void WorkerLoop()
        {
            var last = DateTime.Now;
            while (true)
            {
                if (IsStopped)
                    return;

                var requests = AccumulateRequests(last);
                if (requests == null)
                    return;

                last = DateTime.Now;
                Dictionary<string, RequestInfo> dictionary = requests.ToDictionary(r => r.Request);
                Task.Run(() => SendBatchRequest(requests))
                    .ContinueWith(t => ProcessBatchResult(dictionary, t.Result));
            }
        }

        private List<RequestInfo> AccumulateRequests(DateTime last)
        {
            var requests = new List<RequestInfo>();

            while (_queue.IsEmpty)
            {
                if (IsStopped)
                    return null;
                Trace.WriteLine("queue is empty");
                Thread.Yield();
            }

            while (true)
            {
                Trace.WriteLine($"loop start with {requests.Count} items in queue");
                if (IsStopped)
                    return null;

                RequestInfo r;
                if (_queue.TryDequeue(out r))
                {
                    requests.Add(r);
                }

                if (requests.Count >= MaxBatchSize)
                {
                    Trace.WriteLine($"Batch is full: {requests.Count}");
                    break;
                }

                var delay = DateTime.Now - last;
                if (delay >= MaxDelay)
                {
                    if (requests.Count > 0)
                    {
                        Trace.WriteLine($"No more time to wait({delay.Milliseconds} ms elapsed) but we have {requests.Count} requests");
                        return requests;
                    }

                    Trace.WriteLine($"No more time to wait({delay.Milliseconds} ms elapsed) and requests list is empty, reseting the timer");
                    last = DateTime.Now;
                    Thread.Yield();

                    break;
                }

                if (_queue.IsEmpty)
                    Thread.Yield();
            }

            return requests;
        }

        private Dictionary<string, string> SendBatchRequest(List<RequestInfo> requests)
        {
            return _client.SendBatch(requests.Select(r => r.Request).ToList());
        }

        private static void ProcessBatchResult(
            Dictionary<string, RequestInfo> requestMap,
            Dictionary<string, string> batchResult)
        {
            foreach (var pair in batchResult)
            {
                string r = pair.Key;
                RequestInfo ri = requestMap[r];
                ri.TaskCompletion.SetResult(r);
            }
        }

        public void Dispose()
        {
            Trace.WriteLine("Dispose() called, will stop.");
            _manualResetEvent.Set();
        }
    }
}