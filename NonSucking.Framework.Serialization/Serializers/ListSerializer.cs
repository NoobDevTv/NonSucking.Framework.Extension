using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections;
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
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements)
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

            var memberReference
                = new MemberReference(
                type.TypeKind == TypeKind.Array
                    ? "Length"
                    : "Count");
            var countReference = new ReferenceArgument(new VariableReference(Helper.GetMemberAccessString(property), memberReference));

            GeneratedSerializerCode preIterationStatements = new();



            var count = GetIterationAmount(property.TypeSymbol);
            if (count > -1)
                PublicPropertySerializer.TrySerialize(property, context, writerName, preIterationStatements, count);

            preIterationStatements.Statements.Add(
                        Statement
                        .Expression
                        .Invoke(writerName, nameof(BinaryWriter.Write), arguments: new[] { countReference })
                        .AsStatement());

            var innerStatements = localStatements.MergeBlocksSeperated(statements);
            var iterationStatement
                = Statement
                .Iteration
                 .ForEach(itemName, typeof(void), Helper.GetMemberAccessString(property), BodyGenerator.Create(innerStatements.ToArray()), useVar: true);


            statements.MergeWith(preIterationStatements);
            statements.Statements.Add(iterationStatement);


            return true;
        }


        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements)
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

            var listVariableName = $"{genericArgument.Name}{randomForThisScope}";

            var itemDeserialization = NoosonGenerator.CreateStatementForDeserializing(
                new MemberInfo(genericArgument, genericArgument, genericArgument.Name),
                context,
                readerName
            );

            GeneratedSerializerCode preIterationStatements = new();
            var count = GetIterationAmount(property.TypeSymbol);

            if (count > -1)
                PublicPropertySerializer.TryDeserialize(property, context, readerName, preIterationStatements, count);
            
            listVariableName = itemDeserialization.VariableDeclarations.Single().UniqueName;
            
            var listName = property.CreateUniqueName();


            var start = new VariableReference("0");
            var end = new VariableReference(Helper.GetRandomNameFor("count", property.Name));


            ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, nameof(BinaryReader.ReadInt32))
                        .AsExpression();
            preIterationStatements.Statements.Add(
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

                itemDeserialization.Statements.Add(addStatement);

                var arrayStatement
                    = Statement
                    .Declaration
                    .Assign(listName, SyntaxFactory.ParseExpression($"new {genericArgument}[{end.Name}]"));

                var iterationStatement
                    = Statement
                    .Iteration
                    .For(start, end, indexName, BodyGenerator.Create(itemDeserialization.ToMergedBlock().ToArray()));

                statements.DeclareAndAssign(property, listName, SyntaxFactory.ParseTypeName($"{genericArgument}[]"), null);
                statements.MergeWith(preIterationStatements);
                statements.Statements.Add(arrayStatement);
                statements.Statements.Add(iterationStatement);
            }
            else
            {

                var addStatement
                    = Statement
                    .Expression
                    .Invoke(listName, $"Add", arguments: new[] { new ValueArgument((object)listVariableName) })
                    .AsStatement();

                itemDeserialization.Statements.Add(addStatement);

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


                var iterationStatement
                    = Statement
                    .Iteration
                    .For(start, end, Helper.GetRandomNameFor("i", ""), BodyGenerator.Create(itemDeserialization.MergeBlocksSeperated(statements).ToArray()));

                statements.MergeWith(preIterationStatements);
                statements.DeclareAndAssign(property, listName, SyntaxFactory.ParseTypeName(listInitialize), ctorInvocationExpression);
                statements.Statements.Add(iterationStatement);

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
            while (genericArgument is null)
            {
                if (typeForIter is INamedTypeSymbol nts)
                {

                    if (nts.TypeArguments.Length > 0)
                    {
                        genericArgument = nts.TypeArguments[0];
                        continue;
                    }
                }
                else if (typeForIter is IArrayTypeSymbol ats)
                {
                    genericArgument = ats.ElementType;
                    continue;
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
    }
}
