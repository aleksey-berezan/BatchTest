namespace BatchTest
{
    class SerialClientWrapper : IClientWrapper
    {
        private readonly IClient _client;

        public SerialClientWrapper(IClient client)
        {
            _client = client;
        }

        public string Send(string request)
        {
            return _client.Send(request);
        }

        public void Dispose() { }
    }
}