using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static NonSucking.Framework.Extension.Generators.NoosonGenerator;

using VaVare.Statements;
using NonSucking.Framework.Extension.Generators.Vavare;
using NonSucking.Framework.Extension.Generators.Serializers;

namespace NonSucking.Framework.Extension.Generators
{
    internal static class PublicPropertySerializer
    {

        internal static bool TrySerializePublicProps(MemberInfo memberInfo, string readerName, out StatementSyntax statement)
        {
            var props
                = memberInfo
                .TypeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(property => property.Name != "this[]");

            var statements
                = GenerateStatementsForProps(
                    props
                        .Select(x => new MemberInfo(x.Type, x, x.Name, memberInfo.Name))
                        .ToArray(),
                    MethodType.Serialize

                );
            statement = BlockHelper.GetBlockWithoutBraces(statements);
            return true;

        }

        internal static bool TryDeserializePublicProps(MemberInfo memberInfo, string readerName, out StatementSyntax statement)
        {
            var props
                = memberInfo
                .TypeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(property => property.Name != "this[]");
            string randomForThisScope = Helper.GetRandomNameFor("");
            var statements
                = GenerateStatementsForProps(
                    props
                        .Select(x => new MemberInfo(x.Type, x, $"{x.Name}"))
                        .ToArray(),
                    MethodType.Deserialize
                ).ToList();

            string memberName = $"@{Helper.GetRandomNameFor(memberInfo.Name)}";

            var declaration
                = Statement
                .Declaration
                .Declare(memberName, SyntaxFactory.ParseTypeName(memberInfo.TypeSymbol.ToDisplayString()));


            var ctorSyntax =CtorSerializer.CallCtorAndSetProps((INamedTypeSymbol)memberInfo.TypeSymbol, statements.ToArray(), memberName, DeclareOrAndAssign.DeclareOnly);
            statements.Add(ctorSyntax);
            statement = BlockHelper.GetBlockWithoutBraces(new StatementSyntax[] { declaration, SyntaxFactory.Block(SyntaxFactory.List(statements)), });
            return true;
        }
    }
}
