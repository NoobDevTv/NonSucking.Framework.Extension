using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

using VaVare.Statements;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using System.Linq;
using System.IO;

namespace NonSucking.Framework.Extension.Generators
{
    internal static class MethodCallSerializer
    {
        internal static bool TryDeserialize(MemberInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

            IEnumerable<IMethodSymbol> member
                = type
                .GetMembers("Deserialize")
                .OfType<IMethodSymbol>();

            bool shouldBeGenerated
                = type
                .GetAttributes()
                .Any(x => x.ToString() == NoosonGenerator.genSerializationAttribute.FullName);

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

                statement
                        = Statement
                        .Declaration
                        .DeclareAndAssign($"@{Helper.GetRandomNameFor(property.Name)}", invocationExpression);
            }

            return isUsable;
        }



        internal static bool TrySerialize(MemberInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

            IEnumerable<IMethodSymbol> member
                = type
                    .GetMembers("Serialize")
                    .OfType<IMethodSymbol>();

            bool shouldBeGenerated
                = type
                .GetAttributes()
                .Any(x => x.ToString() == NoosonGenerator.genSerializationAttribute.FullName);

            bool isUsable
                = shouldBeGenerated || member
                .Any(m =>
                    m.Parameters.Length == 1
                    && m.Parameters[0].ToDisplayString() == typeof(BinaryWriter).FullName
                );

            if (isUsable)
            {
                statement
                        = Statement
                        .Expression
                        .Invoke(Helper.GetMemberAccessString(property), "Serialize", arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement();
            }

            return isUsable;
        }
    }
}
