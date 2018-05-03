using System;

namespace Google.Protobuf
{
    public interface IRefMessageType<T>
    {
        ref T CreateMessage();
        RefFieldInfo GetFieldInfo(in uint tag);
        void ConsumeField(ref T message, in uint tag, in object value);
        void CompleteMessage(ref T message);
    }

    public interface IReadableMessageType
    {
        object CreateMessage();
        FieldInfo GetFieldInfo(uint tag);
        void ConsumeField(object message, uint tag, object value);
        object CompleteMessage(object message);
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

    public interface IMessageParser
    {
        object ReadMessage(ref ReadOnlySpan<byte> buffer, int maxRecursionLevels);
    }

    public readonly struct RefFieldInfo
    {
        public ValueType ValueType { get; }
        public IMessageParser MessageParser { get; }

        public RefFieldInfo(IMessageParser messageParser)
        {
            ValueType = ValueType.Message;
            MessageParser = messageParser;
        }

        public RefFieldInfo(ValueType valueType)
        {
            ValueType = valueType;
            MessageParser = null;
        }
    }
}
