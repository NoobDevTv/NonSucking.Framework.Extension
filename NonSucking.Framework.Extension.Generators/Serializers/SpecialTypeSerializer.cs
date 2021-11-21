using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Text;

using VaVare.Generators.Common.Arguments.ArgumentTypes;

using VaVare.Statements;

namespace NonSucking.Framework.Extension.Generators
{
    internal static class SpecialTypeSerializer
    {
        internal static bool TrySerialize(MemberInfo property, string writerName, out StatementSyntax statement)
        {
            var type = property.TypeSymbol;
            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    ValueArgument argument = Helper.GetValueArgumentFrom(property);

                    statement
                        = Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement();
                    return true;
                default:
                    statement = null;
                    return false;
            }
        }

        internal static bool TryDeserialize(MemberInfo property, string readerName, out StatementSyntax statement)
        {
            var type = property.TypeSymbol;

            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    string memberName = $"@{Helper.GetRandomNameFor(property.Name)}";

                    var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, Helper.GetReadMethodCallFrom(type.SpecialType))
                        .AsExpression();

                    statement
                        = Statement
                        .Declaration
                        .DeclareAndAssign(memberName, invocationExpression);

                    return true;
                default:
                    statement = null;
                    return false;
            }
        }
    }
}
