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
#include <google/protobuf/wire_format.h>

#include <google/protobuf/compiler/csharp/csharp_doc_comment.h>
#include <google/protobuf/compiler/csharp/csharp_helpers.h>
#include <google/protobuf/compiler/csharp/csharp_repeated_primitive_field.h>

namespace google {
namespace protobuf {
namespace compiler {
namespace csharp {

RepeatedPrimitiveFieldGenerator::RepeatedPrimitiveFieldGenerator(
    const FieldDescriptor* descriptor, int fieldOrdinal, const Options *options)
    : FieldGeneratorBase(descriptor, fieldOrdinal, options) {
}

RepeatedPrimitiveFieldGenerator::~RepeatedPrimitiveFieldGenerator() {

}

void RepeatedPrimitiveFieldGenerator::GenerateMembers(io::Printer* printer) {
  printer->Print(variables_,
    "private readonly pbc::RepeatedField<$type_name$> $name$_ = new pbc::RepeatedField<$type_name$>();\n");
  WritePropertyDocComment(printer, descriptor_);
  AddPublicMemberAttributes(printer);
  printer->Print(
    variables_,
    "$access_level$ pbc::RepeatedField<$type_name$> $property_name$ {\n"
    "  get { return $name$_; }\n"
    "}\n");
}

void RepeatedPrimitiveFieldGenerator::GenerateMergingCode(io::Printer* printer) {
  printer->Print(
    variables_,
    "$name$_.Add(other.$name$_);\n");
}

void RepeatedPrimitiveFieldGenerator::GenerateParsingCode(io::Printer* printer, const std::string& lvalueName, bool forceNonPacked) {
  variables_["lvalue_name"] = lvalueName.empty() ? variables_["name"] + "_" : lvalueName;
  if (descriptor_->is_packable() && !forceNonPacked) {
    printer->Print(
      variables_,
      "int length = input.ReadLength(ref immediateBuffer);\n"
      "if (length > 0) {\n"
      "  var oldLimit = input.PushLimit(length);\n"
      "  while (!input.ReachedLimit) {\n"
      "    $lvalue_name$.Add(input.Read$capitalized_type_name$(ref immediateBuffer)); \n"
      "  }\n"
      "  input.PopLimit(oldLimit);\n"
      "}\n");
  }
  else {
    printer->Print(
      variables_,
      "$lvalue_name$.Add(input.Read$capitalized_type_name$(ref immediateBuffer));\n");
  }
}

void RepeatedPrimitiveFieldGenerator::GenerateSerializationCode(io::Printer* printer, const std::string& rvalueName) {
  variables_["rvalue_name"] = rvalueName;
  if (descriptor_->is_packable()) {
    int fixedSize = GetFixedSize(descriptor_->type());
    if (fixedSize == -1) {
      printer->Print(
        variables_,
        "{\n"
        "  var packedSize = 0;\n"
        "  for (var i = 0; i < $rvalue_name$.Count; i++) {\n"
        "    packedSize += pb::CodedOutputStream.Compute$capitalized_type_name$Size($rvalue_name$[i]);\n"
        "  }\n");
    }
    else {
      printer->Print(
        "{\n"
        "  var packedSize = $fixed_size$ * $rvalue_name$.Count;\n",
        "rvalue_name", rvalueName,
        "fixed_size", SimpleItoa(fixedSize));
    }
    printer->Print(
      variables_,
      "  if (packedSize > 0) {\n"
      "    output.WriteRawTag($tag_bytes$, ref immediateBuffer);\n"
      "    output.WriteLength(packedSize, ref immediateBuffer);\n"
      "    for (var i = 0; i < $rvalue_name$.Count; i++) {\n"
      "      output.Write$capitalized_type_name$($rvalue_name$[i], ref immediateBuffer);\n"
      "    }\n"
      "  }\n"
      "}\n");
  }
  else {
    printer->Print(
      variables_,
      "for (var i = 0; i < $rvalue_name$.Count; i++) {\n"
      "  output.WriteRawTag($tag_bytes$, ref immediateBuffer);\n"
      "  output.Write$capitalized_type_name$($rvalue_name$[i], ref immediateBuffer);\n"
      "}\n");
  }
}

void RepeatedPrimitiveFieldGenerator::GenerateSerializedSizeCode(io::Printer* printer, const std::string& lvalueName, const std::string& rvalueName) {
  variables_["lvalue_name"] = lvalueName;
  variables_["rvalue_name"] = rvalueName;
  if (descriptor_->is_packed()) {
    int fixedSize = GetFixedSize(descriptor_->type());
    if (fixedSize == -1) {
      printer->Print(
        variables_,
        "{\n"
        "  var packedSize = 0;\n"
        "  for (var i = 0; i < $rvalue_name$.Count; i++) {\n"
        "    packedSize += pb::CodedOutputStream.Compute$capitalized_type_name$Size($rvalue_name$[i]);\n"
        "  }\n");
    }
    else {
      printer->Print(
        "{\n"
        "  var packedSize = $fixed_size$ * $rvalue_name$.Count;\n",
        "rvalue_name", rvalueName,
        "fixed_size", SimpleItoa(fixedSize));
    }
    printer->Print(
      variables_,
      "  if (packedSize > 0) {\n"
      "    $lvalue_name$ += $tag_size$ + packedSize + pb::CodedOutputStream.ComputeLengthSize(packedSize);\n"
      "  }\n"
      "}\n");
  }
  else {
    printer->Print(
      variables_,
      "for (var i = 0; i < $rvalue_name$.Count; i++) {\n"
      "  $lvalue_name$ += $tag_size$ + pb::CodedOutputStream.Compute$capitalized_type_name$Size($rvalue_name$[i]);\n"
      "}\n");
  }
}

void RepeatedPrimitiveFieldGenerator::WriteHash(io::Printer* printer) {
  printer->Print(
    variables_,
    "hash ^= $name$_.GetHashCode();\n");
}
void RepeatedPrimitiveFieldGenerator::WriteEquals(io::Printer* printer) {
  printer->Print(
    variables_,
    "if(!$name$_.Equals(other.$name$_)) return false;\n");
}
void RepeatedPrimitiveFieldGenerator::WriteToString(io::Printer* printer) {
  printer->Print(variables_,
    "PrintField(\"$descriptor_name$\", $name$_, writer);\n");
}

void RepeatedPrimitiveFieldGenerator::GenerateCloningCode(io::Printer* printer) {
  printer->Print(variables_,
    "$name$_ = other.$name$_.Clone();\n");
}

void RepeatedPrimitiveFieldGenerator::GenerateFreezingCode(io::Printer* printer) {
}

}  // namespace csharp
}  // namespace compiler
}  // namespace protobuf
}  // namespace google
