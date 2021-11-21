using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

using VaVare.Statements;
using VaVare.Generators.Common.Arguments.ArgumentTypes;

namespace NonSucking.Framework.Extension.Generators
{
    internal static class EnumSerializer
    {
        internal static bool TrySerialize(MemberInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;

            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                ValueArgument argument = Helper.GetValueArgumentFrom(property, typeSymbol.EnumUnderlyingType);

                statement
                        = Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement();
                return true;
            }
            else
            {
                return false;
            }

        }
        internal static bool TryDeserialize(MemberInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                SpecialType specialType = typeSymbol.EnumUnderlyingType.SpecialType;
                string localName = $"@{Helper.GetRandomNameFor(property.Name)}";

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

                statement
                    = Statement
                    .Declaration
                    .DeclareAndAssign(localName, invocationExpression);

                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
