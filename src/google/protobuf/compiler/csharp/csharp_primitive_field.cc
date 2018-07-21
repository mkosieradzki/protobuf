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
#include <google/protobuf/compiler/csharp/csharp_options.h>
#include <google/protobuf/compiler/csharp/csharp_primitive_field.h>

namespace google {
namespace protobuf {
namespace compiler {
namespace csharp {

PrimitiveFieldGenerator::PrimitiveFieldGenerator(
    const FieldDescriptor* descriptor, int fieldOrdinal, const Options *options)
    : FieldGeneratorBase(descriptor, fieldOrdinal, options) {
  // TODO(jonskeet): Make this cleaner...
  is_value_type = descriptor->type() != FieldDescriptor::TYPE_STRING
      && descriptor->type() != FieldDescriptor::TYPE_BYTES;
  if (!is_value_type) {
    variables_["has_property_check_sufix"] = ".Length != 0";
    variables_["has_property_check"] = variables_["property_name"] + ".Length != 0";
    variables_["other_has_property_check"] = "other." + variables_["property_name"] + ".Length != 0";
  }
}

PrimitiveFieldGenerator::~PrimitiveFieldGenerator() {
}

void PrimitiveFieldGenerator::GenerateMembers(io::Printer* printer) {
  // TODO(jonskeet): Work out whether we want to prevent the fields from ever being
  // null, or whether we just handle it, in the cases of bytes and string.
  // (Basically, should null-handling code be in the getter or the setter?)
  // mkosieradzki: Current solution is OK:
  // - oneof with handling in getter
  // - standard in setter
  // This determines how recursive code-gen works (we can use safely getters
  // anytime we are not in oneof field - and oneofs are not used in recursive fields)
  printer->Print(
    variables_,
    "private $type_name$ $name_def_message$;\n");
  WritePropertyDocComment(printer, descriptor_);
  AddPublicMemberAttributes(printer);
  printer->Print(
    variables_,
    "$access_level$ $type_name$ $property_name$ {\n"
    "  get { return $name$_; }\n"
    "  set {\n");
  if (is_value_type) {
    printer->Print(
      variables_,
      "    $name$_ = value;\n");
  } else {
    printer->Print(
      variables_,
      "    $name$_ = pb::ProtoPreconditions.CheckNotNull(value, \"value\");\n");
  }
  printer->Print(
    "  }\n"
    "}\n");
}

void PrimitiveFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(
    variables_,
    "if ($other_has_property_check$) {\n"
    "  $property_name$ = other.$property_name$;\n"
    "}\n");
}

void PrimitiveFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  // Note: invoke the property setter rather than writing straight to the field,
  // so that we can normalize "null to empty" for strings and bytes.
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["property_name"] : lvalueName;
  printer->Print(
    variables_,
    "$lvalue_name$ = input.Read$capitalized_type_name$(ref immediateBuffer);\n");
}

void PrimitiveFieldGenerator::GenerateSerializationCode(io::Printer* printer, const std::string& rvalueName) {
  variables_["rvalue_name"] = rvalueName;

  if (descriptor_->containing_oneof()) {
    printer->Print(
      variables_,
      "if ($has_property_check$) {\n");
  }
  else {
    printer->Print(
      variables_,
      "if ($rvalue_name$$has_property_check_sufix$) {\n");
  }

  printer->Print(
    variables_,
    "  output.WriteRawTag($tag_bytes$, ref immediateBuffer);\n"
    "  output.Write$capitalized_type_name$($rvalue_name$, ref immediateBuffer);\n"
    "}\n");
}

void PrimitiveFieldGenerator::GenerateSerializedSizeCode(io::Printer* printer, const std::string& lvalueName, const std::string& rvalueName) {
  variables_["lvalue_name"] = lvalueName;
  variables_["rvalue_name"] = rvalueName;
  //Oneof variant igonores rvalue
  if (descriptor_->containing_oneof()) {
    printer->Print(
      variables_,
      "if ($has_property_check$) {\n");
  }
  else {
    printer->Print(
      variables_,
      "if ($rvalue_name$$has_property_check_sufix$) {\n");
  }
  printer->Indent();
  int fixedSize = GetFixedSize(descriptor_->type());
  if (fixedSize == -1) {
    printer->Print(
      variables_,
      "$lvalue_name$ += $tag_size$ + pb::CodedOutputStream.Compute$capitalized_type_name$Size($rvalue_name$);\n");
  } else {
    printer->Print(
      "$lvalue_name$ += $tag_size$ + $fixed_size$;\n",
      "lvalue_name", lvalueName,
      "fixed_size", SimpleItoa(fixedSize),
      "tag_size", variables_["tag_size"]);
  }
  printer->Outdent();
  printer->Print("}\n");
}

void PrimitiveFieldGenerator::WriteHash(io::Printer* printer) {
  const char *text = "if ($has_property_check$) hash ^= $property_name$.GetHashCode();\n";
  if (descriptor_->type() == FieldDescriptor::TYPE_FLOAT) {
    text = "if ($has_property_check$) hash ^= pbc::ProtobufEqualityComparers.BitwiseSingleEqualityComparer.GetHashCode($property_name$);\n";
  } else if (descriptor_->type() == FieldDescriptor::TYPE_DOUBLE) {
    text = "if ($has_property_check$) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode($property_name$);\n";
  }
	printer->Print(variables_, text);
}
void PrimitiveFieldGenerator::WriteEquals(io::Printer* printer) {
  const char *text = "if ($property_name$ != other.$property_name$) return false;\n";
  if (descriptor_->type() == FieldDescriptor::TYPE_FLOAT) {
    text = "if (!pbc::ProtobufEqualityComparers.BitwiseSingleEqualityComparer.Equals($property_name$, other.$property_name$)) return false;\n";
  } else if (descriptor_->type() == FieldDescriptor::TYPE_DOUBLE) {
    text = "if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals($property_name$, other.$property_name$)) return false;\n";
  }
  printer->Print(variables_, text);
}
void PrimitiveFieldGenerator::WriteToString(io::Printer* printer) {
  printer->Print(
    variables_,
    "PrintField(\"$descriptor_name$\", $has_property_check$, $property_name$, writer);\n");
}

void PrimitiveFieldGenerator::GenerateCloningCode(io::Printer* printer) {
  printer->Print(variables_,
    "$name$_ = other.$name$_;\n");
}

PrimitiveOneofFieldGenerator::PrimitiveOneofFieldGenerator(
    const FieldDescriptor* descriptor, int fieldOrdinal, const Options *options)
    : PrimitiveFieldGenerator(descriptor, fieldOrdinal, options) {
  SetCommonOneofFieldVariables(&variables_);
}

PrimitiveOneofFieldGenerator::~PrimitiveOneofFieldGenerator() {
}

void PrimitiveOneofFieldGenerator::GenerateMembers(io::Printer* printer) {
  WritePropertyDocComment(printer, descriptor_);
  AddPublicMemberAttributes(printer);
  printer->Print(
    variables_,
    "$access_level$ $type_name$ $property_name$ {\n"
    "  get { return $has_property_check$ ? ($type_name$) $oneof_name$_ : $default_value$; }\n"
    "  set {\n");
    if (is_value_type) {
      printer->Print(
        variables_,
        "    $oneof_name$_ = value;\n");
    } else {
      printer->Print(
        variables_,
        "    $oneof_name$_ = pb::ProtoPreconditions.CheckNotNull(value, \"value\");\n");
    }
    printer->Print(
      variables_,
      "    $oneof_name$Case_ = $oneof_property_name$OneofCase.$property_name$;\n"
      "  }\n"
      "}\n");
}

void PrimitiveOneofFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(variables_, "$property_name$ = other.$property_name$;\n");
}

void PrimitiveOneofFieldGenerator::WriteToString(io::Printer* printer) {
  printer->Print(variables_,
    "PrintField(\"$descriptor_name$\", $has_property_check$, $oneof_name$_, writer);\n");
}

void PrimitiveOneofFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["property_name"] : lvalueName;
  printer->Print(
   variables_,
  "$lvalue_name$ = input.Read$capitalized_type_name$(ref immediateBuffer);\n");
}

void PrimitiveOneofFieldGenerator::GenerateCloningCode(io::Printer* printer) {
  printer->Print(variables_,
    "$property_name$ = other.$property_name$;\n");
}

}  // namespace csharp
}  // namespace compiler
}  // namespace protobuf
}  // namespace google
