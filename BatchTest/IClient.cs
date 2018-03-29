using System.Collections.Generic;

namespace BatchTest
{
    interface IClient
    {
        Dictionary<string, string> SendBatch(List<string> batchRequest);
        string Send(string request);
    }
}