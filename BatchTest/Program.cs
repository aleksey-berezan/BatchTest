using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BatchTest
{
    class Program
    {
        static void Main()
        {
            // var client = new SerialClientWrapper(new Client());
            using (var clientWrapper = new BatchClientWrapper(new Client()))
            {
                var ts = Stopwatch.StartNew();

                var tasks = new List<Task<string>>();
                foreach (var r in Requests)
                {
                    tasks.Add(Task.Factory.StartNew(() => clientWrapper.Send(r), TaskCreationOptions.LongRunning));
                }

                Task.WaitAll(tasks.ToArray());

                Console.WriteLine($"Elapsed: {ts.Elapsed}");
            }

            Console.ReadLine();
        }

        public static string[] Requests => Enumerable.Range(0, 2000)
                                                     .Select(x => $"request_{x}")
                                                     .ToArray();
    }
}
