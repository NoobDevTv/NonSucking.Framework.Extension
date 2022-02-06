using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NonSucking.Framework.Serialization.Serializers;

using System;
using System.Collections;
using System.IO;
using System.Linq;

using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Models.References;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(60)]
    internal static class ListSerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName,
            GeneratedSerializerCode statements)
        {
            var type = property.TypeSymbol;
            var collectionInterface
                = type
                    .AllInterfaces
                    .FirstOrDefault(x => x.ToString().StartsWith("System.Collections.Generic.IReadOnlyCollection<"));

            if (collectionInterface is null)
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
                    .ForEach(itemName,
                        typeof(void),
                        Helper.GetMemberAccessString(property, castToCollection),
                        BodyGenerator.Create(innerStatements.ToArray()),
                        useVar: true);


            //var iterationStatement
            //    = ForEach(itemName,
            //         genericArgument.ToString(),
            //         Helper.GetMemberAccessString(property, castToCollection),
            //         BodyGenerator.Create(localStatements.ToArray()));

            statements.MergeWith(preIterationStatements);
            statements.Statements.Add(iterationStatement);


            return true;
        }

        private static ForEachStatementSyntax ForEach(string variableName, string varialeType, string enumerableName,
            BlockSyntax body)
            => SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName(varialeType),
                SyntaxFactory.Identifier(variableName),
                ReferenceGenerator.Create(new VariableReference(enumerableName)),
                body);


        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements)
        {
            var type = property.TypeSymbol;
            // Ctor Analyze for Count 
            const string readonlyName = "System.Collections.Generic.IReadOnlyCollection<";
            var collectionInterface
                = type
                      .AllInterfaces
                      .FirstOrDefault(x => x.ToString().StartsWith(readonlyName))
                  ?? (type.ToString().StartsWith(readonlyName) ? type : null);

            //TODO: Check for IReadOnlyCollection< self
            IArrayTypeSymbol? arrType = null;
            if (collectionInterface is null)
            {
                if (property.TypeSymbol is not IArrayTypeSymbol arrType2)
                    return false;

                arrType = arrType2;
            }

            // Is ICollection<T> => Add
            // No => Has Add ? => Add
            // NO => Has Enqueue => Enqueue
            // NO => Has Push => Push


            ITypeSymbol genericArgument = arrType?.ElementType ?? GetGenericTypeOf(collectionInterface, out _);


            var itemDeserialization = NoosonGenerator.CreateStatementForDeserializing(
                new MemberInfo(genericArgument, genericArgument, genericArgument.Name),
                context,
                readerName
            );

            GeneratedSerializerCode preIterationStatements = new();
            var count = GetIterationAmount(property.TypeSymbol);

            if (count > -1)
                PublicPropertySerializer.TryDeserialize(property, context, readerName, preIterationStatements, count);

            var listVariableName = itemDeserialization.VariableDeclarations.Single().UniqueName;

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
                        .ExpressionStatement(
                            SyntaxFactory.ParseExpression($"{listName}[{indexName}]={listVariableName}"));

                itemDeserialization.Statements.Add(addStatement);

                var genericStatement = genericArgument.ToString();
                string arrayCtor;

                if (!genericStatement.EndsWith("[]"))
                {
                    arrayCtor = $"{genericArgument}[{end.Name}]";
                }
                else
                {
                    var index = genericStatement.IndexOf("[]");
                    arrayCtor = genericStatement.Substring(0, index) + $"[{end.Name}][]" +
                                genericStatement.Substring(index + 2);
                }

                var arrayStatement
                    = Statement
                        .Declaration
                        .Assign(listName, SyntaxFactory.ParseExpression($"new {arrayCtor}"));

                var iterationStatement
                    = Statement
                        .Iteration
                        .For(start, end, indexName,
                            BodyGenerator.Create(itemDeserialization.ToMergedBlock().ToArray()));

                statements.DeclareAndAssign(property, listName, SyntaxFactory.ParseTypeName($"{genericArgument}[]"),
                    null);
                statements.MergeWith(preIterationStatements);
                statements.Statements.Add(arrayStatement);
                statements.Statements.Add(iterationStatement);
            }
            else
            {
                string? methodName = null;

                const string fallbackMethodName = nameof(IList.Add) + "_";

                var genericCollectionInterface
                    = type
                        .AllInterfaces
                        .FirstOrDefault(x => x.ToString().StartsWith("System.Collections.Generic.ICollection<"))?.ToDisplayString();

                bool hasCountCtor = false;

                if (genericCollectionInterface is not null)
                {
                    methodName = fallbackMethodName;
                }

                var allmembers = type.GetMembers();

                foreach (var item in allmembers)
                {
                    if (string.IsNullOrWhiteSpace(methodName))
                    {
                        methodName = item.Name switch
                        {
                            nameof(IList.Add) => nameof(IList.Add),
                            nameof(Queue.Enqueue) => nameof(Queue.Enqueue),
                            nameof(Stack.Push) => nameof(Stack.Push),
                            _ => ""
                        };
                    }

                    if (item is IMethodSymbol methodSymbol
                        && methodSymbol.MethodKind == MethodKind.Constructor
                        && methodSymbol.Parameters.Length == 1
                        && methodSymbol.Parameters[0].ToString() == "int")
                    {
                        hasCountCtor = true;
                    }

                    if (hasCountCtor && !string.IsNullOrWhiteSpace(methodName))
                        break;
                }


                var castedListName = Helper.GetRandomNameFor("localList");

                //.ParseDeclareAndAssing(castedListName, cast);


                string listInitialize;


                if (!type.IsAbstract && type.TypeKind != TypeKind.Interface)
                {
                    listInitialize = type.ToDisplayString();
                }
                else if (HasOrIsInterfaces(type, "System.Collections.Generic.IDictionary<",
                             "System.Collections.Generic.IReadOnlyDictionary<"))
                {
                    var namedTypeSymbol = (INamedTypeSymbol)genericArgument;

                    listInitialize =
                        $"System.Collections.Generic.Dictionary<{namedTypeSymbol.TypeArguments[0]},{namedTypeSymbol.TypeArguments[1]}>";
                    genericCollectionInterface = $"System.Collections.Generic.ICollection< System.Collections.Generic.KeyValuePair<{namedTypeSymbol.TypeArguments[0]},{namedTypeSymbol.TypeArguments[1]}>>";
                    methodName = fallbackMethodName;


                }
                else if (HasOrIsInterfaces(type, "System.Collections.IList<",
                             "System.Collections.Generic.IReadOnlyCollection<"))
                {
                    listInitialize = $"System.Collections.Generic.List<{genericArgument}>";
                    genericCollectionInterface = $"System.Collections.Generic.ICollection<{genericArgument}>";
                    methodName = nameof(IList.Add);
                }
                else
                {
                    statements.Clear();
                    //TODO Warning / Error
                    return false;
                }


                var castDeclaration
                    = Statement
                        .Declaration
                        .ParseDeclareAndAssing(castedListName,
                            methodName == fallbackMethodName
                                ? Helper.Cast(listName, genericCollectionInterface)
                                : listName);

                if (!string.IsNullOrWhiteSpace(methodName))
                {
                    var addStatement
                        = Statement
                            .Expression
                            .Invoke(
                                castedListName,
                                methodName!.TrimEnd('_'),
                                arguments: new[] { new VariableArgument(listVariableName) })
                            .AsStatement();
                    itemDeserialization.Statements.Add(addStatement);
                }
                else
                    ; //Diagnostic

                ExpressionSyntax ctorInvocationExpression
                    = Statement
                        .Expression
                        .Invoke(
                            $"new {listInitialize}",
                            arguments: hasCountCtor
                                ? new[] { new ValueArgument((object)end.Name) }
                                : Array.Empty<IArgument>())
                        .AsExpression();

                var iterationStatement
                    = Statement
                        .Iteration
                        .For(start, end, Helper.GetRandomNameFor("i", ""),
                            BodyGenerator.Create(itemDeserialization.ToMergedBlock().ToArray()));

                statements.MergeWith(preIterationStatements);
                statements.DeclareAndAssign(property, listName, SyntaxFactory.ParseTypeName(listInitialize),
                    ctorInvocationExpression);
                statements.Statements.Add(castDeclaration);
                statements.Statements.Add(iterationStatement);
            }

            return true;
        }

        private static bool HasOrIsInterfaces(ITypeSymbol symbol, params string[] interfaces)
        {
            foreach (var @interface in interfaces)
            {
                if (symbol.ToDisplayString().StartsWith(@interface)
                    || symbol.AllInterfaces.Any(x => x.ToString().StartsWith(@interface)))
                {
                    return true;
                }
            }

            return false;
        }


        private static int GetIterationAmount(ITypeSymbol? type, int lastAmount = -1)
        {
            if (type is null || type is IArrayTypeSymbol)
                return lastAmount;

            if (type.Interfaces.Any(x => x.Name == typeof(IEnumerable).Name))
                return lastAmount;
            return GetIterationAmount(type.BaseType, ++lastAmount);
        }

        private static ITypeSymbol GetGenericTypeOf(ITypeSymbol? type, out bool isTypeGeneric)
        {
            ITypeSymbol? genericArgument = null;
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