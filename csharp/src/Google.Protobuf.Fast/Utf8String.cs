namespace Google.Protobuf.Fast
{
    public struct Utf8String
    {
        public int Length => 0;

        //TODO: Remove
        public static implicit operator string(Utf8String str) => string.Empty;
        public static implicit operator Utf8String(string str) => new Utf8String();
    }
}
