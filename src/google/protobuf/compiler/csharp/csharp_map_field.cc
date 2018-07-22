// Protocol Buffers - Google's data interchange format
// Copyright 2015 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#include <sstream>

#include <google/protobuf/compiler/code_generator.h>
#include <google/protobuf/compiler/plugin.h>
#include <google/protobuf/descriptor.h>
#include <google/protobuf/descriptor.pb.h>
#include <google/protobuf/io/printer.h>
#include <google/protobuf/io/zero_copy_stream.h>
#include <google/protobuf/stubs/strutil.h>
#include <google/protobuf/wire_format.h>

#include <google/protobuf/compiler/csharp/csharp_doc_comment.h>
#include <google/protobuf/compiler/csharp/csharp_helpers.h>
#include <google/protobuf/compiler/csharp/csharp_map_field.h>

namespace google {
namespace protobuf {
namespace compiler {
namespace csharp {

MapFieldGenerator::MapFieldGenerator(const FieldDescriptor* descriptor,
                                     int fieldOrdinal,
                                     const Options* options)
    : FieldGeneratorBase(descriptor, fieldOrdinal, options) {
  const FieldDescriptor* key_descriptor =
    descriptor_->message_type()->FindFieldByName("key");
  const FieldDescriptor* value_descriptor =
    descriptor_->message_type()->FindFieldByName("value");
  variables_["key_default_value"] = default_value(key_descriptor);
  variables_["value_default_value"] = default_value(value_descriptor);
  variables_["key_type_name"] = type_name(key_descriptor);
  variables_["value_type_name"] = type_name(value_descriptor);
  variables_["key_tag"] = SimpleItoa(internal::WireFormat::MakeTag(key_descriptor));
  variables_["value_tag"] = SimpleItoa(internal::WireFormat::MakeTag(value_descriptor));
  variables_["key_type_capitalized_name"] = capitalized_type_name(key_descriptor);
  variables_["value_type_capitalized_name"] = capitalized_type_name(value_descriptor);
}

MapFieldGenerator::~MapFieldGenerator() {
}

void MapFieldGenerator::GenerateMembers(io::Printer* printer) {   
  printer->Print(
    variables_,
    "private readonly pbc::MapField<$key_type_name$, $value_type_name$> $name$_ = new pbc::MapField<$key_type_name$, $value_type_name$>();\n");
  WritePropertyDocComment(printer, descriptor_);
  AddPublicMemberAttributes(printer);
  printer->Print(
    variables_,
    "$access_level$ pbc::MapField<$key_type_name$, $value_type_name$> $property_name$ {\n"
    "  get { return $name$_; }\n"
    "}\n");
}

void MapFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(
      variables_,
      "$name$_.Add(other.$name$_);\n");
}

void MapFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  const FieldDescriptor* key_descriptor =
    descriptor_->message_type()->FindFieldByName("key");
  const FieldDescriptor* value_descriptor =
    descriptor_->message_type()->FindFieldByName("value");
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["name"] + "_" : lvalueName;
  std::unique_ptr<FieldGeneratorBase> key_generator(
    CreateFieldGenerator(key_descriptor, 1, this->options()));
  std::unique_ptr<FieldGeneratorBase> value_generator(
    CreateFieldGenerator(value_descriptor, 2, this->options()));

  printer->Print(
    variables_,
    "var mapOldLimit = input.BeginReadNested(ref immediateBuffer);\n"
    "$key_type_name$ entryKey = $key_default_value$;\n"
    "$value_type_name$ entryValue = $value_default_value$;\n"
    "uint ntag;\n"
    "while ((ntag = input.ReadTag(ref immediateBuffer)) != 0) {\n");
  printer->Indent();
  printer->Print(
    variables_,
    "if (ntag == $key_tag$) {\n");
  printer->Indent();
  key_generator->GenerateParsingCode(printer, "entryKey", false);
  printer->Outdent();
  printer->Print(
    variables_,
    "} else if (ntag == $value_tag$) {\n");
  printer->Indent();
  value_generator->GenerateParsingCode(printer, "entryValue", false);
  printer->Outdent();
  printer->Print(
    "} else {\n"
    "  input.SkipLastField(ref immediateBuffer);\n"
    "}\n");
  printer->Outdent();
  printer->Print(
    "}\n");
  if (value_descriptor->type() == FieldDescriptor::Type::TYPE_MESSAGE && default_value(value_descriptor) == "null") {
    printer->Print(
      variables_,
      "if (entryValue == null) {\n"
      "  entryValue = new $value_type_name$();\n"
      "}\n");
  }
  printer->Print(
    variables_,
    "$lvalue_name$[entryKey] = entryValue;\n"
    "input.EndReadNested(mapOldLimit);\n");
}

void MapFieldGenerator::GenerateSerializationCode(io::Printer* printer, const std::string& rvalueName) {
  const FieldDescriptor* key_descriptor =
    descriptor_->message_type()->FindFieldByName("key");
  const FieldDescriptor* value_descriptor =
    descriptor_->message_type()->FindFieldByName("value");
  variables_["rvalue_name"] = rvalueName;
  std::unique_ptr<FieldGeneratorBase> key_generator(
    CreateFieldGenerator(key_descriptor, 1, this->options()));
  std::unique_ptr<FieldGeneratorBase> value_generator(
    CreateFieldGenerator(value_descriptor, 2, this->options()));

  printer->Print(
    variables_,
    "foreach (var entry in $rvalue_name$) {\n");
  printer->Indent();
  printer->Print("var messageSize = 0;\n");
  key_generator->GenerateSerializedSizeCode(printer, "messageSize", "entry.Key");
  value_generator->GenerateSerializedSizeCode(printer, "messageSize", "entry.Value");
  printer->Print(
    variables_,
    "output.WriteRawTag($tag_bytes$, ref immediateBuffer);\n"
    "output.WriteLength(messageSize, ref immediateBuffer);\n");
  key_generator->GenerateSerializationCode(printer, "entry.Key");
  value_generator->GenerateSerializationCode(printer, "entry.Value");
  printer->Outdent();
  printer->Print("}\n");
}

void MapFieldGenerator::GenerateSerializedSizeCode(io::Printer* printer, const std::string& lvalueName, const std::string& rvalueName) {
  const FieldDescriptor* key_descriptor =
    descriptor_->message_type()->FindFieldByName("key");
  const FieldDescriptor* value_descriptor =
    descriptor_->message_type()->FindFieldByName("value");
  variables_["lvalue_name"] = lvalueName;
  variables_["rvalue_name"] = rvalueName;
  std::unique_ptr<FieldGeneratorBase> key_generator(
    CreateFieldGenerator(key_descriptor, 1, this->options()));
  std::unique_ptr<FieldGeneratorBase> value_generator(
    CreateFieldGenerator(value_descriptor, 2, this->options()));

  printer->Print(
    variables_,
    "foreach (var entry in $rvalue_name$) {\n");
  printer->Indent();
  printer->Print("var messageSize = 0;\n");
  key_generator->GenerateSerializedSizeCode(printer, "messageSize", "entry.Key");
  value_generator->GenerateSerializedSizeCode(printer, "messageSize", "entry.Value");
  printer->Print(
    variables_,
    "$lvalue_name$ += $tag_size$ + pb::CodedOutputStream.ComputeLengthSize(messageSize) + messageSize;\n");
  printer->Outdent();
  printer->Print("}\n");
}

void MapFieldGenerator::WriteHash(io::Printer* printer) {
  printer->Print(
    variables_,
    "hash ^= $property_name$.GetHashCode();\n");
}
void MapFieldGenerator::WriteEquals(io::Printer* printer) {
  printer->Print(
    variables_,
    "if (!$property_name$.Equals(other.$property_name$)) return false;\n");
}

void MapFieldGenerator::WriteToString(io::Printer* printer) {
    // TODO: If we ever actually use ToString, we'll need to impleme this...
}

void MapFieldGenerator::GenerateCloningCode(io::Printer* printer) {
  printer->Print(variables_,
    "$name$_ = other.$name$_.Clone();\n");
}

void MapFieldGenerator::GenerateFreezingCode(io::Printer* printer) {
}

}  // namespace csharp
}  // namespace compiler
}  // namespace protobuf
}  // namespace google
