namespace Google.Protobuf.ProtoPiper
{
    public interface IReadableMessageType
    {
        object CreateMessage();
        void ConsumeField(object message, uint tag, WireFormat.WireType wireType, object field);
        IReadableMessageType GetNestedType(uint tag);
    }
}
