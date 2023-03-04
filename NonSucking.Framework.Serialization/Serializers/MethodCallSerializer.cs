using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

using VaVare.Statements;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using System.Linq;
using System.IO;
using NonSucking.Framework.Serialization.Attributes;
using NonSucking.Framework.Serialization.Serializers;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(40)]
    internal static class MethodCallSerializer
    {
        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, SerializerMask includedSerializers)
        {
            var generateGeneric = context.ReaderTypeName is null;
            var type = property.TypeSymbol;

            IEnumerable<IMethodSymbol> member
                = type
                .GetMembers("Deserialize")
                .OfType<IMethodSymbol>();

            bool hasAttribute = type.TryGetAttribute(AttributeTemplates.GenSerializationAttribute, out var attrData);

            bool shouldBeGenerated = hasAttribute;
            if (hasAttribute)
            {
                Helper.GetGenAttributeData(attrData!, out var generateDefaultReader, out var generateDefaultWriter,
                    out var directReaders, out var directWriters);
                const string binaryReaderName = "System.IO.BinaryReader";
                shouldBeGenerated = context.UseAdvancedTypes
                    ? generateGeneric && generateDefaultReader
                    : context.ReaderTypeName == binaryReaderName
                      || directReaders.Any(x =>
                                           {
                                               var name = x?.ToDisplayString();
                                               return name == context.ReaderTypeName || name == binaryReaderName;
                                           });
            }
            bool isUsable
                = shouldBeGenerated
                    || member.Any(m => m.IsStatic && Helper.CheckSignature(context, m, "IBinaryReader"));

            if (isUsable)
            {
                var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(type.ToString(), "Deserialize", arguments: new[] { new ValueArgument((object)readerName) })
                        .AsExpression();

                statements.DeclareAndAssign(property, property.CreateUniqueName(), type, invocationExpression);
            }
            else if (hasAttribute)
            {
                ReportMissingCompatibility(context, property, attrData!, context.ReaderTypeName ?? "");
            }

            return isUsable;
        }

        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements, SerializerMask includedSerializers)
        {
            var generateGeneric = context.WriterTypeName is null;
            var type = property.TypeSymbol;
            var methodName = "Serialize";



            IEnumerable<IMethodSymbol> member
                = type
                    .GetMembers("Serialize")
                    .OfType<IMethodSymbol>();

            bool hasAttribute = type.TryGetAttribute(AttributeTemplates.GenSerializationAttribute, out var attrData);

            bool shouldBeGenerated = hasAttribute;
            if (hasAttribute)
            {
                Helper.GetGenAttributeData(attrData!, out var generateDefaultReader, out var generateDefaultWriter,
                    out var directReaders, out var directWriters);
                const string binaryWriterName = "System.IO.BinaryWriter";
                shouldBeGenerated = context.UseAdvancedTypes
                    ? generateGeneric && generateDefaultWriter
                    : context.WriterTypeName == binaryWriterName
                      || directWriters.Any(x =>
                                           {
                                               var name = x?.ToDisplayString();
                                               return name == context.WriterTypeName || name == binaryWriterName;
                                           });
            }

            var m = member.FirstOrDefault(m => Helper.CheckSignature(context, m, "IBinaryWriter"));
            bool isUsable
                = shouldBeGenerated || m is not null;

            if (isUsable)
            {
                if (shouldBeGenerated || m!.IsStatic)
                {
                    statements.Statements.Add(Statement
                        .Expression
                        .Invoke(type.ToDisplayString(), "Serialize", arguments: new[] { ValueArgument.Parse(Helper.GetMemberAccessString(property)), new ValueArgument((object)writerName) })
                        .AsStatement());
                }
                else if (!m!.IsStatic)
                {
                    statements.Statements.Add(Statement
                        .Expression
                        .Invoke(Helper.GetMemberAccessString(property), "Serialize", arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement());
                }
            }
            else if (hasAttribute)
            {
                ReportMissingCompatibility(context, property, attrData!, context.WriterTypeName ?? "");
            }

            return isUsable;
        }

        private static void ReportMissingCompatibility(NoosonGeneratorContext context, MemberInfo property, AttributeData attrData, string typeName)
        {
            var addendum = context.ReaderTypeName is null && context.WriterTypeName is null
                ? "Generic serializer needs to be activated!"
                : $"A direct writer for type {typeName} needs to be added";
            context.AddDiagnostic("0013", "", $"{property.Name} can not be serialized because of serializer incompatibility.", property.Symbol, DiagnosticSeverity.Error);
            var syntaxReference = attrData!.ApplicationSyntaxReference;
            if (syntaxReference is not null)
                context.AddDiagnostic("0013", "", addendum, Location.Create(syntaxReference.SyntaxTree, syntaxReference.Span), DiagnosticSeverity.Error);
        }

    }
}
