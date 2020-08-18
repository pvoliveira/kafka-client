namespace KafkaClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class KafkaHostConnection : IDisposable
    {
        private readonly TcpClient client;
        private readonly NetworkStream stream;

        private readonly ConcurrentDictionary<int, PendindRequest> pendindRequests = new ConcurrentDictionary<int, PendindRequest>();

        private int lastCorrelationId;

        private readonly Task listenerTask;

        private readonly CancellationTokenSource stopTokenSource = new CancellationTokenSource();
        private readonly string cliendId;

        public KafkaHostConnection(string host, int port, string cliendId)
        {
            this.cliendId = cliendId;
            this.client = new TcpClient(host, port);
            this.stream = this.client.GetStream();

            this.listenerTask = Task.Run(this.ListenStream);
        }

        private async Task ListenStream()
        {
            while (!this.stopTokenSource.IsCancellationRequested)
            {
                while (!this.stream.DataAvailable)
                {
                    if (this.stopTokenSource.IsCancellationRequested)
                        return;

                    await Task.Delay(100).ConfigureAwait(false);
                }

                var messageSize = this.stream.ReadInt32();

                var tmp = new byte[messageSize];
                this.stream.Read(tmp);

                using var payload = new MemoryStream(tmp);
                var correlationId = payload.ReadInt32();

                if (!this.pendindRequests.TryRemove(correlationId, out var request))
                {
                    continue;
                }

                var message = (IResponse) Activator.CreateInstance(request.ResponseType)!;

                if (message is IResponseV2)
                    _ = payload.ReadTaggedFields();

                message.Read(payload);

                if (payload.Length != payload.Position)
                    throw new Exception("Some data was not read from response");

                request.CompletionSource.TrySetResult(message);
            }
        }

        public Task<TResponse> SendAsync<TResponse>(IRequestMessage<TResponse> request, TimeSpan timeout)
            where TResponse : IResponse, new()
        {
            var pendindRequest = new PendindRequest(
                timeout,
                typeof(TResponse));

            lock (this.stream)
            {
                this.pendindRequests.TryAdd(++this.lastCorrelationId, pendindRequest);

                this.stream.WriteMessage(
                    new Request(
                        this.lastCorrelationId,
                        this.cliendId,
                        request));
            }

            return pendindRequest.GetTask<TResponse>();
        }

        public void Dispose()
        {
            this.stopTokenSource.Cancel();

            this.listenerTask.GetAwaiter().GetResult();

            this.client.Dispose();
        }

        private class PendindRequest
        {
            public TimeSpan Timeout { get; }

            public Type ResponseType { get; }

            public readonly TaskCompletionSource<IResponse> CompletionSource =
                new TaskCompletionSource<IResponse>();

            public PendindRequest(TimeSpan timeout, Type responseType)
            {
                this.Timeout = timeout;
                this.ResponseType = responseType;
            }

            public Task<TResponse> GetTask<TResponse>() where TResponse : IResponse =>
                this.CompletionSource.Task.ContinueWith(x => (TResponse) x.Result);
        }
    }
}
