namespace KafkaClient
{
    using System.Buffers;
    using System.IO;
    using KafkaClient.Messages;

    public interface IResponse
    {
        void Read(ref SequenceReader<byte> source);
    }

    public interface IResponseV2 : IResponse
    {
        TaggedField[] TaggedFields { get; }
    }
}
