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
            
            var type = property.TypeSymbol;

            IEnumerable<IMethodSymbol> member
                = type
                .GetMembers("Deserialize")
                .OfType<IMethodSymbol>();

            bool shouldBeGenerated = type.TryGetAttribute(AttributeTemplates.GenSerializationAttribute, out _);

            bool isUsable
                = shouldBeGenerated
                    || member
                        .Any(m =>
                            m.Parameters.Length == 1
                            && m.Parameters[0].ToDisplayString() == typeof(BinaryReader).FullName
                            && m.IsStatic
                        );

            if (isUsable)
            {
                var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(type.ToString(), "Deserialize", arguments: new[] { new ValueArgument((object)readerName) })
                        .AsExpression();

                statements.DeclareAndAssign(property, property.CreateUniqueName(), type, invocationExpression);
            }

            return isUsable;
        }




        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements, SerializerMask includedSerializers)
        {
            
            var type = property.TypeSymbol;
            var methodName = "Serialize";



            IEnumerable<IMethodSymbol> member
                = type
                    .GetMembers("Serialize")
                    .OfType<IMethodSymbol>();

            bool shouldBeGenerated = type.TryGetAttribute(AttributeTemplates.GenSerializationAttribute, out _);

            bool isUsable
                = shouldBeGenerated || member
                .Any(m =>
                    m.Parameters.Length == 1
                    && m.Parameters[0].ToDisplayString() == typeof(BinaryWriter).FullName
                );

            if (isUsable)
            {
                statements.Statements.Add(Statement
                        .Expression
                        .Invoke(Helper.GetMemberAccessString(property), "Serialize", arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement());
            }

            return isUsable;
        }
    }
}
