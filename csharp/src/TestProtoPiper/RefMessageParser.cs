using System;

namespace Google.Protobuf
{
    sealed class RefMessageParser<T> : IMessageParser
    {
        private IRefMessageType<T> messageType;

        public RefMessageParser(IRefMessageType<T> messageType)
        {
            this.messageType = messageType;
        }

        public object ReadMessage(ref ReadOnlySpan<byte> buffer, int maxRecursionLevels = 32)
        {
            var message = messageType.CreateMessage();

            //TODO: Add support for packed encoding
            uint tag, prevTag = 0;
            RefFieldInfo fieldInfo = default;
            WireFormat.WireType wireType = default;
            while ((tag = CodedInputSpanParser.ReadTag(ref buffer)) != 0)
            {
                // Do not ask again for field info in case of a repeated field
                if (tag != prevTag)
                {
                    fieldInfo = messageType.GetFieldInfo(tag);
                    wireType = WireFormat.GetTagWireType(tag);
                    prevTag = tag;
                }

                switch (wireType)
                {
                    case WireFormat.WireType.Varint:
                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                CodedInputSpanParser.ReadRawVarint32(ref buffer);
                                break;
                            case ValueType.Int32:
                                messageType.ConsumeField(ref message, in tag, (int)CodedInputSpanParser.ReadRawVarint32(ref buffer));
                                break;
                            case ValueType.Int64:
                                messageType.ConsumeField(ref message, in tag, (long)CodedInputSpanParser.ReadRawVarint64(ref buffer));
                                break;
                            case ValueType.UInt32:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.ReadRawVarint32(ref buffer));
                                break;
                            case ValueType.UInt64:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.ReadRawVarint64(ref buffer));
                                break;
                            case ValueType.SInt32:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.DecodeZigZag32(CodedInputSpanParser.ReadRawVarint32(ref buffer)));
                                break;
                            case ValueType.SInt64:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.DecodeZigZag64(CodedInputSpanParser.ReadRawVarint64(ref buffer)));
                                break;
                            case ValueType.Bool:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.ReadRawVarint32(ref buffer) != 0);
                                break;
                            case ValueType.Enum:
                                messageType.ConsumeField(ref message, in tag, (int)CodedInputSpanParser.ReadRawVarint32(ref buffer));
                                break;
                            default:
                                //TODO: Consider skipping instead
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.Fixed32:
                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                CodedInputSpanParser.SkipRawBytes(ref buffer, 4);
                                break;
                            case ValueType.Float:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.Int32BitsToSingle((int)CodedInputSpanParser.ReadFixed32(ref buffer)));
                                break;
                            case ValueType.Fixed32:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.ReadFixed32(ref buffer));
                                break;
                            case ValueType.SFixed32:
                                messageType.ConsumeField(ref message, in tag, (int)CodedInputSpanParser.ReadFixed32(ref buffer));
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.Fixed64:
                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                CodedInputSpanParser.SkipRawBytes(ref buffer, 8);
                                break;
                            case ValueType.Double:
                                messageType.ConsumeField(ref message, in tag, BitConverter.Int64BitsToDouble((long)CodedInputSpanParser.ReadFixed64(ref buffer)));
                                break;
                            case ValueType.Fixed64:
                                messageType.ConsumeField(ref message, in tag, CodedInputSpanParser.ReadFixed64(ref buffer));
                                break;
                            case ValueType.SFixed64:
                                messageType.ConsumeField(ref message, in tag, (long)CodedInputSpanParser.ReadFixed64(ref buffer));
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.LengthDelimited:
                        var length = CodedInputSpanParser.ReadLength(ref buffer);
                        if (maxRecursionLevels <= 0)
                        {
                            throw new Exception();
                            //TODO: Handle recursion limit
                            //throw InvalidProtocolBufferException.RecursionLimitExceeded();
                        }
                        if (length > buffer.Length)
                        {
                            throw new Exception();
                            //TODO: Better exception
                        }

                        var nestedBuffer = buffer.Slice(0, length);
                        buffer = buffer.Slice(length);

                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                break;
                            case ValueType.Double:
                                //TODO: Support packed enoding
                                throw new NotImplementedException();
                            case ValueType.Float:
                                throw new NotImplementedException();
                            case ValueType.Int32:
                                throw new NotImplementedException();
                            case ValueType.Int64:
                                throw new NotImplementedException();
                            case ValueType.UInt32:
                                throw new NotImplementedException();
                            case ValueType.UInt64:
                                throw new NotImplementedException();
                            case ValueType.SInt32:
                                throw new NotImplementedException();
                            case ValueType.SInt64:
                                throw new NotImplementedException();
                            case ValueType.Fixed32:
                                throw new NotImplementedException();
                            case ValueType.Fixed64:
                                throw new NotImplementedException();
                            case ValueType.SFixed32:
                                throw new NotImplementedException();
                            case ValueType.SFixed64:
                                throw new NotImplementedException();
                            case ValueType.Bool:
                                throw new NotImplementedException();
                            case ValueType.Enum:
                                throw new NotImplementedException();
                            case ValueType.String:
                                //messageType.ConsumeField(message, tag, nestedBuffer);
                                break;
                            case ValueType.Bytes:
                                //messageType.ConsumeField(message, tag, nestedBuffer);
                                break;
                            case ValueType.Message:
                                messageType.ConsumeField(ref message, in tag, fieldInfo.MessageParser.ReadMessage(ref nestedBuffer, maxRecursionLevels - 1));
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.StartGroup:
                        CodedInputSpanParser.SkipGroup(ref buffer, in tag, maxRecursionLevels);
                        break;
                    case WireFormat.WireType.EndGroup:
                        //TODO: Add proper exception
                        throw new Exception();
                    default:
                        //TODO: Add proper exception
                        throw new Exception();
                }
            }

            messageType.CompleteMessage(ref message);
            return message;
        }
    }
}
