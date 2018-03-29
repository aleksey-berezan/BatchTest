using System;

namespace BatchTest
{
    interface IClientWrapper : IDisposable
    {
        string Send(string request);
    }
}