using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using static NonSucking.Framework.Serialization.NoosonGenerator;

using VaVare.Statements;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;

namespace NonSucking.Framework.Serialization
{
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
                               => p.Name == "Value");
            return p with { Parent = property.Name };
        }
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, int baseTypesLevelProperties = int.MaxValue)
        {
            if (!CanBeNull(property))
                return false;
            var localIsNotNullName = Helper.GetRandomNameFor("isNotNull", Helper.GetRandomNameFor(property.Name, property.Parent.Replace('.', '_')));

            var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            var propertyAccessorName = Helper.GetMemberAccessString(property);

            var isNotNullCheck = SyntaxFactory.IsPatternExpression(
                SyntaxFactory.IdentifierName(propertyAccessorName),
                SyntaxFactory.UnaryPattern(SyntaxFactory.ConstantPattern(nullLiteral)));

            var isNotNullDeclaration = Statement.Declaration.DeclareAndAssign(localIsNotNullName, isNotNullCheck);
            statements.Statements.Add(isNotNullDeclaration);

            var m = property.TypeSymbol.IsValueType
                ? GetNullableValue(property, baseTypesLevelProperties)
                : new MemberInfo(GetNonNullableTypeSymbol(property.TypeSymbol), property.Symbol, property.Name,
                    property.Parent);
            var innerSerialize = NoosonGenerator.CreateStatementForSerializing(m, context, readerName, true);
            var b = BodyGenerator.Create(innerSerialize.ToMergedBlock().ToArray());

            var writeNullable = Statement.Expression.Invoke(writerName, "Write",
                new[] { new VariableArgument(localIsNotNullName) });
                
            statements.Statements.Add(writeNullable.AsStatement());
            statements.Statements.Add(SyntaxFactory.IfStatement(SyntaxFactory.IdentifierName(localIsNotNullName), b));
            

            return true;

        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, int baseTypesLevelProperties = int.MaxValue)
        {
            if (!CanBeNull(property))
                return false;

            var elementType = GetNonNullableTypeSymbol(property.TypeSymbol);
            var m = new MemberInfo(elementType, property.Symbol, property.Name + (elementType.IsValueType ? "ValueType" : ""), property.Parent);

            var innerDeserialize = CreateStatementForDeserializing(m, context, readerName, true);

            LocalDeclarationStatementSyntax Transform(GeneratedSerializerCode.SerializerVariable variable)
            {
                var d = variable.Declaration;
                if (variable.OriginalMember == m)
                {
                    var newDecl = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(d.Declaration.Type.ToFullString() + "?"), d.Declaration.Variables);
                    return SyntaxFactory.LocalDeclarationStatement(d.AttributeLists, d.AwaitKeyword, d.UsingKeyword,
                        d.Modifiers, newDecl, d.SemicolonToken);
                }
                return d;
            }

            IEnumerable<StatementSyntax> innerStatements = null;
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
            return true;
        }
    }
}
