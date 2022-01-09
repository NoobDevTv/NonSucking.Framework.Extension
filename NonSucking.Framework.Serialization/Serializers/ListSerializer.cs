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
using NonSucking.Framework.Serialization.Serializers;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(60)]
    internal static class ListSerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements)
        {
            var type = property.TypeSymbol;
            var collectionInterface
                = type
                .AllInterfaces
                .FirstOrDefault(x => x.ToString().StartsWith("System.Collections.Generic.ICollection<"));

            bool isGenericCollection
                = collectionInterface is not null
                    || property.TypeSymbol.ToString().StartsWith("System.Collections.Generic.ICollection<");

            if (!isGenericCollection)
            {
                return false;
            }

            ITypeSymbol genericArgument = GetGenericTypeOf(collectionInterface, out _);

            string itemName = Helper.GetRandomNameFor("item", property.Name);

            var localStatements
                = NoosonGenerator.CreateStatementForSerializing(
                        new MemberInfo(genericArgument, genericArgument, itemName),
                        context,
                        writerName);

            var isArray = type.TypeKind == TypeKind.Array;

            var memberReference
                = new MemberReference(
                    isArray ? "Length" : "Count"
                );

            ITypeSymbol? castToCollection = isArray ? null : collectionInterface;

            var countReference
                = new ReferenceArgument(
                    new VariableReference(Helper.GetMemberAccessString(property, castToCollection), memberReference)
                );

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
                 .ForEach(itemName, typeof(void), Helper.GetMemberAccessString(property, castToCollection), BodyGenerator.Create(innerStatements.ToArray()), useVar: true);


            statements.MergeWith(preIterationStatements);
            statements.Statements.Add(iterationStatement);


            return true;
        }


        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements)
        {

            var type = property.TypeSymbol;
            var collectionInterface
                = type
                .AllInterfaces
                .FirstOrDefault(x => x.ToString().StartsWith("System.Collections.Generic.ICollection<"));

            bool isGenericCollection
                = collectionInterface is not null
                    || property.TypeSymbol.ToString().StartsWith("System.Collections.Generic.ICollection<");

            if (!isGenericCollection)
            {
                return false;
            }

            ITypeSymbol genericArgument = GetGenericTypeOf(collectionInterface, out _);

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
                var cast = Helper.Cast(listName, collectionInterface.ToDisplayString());
                var castedListName = Helper.GetRandomNameFor("localList");
                var castDeclaration
                    = Statement
                    .Declaration
                    .ParseDeclareAndAssing(castedListName, cast);

                var addStatement
                    = Statement
                    .Expression
                    .Invoke(castedListName, $"Add", arguments: new[] { new VariableArgument(listVariableName) })
                    .AsStatement();

                itemDeserialization.Statements.Add(addStatement);

                string listInitialize;


                if (!type.IsAbstract && type.TypeKind != TypeKind.Interface)
                {
                    listInitialize = type.ToDisplayString();

                }
                else if (type.AllInterfaces.Any(x => x.ToString().StartsWith("System.Collections.Generic.IDictionary<")))
                {
                    var namedTypeSymbol = genericArgument as INamedTypeSymbol;

                    listInitialize = $"System.Collections.Generic.Dictionary<{namedTypeSymbol.TypeArguments[0]},{namedTypeSymbol.TypeArguments[1]}>";
                }
                else if (type.AllInterfaces.Any(x => x.ToString().StartsWith("System.Collections.IList<")))
                {
                    listInitialize = $"System.Collections.Generic.List<{genericArgument}>";
                }
                else
                {
                    //TODO Warning / Error
                    return false;
                }


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
                statements.Statements.Add(castDeclaration);
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
