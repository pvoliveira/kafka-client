namespace KafkaClient
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;
    using KafkaClient.Messages;

    class Program
    {
        static async Task Main(string[] args)
        {
            var sw = new Stopwatch();

            Console.ReadKey();

            sw.Start();

            var memberId = Guid.NewGuid().ToString();
            const string groupId = "print-console-handler";

            var host = await KafkaHost.MakeHostAsync(
                "localhost",
                9092,
                "test-client-id");

            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var apiVersion = await host.SendAsync(
                new ApiVersionV2Request(),
                TimeSpan.FromSeconds(30));

            var topicMetadata = await host.SendAsync(
                new TopicMetadataV9Request(
                    new[] { new TopicMetadataV9Request.Topic("test-topic") },
                    false,
                    true,
                    true),
                TimeSpan.FromSeconds(30));

            var findCoordResponse = await host.SendAsync(
                new FindCoordinatorV3Request(string.Empty, 0),
                TimeSpan.FromSeconds(30));

            var joinGroupResponse = await host.SendAsync(
                new JoinGroupV7Request(
                    "print-console-handler",
                    300000,
                    3000,
                    string.Empty,
                    null,
                    "consumer",
                    new[] { new JoinGroupV7Request.Protocol("consumer", Array.Empty<byte>()), }),
                TimeSpan.FromSeconds(30));

            var joinGroupResponse1 = await host.SendAsync(
                new JoinGroupV7Request(
                    "print-console-handler",
                    300000,
                    3000,
                    joinGroupResponse.MemberId,
                    null,
                    "consumer",
                    new[] { new JoinGroupV7Request.Protocol("consumer", Array.Empty<byte>()), }),
                TimeSpan.FromSeconds(30));

            var heartbeatResponse = await host.SendAsync(
                new HeartbeatV4Request(
                    groupId,
                    joinGroupResponse1.GenerationId,
                    joinGroupResponse1.MemberId),
                TimeSpan.FromSeconds(30));

            var offsetFetchResponse = await host.SendAsync(
                new OffsetFetchV5Request(
                    groupId,
                    new[]
                    {
                        new OffsetFetchV5Request.Topic(
                            "test-topic",
                            new[] { 0, 1, 2 })
                    }),
                TimeSpan.FromSeconds(30));

            for (int i = 0; i < 2; i++)
            {

                var produceResponse = await ProduceMessage(host, now);
                var fetchResponse = await FetchMessage(host);

                Console.WriteLine(i);
            }

            sw.Stop();
            //await Task.Delay(5000);

            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        private static Task<FetchV11Response> FetchMessage(KafkaHost connection)
        {
            return connection.SendAsync(
                new FetchV11Request
                {
                    ReplicaId = -1,
                    MaxWaitTime = 5000,
                    MinBytes = 0,
                    MaxBytes = 1024 * 16 * 3,
                    IsolationLevel = 1,
                    Topics = new[]
                    {
                        new FetchV11Request.Topic
                        {
                            Name = "test-topic",
                            Partitions = new[]
                            {
                                new FetchV11Request.Partition
                                {
                                    Id = 0,
                                    FetchOffset = 0,
                                    PartitionMaxBytes = 1024 * 16
                                },
                                new FetchV11Request.Partition
                                {
                                    Id = 1,
                                    FetchOffset = 0,
                                    PartitionMaxBytes = 1024 * 16
                                },
                                new FetchV11Request.Partition
                                {
                                    Id = 2,
                                    FetchOffset = 0,
                                    PartitionMaxBytes = 1024 * 16
                                },
                            }
                        }
                    }
                },
                TimeSpan.FromSeconds(30));
        }

        private static Task<ProduceV8Response> ProduceMessage(KafkaHost connection, long now)
        {
            return connection.SendAsync(
                new ProduceV8Request(
                    ProduceAcks.Leader,
                    5000,
                    new[]
                    {
                        new ProduceV8Request.Topic(
                            "test-client",
                            new[]
                            {
                                new ProduceV8Request.Partition(
                                    0,
                                    new RecordBatch
                                    {
                                        BaseOffset = 0,
                                        LastOffsetDelta = 0,
                                        FirstTimestamp = now,
                                        MaxTimestamp = now,
                                        Records = new[]
                                        {
                                            new RecordBatch.Record
                                            {
                                                TimestampDelta = 0,
                                                OffsetDelta = 0,
                                                Key = Encoding.UTF8.GetBytes("teste_key"),
                                                Value = Encoding.UTF8.GetBytes("teste_value"),
                                                Headers = new[]
                                                {
                                                    new RecordBatch.Header
                                                    {
                                                        Key = "teste_header_key",
                                                        Value = Encoding.UTF8.GetBytes("teste_header_value")
                                                    }
                                                }
                                            }
                                        }
                                    }),
                            }),
                    }
                ),
                TimeSpan.FromSeconds(30));
        }
    }
}
