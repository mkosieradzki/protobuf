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

#include <google/protobuf/compiler/csharp/csharp_doc_comment.h>
#include <google/protobuf/compiler/csharp/csharp_helpers.h>
#include <google/protobuf/compiler/csharp/csharp_options.h>
#include <google/protobuf/compiler/csharp/csharp_wrapper_field.h>

namespace google {
namespace protobuf {
namespace compiler {
namespace csharp {

WrapperFieldGenerator::WrapperFieldGenerator(const FieldDescriptor* descriptor,
                                       int fieldOrdinal, const Options *options)
    : FieldGeneratorBase(descriptor, fieldOrdinal, options) {
  variables_["has_property_check"] = name() + "_ != null";
  variables_["has_property_check_sufix"] = " != null";
  variables_["has_not_property_check"] = name() + "_ == null";
  const FieldDescriptor* wrapped_field = descriptor->message_type()->field(0);
  is_value_type = wrapped_field->type() != FieldDescriptor::TYPE_STRING &&
      wrapped_field->type() != FieldDescriptor::TYPE_BYTES;
  if (is_value_type) {
    variables_["nonnullable_type_name"] = type_name(wrapped_field);
  }
  variables_["wrapped_type_capitalized_name"] = capitalized_type_name(wrapped_field);
}

WrapperFieldGenerator::~WrapperFieldGenerator() {
}

void WrapperFieldGenerator::GenerateMembers(io::Printer* printer) {
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

void WrapperFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(
    variables_,
    "if (other.$has_property_check$) {\n"
    "  if ($has_not_property_check$ || other.$property_name$ != $default_value$) {\n"
    "    $property_name$ = other.$property_name$;\n"
    "  }\n"
    "}\n");
}

void WrapperFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["property_name"] : lvalueName;
  printer->Print(
    variables_,
    "$type_name$ value = input.ReadWrapped$wrapped_type_capitalized_name$(ref immediateBuffer);\n"
    "if ($lvalue_name$ == null || value != $default_value$) {\n"
    "  $lvalue_name$ = value;\n"
    "}\n");
}

void WrapperFieldGenerator::GenerateSerializationCode(io::Printer* printer, const std::string& rvalueName) {
  variables_["rvalue_name"] = rvalueName;
  printer->Print(
    variables_,
    "if ($rvalue_name$$has_property_check_sufix$) {\n"
    "  output.WriteRawTag($tag_bytes$, ref immediateBuffer);\n"
    "  output.WriteWrapped$wrapped_type_capitalized_name$($rvalue_name$, ref immediateBuffer);\n"
    "}\n");
}

void WrapperFieldGenerator::GenerateSerializedSizeCode(io::Printer* printer, const std::string& lvalueName, const std::string& rvalueName) {
  variables_["lvalue_name"] = lvalueName;
  variables_["rvalue_name"] = rvalueName;
  printer->Print(
    variables_,
    "if ($rvalue_name$$has_property_check_sufix$) {\n"
    "  $lvalue_name$ += $tag_size$ + pb::CodedOutputStream.ComputeWrapped$wrapped_type_capitalized_name$Size($rvalue_name$);\n"
    "}\n");
}

void WrapperFieldGenerator::WriteHash(io::Printer* printer) {
  const char *text = "if ($has_property_check$) hash ^= $property_name$.GetHashCode();\n";
  if (descriptor_->message_type()->field(0)->type() == FieldDescriptor::TYPE_FLOAT) {
    text = "if ($has_property_check$) hash ^= pbc::ProtobufEqualityComparers.BitwiseNullableSingleEqualityComparer.GetHashCode($property_name$);\n";
  }
  else if (descriptor_->message_type()->field(0)->type() == FieldDescriptor::TYPE_DOUBLE) {
    text = "if ($has_property_check$) hash ^= pbc::ProtobufEqualityComparers.BitwiseNullableDoubleEqualityComparer.GetHashCode($property_name$);\n";
  }
  printer->Print(variables_, text);
}

void WrapperFieldGenerator::WriteEquals(io::Printer* printer) {
  const char *text = "if ($property_name$ != other.$property_name$) return false;\n";
  if (descriptor_->message_type()->field(0)->type() == FieldDescriptor::TYPE_FLOAT) {
    text = "if (!pbc::ProtobufEqualityComparers.BitwiseNullableSingleEqualityComparer.Equals($property_name$, other.$property_name$)) return false;\n";
  }
  else if (descriptor_->message_type()->field(0)->type() == FieldDescriptor::TYPE_DOUBLE) {
    text = "if (!pbc::ProtobufEqualityComparers.BitwiseNullableDoubleEqualityComparer.Equals($property_name$, other.$property_name$)) return false;\n";
  }
  printer->Print(variables_, text);
}

void WrapperFieldGenerator::WriteToString(io::Printer* printer) {
  // TODO: Implement if we ever actually need it...
}

void WrapperFieldGenerator::GenerateCloningCode(io::Printer* printer) {
  printer->Print(variables_,
    "$property_name$ = other.$property_name$;\n");
}

WrapperOneofFieldGenerator::WrapperOneofFieldGenerator(
    const FieldDescriptor* descriptor, int fieldOrdinal, const Options *options)
    : WrapperFieldGenerator(descriptor, fieldOrdinal, options) {
  SetCommonOneofFieldVariables(&variables_);
  const FieldDescriptor* wrapped_field = descriptor->message_type()->field(0);
  variables_["wrapped_type_capitalized_name"] = capitalized_type_name(wrapped_field);
}

WrapperOneofFieldGenerator::~WrapperOneofFieldGenerator() {
}

void WrapperOneofFieldGenerator::GenerateMembers(io::Printer* printer) {
  WritePropertyDocComment(printer, descriptor_);
  AddPublicMemberAttributes(printer);
  printer->Print(
    variables_,
    "$access_level$ $type_name$ $property_name$ {\n"
    "  get { return $has_property_check$ ? ($type_name$) $oneof_name$_ : ($type_name$) null; }\n"
    "  set {\n"
    "    $oneof_name$_ = value;\n"
    "    $oneof_name$Case_ = value == null ? $oneof_property_name$OneofCase.None : $oneof_property_name$OneofCase.$property_name$;\n"
    "  }\n"
    "}\n");
}

void WrapperOneofFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(variables_, "$property_name$ = other.$property_name$;\n");
}

void WrapperOneofFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["property_name"] : lvalueName;
  printer->Print(
    variables_,
    "$lvalue_name$ = input.ReadWrapped$wrapped_type_capitalized_name$(ref immediateBuffer);\n");
}

void WrapperOneofFieldGenerator::GenerateSerializationCode(io::Printer* printer, const std::string& rvalueName) {
  // TODO: I suspect this is wrong...
  printer->Print(
    variables_,
    "if ($has_property_check$) {\n"
    "  output.WriteRawTag($tag_bytes$, ref immediateBuffer);\n"
    "  output.WriteWrapped$wrapped_type_capitalized_name$($property_name$, ref immediateBuffer);\n"
    "}\n");
}

void WrapperOneofFieldGenerator::GenerateSerializedSizeCode(io::Printer* printer, const std::string& lvalueName, const std::string& rvalueName) {
  // TODO: I suspect this is wrong...
  variables_["lvalue_name"] = lvalueName;
  // NOTE: This one works only for property_name (ignoring rvalue)
  printer->Print(
    variables_,
    "if ($has_property_check$) {\n"
    "  $lvalue_name$ += $tag_size$ + pb::CodedOutputStream.ComputeWrapped$wrapped_type_capitalized_name$Size($property_name$);\n"
    "}\n");
}

}  // namespace csharp
}  // namespace compiler
}  // namespace protobuf
}  // namespace google
