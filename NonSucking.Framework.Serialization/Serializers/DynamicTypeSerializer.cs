using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NonSucking.Framework.Serialization.Serializers;

using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(11)]
    public static class DynamicTypeSerializer
    {
        private static bool TryGetAttribute(ITypeSymbol? symbol, out AttributeData? attr)
        {
            if (symbol == null)
            {
                attr = null;
                return false;
            }
            if (!symbol.TryGetAttribute(AttributeTemplates.DynamicType, out attr))
            {
                return TryGetAttribute(symbol.BaseType, out attr);
            }

            return true;
        }
        private static bool IsValidType(MemberInfo property, out IEnumerable<ITypeSymbol>? possibleTypes, out ITypeSymbol? resolverType, out AttributeData? attr)
        {
            if (!property.Symbol.TryGetAttribute(AttributeTemplates.DynamicType, out attr))
            {
                if (!TryGetAttribute(property.TypeSymbol, out attr))
                {
                    possibleTypes = null;
                    resolverType = null;
                    return false;
                }
            }

            possibleTypes = attr!.ConstructorArguments[0].Values.Select(x => x.Value).OfType<ITypeSymbol>();
            resolverType = attr.NamedArguments.FirstOrNull(x => x.Key == "Resolver")?.Value.Value as ITypeSymbol;
            return true;
        }
        private static ThrowStatementSyntax ThrowException(string exceptionName)
        {
            return SyntaxFactory.ThrowStatement(
                SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName(exceptionName))
                    .WithArgumentList(SyntaxFactory.ArgumentList()));
        }
        private static ThrowStatementSyntax ThrowNotSupportedException()
        {
            return ThrowException($"{nameof(System)}.{nameof(NotSupportedException)}");
        }
        private static ThrowStatementSyntax ThrowInvalidCastException()
        {
            return ThrowException($"{nameof(System)}.{nameof(InvalidCastException)}");
        }

        internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string writerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            if (!IsValidType(property, out var possibleTypes, out var resolverType, out var dynamicTypeAttribute))
                return Continuation.NotExecuted;
            
            if (!TryGetResolver(context, resolverType, dynamicTypeAttribute, out var resolver))
            {
                return Continuation.NotExecuted;
            }

            var (tempVariable, tempVariableAccessorName) = Helper.CreateTempIfNeeded(property, statements);

            int typeId = 0;

            var switchSections = new List<SwitchSectionSyntax>();

            var castedName = Helper.GetRandomNameFor("casted", tempVariable.CreateUniqueName());

            foreach (var t in possibleTypes!)
            {
                typeId++;

                if (!Helper.IsAssignable(tempVariable.TypeSymbol, t))
                    continue;

                var newMemberInfo =
                    tempVariable with { Name = castedName, TypeSymbol = t.WithNullableAnnotation(tempVariable.TypeSymbol.NullableAnnotation), Parent = "" };

                var innerSerialize = NoosonGenerator.CreateStatementForSerializing(newMemberInfo, context, writerName, includedSerializers, SerializerMask.DynamicTypeSerializer);


                var invocationExpression
                    = Statement
                        .Expression
                        .Invoke(writerName, "Write", new[] { new ValueArgument(typeId) })
                        .AsStatement();

                var b = BodyGenerator.Create(innerSerialize.ToMergedBlock().Append(SyntaxFactory.BreakStatement()).Prepend(invocationExpression).ToArray());
                switchSections.Add(SyntaxFactory.SwitchSection(
                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                        SyntaxFactory.CasePatternSwitchLabel(
                            SyntaxFactory.DeclarationPattern(
                                SyntaxFactory.ParseTypeName(t.ToDisplayString()),
                                SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(castedName)))
                            , SyntaxFactory.Token(SyntaxKind.ColonToken))),
                    SyntaxFactory.SingletonList<StatementSyntax>(b)
                ));

            }

            SyntaxList<StatementSyntax> defaultCaseStatements;
            
            if (resolver is null)
            {
                defaultCaseStatements = SyntaxFactory.SingletonList<StatementSyntax>(ThrowNotSupportedException());
            }
            else
            {
                var resolverVal = resolver.Value;
                var writeInvalidTypeId = Statement
                        .Expression
                        .Invoke(writerName, "Write", new[] { new ValueArgument(0) })
                        .AsStatement();
                var solveMethod = GetSolveResolve(resolverVal, "Solve", context.WriterTypeName ?? Consts.GenericParameterWriterName);

                var getType = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(tempVariableAccessorName),
                    SyntaxFactory.IdentifierName("GetType")
                ));
                var resolveInfoName = Helper.GetRandomNameFor("serializeResolve");

                var assignment = GetMappedSolveResolve(resolveInfoName, solveMethod, getType);

                var dummySymbol =
                    context.GlobalContext.Compilation.GetTypeByMetadataName(AttributeTemplates.GenSerializationAttribute
                        .FullName);
                var m = new MemberInfo(resolverVal.identifierType, dummySymbol!, "Identifier",resolveInfoName);
                var innerSerialize = NoosonGenerator.CreateStatementForSerializing(m, context, writerName);
                context.GeneratedFile.Usings.Add("System.Runtime.CompilerServices");

                var accessSerializerDelegate = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(resolveInfoName),
                    SyntaxFactory.IdentifierName("Serialize"));
                var delegateType = GetDelegateType(context, property);
                var castedDelegateName = Helper.GetRandomNameFor("serializeCall");
                var castedDelegate = CreateCastedDelegate(castedDelegateName, delegateType, accessSerializerDelegate);

                var suppressNullableCallDelegate = SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.SuppressNullableWarningExpression,
                    SyntaxFactory.IdentifierName(castedDelegateName));
                var callSerialize = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(suppressNullableCallDelegate)
                    .WithArgumentList(SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                                                    {
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(writerName)),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(
                                                                Helper.GetMemberAccessString(property)))
                                                    }))));
                
                defaultCaseStatements = SyntaxFactory.List(
                    innerSerialize.ToMergedBlock()
                        .Prepend(assignment)
                        .Prepend(writeInvalidTypeId)
                        .Append(castedDelegate)
                        .Append(callSerialize)
                        .Append(SyntaxFactory.BreakStatement()));
            }
            
            switchSections.Add(SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()),
                defaultCaseStatements));

            statements.Statements.Add(SyntaxFactory.SwitchStatement(SyntaxFactory.IdentifierName(tempVariableAccessorName))
                .WithSections(new SyntaxList<SwitchSectionSyntax>(
                    switchSections
                )));
            return Continuation.Done;
        }

        private static GenericNameSyntax GetDelegateType(NoosonGeneratorContext context, MemberInfo property)
        {
            bool serialize = context.MethodType == MethodType.Serialize;
            var readerWriterName = serialize
                ? (context.WriterTypeName ?? Consts.GenericParameterWriterName)
                : (context.ReaderTypeName ?? Consts.GenericParameterReaderName);
            return SyntaxFactory.GenericName(serialize ? "Action" : "Func")
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                                                {
                                                    SyntaxFactory.ParseTypeName(readerWriterName),
                                                    SyntaxFactory.ParseTypeName(property.TypeSymbol
                                                        .ToDisplayString())
                                                })));
        }

        static LocalDeclarationStatementSyntax GetMappedSolveResolve(string resolveInfoName, ExpressionSyntax solveResolveMethod, ExpressionSyntax mappingIdentifier)
        {
            return Statement.Declaration.DeclareAndAssign(resolveInfoName,
                SyntaxFactory.InvocationExpression(
                    solveResolveMethod,
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                                                    {
                                                        SyntaxFactory.Argument(mappingIdentifier)
                                                    }))));
        }

        static MemberAccessExpressionSyntax GetSolveResolve((SimpleNameSyntax identifier, SimpleNameSyntax interfaceTypeIdentifier, ITypeSymbol identifierType) resolver, string methodName, string readerWriterTypeName)
        {
            var instanceAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                resolver.identifier,
                SyntaxFactory.IdentifierName("Instance")
            );
            var instanceAccessCasted = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.CastExpression(resolver.interfaceTypeIdentifier!, instanceAccess));
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                instanceAccessCasted,
                SyntaxFactory.GenericName(methodName)
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(readerWriterTypeName))))
            );
        }

        static LocalDeclarationStatementSyntax CreateCastedDelegate(string variableName, TypeSyntax delegateType, ExpressionSyntax accessDelegate)
        {
            var unsafeAs = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Unsafe"),
                SyntaxFactory.GenericName("As")
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList(new[] { delegateType })))
            );
            var unsafeAsCall = SyntaxFactory.InvocationExpression(
                unsafeAs,
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                                                                       {
                                                                           SyntaxFactory.Argument(
                                                                               accessDelegate)
                                                                       })));

            return Statement.Declaration.DeclareAndAssign(variableName, unsafeAsCall);
        }

        private static ITypeSymbol ConstructGenericType(ITypeSymbol type, INamedTypeSymbol? resolverTypeSymbol)
        {
            if (type is ITypeParameterSymbol typeParameterSymbol)
            {
                if (resolverTypeSymbol is null)
                    return type;
                var matchedTypeParam =
                    resolverTypeSymbol.TypeParameters.Select((param, index) => (param, index)).FirstOrDefault(
                        x => SymbolEqualityComparer.Default.Equals(x.param, typeParameterSymbol));
                return resolverTypeSymbol.TypeArguments[matchedTypeParam.index];
            }

            if (type is INamedTypeSymbol { TypeParameters.Length: > 0 } genericTypeParam)
            {
                var resolved = new ITypeSymbol[genericTypeParam.TypeParameters.Length];
                for (int i = 0; i < resolved.Length; i++)
                {
                    resolved[i] = ConstructGenericType(genericTypeParam.TypeArguments[i], resolverTypeSymbol);
                }
                return genericTypeParam.OriginalDefinition.Construct(resolved);
            }

            return type;
        }

        private static bool TryGetResolver(NoosonGeneratorContext context, ITypeSymbol? resolverType, AttributeData? attrData,
            out (SimpleNameSyntax identifier, SimpleNameSyntax interfaceTypeIdentifier, ITypeSymbol identifierType)? resolver)
        {
            resolver = null;
            if (resolverType is not null)
            {
                const string ResolverInterface =
                    "NonSucking.Framework.Serialization.INoosonRuntimeTypeResolver<TTypeIdentifier>";
                var baseInterface = resolverType.OriginalDefinition.AllInterfaces.FirstOrDefault(
                    x => x.OriginalDefinition.ToDisplayString() == ResolverInterface);

                if (baseInterface is null)
                {
                    context.AddDiagnostic(Diagnostics.InvalidDynamicResolver.Format(resolverType, ResolverInterface), attrData?.ApplicationSyntaxReference?.GetLocation() ?? Location.None, DiagnosticSeverity.Error);
                    return false;
                }

                INamedTypeSymbol? resolverTypeSymbol = resolverType as INamedTypeSymbol;

                if (resolverTypeSymbol is not null && resolverTypeSymbol.IsUnboundGenericType)
                {
                    if (resolverTypeSymbol.TypeParameters.Length > 0)
                    {
                        context.AddDiagnostic(Diagnostics.InvalidOpenGenericResolver.Format(resolverType, " without any type parameters"), resolverType,
                            DiagnosticSeverity.Error);
                        return false;
                    }

                    static Continuation CheckAndResolve(NoosonGeneratorContext context, string? typeName, string genericName, ITypeParameterSymbol typeParameter, ITypeSymbol typeArgument, ref ITypeSymbol res)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(typeParameter, typeArgument))
                            return Continuation.Continue;
                        var compilation = context.GlobalContext.Compilation;
                        if (typeName is not null && compilation.GetTypeByMetadataName(typeName) is { } resolvedTypeSymbol)
                        {
                            if (!Helper.IsAssignable(typeArgument, resolvedTypeSymbol))
                            {
                                return Continuation.NotExecuted;
                            }
                            res = resolvedTypeSymbol;
                        }
                        else
                        {
                            res = compilation.CreateErrorTypeSymbol(null, genericName, 0);
                        }

                        return Continuation.Done;
                    }

                    var res = new ITypeSymbol[resolverTypeSymbol.TypeParameters.Length];
                    for (int i = 0; i < res.Length; i++)
                    {
                        var typeParam = resolverTypeSymbol.TypeParameters[i];
                        var matched = CheckAndResolve(context, context.ReaderTypeName, Consts.GenericParameterReaderName,
                            typeParam, baseInterface.TypeArguments[0], ref res[i]);
                        if (matched == Continuation.Continue)
                        {
                            matched = CheckAndResolve(context, context.WriterTypeName, Consts.GenericParameterWriterName,
                                typeParam, baseInterface.TypeArguments[1], ref res[i]);
                        }

                        if (matched != Continuation.Done)
                        {
                            context.AddDiagnostic(Diagnostics.InvalidOpenGenericResolver.Format(resolverType, " and the open parameter could not be resolved"), resolverType,
                                DiagnosticSeverity.Error);
                            return false;
                        }
                    }

                    resolverType = resolverTypeSymbol = resolverTypeSymbol.OriginalDefinition.Construct(res);
                }
                
                var baseInterfaceConstr = (INamedTypeSymbol)ConstructGenericType(baseInterface, resolverTypeSymbol);
                var identifierType = baseInterfaceConstr.TypeArguments.First();

                if (identifierType is ITypeParameterSymbol)
                {
                    context.AddDiagnostic(Diagnostics.InvalidOpenGenericResolver.Format(resolverType, ""), resolverType,
                        DiagnosticSeverity.Error);
                    return false;
                }

                if (!Helper.CheckSingleton(context, resolverType.OriginalDefinition))
                {
                    context.AddDiagnostic(Diagnostics.SingletonImplementationRequired.Format("type resolvers"), resolverType,
                        DiagnosticSeverity.Error);
                    return false;
                }

                var interfaceTypeIdentifier = SyntaxFactory.IdentifierName(baseInterfaceConstr.ToDisplayString());

                var resolverIdentifier = SyntaxFactory.IdentifierName(resolverType.ToDisplayString());

                resolver = (resolverIdentifier, interfaceTypeIdentifier, identifierType);
            }

            return true;
        }

        internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            if (!IsValidType(property, out var possibleTypes, out var resolverType, out var dynamicTypeAttr))
                return Continuation.NotExecuted;
            if (!TryGetResolver(context, resolverType, dynamicTypeAttr, out var resolver))
            {
                return Continuation.NotExecuted;
            }
            var invocationExpression
                = Statement
                    .Expression
                    .Invoke(readerName, "ReadInt32")
                    .AsExpression();
            var typeIdName = Helper.GetRandomNameFor("typeID", property.CreateUniqueName());
            var typeIdIdentifier = SyntaxFactory.IdentifierName(typeIdName);
            statements.Statements.Add(Statement.Declaration.DeclareAndAssign(typeIdName, invocationExpression));

            var propName = property.CreateUniqueName();

            statements.DeclareAndAssign(property, propName, property.TypeSymbol, null);

            int typeId = 0;

            var ifSections = new List<IfStatementSyntax>();

            static ExpressionSyntax CompareTypeId(TypeSyntax identifierType, IdentifierNameSyntax typeIdIdentifier, ExpressionSyntax typeIdLiteral)
            {
                return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.GenericName(
                                        SyntaxFactory.Identifier("EqualityComparer"))
                                    .WithTypeArgumentList(
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                identifierType))),
                                SyntaxFactory.IdentifierName("Default")),
                            SyntaxFactory.IdentifierName("Equals")))
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(typeIdIdentifier),
                            SyntaxFactory.Argument(typeIdLiteral)
                        })));
            }

            var typeIdType = SyntaxFactory.ParseTypeName("int");

            foreach (var t in possibleTypes!)
            {
                typeId++;

                var typeIdLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(typeId));
                if (!Helper.IsAssignable(property.TypeSymbol, t))
                {
                    ifSections.Add(
                        SyntaxFactory.IfStatement(
                            CompareTypeId(typeIdType, typeIdIdentifier, typeIdLiteral),
                        SyntaxFactory.Block(ThrowInvalidCastException())
                        ));
                    continue;
                }

                var newMemberInfo =
                    property with { TypeSymbol = t.WithNullableAnnotation(property.TypeSymbol.NullableAnnotation) };

                var innerDeserialize = NoosonGenerator.CreateStatementForDeserializing(newMemberInfo, context, readerName, includedSerializers, SerializerMask.DynamicTypeSerializer);

                var resValue = innerDeserialize.VariableDeclarations.Single();

                innerDeserialize.Statements.Add(Statement.Declaration.Assign(propName, SyntaxFactory.IdentifierName(resValue.UniqueName)));

                var b = SyntaxFactory.Block(innerDeserialize.ToMergedBlock());
                ifSections.Add(
                    SyntaxFactory.IfStatement(
                        CompareTypeId(typeIdType, typeIdIdentifier, typeIdLiteral),
                        b
                    ));
            }
            
            
            List<StatementSyntax> defaultCaseStatement = new();
            
            if (resolver is null)
            {
                defaultCaseStatement.Add(ThrowNotSupportedException());
            }
            else
            {
                var resolverVal = resolver.Value;
                var resolveMethod = GetSolveResolve(resolverVal, "Resolve", context.ReaderTypeName ?? Consts.GenericParameterReaderName);
                
                var dummySymbol =
                    context.GlobalContext.Compilation.GetTypeByMetadataName(AttributeTemplates.GenSerializationAttribute
                        .FullName);
                
                var resolveInfoName = Helper.GetRandomNameFor("serializeResolve");
                var m = new MemberInfo(resolverVal.identifierType, dummySymbol!, "Identifier",resolveInfoName);
                var innerDeserialize = NoosonGenerator.CreateStatementForDeserializing(m, context, readerName);
                var identifierVariable = innerDeserialize.VariableDeclarations.Single();
                
                var assignment = GetMappedSolveResolve(
                    resolveInfoName,
                    resolveMethod,
                    SyntaxFactory.IdentifierName(identifierVariable.UniqueName));
                
                context.GeneratedFile.Usings.Add("System.Runtime.CompilerServices");

                var accessSerializerDelegate = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(resolveInfoName),
                    SyntaxFactory.IdentifierName("Deserialize"));
                var delegateType = GetDelegateType(context, property);
                var castedDelegateName = Helper.GetRandomNameFor("deserializeCall");
                var castedDelegate = CreateCastedDelegate(castedDelegateName, delegateType, accessSerializerDelegate);
                
                var suppressNullableCallDelegate = SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.SuppressNullableWarningExpression,
                    SyntaxFactory.IdentifierName(castedDelegateName));
                var callDeserialize = SyntaxFactory.InvocationExpression(suppressNullableCallDelegate)
                    .WithArgumentList(SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                                                    {
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(readerName))
                                                    })));
                var setDeserializedObject = Statement.Declaration.Assign(propName, callDeserialize);
                defaultCaseStatement.AddRange(innerDeserialize.ToMergedBlock());
                defaultCaseStatement.Add(assignment);
                defaultCaseStatement.Add(castedDelegate);
                defaultCaseStatement.Add(setDeserializedObject);
            }

            if (ifSections.Count == 0)
            {
                statements.Statements.AddRange(defaultCaseStatement);
            }
            else
            {
                var currentElse = SyntaxFactory.ElseClause(SyntaxFactory.Block(defaultCaseStatement));
                for (int i = ifSections.Count - 1; i >= 1; i--)
                {
                    currentElse = SyntaxFactory.ElseClause(ifSections[i].WithElse(currentElse));
                }
                statements.Statements.Add(ifSections[0].WithElse(currentElse));
            }

            return Continuation.Done;
        }
    }
}