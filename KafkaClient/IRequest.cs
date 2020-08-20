namespace KafkaClient
{
    using System.Buffers;
    using System.IO;
    using KafkaClient.Messages;

    public interface IRequest
    {
        void Write(Stream destination);
    }

    public interface IRequestV2 : IRequest
    {
        TaggedField[] TaggedFields { get; }
    }
}
