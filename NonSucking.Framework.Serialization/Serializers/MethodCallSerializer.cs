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

            var member
                = type.GetMembersWithBase<IMethodSymbol>((m) => (m.Name == context.GlobalContext.GetConfigForSymbol(m).NameOfStaticDeserializeWithCtor)
                        && m.Parameters.Length > 0
                        && Helper.MatchReaderWriterParameter(context, m.Parameters.First()))
                        .FirstOrDefault();

            var generated = Helper.GetFirstMemberWithBase(context, type,
                (m) => m.OverridenName == context.GlobalContext.Config.NameOfStaticDeserializeWithCtor
                       && m.Parameters.Count == 1
                       && Helper.MatchReaderWriterParameter(context, m.Parameters.First(), m)
                , 0);

            var baseDeserialize = PublicPropertySerializer.GetBaseDeserialize(property, context, false);

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
                = shouldBeGenerated || member is not null || generated.Item1 is not null || baseDeserialize is not null;

            if (isUsable)
            {
                if (member is not null || generated.Item1 is not null)
                {
                    string typeName = type.ToDisplayString();
                    string methodName = context.GlobalContext.Config.NameOfStaticDeserializeWithCtor;
                    if (member is not null)
                    {
                        methodName = member.Name;
                        typeName = member.ContainingType.ToDisplayString();
                    }
                    else if (generated.Item1 is not null)
                    {
                        methodName = generated.Item1.OverridenName;
                        typeName = generated.Item2!.ToDisplayString();
                    }


                    var invocation
                            = Statement
                            .Expression
                            .Invoke(typeName, methodName, arguments: new[] { new ValueArgument((object)readerName) }).AsExpression();

                    statements.DeclareAndAssign(property, property.CreateUniqueName(), type, invocation);
                }
                else if(baseDeserialize is not null)
                {
                    CreateInvocationForOutDeserialize(property, context, readerName, statements, baseDeserialize.Value);
                }
            }
            else if (hasAttribute)
            {
                ReportMissingCompatibility(context, property, attrData!, context.ReaderTypeName ?? "");
            }

            return isUsable;
        }

        private static void CreateInvocationForOutDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, BaseDeserializeInformation baseDeserialize)
        {
            List<ArgumentSyntax> arguments = Helper.GetArgumentsFromGenMethod(readerName, property, statements, baseDeserialize.Parameters.Select(x => x.parameterName));
            Helper.ConvertToStatement(statements, baseDeserialize.typeName, baseDeserialize.methodName, arguments);

            try
            {
                Initializer initializer = Initializer.InitializerList;
                string name = property.CreateUniqueName();

                GeneratedSerializerCode ctorSyntax = CtorSerializer.CallCtorAndSetProps(
                    (INamedTypeSymbol)property.TypeSymbol,
                    statements, property, name, initializer);
                statements.MergeWith(ctorSyntax);
            }
            catch (NotSupportedException)
            {
                context.AddDiagnostic(Diagnostics.InstanceCreationImpossible,
                    property.Symbol,
                    DiagnosticSeverity.Error
                );
            }
        }

        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements, SerializerMask includedSerializers)
        {
            var generateGeneric = context.WriterTypeName is null;
            var type = property.TypeSymbol;

            var member
                = type.GetMembersWithBase<IMethodSymbol>((m) => (m.Name == Consts.Serialize
                            || m.Name == context.MethodName
                            || m.Name == context.GlobalContext.GetConfigForSymbol(m).NameOfSerialize)
                        && Helper.CheckSignature(context, m, Consts.GenericParameterWriterInterfaceFull)
                        )
                .FirstOrDefault();
            var generated = Helper.GetFirstMemberWithBase(context, type.BaseType,
                (m) => m.Name == Consts.Serialize
                        || m.Name == context.MethodName);

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

            bool isUsable
                = shouldBeGenerated || member is not null || generated.Item1 is not null;

            if (isUsable)
            {
                if (shouldBeGenerated || (member?.IsStatic ?? false))
                {
                    string methodName = Consts.Serialize;
                    if (hasAttribute)
                    {
                        methodName = context.GlobalContext.GetConfigForSymbol(type).NameOfSerialize;
                    }
                    else if (member is { } method)
                    {
                        methodName = method.Name;
                    }
                    statements.Statements.Add(Statement
                        .Expression
                        .Invoke(type.ToDisplayString(), methodName, arguments: new[] { ValueArgument.Parse(Helper.GetMemberAccessString(property)), new ValueArgument((object)writerName) })
                        .AsStatement());
                }
                else if (generated.Item1 is GeneratedMethod gm)
                {
                    if (gm.IsStatic)
                        statements.Statements.Add(Statement
                            .Expression
                                .Invoke(generated.Item2!.ToDisplayString(), gm.Name, arguments: new[] { ValueArgument.Parse(Helper.GetMemberAccessString(property)), new ValueArgument((object)writerName) })
                            .AsStatement());
                    else
                        statements.Statements.Add(Statement
                            .Expression
                                .Invoke(Helper.GetMemberAccessString(property), gm.OverridenName, arguments: new[] { ValueArgument.Parse(Helper.GetMemberAccessString(property)), new ValueArgument((object)writerName) })
                            .AsStatement());

                }
                else if (!member!.IsStatic)
                {
                    statements.Statements.Add(Statement
                        .Expression
                        .Invoke(Helper.GetMemberAccessString(property), member.Name, arguments: new[] { new ValueArgument((object)writerName) })
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
            context.AddDiagnostic(Diagnostics.SerializerIncompatibility.Format(property.Name), property.Symbol, DiagnosticSeverity.Error);
            if (attrData.ApplicationSyntaxReference is { } syntaxReference)
                context.AddDiagnostic(Diagnostics.General.Format(addendum), syntaxReference.GetLocation(), DiagnosticSeverity.Error);
        }

    }
}
