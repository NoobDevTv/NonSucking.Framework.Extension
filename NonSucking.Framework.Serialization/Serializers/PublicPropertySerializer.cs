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

using Accessibility = Microsoft.CodeAnalysis.Accessibility;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(100)]
    internal static class PublicPropertySerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            BaseSerializeInformation? hasBaseSerialize = Helper.GetBaseSerialize(property, context, false);

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
                context.AddDiagnostic(Diagnostics.WriteOnlyPropertyUnsupported,
                    property.TypeSymbol,
                    DiagnosticSeverity.Error
                );
            }

            props = FilterPropsForNotWriteOnly(props).ToList();

            if (hasBaseSerialize is not null)
            {
                if (!hasBaseSerialize.Value.IsAbstract && !hasBaseSerialize.Value.IsVirtual && !hasBaseSerialize.Value.IsOverride)
                {
                    context.AddDiagnostic(Diagnostics.BaseWillBeShadowed, Helper.GetExistingLocationFrom(hasBaseSerialize.Value.Symbol, property.TypeSymbol),
                        DiagnosticSeverity.Warning
                    );
                    var shouldOverride = Helper.GetBaseSerialize(property, context, true);
                    if (shouldOverride is not null && hasBaseSerialize.Value.MethodName == context.MethodName)
                        context.Modifiers.Add(Modifiers.New);
                    else
                        context.Modifiers.Add(Modifiers.Virtual);
                }
                else
                {
                    var shouldOverride = Helper.GetBaseSerialize(property, context, true);
                    if (shouldOverride is not null && hasBaseSerialize.Value.MethodName == context.MethodName)
                        context.Modifiers.Add(Modifiers.Override);
                    else
                        context.Modifiers.Add(Modifiers.Virtual);
                }

                if (!hasBaseSerialize.Value.IsAbstract)//Do not generate when done for a different type
                {
                    statements.Statements.Add(Statement.Expression
                        .Invoke("base", hasBaseSerialize.Value.MethodName, arguments: new[] { ValueArgument.Parse(readerName) })
                        .AsStatement());
                }
            }
            else
            {
                if (!property.TypeSymbol.IsValueType)
                {
                    //TODO: investigate...should not have any influence on output 
                    // if (BaseHasNoosonAttribute(property.TypeSymbol.BaseType))
                    // {
                    //     context.Modifiers.Add(Modifiers.Override);
                    // }
                    // else
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
                    context.MethodName,
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
            var hasBaseDeserialize = GetBaseDeserialize(property, context, false);

            IEnumerable<(MemberInfo memberInfo, int depth)> props
                = Helper.GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
                    .Where(property =>
                        property.memberInfo.Name != "this[]")
                    .Select(x => (memberInfo: x.memberInfo with { Parent = property.Name }, x.depth));

            IEnumerable<IPropertySymbol> writeOnlies = props.Select(x => x.memberInfo.Symbol).OfType<IPropertySymbol>()
                .Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (IPropertySymbol? onlyWrite in writeOnlies)
            {
                context.AddDiagnostic(Diagnostics.WriteOnlyPropertyUnsupported,
                    property.TypeSymbol,
                    DiagnosticSeverity.Error
                );
            }

            props = FilterPropsForNotWriteOnly(props);

            string typeName = context.ReaderTypeName ?? Consts.GenericParameterReaderName;

            if (context.MethodType == MethodType.DeserializeIntoInstance)
                statements.VariableDeclarations.Add(new GeneratedSerializerCode.SerializerVariable(SyntaxFactory.ParseTypeName(property.TypeSymbol.ToDisplayString()), property, Consts.InstanceParameterName, null, false));

            GenerateStatements(
                property,
                context,
                readerName,
                statements,
                new[] { new GeneratedMethodParameter(typeName, readerName, new(), $"The <see cref=\"{context.ReaderTypeName ?? Consts.GenericParameterReaderInterfaceFull}\"/> to deserialize from.") },
                props,
                hasBaseDeserialize);

            try
            {
                Initializer initializer = context.MethodType == MethodType.DeserializeWithCtor ? Initializer.InitializerList : Initializer.Properties;
                string name = context.MethodType == MethodType.DeserializeWithCtor ? property.CreateUniqueName() : Consts.InstanceParameterName;

                GeneratedSerializerCode ctorSyntax = CtorSerializer.CallCtorAndSetProps(
                    (INamedTypeSymbol)property.TypeSymbol,
                    statements, property, name, initializer);
                if (context.MethodType == MethodType.DeserializeWithCtor)
                    statements.VariableDeclarations.Clear();
                statements.MergeWith(ctorSyntax);
            }
            catch (NotSupportedException)
            {
                context.AddDiagnostic(Diagnostics.InstanceCreationImpossible,
                    property.Symbol,
                    DiagnosticSeverity.Error
                );
            }

            return true;
        }

        internal static BaseDeserializeInformation? GetBaseDeserialize(MemberInfo property, NoosonGeneratorContext context, bool compareWithOwnSignature, List<GeneratedMethodParameter>? requiredParameter = null)
        {
            IMethodSymbol? baseDeserializeSymbol = Helper.GetFirstMemberWithBase<IMethodSymbol>(property.TypeSymbol.BaseType,
                   (im) =>
                   {
                       return im.Parameters.Length > 1
                               && (requiredParameter == null
                                   || im.Parameters.Length == requiredParameter.Count)
                               && ((!compareWithOwnSignature
                                    && im.Name == context.GlobalContext
                                        .GetConfigForSymbol(im)
                                        .NameOfStaticDeserializeWithOutParams)
                                   || im.Name == context.GlobalContext.Config
                                       .NameOfStaticDeserializeWithOutParams)
                               && context.MethodType != MethodType.DeserializeSelf
                               && Helper.MatchReaderWriterParameter(context, im.Parameters.First())
                               && im.Parameters.Skip(1)
                                   .All(x => x.RefKind == RefKind.Out)
                               && (requiredParameter is null
                                   || im.Parameters.ForAll(requiredParameter,
                                       (a, b) => a.Type.ToDisplayString() ==
                                                 b.Type));
                   },
                   context);

            BaseDeserializeInformation? hasBaseDeserialize = null;

            (GeneratedMethod? generatedMethod, ITypeSymbol? generatedType) = Helper.GetFirstMemberWithBase(context, property.TypeSymbol.BaseType,
                (m) =>
                {
                    return (m.OverridenName == context.MethodName ||
                                m.Name == Consts.Deserialize)
                            && m.IsStatic
                            && m.Parameters.Count > 1
                            && (requiredParameter is null
                                || m.Parameters.Count == requiredParameter.Count)
                            && m.Parameters.First() is { } readerParam
                            && Helper.MatchReaderWriterParameter(context, readerParam, m)
                            && m.Parameters.Skip(1).All(x => x.IsOut)
                            && (requiredParameter is null
                                || m.Parameters.ForAll(requiredParameter,
                                    (a, b) => a.Type == b.Type));
                });

            hasBaseDeserialize = Helper.GetBaseDeserializeInformation(baseDeserializeSymbol, generatedMethod, generatedType);
            return hasBaseDeserialize;
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

        internal static void GenerateStatements(
            MemberInfo property,
            NoosonGeneratorContext context,
            string readerName,
            GeneratedSerializerCode statements,
            IEnumerable<GeneratedMethodParameter> leadingMethodParameters,
            IEnumerable<(MemberInfo memberInfo, int depth)> props,
            BaseDeserializeInformation? hasBaseDeserialize)
        {
            Dictionary<string, string> scopeVariableNameMappings = new();
            List<MemberInfo> filteredProps = new();
            var propCodes = GetPropCodes(property, context, props, hasBaseDeserialize, scopeVariableNameMappings, filteredProps);

            ITypeSymbol typeSymbol = property.TypeSymbol;
            bool couldGeneratePartial = false;
            if (typeSymbol.DeclaringSyntaxReferences.Length > 0)
            {
                SyntaxReference firstSyntax = typeSymbol.DeclaringSyntaxReferences[0];
                if (firstSyntax.SyntaxTree.GetRoot().FindNode(firstSyntax.Span) is TypeDeclarationSyntax tds
                    && tds.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                {
                    var genFile = GetGeneratedType(context, typeSymbol);
                    GeneratedType genType = genFile.GeneratedTypes.First();
                    GeneratedMethod? genMethod = GetOrCreateGenMethod(
                        property,
                        context,
                        readerName,
                        leadingMethodParameters,
                        hasBaseDeserialize,
                        scopeVariableNameMappings,
                        filteredProps,
                        propCodes,
                        typeSymbol,
                        genType);
                    if (genMethod is not null)
                    {
                        List<ArgumentSyntax> arguments = Helper.GetArgumentsFromGenMethod(
                            readerName,
                            property,
                            statements,
                            genMethod.Parameters.Skip(1).Select(x => x.SerializerVariable!.Value.OriginalMember.Name));
                        Helper.ConvertToStatement(statements, typeSymbol.ToDisplayString(), genMethod.OverridenName, arguments);

                        genFile.Usings.UnionWith(context.GeneratedFile.Usings); // TODO: better workaround, use genFile directly in GetPropCodes
                        couldGeneratePartial = true;
                    }
                }
            }
            if (!couldGeneratePartial)
            {
                GenerateNonPartial();
            }

            void GenerateNonPartial()
            {
                foreach ((bool _, GeneratedSerializerCode propCode) in propCodes)
                {
                    statements.Statements.AddRange(propCode.ToMergedBlock());
                    foreach (var v in propCode.VariableDeclarations)
                    {
                        statements.VariableDeclarations.Add(new GeneratedSerializerCode.SerializerVariable(
                            v.TypeSyntax,
                            v.OriginalMember,
                            v.UniqueName,
                            v.InitialValue,
                            true
                            ));
                    }
                }
                // declarationNames.AddRange(statements.Statements
                //     .OfType<LocalDeclarationStatementSyntax>()
                //     .Concat(
                //         statements.Statements
                //             .OfType<BlockSyntax>()
                //             .SelectMany(x => x.Statements.OfType<LocalDeclarationStatementSyntax>())
                //     )
                //     .SelectMany(declaration => declaration.Declaration.Variables)
                //     .Select(variable => variable.Identifier.Text));
            }
        }





        private static GeneratedMethod? GetOrCreateGenMethod(
            MemberInfo property,
            NoosonGeneratorContext context,
            string readerName,
            IEnumerable<GeneratedMethodParameter> leadingMethodParameters,
            BaseDeserializeInformation? hasBaseDeserialize,
            Dictionary<string, string> scopeVariableNameMappings,
            List<MemberInfo> filteredProps,
            List<(bool assign, GeneratedSerializerCode propCode)> propCodes,
            ITypeSymbol typeSymbol,
            GeneratedType genType)
        {
            var genMethodName = context.GlobalContext.Config.NameOfStaticDeserializeWithOutParams;
            GeneratedMethod? genMethod =
                genType.Methods.FirstOrDefault(x => x.OverridenName == genMethodName
                                                    && x.Parameters.Count > 0 
                                                    && Helper.MatchReaderWriterParameter(context, x.Parameters.First(), x)  /*TODO: && x.Typestuff*/);
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
                                    new TypeParameterConstraint($"{context.GlobalContext.Config.GeneratedNamespace}.IBinaryReader"))
                    }
                    : Array.Empty<TypeParameterConstraintClause>();

                Accessibility minimumApplicableModifier = Accessibility.Public;

                static Accessibility GetMinimumAccessibility(Accessibility a, Accessibility b)
                {
                    return (Accessibility)Math.Min((int)a, (int)b);
                }

                void ApplyApplicableModifier(Accessibility accessibility)
                {
                    minimumApplicableModifier = GetMinimumAccessibility(accessibility, minimumApplicableModifier);
                }

                genMethod = new(
                    null,
                    Consts.Deserialize,
                    genMethodName,
                    new(),
                    new() { Modifiers.Static },
                    typeParams,
                    typeConstraints,
                    new(),
                    $"Deserializes the properties of a <see cref=\"{typeSymbol.ToSummaryName()}\"/> type.");

                if (hasBaseDeserialize is not null)
                {
                    string?[] redirects = GetRedirects(context, hasBaseDeserialize.Value, scopeVariableNameMappings, filteredProps, propCodes, out _);

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
                        SyntaxFactory.IdentifierName(hasBaseDeserialize.Value.methodName)), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(localArguments)));

                    genMethod.Body.Statements.Add(SyntaxFactory.ExpressionStatement(invocation));
                }


                foreach (var item in leadingMethodParameters)
                {
                    genMethod.Parameters.Add(item);
                }

                static Accessibility GetCommonAccessibilityEnum(IEnumerable<ITypeSymbol> typeSymbols)
                {
                    Accessibility accessibility = Accessibility.Public;
                    foreach (var t in typeSymbols)
                    {
                        accessibility = GetMinimumAccessibility(accessibility, GetCommonAccessibility(t));
                    }

                    return accessibility;
                }
                static Accessibility GetCommonAccessibility(ITypeSymbol typeSymbol)
                {
                    if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                        return GetCommonAccessibility(arrayTypeSymbol.ElementType);
                    if (typeSymbol is ITypeParameterSymbol typeParamSymbol)
                        return GetCommonAccessibilityEnum(typeParamSymbol.ConstraintTypes);
                    if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                        return GetMinimumAccessibility(typeSymbol.DeclaredAccessibility,
                            GetCommonAccessibilityEnum(namedTypeSymbol.TypeParameters));
                    return typeSymbol.DeclaredAccessibility;
                }

                foreach ((bool assign, GeneratedSerializerCode propCode) in propCodes)
                {
                    GeneratedSerializerCode.SerializerVariable variable = propCode.VariableDeclarations.Last();
                    string outVarName = variable.UniqueName; // + "_out";
                    if (!SymbolEqualityComparer.Default.Equals(typeSymbol, variable.OriginalMember.TypeSymbol))
                        ApplyApplicableModifier(GetCommonAccessibility(variable.OriginalMember.TypeSymbol));
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

                static void FromAccessibility(List<Modifiers> modifiers, Accessibility accessibility)
                {
                    switch (accessibility)
                    {
                        case Accessibility.Private:
                            modifiers.Insert(0, Modifiers.Private);
                            break;
                        case Accessibility.ProtectedAndInternal:
                            modifiers.Insert(0, Modifiers.Protected);
                            modifiers.Insert(1, Modifiers.Internal);
                            break;
                        case Accessibility.Protected:
                            modifiers.Insert(0, Modifiers.Protected);
                            break;
                        case Accessibility.Internal:
                            modifiers.Insert(0, Modifiers.Internal);
                            break;
                        case Accessibility.Public:
                            modifiers.Insert(0, Modifiers.Public);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null);
                    }
                }
                FromAccessibility(genMethod.Modifier, minimumApplicableModifier);
                if (genMethod.Parameters.Count == 1)
                    genMethod = null;
                if (genMethod is not null)
                {
                    var anyMethodSameName = GetBaseDeserialize(property, context, true, genMethod.Parameters);

                    if (anyMethodSameName is not null)
                    {
                        genMethod.Modifier.Add(Modifiers.New);
                    }
                }
                if (genMethod is not null)
                    genType.Methods.Add(genMethod);
            }

            return genMethod;
        }

        private static string?[] GetRedirects(
            NoosonGeneratorContext context,
            BaseDeserializeInformation hasBaseDeserialize,
            Dictionary<string, string> scopeVariableNameMappings,
            List<MemberInfo> filteredProps,
            List<(bool assign, GeneratedSerializerCode propCode)> propCodes,
            out int notMatched)
        {
            notMatched = filteredProps.Count;
            var redirects = new string?[hasBaseDeserialize.Parameters.Count];

            Helper.GetFirstMemberWithBase(context, hasBaseDeserialize.Symbol?.ContainingType, (m) => { return false; });
            int insertedIndex = 0;
            for (var index = 0; index < hasBaseDeserialize.Parameters.Count; index++)
            {
                var (localTypeName, parameterName) = hasBaseDeserialize.Parameters[index];

                for (int filterPropIndex = 0; filterPropIndex < filteredProps.Count; filterPropIndex++)
                {
                    var mi = filteredProps[filterPropIndex];
                    if (mi.TypeSymbol.ToDisplayString() == localTypeName &&
                        Helper.MatchIdentifierWithPropName(mi.Name, parameterName))
                    {
                        var uniqueName = redirects[index] = scopeVariableNameMappings[mi.Symbol.Name] = mi.CreateUniqueName();

                        var code = new GeneratedSerializerCode();
                        code.VariableDeclarations.Add(new GeneratedSerializerCode.SerializerVariable(SyntaxFactory.ParseTypeName(localTypeName), mi, uniqueName, null, false));
                        propCodes.Insert(insertedIndex++, (false, code));
                        if (filterPropIndex == index)
                            notMatched--;
                        break;
                    }
                }
            }

            return redirects;
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
                    typeSymbol.IsAbstract,
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
            BaseDeserializeInformation? hasBaseDeserialize,
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

    internal record struct BaseDeserializeInformation(List<(string typeName, string parameterName)> Parameters, string typeName, string methodName, IMethodSymbol? Symbol)
    {
        public static implicit operator (List<(string typeName, string parameterName)> Parameters, string typeName, string methodName, IMethodSymbol? Symbol)(BaseDeserializeInformation value)
        {
            return (value.Parameters, value.typeName, value.methodName, value.Symbol);
        }

        public static implicit operator BaseDeserializeInformation((List<(string typeName, string parameterName)> Parameters, string typeName, string methodName, IMethodSymbol? Symbol) value)
        {
            return new BaseDeserializeInformation(value.Parameters, value.typeName, value.methodName, value.Symbol);
        }
    }

    internal record struct BaseSerializeInformation(bool IsVirtual, bool IsAbstract, bool IsOverride, string MethodName, IMethodSymbol? Symbol)
    {
        public static implicit operator (bool IsVirtual, bool IsAbstract, bool IsOverride, string MethodName, IMethodSymbol? Symbol)(BaseSerializeInformation value)
        {
            return (value.IsVirtual, value.IsAbstract, value.IsOverride, value.MethodName, value.Symbol);
        }

        public static implicit operator BaseSerializeInformation((bool IsVirtual, bool IsAbstract, bool IsOverride, string MethodName, IMethodSymbol? Symbol) value)
        {
            return new BaseSerializeInformation(value.IsVirtual, value.IsAbstract, value.IsOverride, value.MethodName, value.Symbol);
        }
    }
}