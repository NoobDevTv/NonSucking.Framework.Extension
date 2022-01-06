using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using VaVare.Generators.Common.Arguments.ArgumentTypes;

using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    internal static class SpecialTypeSerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements)
        {
            var type = property.TypeSymbol;
            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    ValueArgument argument = Helper.GetValueArgumentFrom(property);

                    statements.Statements.Add(Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement());
                    return true;
                default:

                    return false;
            }
        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements)
        {
            var type = property.TypeSymbol;

            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:

                    var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, Helper.GetReadMethodCallFrom(type.SpecialType))
                        .AsExpression();

                    // statements.Add(Statement
                    //     .Declaration
                    //     .DeclareAndAssign(memberName, invocationExpression));
                    statements.DeclareAndAssign(property, property.CreateUniqueName(), type, invocationExpression);

                    return true;
                default:

                    return false;
            }
        }
    }
}
