using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NonSucking.Framework.Serialization.Serializers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using VaVare;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Models;
using VaVare.Statements;

using static NonSucking.Framework.Serialization.NoosonGenerator;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(100)]
    internal static class PublicPropertySerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            IMethodSymbol? hasBaseSerializeSymbol = Helper.GetFirstMemberWithBase(property.TypeSymbol.BaseType,
                    (s) => s is IMethodSymbol im
                           && im.Name == Consts.Serialize
                           && Helper.CheckSignature(context, im, "IBinaryWriter"))
                as IMethodSymbol;
            (bool IsVirtual, bool IsAbstract, IMethodSymbol? Symbol)? hasBaseSerialize = null;
            if (hasBaseSerializeSymbol is null)
            {
                (GeneratedMethod? generatedMethod, GeneratedType? _) = Helper.GetFirstMemberWithBase(context.GlobalContext, property.TypeSymbol.BaseType,
                    (m) => m.Name == Consts.Serialize
                           && !m.IsStatic
                           && m.Parameters.Count == 1);
                if (generatedMethod is not null)
                    hasBaseSerialize = (generatedMethod.IsVirtual, generatedMethod.IsAbstract, null);
            }
            else
            {
                hasBaseSerialize = (hasBaseSerializeSymbol.IsVirtual, hasBaseSerializeSymbol.IsAbstract, hasBaseSerializeSymbol);
            }

            var props
                = Helper.GetMembersWithBase(property.TypeSymbol,
                        hasBaseSerialize is null ? baseTypesLevelProperties : 0)
                    .Where(property =>
                        property.memberInfo.Name != "this[]")
                    .Select(x => (memberInfo: x.memberInfo with { Parent = property.FullName }, x.depth));

            IEnumerable<IPropertySymbol> writeOnlies = props
                .Select(x => x.memberInfo.Symbol).OfType<IPropertySymbol>()
                .Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (IPropertySymbol? onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                    "",
                    "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                    property.TypeSymbol,
                    DiagnosticSeverity.Error
                );
            }

            props = FilterPropsForNotWriteOnly(props).ToList();

            if (hasBaseSerialize is not null)
            {
                if (!hasBaseSerialize.Value.IsAbstract && !hasBaseSerialize.Value.IsVirtual)
                {
                    context.AddDiagnostic("0014",
                        "",
                        "Base Serialize is neither virtual nor abstract and therefore a shadow serialize will be implemented, which might not be wanted. Please consult your doctor or apotheker.",
                        NoosonGeneratorContext.GetExistingFrom(hasBaseSerialize.Value.Symbol, property.TypeSymbol),
                        DiagnosticSeverity.Warning
                    );
                    context.Modifiers.Add(Modifiers.New);
                }
                else
                {
                    context.Modifiers.Add(Modifiers.Override);
                }

                if (!hasBaseSerialize.Value.IsAbstract)
                {
                    statements.Statements.Add(Statement.Expression
                        .Invoke("base", Consts.Serialize, arguments: new[] { ValueArgument.Parse(readerName) })
                        .AsStatement());
                }
            }
            else
            {
                if (!property.TypeSymbol.IsValueType)
                {
                    if (BaseHasNoosonAttribute(property.TypeSymbol.BaseType))
                    {
                        context.Modifiers.Add(Modifiers.Override);
                    }
                    else
                    {
                        context.Modifiers.Add(Modifiers.Virtual);
                    }
                }
            }

            Dictionary<string, string> scopeVariableNameMappings = new();

            foreach ((MemberInfo prop, int _) in OrderProps(props))
            {
                prop.ScopeVariableNameMappings = scopeVariableNameMappings;
                scopeVariableNameMappings[prop.Name] = Helper.GetMemberAccessString(prop);
                NoosonGeneratorContext newContext = new(context.GlobalContext,
                    context.GeneratorContext,
                    context.GeneratedFile,
                    context.DefaultGeneratedType,
                    context.ReaderWriterName,
                    prop.Symbol,
                    context.UseAdvancedTypes,
                    MethodType.Serialize,
                    context.WriterTypeName,
                    context.ReaderTypeName);

                GeneratedSerializerCode? propCode = GenerateStatementsForMember(prop, newContext, MethodType.Serialize);
                if (propCode is null)
                {
                    continue;
                }

                statements.Statements.AddRange(propCode.ToMergedBlock());
            }

            return true;
        }

        internal static bool TryDeserialize(
            MemberInfo property,
            NoosonGeneratorContext context,
            string readerName,
            GeneratedSerializerCode statements,
            SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            IMethodSymbol? hasBaseDeserializeSymbol = Helper.GetFirstMemberWithBase(property.TypeSymbol.BaseType,
                   (s) => s is IMethodSymbol im
                          && im.Name == Consts.Deserialize
                          && im.Parameters.Skip(1).All(x => x.RefKind == RefKind.Out))
               as IMethodSymbol;

            (List<(string typeName, string parameterName)> Parameters, string typeName, IMethodSymbol? Symbol)? hasBaseDeserialize = null;
            if (hasBaseDeserializeSymbol is null)
            {
                (GeneratedMethod? generatedMethod, GeneratedType? generatedType) = Helper.GetFirstMemberWithBase(context.GlobalContext, property.TypeSymbol.BaseType,
                    (m) => m.Name == Consts.Deserialize
                           && m.IsStatic
                           && m.Parameters.Count > 1
                           && m.Parameters.Skip(1).All(x => x.IsOut));
                if (generatedMethod is not null && generatedType is not null)
                {
                    hasBaseDeserialize = (generatedMethod.Parameters.Skip(1).Select(x => (x.Type, x.SerializerVariable!.Value.OriginalMember.Name)).ToList(), generatedType.DisplayName, null);
                }
            }
            else
            {
                hasBaseDeserialize = (hasBaseDeserializeSymbol.Parameters.Skip(1).Select(x => (x.Type.ToDisplayString(), x.Name)).ToList(), hasBaseDeserializeSymbol.ContainingType.ToDisplayString(), hasBaseDeserializeSymbol);
            }

            IEnumerable<(MemberInfo memberInfo, int depth)> props
                = Helper.GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
                    .Where(property =>
                        property.memberInfo.Name != "this[]")
                    .Select(x => (memberInfo: x.memberInfo with { Parent = property.Name }, x.depth));

            IEnumerable<IPropertySymbol> writeOnlies = props.Select(x => x.memberInfo.Symbol).OfType<IPropertySymbol>()
                .Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (IPropertySymbol? onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                    "",
                    "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                    property.TypeSymbol,
                    DiagnosticSeverity.Error
                );
            }

            props = FilterPropsForNotWriteOnly(props);

            string typeName = context.ReaderTypeName ?? Consts.GenericParameterReaderName;

            if (context.MethodType == MethodType.DeserializeIntoInstance)
                statements.VariableDeclarations.Add(new GeneratedSerializerCode.SerializerVariable(SyntaxFactory.ParseTypeName(property.TypeSymbol.ToDisplayString()), property, Consts.InstanceParameterName, null));

            var declerationNames = GenerateStatements(
                property,
                context,
                readerName,
                statements,
                new[] { new GeneratedMethodParameter(typeName, readerName, new(), $"The <see cref=\"{context.ReaderTypeName}\"/> to deserialize from.") },
                props,
                hasBaseDeserialize);

            try
            {
                Initializer initializer = context.MethodType == MethodType.DeserializeWithCtor ? Initializer.InitializerList : Initializer.Properties;
                string name = context.MethodType == MethodType.DeserializeWithCtor ? property.CreateUniqueName() : Consts.InstanceParameterName;

                GeneratedSerializerCode ctorSyntax = CtorSerializer.CallCtorAndSetProps(
                    (INamedTypeSymbol)property.TypeSymbol,
                    declerationNames, property, name, initializer);
                statements.MergeWith(ctorSyntax);
            }
            catch (NotSupportedException)
            {
                context.AddDiagnostic("0006",
                    "",
                    "No instance could be created with the constructors in this type. Add a custom ctor call, property mapping or a ctor with matching arguments.",
                    property.Symbol,
                    DiagnosticSeverity.Error
                );
            }

            return true;
        }

        private static IEnumerable<(MemberInfo memberInfo, int depth)> FilterPropsForNotWriteOnly(IEnumerable<(MemberInfo memberInfo, int)> props)
        {
            props = props.Where(x =>
            {
                if (x.memberInfo.Symbol is IPropertySymbol ps
                    && !ps.IsWriteOnly
                    && ps.GetMethod is not null)
                {
                    return true;
                }
                else if (x.memberInfo.Symbol is IFieldSymbol fs)
                {
                    return true;
                }

                return false;
            });
            return props;
        }

        private static IEnumerable<(MemberInfo memberInfo, int depth)> OrderProps(IEnumerable<(MemberInfo memberInfo, int depth)> props)
        {
            return props.OrderBy(x =>
            {
                AttributeData? attr = x.memberInfo.Symbol.GetAttribute(AttributeTemplates.Order);
                return attr == null ? int.MaxValue : (int)attr.ConstructorArguments[0].Value!;
            }).ThenByDescending(x => x.depth);
        }

        private static List<string> GenerateStatements(
            MemberInfo property,
            NoosonGeneratorContext context,
            string readerName,
            GeneratedSerializerCode statements,
            IEnumerable<GeneratedMethodParameter> leadingMethodParameters,
            IEnumerable<(MemberInfo memberInfo, int depth)> props,
            (List<(string typeName, string parameterName)> Parameters, string typeName, IMethodSymbol? Symbol)? hasBaseDeserialize)
        {
            Dictionary<string, string> scopeVariableNameMappings = new();
            List<string> declerationNames = new();
            List<MemberInfo> filteredProps = new();
            var propCodes = GetPropCodes(property, context, props, hasBaseDeserialize, scopeVariableNameMappings, filteredProps);

            ITypeSymbol typeSymbol = property.TypeSymbol;
            if (typeSymbol.DeclaringSyntaxReferences.Length > 0 && propCodes.Count > 0)
            {
                SyntaxReference firstSyntax = typeSymbol.DeclaringSyntaxReferences[0];
                if (firstSyntax.SyntaxTree.GetRoot().FindNode(firstSyntax.Span) is TypeDeclarationSyntax tds
                    && tds.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                {
                    var genFile = GetGeneratedType(context, typeSymbol);
                    GeneratedType genType = genFile.GeneratedTypes.First();
                    GeneratedMethod? genMethod = GetOrCreateGenMethod(
                        context,
                        readerName,
                        leadingMethodParameters,
                        hasBaseDeserialize,
                        scopeVariableNameMappings,
                        filteredProps,
                        propCodes,
                        typeSymbol,
                        genType);
                    List<ArgumentSyntax> arguments = GetArgumentsFromGenMethod(readerName, declerationNames, genMethod);
                    ConvertToStatement(statements, typeSymbol, genMethod, arguments);
                }
                else
                {
                    GenerateNonPartial();
                }
            }
            else
            {
                GenerateNonPartial();
            }

            void GenerateNonPartial()
            {
                foreach ((bool _, GeneratedSerializerCode propCode) in propCodes)
                {
                    statements.Statements.AddRange(propCode.ToMergedBlock());
                }
                declerationNames.AddRange(statements.Statements
                    .OfType<LocalDeclarationStatementSyntax>()
                    .Concat(
                        statements.Statements
                            .OfType<BlockSyntax>()
                            .SelectMany(x => x.Statements.OfType<LocalDeclarationStatementSyntax>())
                    )
                    .SelectMany(declaration => declaration.Declaration.Variables)
                    .Select(variable => variable.Identifier.Text));
            }
            return declerationNames;
        }

        private static void ConvertToStatement(
            GeneratedSerializerCode statements,
            ITypeSymbol typeSymbol,
            GeneratedMethod genMethod,
            List<ArgumentSyntax> arguments)
        {
            MemberAccessExpressionSyntax memberAccess = SyntaxFactory
                .MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(typeSymbol.ToDisplayString()),
                    SyntaxFactory.IdentifierName(genMethod.Name));

            statements.Statements.Add(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(memberAccess,
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(arguments)))));
        }

        private static List<ArgumentSyntax> GetArgumentsFromGenMethod(
            string readerName,
            List<string> declerationNames,
            GeneratedMethod genMethod)
        {
            List<ArgumentSyntax> arguments = new()
                        {SyntaxFactory.Argument(SyntaxFactory.IdentifierName(readerName))};
            foreach (var item in genMethod.Parameters.Skip(1))
            {
                string variableName = item.Name;
                declerationNames.Add(variableName);
                arguments.Add(SyntaxFactory
                    .Argument(null,
                        SyntaxFactory.Token(SyntaxKind.OutKeyword),
                        SyntaxFactory.DeclarationExpression(
                            SyntaxFactory.IdentifierName(
                                SyntaxFactory.Identifier(
                                    SyntaxFactory.TriviaList(),
                                    SyntaxKind.VarKeyword,
                                    "var",
                                    "var",
                                    SyntaxFactory.TriviaList())),
                            SyntaxFactory.SingleVariableDesignation(
                                SyntaxFactory.Identifier(variableName)))));
            }

            return arguments;
        }

        private static GeneratedMethod GetOrCreateGenMethod(
            NoosonGeneratorContext context,
            string readerName,
            IEnumerable<GeneratedMethodParameter> leadingMethodParameters,
            (List<(string typeName, string parameterName)> Parameters, string typeName, IMethodSymbol? Symbol)? hasBaseDeserialize,
            Dictionary<string, string> scopeVariableNameMappings,
            List<MemberInfo> filteredProps,
            List<(bool assign, GeneratedSerializerCode propCode)> propCodes,
            ITypeSymbol typeSymbol,
            GeneratedType genType)
        {
            GeneratedMethod? genMethod =
                genType.Methods.FirstOrDefault(x => x.Name == Consts.Deserialize /*&& TODO: x.Typestuff*/);
            bool genMethodExisted = genMethod is not null;
            if (genMethod is null)
            {
                var generateGeneric = context.ReaderTypeName is null;
                var typeParams = generateGeneric
                    ? new[]
                    {
                                new TypeParameter(Consts.GenericParameterReaderName,
                                    xmlDocumentation: "The type of the instance to deserialize.")
                    }
                    : Array.Empty<TypeParameter>();
                var typeConstraints = generateGeneric
                    ? new[]
                    {
                                new TypeParameterConstraintClause(Consts.GenericParameterReaderName,
                                    new TypeParameterConstraint("NonSucking.Framework.Serialization.IBinaryReader"))
                    }
                    : Array.Empty<TypeParameterConstraintClause>();

                genMethod = new(
                    null,
                    Consts.Deserialize,
                    new(),
                    new() { Modifiers.Public, Modifiers.Static },
                    typeParams,
                    typeConstraints,
                    new(),
                    $"Deserializes the properties of a <see cref=\"{typeSymbol.ToSummaryName()}\"/> type.");
                genType.Methods.Add(genMethod);


                if (hasBaseDeserialize is not null)
                {
                    var redirects = new string?[hasBaseDeserialize.Value.Parameters.Count];
                    int insertedIndex = 0;
                    for (var index = 0; index < hasBaseDeserialize.Value.Parameters.Count; index++)
                    {
                        var (localTypeName, parameterName) = hasBaseDeserialize.Value.Parameters[index];
                        foreach (var mi in filteredProps)
                        {
                            if (mi.TypeSymbol.ToDisplayString() == localTypeName &&
                                Helper.MatchIdentifierWithPropName(mi.Name, parameterName))
                            {
                                var uniqueName = redirects[index] = scopeVariableNameMappings[mi.Symbol.Name] = mi.CreateUniqueName();

                                var code = new GeneratedSerializerCode();
                                code.VariableDeclarations.Add(new GeneratedSerializerCode.SerializerVariable(SyntaxFactory.ParseTypeName(localTypeName), mi, uniqueName, null));
                                propCodes.Insert(insertedIndex++, (false, code));
                                break;
                            }
                        }
                    }
                    List<ArgumentSyntax> localArguments = new()
                        {SyntaxFactory.Argument(SyntaxFactory.IdentifierName(readerName))};

                    localArguments.AddRange(
                        redirects
                        .Select(x =>
                            SyntaxFactory
                                .Argument(SyntaxFactory.IdentifierName(x ?? "_"))
                                .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))));

                    var invocation = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(hasBaseDeserialize.Value.typeName),
                        SyntaxFactory.IdentifierName(Consts.Deserialize)), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(localArguments)));

                    genMethod.Body.Statements.Add(SyntaxFactory.ExpressionStatement(invocation));
                }


                foreach (var item in leadingMethodParameters)
                {
                    genMethod.Parameters.Add(item);
                }

                foreach ((bool assign, GeneratedSerializerCode propCode) in propCodes)
                {
                    GeneratedSerializerCode.SerializerVariable variable = propCode.VariableDeclarations[0];
                    string outVarName = variable.UniqueName; // + "_out";
                    genMethod.Parameters.Add(new GeneratedMethodParameter(
                        variable.TypeSyntax.ToFullString(),
                        outVarName,
                        new() { ParameterModifiers.Out },
                        $"The deserialized instance of the property <see cref=\"{variable.OriginalMember.Symbol.ToSummaryName()}\"/>.",
                        variable));

                    ExpressionSyntax valueToAssign = variable.InitialValue?.Value
                                                     ?? variable.Declaration.Declaration.Variables
                                                         .FirstOrDefault()
                                                         ?.Initializer?.Value
                                                     ?? variable.CreateDefaultValue().Value;
                    if (assign)
                    {
                        genMethod.Body.Statements.Add(Statement.Declaration.Assign(outVarName, valueToAssign));

                    }
                    genMethod.Body.Statements.AddRange(propCode.Statements);
                }
            }

            return genMethod;
        }

        private static GeneratedFile GetGeneratedType(NoosonGeneratorContext context, ITypeSymbol typeSymbol)
        {
            bool typeExisted = context.GlobalContext.TryResolve(typeSymbol, out var genFile);
            if (!typeExisted)
            {
                var genTypes = new List<GeneratedType>();
                genFile = new(typeSymbol.ContainingNamespace.Name, typeSymbol.CanBeReferencedByName ? typeSymbol.Name : typeSymbol.ToDisplayString(), genTypes, new());
                genTypes.Add(new GeneratedType(genFile.Name,
                    typeSymbol.ToDisplayString(),
                    typeSymbol.IsRecord,
                    typeSymbol.IsValueType,
                    CreateTypeParameters(typeSymbol),
                    Array.Empty<TypeParameterConstraintClause>(),
                    new(),
                    new() { Modifiers.Partial },
                    null,
                    context.GlobalContext.Resolve(typeSymbol.ContainingType)?.GeneratedTypes.FirstOrDefault()));

                context.GlobalContext.Add(typeSymbol, genFile);
            }

            return genFile;
        }

        private static List<(bool assign, GeneratedSerializerCode propCode)> GetPropCodes(
            MemberInfo property,
            NoosonGeneratorContext context,
            IEnumerable<(MemberInfo memberInfo, int depth)> props,
            (List<(string typeName, string parameterName)> Parameters, string typeName, IMethodSymbol? Symbol)? hasBaseDeserialize,
            Dictionary<string, string> scopeVariableNameMappings,
            List<MemberInfo> filteredProps)
        {
            List<(bool assign, GeneratedSerializerCode propCode)> propCodes = new();
            foreach ((MemberInfo prop, int depth) in OrderProps(props))
            {
                if (hasBaseDeserialize is not null
                    && !SymbolEqualityComparer.Default.Equals(prop.Symbol.ContainingType, property.TypeSymbol))
                {
                    filteredProps.Add(prop);
                    continue;
                }

                prop.ScopeVariableNameMappings = scopeVariableNameMappings;
                GeneratedSerializerCode? propCode = GenerateStatementsForMember(prop, context with { MethodType = MethodType.DeserializeWithCtor }, MethodType.DeserializeWithCtor);

                if (propCode is null)
                {
                    continue;
                }

                foreach (GeneratedSerializerCode.SerializerVariable item in propCode.VariableDeclarations)
                {
                    scopeVariableNameMappings[item.OriginalMember.Name] = item.UniqueName;
                }

                propCodes.Add((true, propCode));
            }

            return propCodes;
        }
    }
}