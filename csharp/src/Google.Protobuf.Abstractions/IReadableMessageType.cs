namespace Google.Protobuf
{
    public interface IReadableMessageType
    {
        object CreateMessage();
        void ConsumeInt32(object message, uint tag, int value);
        void ConsumeUInt32(object message, uint tag, uint value);
        FieldInfo GetFieldInfo(uint tag);
        bool IgnoreUnknown { get; }
    }

    public interface IMessageType : IReadableMessageType { }

    public enum ValueType
    {
        Unknown = 0,
        Double = 1,
        Float = 2,
        Int32 = 3,
        Int64 = 4,
        UInt32 = 5,
        UInt64 = 6,
        SInt32 = 7,
        SInt64 = 8,
        Fixed32 = 9,
        Fixed64 = 10,
        SFixed32 = 11,
        SFixed64 = 12,
        Bool = 13,
        String = 14,
        Bytes = 15,
        Enum = 64,
        Message = 128
    }

    public readonly struct FieldInfo
    {
        public ValueType ValueType { get; }
        public IReadableMessageType MessageType { get; }

        public FieldInfo(IReadableMessageType messageType)
        {
            ValueType = ValueType.Message;
            MessageType = messageType;
        }

        public FieldInfo(ValueType valueType)
        {
            ValueType = valueType;
            MessageType = null;
        }
    }
}
