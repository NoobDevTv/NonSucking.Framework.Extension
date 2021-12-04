using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Text;

using VaVare.Generators.Common.Arguments.ArgumentTypes;

using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    internal static class SpecialTypeSerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, out ICollection<StatementSyntax> statements)
        {
            var type = property.TypeSymbol;
            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    ValueArgument argument = Helper.GetValueArgumentFrom(property);

                    statements
                        = new[]{Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement() };
                    return true;
                default:
                    statements = null;
                    return false;
            }
        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, out ICollection<StatementSyntax> statements)
        {
            var type = property.TypeSymbol;

            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    string memberName = $"{Helper.GetRandomNameFor(property.Name, property.Parent)}";

                    var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, Helper.GetReadMethodCallFrom(type.SpecialType))
                        .AsExpression();

                    statements
                        = new[]{Statement
                        .Declaration
                        .DeclareAndAssign(memberName, invocationExpression)};

                    return true;
                default:
                    statements = null;
                    return false;
            }
        }
    }
}
