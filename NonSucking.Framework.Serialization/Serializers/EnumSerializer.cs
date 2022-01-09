using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

using VaVare.Statements;
using VaVare.Generators.Common.Arguments.ArgumentTypes;

namespace NonSucking.Framework.Serialization
{
    internal static class EnumSerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName,List<StatementSyntax> statements)
        {

            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                ValueArgument argument = Helper.GetValueArgumentFrom(property, typeSymbol.EnumUnderlyingType);

                statements.Add(Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement());
                return true;
            }
            else
            {
                return false;
            }

        }
        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,List<StatementSyntax> statements)
        {
            
            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                SpecialType specialType = typeSymbol.EnumUnderlyingType.SpecialType;
                string localName = $"{Helper.GetRandomNameFor(property.Name, property.Parent)}";

                ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, Helper.GetReadMethodCallFrom(specialType))
                        .AsExpression();

                var typeSyntax
                    = SyntaxFactory
                    .ParseTypeName(typeSymbol.Name);

                invocationExpression
                    = SyntaxFactory
                    .CastExpression(typeSyntax, invocationExpression);

                statements.Add(Statement
                    .Declaration
                    .DeclareAndAssign(localName, invocationExpression));

                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
