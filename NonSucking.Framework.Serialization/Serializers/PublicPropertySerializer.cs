﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static NonSucking.Framework.Serialization.NoosonGenerator;

using VaVare.Statements;
using NonSucking.Framework.Serialization.Vavare;
using NonSucking.Framework.Serialization.Serializers;

namespace NonSucking.Framework.Serialization
{
    internal static class PublicPropertySerializer
    {

        internal static bool TrySerialize(MemberInfo memberInfo, NoosonGeneratorContext context, string readerName, out StatementSyntax statement)
        {
            var props
                = Helper.GetMembersWithBase(memberInfo.TypeSymbol)
                .Where(property =>
                    property.Name != "this[]");

            var writeOnlies = props.Select(x => x.Symbol).OfType<IPropertySymbol>().Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (var onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                       "",
                       "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                       memberInfo.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            props = FilterPropsForNotWriteOnly(props);

            var statements
                = GenerateStatementsForProps(
                    props
                        .Select(x => x with { Name = memberInfo.FullName })
                        .ToArray(),
                    context,
                    MethodType.Serialize

                );
            statement = BlockHelper.GetBlockWithoutBraces(statements);
            return true;

        }

        private static IEnumerable<MemberInfo> FilterPropsForNotWriteOnly(IEnumerable<MemberInfo> props)
        {
            props = props.Where(x =>
            {
                if (x.Symbol is IPropertySymbol ps
                    && !ps.IsWriteOnly
                    && ps.GetMethod is not null)
                {
                    return true;
                }
                else if (x.Symbol is IFieldSymbol fs)
                {
                    return true;
                }
                return false;

            });
            return props;
        }

        internal static bool TryDeserialize(MemberInfo memberInfo, NoosonGeneratorContext context, string readerName, out StatementSyntax statement)
        {
            var props
               = Helper.GetMembersWithBase(memberInfo.TypeSymbol)
               .Where(property =>
                   property.Name != "this[]");

            var writeOnlies = props.Select(x => x.Symbol).OfType<IPropertySymbol>().Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (var onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                       "",
                       "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                       memberInfo.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            props = FilterPropsForNotWriteOnly(props);


            string randomForThisScope = Helper.GetRandomNameFor("");
            var statements
                = GenerateStatementsForProps(
                    props.ToArray(),
                    context,
                    MethodType.Deserialize
                ).ToList();

            string memberName = $"@{Helper.GetRandomNameFor(memberInfo.Name)}";

            var declaration
                = Statement
                .Declaration
                .Declare(memberName, SyntaxFactory.ParseTypeName(memberInfo.TypeSymbol.ToDisplayString()));

            try
            {

                var ctorSyntax = CtorSerializer.CallCtorAndSetProps((INamedTypeSymbol)memberInfo.TypeSymbol, statements.ToArray(), memberName, DeclareOrAndAssign.DeclareOnly);
                statements.Add(ctorSyntax);

            }
            catch (NotSupportedException)
            {
                context.AddDiagnostic("0006",
                   "",
                   "No instance could be created with the constructors in this type. Add a custom ctor call, property mapping or a ctor with matching arguments.",
                   memberInfo.Symbol,
                   DiagnosticSeverity.Error
                   );
            }
            statement = BlockHelper.GetBlockWithoutBraces(new StatementSyntax[] { declaration, SyntaxFactory.Block(SyntaxFactory.List(statements)), });
            return true;
        }
    }
}
