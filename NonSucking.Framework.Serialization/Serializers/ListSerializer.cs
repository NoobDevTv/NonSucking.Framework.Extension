using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using VaVare.Generators.Common.Arguments.ArgumentTypes;

using VaVare.Generators.Common;

using VaVare.Models.References;

using VaVare.Statements;
using NonSucking.Framework.Serialization.Vavare;

namespace NonSucking.Framework.Serialization
{
    internal static class ListSerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;
            bool isIEnumerable
                = type
                .AllInterfaces
                .Any(x => x.Name == typeof(IEnumerable).Name)
                || property.TypeSymbol.Name == typeof(IEnumerable).Name;

            if (!isIEnumerable)
            {
                return false;
            }

            bool isIEnumerableInterfaceSelf
                = type.Name == nameof(IEnumerable);

            if (isIEnumerableInterfaceSelf)
            {
                //Diagnostic Error for not supported type
                context.AddDiagnostic("0005",
                    "",
                    "IEnumerable is not supported for serialization, implement own serializer or this value will be lost.",
                    property.Symbol,
                    DiagnosticSeverity.Error
                    );
                return true;
            }

            ITypeSymbol genericArgument;
            if (type is INamedTypeSymbol nts)
            {
                genericArgument = nts.TypeArguments[0];
            }
            else if (type is IArrayTypeSymbol ats)
            {
                genericArgument = ats.ElementType;
            }
            else
            {
                throw new NotSupportedException();
            }

            string itemName = Helper.GetRandomNameFor("item");

            //TryGeneratePublicPropsLines(builder, writerName, true);
            //TODO

            StatementSyntax[] statements = Array.Empty<StatementSyntax>();


            if ((int)genericArgument.SpecialType is >= 7 and <= 20) //List<string>, List<int>
            {
                statements
                    = new[]{
                        NoosonGenerator.CreateStatementForSerializing(
                            new MemberInfo(genericArgument, genericArgument, itemName),
                            context,
                            writerName
                        )
                    };
            }
            else
            {
                var genericInfo = new[] { new MemberInfo(genericArgument, genericArgument, itemName) };
                statements
                    = NoosonGenerator.GenerateStatementsForProps(genericInfo, context, MethodType.Serialize)
                    .ToArray();
            }


            var memberReference
                = new MemberReference(
                type.TypeKind == TypeKind.Array
                    ? "Length"
                    : "Count");
            var countRefernce = new ReferenceArgument(new VariableReference(Helper.GetMemberAccessString(property), memberReference));


            var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(writerName, nameof(BinaryWriter.Write), arguments: new[] { countRefernce })
                        .AsStatement();


            var iterationStatement
                = Statement
                .Iteration
                 .ForEach(itemName, typeof(void), Helper.GetMemberAccessString(property), BodyGenerator.Create(statements), useVar: true);


            statement = BlockHelper.GetBlockWithoutBraces(new StatementSyntax[] { invocationExpression, iterationStatement });


            return true;
        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

            bool isEnumerable
                = type
                .AllInterfaces
                .Any(x => x.Name == typeof(IEnumerable).Name)
                || property.TypeSymbol.Name == typeof(IEnumerable).Name;

            if (!isEnumerable)
            {
                return false;
            }

            bool isIEnumerableSelf = type.Name == nameof(IEnumerable);
            if (isIEnumerableSelf)
            {
                //Diagnostic Error for not supported type
                context.AddDiagnostic("0005",
                    "",
                    "IEnumerable is not supported for deserialization, implement own deserializer or this value will be lost.",
                    property.Symbol,
                    DiagnosticSeverity.Error
                    );
                return true;
            }

            ITypeSymbol genericArgument;
            if (type is INamedTypeSymbol nts)
            {
                genericArgument = nts.TypeArguments[0];
            }
            else if (type is IArrayTypeSymbol ats)
            {
                genericArgument = ats.ElementType;
            }
            else
            {
                throw new NotSupportedException();
            }

            var randomForThisScope = Helper.GetRandomNameFor("");

            List<StatementSyntax> statements = new();
            var listVariableName = $"{genericArgument.Name}{Helper.localVariableSuffix}{randomForThisScope}";

            if ((int)genericArgument.SpecialType is >= 7 and <= 20) //List<string>, List<int>
            {
                statements.Add(NoosonGenerator.CreateStatementForDeserializing(
                            new MemberInfo(genericArgument, genericArgument, genericArgument.Name),
                            context,
                            readerName
                        ));
                var localDeclerationSyntax = statements[0] as LocalDeclarationStatementSyntax;
                listVariableName = localDeclerationSyntax.Declaration.Variables.First().Identifier.ToFullString();
            }
            else
            {
                var genericInfo = new[] { new MemberInfo(genericArgument, genericArgument, listVariableName) };
                var gsfp 
                    = NoosonGenerator
                    .GenerateStatementsForProps(genericInfo, context, MethodType.Deserialize);
                statements.AddRange(gsfp);
            }

            var listName = $"@{Helper.GetRandomNameFor(property.Name)}";

            var addStatement
                = Statement
                .Expression
                .Invoke(listName, $"Add", arguments: new[] { new ValueArgument((object)listVariableName) })
                .AsStatement();

            statements.Add(addStatement);

            var start = new VariableReference("0");
            var end = new VariableReference(Helper.GetRandomNameFor("count" + property.Name));


            ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, nameof(BinaryReader.ReadInt32))
                        .AsExpression();

            var countStatement
                = Statement
                .Declaration
                .DeclareAndAssign(end.Name, invocationExpression);

            ExpressionSyntax ctorInvocationExpression
                = Statement
                .Expression
                .Invoke($"new System.Collections.Generic.List<{genericArgument}>", arguments: new[] { new ValueArgument((object)end.Name) })
                .AsExpression();

            var listStatement
                = Statement
                .Declaration
                .DeclareAndAssign(listName, ctorInvocationExpression);

            var iterationStatement
                = Statement
                .Iteration
                .For(start, end, Helper.GetRandomNameFor("i"), BodyGenerator.Create(statements.ToArray()));

            statement
                = BlockHelper.GetBlockWithoutBraces(new StatementSyntax[] { countStatement, listStatement, iterationStatement });

            return true;
        }
    }
}
