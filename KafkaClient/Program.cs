﻿namespace KafkaClient
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using KafkaClient.Messages;

    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new KafkaHostConnection(
                "localhost",
                9092,
                "test_client");

            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var apiVersion = await connection.SendAsync(
                new ApiVersionRequest(),
                TimeSpan.FromSeconds(30));

            var topicMetadata = await connection.SendAsync(
                new TopicMetadataV1Request(new[] { "test-topic", "test-gzip", "test-json" }),
                TimeSpan.FromSeconds(30));

            var offsetFetchResponse = await connection.SendAsync(
                new OffsetFetchV5Request(
                    "print-console-handler",
                    new[]
                    {
                        new OffsetFetchV5Request.Topic(
                            "test-topic",
                            new[] { 0, 1, 2 })
                    }),
                TimeSpan.FromSeconds(30));

            var produceResponse = await ProduceMessage(connection, now);
            var fetchResponse = await FetchMessage(connection);

            await Task.Delay(5000);
        }

        private static Task<FetchV11Response> FetchMessage(KafkaHostConnection connection)
        {
            return connection.SendAsync(
                new FetchV11Request
                {
                    ReplicaID = -1,
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
                                    ID = 0,
                                    FetchOffset = 0,
                                    PartitionMaxBytes = 1024 * 16
                                },
                                new FetchV11Request.Partition
                                {
                                    ID = 1,
                                    FetchOffset = 0,
                                    PartitionMaxBytes = 1024 * 16
                                },
                                new FetchV11Request.Partition
                                {
                                    ID = 2,
                                    FetchOffset = 0,
                                    PartitionMaxBytes = 1024 * 16
                                },
                            }
                        }
                    }
                },
                TimeSpan.FromSeconds(30));
        }

        private static Task<ProduceV8Response> ProduceMessage(KafkaHostConnection connection, long now)
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
