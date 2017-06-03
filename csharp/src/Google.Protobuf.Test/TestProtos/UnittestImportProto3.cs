// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: google/protobuf/unittest_import_proto3.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Google.Protobuf.TestProtos {

  /// <summary>Holder for reflection information generated from google/protobuf/unittest_import_proto3.proto</summary>
  public static partial class UnittestImportProto3Reflection {

    #region Descriptor
    /// <summary>File descriptor for google/protobuf/unittest_import_proto3.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static UnittestImportProto3Reflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cixnb29nbGUvcHJvdG9idWYvdW5pdHRlc3RfaW1wb3J0X3Byb3RvMy5wcm90",
            "bxIYcHJvdG9idWZfdW5pdHRlc3RfaW1wb3J0GjNnb29nbGUvcHJvdG9idWYv",
            "dW5pdHRlc3RfaW1wb3J0X3B1YmxpY19wcm90bzMucHJvdG8iGgoNSW1wb3J0",
            "TWVzc2FnZRIJCgFkGAEgASgFKlkKCkltcG9ydEVudW0SGwoXSU1QT1JUX0VO",
            "VU1fVU5TUEVDSUZJRUQQABIOCgpJTVBPUlRfRk9PEAcSDgoKSU1QT1JUX0JB",
            "UhAIEg4KCklNUE9SVF9CQVoQCUI8Chhjb20uZ29vZ2xlLnByb3RvYnVmLnRl",
            "c3RIAfgBAaoCGkdvb2dsZS5Qcm90b2J1Zi5UZXN0UHJvdG9zUABiBnByb3Rv",
            "Mw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Google.Protobuf.TestProtos.UnittestImportPublicProto3Reflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Google.Protobuf.TestProtos.ImportEnum), }, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Google.Protobuf.TestProtos.ImportMessage), global::Google.Protobuf.TestProtos.ImportMessage.Parser, new[]{ "D" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Enums
  public enum ImportEnum {
    [pbr::OriginalName("IMPORT_ENUM_UNSPECIFIED")] Unspecified = 0,
    [pbr::OriginalName("IMPORT_FOO")] ImportFoo = 7,
    [pbr::OriginalName("IMPORT_BAR")] ImportBar = 8,
    [pbr::OriginalName("IMPORT_BAZ")] ImportBaz = 9,
  }

  #endregion

  #region Messages
  #if !NET35
  public sealed partial class ImportMessage : pb::IAsyncMessage<ImportMessage> {
  #else
  public sealed partial class ImportMessage : pb::IMessage<ImportMessage> {
  #endif
    private static readonly pb::MessageParser<ImportMessage> _parser = new pb::MessageParser<ImportMessage>(() => new ImportMessage());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ImportMessage> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Google.Protobuf.TestProtos.UnittestImportProto3Reflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ImportMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ImportMessage(ImportMessage other) : this() {
      d_ = other.d_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ImportMessage Clone() {
      return new ImportMessage(this);
    }

    /// <summary>Field number for the "d" field.</summary>
    public const int DFieldNumber = 1;
    private int d_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int D {
      get { return d_; }
      set {
        d_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ImportMessage);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ImportMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (D != other.D) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (D != 0) hash ^= D.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (D != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(D);
      }
    }

    #if !NET35
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public async Task WriteToAsync(pb::CodedOutputStream output, CancellationToken cancellationToken) {
      if (D != 0) {
        await output.WriteRawTagAsync(8, cancellationToken).ConfigureAwait(false);
        await output.WriteInt32Async(D, cancellationToken).ConfigureAwait(false);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (D != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(D);
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ImportMessage other) {
      if (other == null) {
        return;
      }
      if (other.D != 0) {
        D = other.D;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 8: {
            D = input.ReadInt32();
            break;
          }
        }
      }
    }

    #if !NET35
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public async Task MergeFromAsync(pb::CodedInputStream input, CancellationToken cancellationToken) {
      uint tag;
      while ((tag = await input.ReadTagAsync(cancellationToken).ConfigureAwait(false)) != 0) {
        switch(tag) {
          default:
            await input.SkipLastFieldAsync(cancellationToken).ConfigureAwait(false);
            break;
          case 8: {
            D = await input.ReadInt32Async(cancellationToken).ConfigureAwait(false);
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code
