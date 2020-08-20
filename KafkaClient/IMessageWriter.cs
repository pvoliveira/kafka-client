using System.Buffers;

namespace KafkaClient
{
    public interface IMessageWriter<TMessage>
    {
        void WriteMessage(TMessage message, IBufferWriter<byte> output);
    }
}
