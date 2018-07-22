// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
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

#include <google/protobuf/compiler/csharp/csharp_doc_comment.h>
#include <google/protobuf/compiler/csharp/csharp_helpers.h>
#include <google/protobuf/compiler/csharp/csharp_message_field.h>
#include <google/protobuf/compiler/csharp/csharp_options.h>

namespace google {
namespace protobuf {
namespace compiler {
namespace csharp {

MessageFieldGenerator::MessageFieldGenerator(const FieldDescriptor* descriptor,
                                             int fieldOrdinal,
                                             const Options *options)
    : FieldGeneratorBase(descriptor, fieldOrdinal, options) {
  variables_["has_property_check"] = name() + "_ != null";
  variables_["has_property_check_sufix"] = " != null";
  variables_["has_not_property_check"] = name() + "_ == null";
}

MessageFieldGenerator::~MessageFieldGenerator() {

}

void MessageFieldGenerator::GenerateMembers(io::Printer* printer) {
  printer->Print(
    variables_,
    "private $type_name$ $name$_;\n");
  WritePropertyDocComment(printer, descriptor_);
  AddPublicMemberAttributes(printer);
  printer->Print(
    variables_,
    "$access_level$ $type_name$ $property_name$ {\n"
    "  get { return $name$_; }\n"
    "  set {\n"
    "    $name$_ = value;\n"
    "  }\n"
    "}\n");
}

void MessageFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(
    variables_,
    "if (other.$has_property_check$) {\n"
    "  if ($has_not_property_check$) {\n"
    "    $name$_ = new $type_name$();\n"
    "  }\n"
    "  $property_name$.MergeFrom(other.$property_name$);\n"
    "}\n");
}

void MessageFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["name"] + "_" : lvalueName;
  printer->Print(
    variables_,
    "if ($lvalue_name$ == null) {\n"
    "  $lvalue_name$ = new $type_name$();\n"
    "}\n"
    "var oldLimit = input.BeginReadNested(ref immediateBuffer);\n"
    "$lvalue_name$.MergeFrom(input, ref immediateBuffer);\n"
    "input.EndReadNested(oldLimit);\n"); // No need to support TYPE_GROUP...
}

void MessageFieldGenerator::GenerateSerializationCode(io::Printer* printer, const std::string& rvalueName) {
  variables_["rvalue_name"] = rvalueName;
  printer->Print(
    variables_,
    "if ($rvalue_name$$has_property_check_sufix$) {\n"
    "  output.WriteRawTag($tag_bytes$, ref immediateBuffer);\n"
    "  output.WriteLength($rvalue_name$.CalculateSize(), ref immediateBuffer);\n"
    "  $rvalue_name$.WriteTo(output, ref immediateBuffer);\n"
    "}\n");
}

void MessageFieldGenerator::GenerateSerializedSizeCode(io::Printer* printer, const std::string& lvalueName, const std::string& rvalueName) {
  variables_["lvalue_name"] = lvalueName;
  variables_["rvalue_name"] = rvalueName;
  printer->Print(
    variables_,
    "if ($rvalue_name$$has_property_check_sufix$) {\n"
    "  $lvalue_name$ += $tag_size$ + pb::CodedOutputStream.ComputeMessageSize($rvalue_name$);\n"
    "}\n");
}

void MessageFieldGenerator::WriteHash(io::Printer* printer) {
  printer->Print(
    variables_,
    "if ($has_property_check$) hash ^= $property_name$.GetHashCode();\n");
}
void MessageFieldGenerator::WriteEquals(io::Printer* printer) {
  printer->Print(
    variables_,
    "if (!object.Equals($property_name$, other.$property_name$)) return false;\n");
}
void MessageFieldGenerator::WriteToString(io::Printer* printer) {
  variables_["field_name"] = GetFieldName(descriptor_);
  printer->Print(
    variables_,
    "PrintField(\"$field_name$\", has$property_name$, $name$_, writer);\n");
}

void MessageFieldGenerator::GenerateCloningCode(io::Printer* printer) {
  printer->Print(variables_,
    "$name$_ = other.$has_property_check$ ? other.$name$_.Clone() : null;\n");
}

void MessageFieldGenerator::GenerateFreezingCode(io::Printer* printer) {
}

MessageOneofFieldGenerator::MessageOneofFieldGenerator(
    const FieldDescriptor* descriptor,
	  int fieldOrdinal,
    const Options *options)
    : MessageFieldGenerator(descriptor, fieldOrdinal, options) {
  SetCommonOneofFieldVariables(&variables_);
}

MessageOneofFieldGenerator::~MessageOneofFieldGenerator() {

}

void MessageOneofFieldGenerator::GenerateMembers(io::Printer* printer) {
  WritePropertyDocComment(printer, descriptor_);
  AddPublicMemberAttributes(printer);
  printer->Print(
    variables_,
    "$access_level$ $type_name$ $property_name$ {\n"
    "  get { return $has_property_check$ ? ($type_name$) $oneof_name$_ : null; }\n"
    "  set {\n"
    "    $oneof_name$_ = value;\n"
    "    $oneof_name$Case_ = value == null ? $oneof_property_name$OneofCase.None : $oneof_property_name$OneofCase.$property_name$;\n"
    "  }\n"
    "}\n");
}

void MessageOneofFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(variables_, 
    "if ($property_name$ == null) {\n"
    "  $property_name$ = new $type_name$();\n"
    "}\n"
    "$property_name$.MergeFrom(other.$property_name$);\n");
}

void MessageOneofFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["property_name"] : lvalueName;
  // TODO(jonskeet): We may be able to do better than this
  printer->Print(
    variables_,
    "$type_name$ subBuilder = new $type_name$();\n"
    "if ($has_property_check$) {\n"
    "  subBuilder.MergeFrom($property_name$);\n"
    "}\n"
    "var oldLimit = input.BeginReadNested(ref immediateBuffer);\n"
    "subBuilder.MergeFrom(input, ref immediateBuffer);\n" // No support of TYPE_GROUP
    "input.EndReadNested(oldLimit);\n"
    "$lvalue_name$ = subBuilder;\n");
}

void MessageOneofFieldGenerator::WriteToString(io::Printer* printer) {
  printer->Print(
    variables_,
    "PrintField(\"$descriptor_name$\", $has_property_check$, $oneof_name$_, writer);\n");
}

void MessageOneofFieldGenerator::GenerateCloningCode(io::Printer* printer) {
  printer->Print(variables_,
    "$property_name$ = other.$property_name$.Clone();\n");
}

}  // namespace csharp
}  // namespace compiler
}  // namespace protobuf
}  // namespace google
