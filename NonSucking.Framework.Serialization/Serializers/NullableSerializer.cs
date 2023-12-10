using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using VaVare.Statements;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;

using static NonSucking.Framework.Serialization.NoosonGenerator;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(10)]
    internal static class NullableSerializer
    {
        private static bool CanBeNull(MemberInfo property)
        {
            if (property.TypeSymbol.Kind == SymbolKind.TypeParameter)
                return false;
            return property.TypeSymbol.NullableAnnotation is NullableAnnotation.Annotated or NullableAnnotation.None;
        }

        private static ITypeSymbol GetNonNullableTypeSymbolAnnotated(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.NullableAnnotation != NullableAnnotation.Annotated)
                return typeSymbol;
            return typeSymbol.ConstructedFrom;
        }
        private static ITypeSymbol GetNonNullableTypeSymbol(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                return typeSymbol.IsValueType
                    ? namedTypeSymbol.TypeArguments[0]
                    : GetNonNullableTypeSymbolAnnotated(namedTypeSymbol);
            

            return typeSymbol;
        }

        private static MemberInfo GetNullableValue(MemberInfo property, int baseTypesLevelProperties)
        {
            if (property.Symbol is not IPropertySymbol propertySymbol)
                throw new NotSupportedException();
            var p
                = Helper.GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
                    .First(p 
                               => p.memberInfo.Name == "Value");
            return p.memberInfo with { Parent = property.FullName };
        }
        internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers, int baseTypesLevelProperties = int.MaxValue)
        {
            if (!CanBeNull(property))
                return Continuation.NotExecuted;

            var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            var (tempVariable, tempVariableAccessorName) = Helper.CreateTempIfNeeded(property, statements);
            var tempVariableIdentifier = SyntaxFactory.IdentifierName(tempVariableAccessorName);

            var isNotNullCheck = SyntaxFactory.IsPatternExpression(
                tempVariableIdentifier,
                SyntaxFactory.UnaryPattern(SyntaxFactory.ConstantPattern(nullLiteral)));

            var m = tempVariable.TypeSymbol.IsValueType
                ? GetNullableValue(tempVariable, baseTypesLevelProperties)
                : new MemberInfo(GetNonNullableTypeSymbol(tempVariable.TypeSymbol), tempVariable.Symbol, tempVariable.Name,
                    tempVariable.Parent);
            var innerSerialize = NoosonGenerator.CreateStatementForSerializing(m, context, readerName, includedSerializers, SerializerMask.NullableSerializer);
            var b = BodyGenerator.Create(innerSerialize.ToMergedBlock().ToArray());

            var writeNullable = Statement.Expression.Invoke(writerName, "Write",
                new[] { new InvocationArgument(isNotNullCheck) });

            statements.Statements.Add(writeNullable.AsStatement());
            statements.Statements.Add(SyntaxFactory.IfStatement(isNotNullCheck, b));
            

            return Continuation.Done;

        }

        internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers, int baseTypesLevelProperties = int.MaxValue)
        {
            if (!CanBeNull(property))
                return Continuation.NotExecuted;

            var elementType = GetNonNullableTypeSymbol(property.TypeSymbol);
            var m = new MemberInfo(elementType, property.Symbol, property.Name + (elementType.IsValueType ? "ValueType" : ""), property.Parent);

            var innerDeserialize = CreateStatementForDeserializing(m, context, readerName, includedSerializers, SerializerMask.NullableSerializer);

            TypeSyntax Transform(GeneratedSerializerCode.SerializerVariable variable)
            {
                return variable.OriginalMember != m 
                    ? variable.TypeSyntax 
                    : SyntaxFactory.ParseTypeName(variable.TypeSyntax.ToFullString() + "?");
            }

            IEnumerable<StatementSyntax> innerStatements;
            if (elementType.IsValueType)
            {
                var propName = property.CreateUniqueName();
                statements.DeclareAndAssign(property, propName, property.TypeSymbol, null);
                
                innerStatements = innerDeserialize.ToMergedBlock();
                innerStatements =
                    innerStatements.Append(Statement.Declaration.Assign
                    (propName, SyntaxFactory.IdentifierName(
                        innerDeserialize.VariableDeclarations.Single(x => x.OriginalMember == m).UniqueName)));
            }
            else
            {
                innerStatements = innerDeserialize.MergeBlocksSeperated(statements, Transform);
            }
            var b = BodyGenerator.Create(innerStatements.ToArray());

            var nullableRead = Statement.Expression
                .Invoke(readerName, "ReadBoolean");

            statements.Statements.Add(SyntaxFactory.IfStatement(nullableRead.AsExpression(), b));
            return Continuation.Done;
        }
    }
}
