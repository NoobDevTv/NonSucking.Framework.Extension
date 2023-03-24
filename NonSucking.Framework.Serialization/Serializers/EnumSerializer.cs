using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Statements;
using VaVare.Generators.Common.Arguments.ArgumentTypes;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(30)]
    internal static class EnumSerializer
    {
        internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {

            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return Continuation.NotExecuted;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                ValueArgument argument = Helper.GetValueArgumentFrom(property, typeSymbol.EnumUnderlyingType);

                statements.Statements.Add(Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement());
                return Continuation.Done;
            }
            else
            {
                return Continuation.NotExecuted;
            }

        }
        internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {
            
            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return Continuation.NotExecuted;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                SpecialType specialType = typeSymbol.EnumUnderlyingType!.SpecialType;

                ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, Helper.GetReadMethodCallFrom(specialType))
                        .AsExpression();

                var typeSyntax
                    = SyntaxFactory
                    .ParseTypeName(typeSymbol.ToDisplayString());

                invocationExpression
                    = SyntaxFactory
                    .CastExpression(typeSyntax, invocationExpression);

                statements.DeclareAndAssign(property, property.CreateUniqueName(), typeSymbol, invocationExpression);

                return Continuation.Done;
            }
            else
            {
                return Continuation.NotExecuted;
            }

        }
    }
}
