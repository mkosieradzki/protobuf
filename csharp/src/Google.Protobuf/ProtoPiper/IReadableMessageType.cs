namespace Google.Protobuf.ProtoPiper
{
    public interface IReadableMessageType
    {
        object CreateMessage();
        void ConsumeField(object message, uint tag, object field);
        FieldType GetFieldType(uint tag);
    }

    public readonly struct FieldType
    {
        public FieldType(IReadableMessageType messageType)
        {
            MessageType = messageType;
        }

        IReadableMessageType MessageType { get; }
    }
}
