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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Drawing;

namespace NonSucking.Framework.Serialization
{
    internal static class ListSerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, List<StatementSyntax> statements)
        {

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

            ITypeSymbol genericArgument = GetGenericTypeOf(type, out _);

            string itemName = Helper.GetRandomNameFor("item", property.Name);



            var localStatements
                = NoosonGenerator.CreateStatementForSerializing(
                        new MemberInfo(genericArgument, genericArgument, itemName),
                        context,
                        writerName);
            //NoosonGenerator.CreateStatementForSerializing(property, context, writerName);

            var memberReference
                = new MemberReference(
                type.TypeKind == TypeKind.Array
                    ? "Length"
                    : "Count");
            var countReference = new ReferenceArgument(new VariableReference(Helper.GetMemberAccessString(property), memberReference));

            List<StatementSyntax> preIterationStatements = new();



            var count = GetIterationAmount(property.TypeSymbol);
            if (count > -1)
                PublicPropertySerializer.TrySerialize(property, context, writerName, preIterationStatements, count);

            preIterationStatements.Add(
                        Statement
                        .Expression
                        .Invoke(writerName, nameof(BinaryWriter.Write), arguments: new[] { countReference })
                        .AsStatement());


            var iterationStatement
                = Statement
                .Iteration
                 .ForEach(itemName, typeof(void), Helper.GetMemberAccessString(property), BodyGenerator.Create(localStatements.ToArray()), useVar: true);


            statements.AddRange(preIterationStatements);
            statements.Add(iterationStatement);


            return true;
        }


        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, List<StatementSyntax> statements)
        {

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

            ITypeSymbol genericArgument = GetGenericTypeOf(type, out var isTypeGeneric);

            var randomForThisScope = Helper.GetRandomNameFor("", property.Name);

            List<StatementSyntax> localStatements = new();
            var listVariableName = $"{genericArgument.Name}{randomForThisScope}";

            localStatements.AddRange(NoosonGenerator.CreateStatementForDeserializing(
                        new MemberInfo(genericArgument, genericArgument, genericArgument.Name),
                        context,
                        readerName
                    ));

            List<StatementSyntax> preIterationStatements = new();
            var count = GetIterationAmount(property.TypeSymbol);

            if (count > -1)
                PublicPropertySerializer.TryDeserialize(property, context, readerName, preIterationStatements, count);

            LocalDeclarationStatementSyntax localDeclarationStatement;

            if (localStatements[0] is BlockSyntax blockSyntax)
                localDeclarationStatement = GetDeclerationStatement(blockSyntax.Statements);
            else
                localDeclarationStatement = GetDeclerationStatement(localStatements);

            listVariableName = localDeclarationStatement.Declaration.Variables.First().Identifier.ToFullString();

            var listName = $"{Helper.GetRandomNameFor(property.Name, property.Parent)}";


            var start = new VariableReference("0");
            var end = new VariableReference(Helper.GetRandomNameFor("count", property.Name));


            ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, nameof(BinaryReader.ReadInt32))
                        .AsExpression();

            preIterationStatements.Add(
                Statement
                .Declaration
                .DeclareAndAssign(end.Name, invocationExpression));

            if (type is IArrayTypeSymbol arrayType)
            {
                var indexName = Helper.GetRandomNameFor("i", "");

                var nullArrayStatement
                    = SyntaxFactory.ParseStatement($"{genericArgument}[] {listName};");

                var addStatement
                    = SyntaxFactory
                    .ExpressionStatement(SyntaxFactory.ParseExpression($"{listName}[{indexName}]={listVariableName}"));

                localStatements.Add(addStatement);

                var arrayStatement
                    = Statement
                    .Declaration
                    .Assign(listName, SyntaxFactory.ParseExpression($"new {genericArgument}[{end.Name}]"));

                var iterationStatement
                    = Statement
                    .Iteration
                    .For(start, end, indexName, BodyGenerator.Create(localStatements.ToArray()));

                statements.Add(nullArrayStatement);
                statements.AddRange(preIterationStatements);
                statements.Add(arrayStatement);
                statements.Add(iterationStatement);
            }
            else
            {

                var addStatement
                    = Statement
                    .Expression
                    .Invoke(listName, $"Add", arguments: new[] { new ValueArgument((object)listVariableName) })
                    .AsStatement();

                localStatements.Add(addStatement);

                string listInitialize;
                if (!type.AllInterfaces.Any(x => x.ToDisplayString().StartsWith("System.Collections.ICollection")))
                    listInitialize = $"System.Collections.Generic.List<{genericArgument}>";
                else
                    listInitialize = type.ToDisplayString();


                ExpressionSyntax ctorInvocationExpression
                    = Statement
                    .Expression
                    .Invoke($"new {listInitialize}", arguments: new[] { new ValueArgument((object)end.Name) })
                    .AsExpression();

                var listStatement
                    = Statement
                    .Declaration
                    .DeclareAndAssign(listName, ctorInvocationExpression);

                var iterationStatement
                    = Statement
                    .Iteration
                    .For(start, end, Helper.GetRandomNameFor("i", ""), BodyGenerator.Create(localStatements.ToArray()));

                statements.AddRange(preIterationStatements);
                statements.Add(listStatement);
                statements.Add(iterationStatement);

            }

            return true;
        }


        private static int GetIterationAmount(ITypeSymbol type, int lastAmount = -1)
        {
            if (type is null || type is IArrayTypeSymbol)
                return lastAmount;

            if (type.Interfaces.Any(x => x.Name == typeof(IEnumerable).Name))
                return lastAmount;
            return GetIterationAmount(type.BaseType, ++lastAmount);
        }

        private static ITypeSymbol GetGenericTypeOf(ITypeSymbol type, out bool isTypeGeneric)
        {
            ITypeSymbol genericArgument = null;
            var typeForIter = type;
            while (genericArgument == null)
            {
                if (typeForIter is INamedTypeSymbol nts)
                {

                    if (nts.TypeArguments.Length > 0)
                    {
                        genericArgument = nts.TypeArguments[0];
                        break;
                    }
                }
                else if (typeForIter is IArrayTypeSymbol ats)
                {
                    genericArgument = ats.ElementType;
                    break;
                }
                else
                {
                    throw new NotSupportedException();
                }

                typeForIter = typeForIter.BaseType;
            }

            isTypeGeneric = ReferenceEquals(typeForIter, type);

            return genericArgument;
        }
        private static LocalDeclarationStatementSyntax GetDeclerationStatement(IReadOnlyCollection<StatementSyntax> statements)
        {
            LocalDeclarationStatementSyntax localDeclarationStatement;

            if (statements.First() is LocalDeclarationStatementSyntax localDeclarationStatement2)
            {
                localDeclarationStatement = localDeclarationStatement2;
            }
            else
            {
                localDeclarationStatement = null;
            }

            return localDeclarationStatement;
        }
    }
}
