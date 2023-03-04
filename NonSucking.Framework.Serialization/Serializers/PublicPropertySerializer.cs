using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NonSucking.Framework.Serialization.Serializers;

using System;
using System.Collections.Generic;
using System.Linq;

using VaVare;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Models;
using VaVare.Statements;

using static NonSucking.Framework.Serialization.NoosonGenerator;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(70)]
    internal static class PublicPropertySerializer
    {
        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            IMethodSymbol? hasBaseSerialize = Helper.GetFirstMemberWithBase(property.TypeSymbol.BaseType,
                    (s) => s is IMethodSymbol im
                           && im.Name == "Serialize"
                           && Helper.CheckSignature(context, im, "IBinaryWriter"))
                as IMethodSymbol;

            IEnumerable<MemberInfo> props
                = Helper.GetMembersWithBase(property.TypeSymbol,
                        hasBaseSerialize is null ? baseTypesLevelProperties : 0)
                    .Where(property =>
                        property.Name != "this[]")
                    .Select(x => x with { Parent = property.FullName });

            IEnumerable<IPropertySymbol> writeOnlies = props
                .Select(x => x.Symbol).OfType<IPropertySymbol>()
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
                if (!hasBaseSerialize.IsAbstract && !hasBaseSerialize.IsVirtual)
                {
                    context.AddDiagnostic("0014",
                        "",
                        "Base Serialize is neither virtual nor abstract and therefore a shadow serialize will be implemented, which might not be wanted. Please consult your doctor or apotheker.",
                        NoosonGeneratorContext.GetExistingFrom(hasBaseSerialize, property.TypeSymbol),
                        DiagnosticSeverity.Warning
                    );
                    context.Modifiers.Add(Modifiers.New);
                }
                else
                {
                    context.Modifiers.Add(Modifiers.Override);
                }

                if (!hasBaseSerialize.IsAbstract)
                {
                    statements.Statements.Add(Statement.Expression
                        .Invoke("base", "Serialize", arguments: new[] { ValueArgument.Parse(readerName) })
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

            foreach (MemberInfo prop in OrderProps(props))
            {
                prop.ScopeVariableNameMappings = scopeVariableNameMappings;
                scopeVariableNameMappings[prop.Name] = Helper.GetMemberAccessString(prop);
                NoosonGeneratorContext newContext = new(context.GlobalContext,
                    context.GeneratorContext,
                    context.GeneratedType,
                    context.ReaderWriterName,
                    prop.Symbol,
                    context.UseAdvancedTypes,
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

        private static IEnumerable<MemberInfo> OrderProps(IEnumerable<MemberInfo> props)
        {
            return props.OrderBy(x =>
            {
                AttributeData? attr = x.Symbol.GetAttribute(AttributeTemplates.Order);
                return attr == null ? int.MaxValue : (int)attr.ConstructorArguments[0].Value!;
            });
        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            IEnumerable<MemberInfo> props
                = Helper.GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
                    .Where(property =>
                        property.Name != "this[]")
                    .Select(x => x with { Parent = property.Name });

            IEnumerable<IPropertySymbol> writeOnlies = props.Select(x => x.Symbol).OfType<IPropertySymbol>()
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

            Dictionary<string, string> scopeVariableNameMappings = new();
            List<GeneratedSerializerCode> propCodes = new();
            List<string> declerationNames = new();
            foreach (MemberInfo prop in OrderProps(props))
            {
                prop.ScopeVariableNameMappings = scopeVariableNameMappings;
                GeneratedSerializerCode? propCode = GenerateStatementsForMember(prop, context, MethodType.Deserialize);

                if (propCode is null)
                {
                    continue;
                }

                foreach (GeneratedSerializerCode.SerializerVariable item in propCode.VariableDeclarations)
                {
                    scopeVariableNameMappings[item.OriginalMember.Name] = item.UniqueName;
                }

                propCodes.Add(propCode);
            }

            GenerateStatements(property, context, readerName, statements, propCodes, declerationNames);

            try
            {
                GeneratedSerializerCode ctorSyntax = CtorSerializer.CallCtorAndSetProps(
                    (INamedTypeSymbol)property.TypeSymbol,
                    declerationNames, property, property.CreateUniqueName());
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

        private static void GenerateStatements(MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, List<GeneratedSerializerCode> propCodes, List<string> declerationNames)
        {
            ITypeSymbol typeSymbol = property.TypeSymbol;
            if (typeSymbol.DeclaringSyntaxReferences.Length > 0 && propCodes.Count > 0)
            {
                SyntaxReference firstSyntax = typeSymbol.DeclaringSyntaxReferences[0];
                if (firstSyntax.SyntaxTree.GetRoot().FindNode(firstSyntax.Span) is TypeDeclarationSyntax tds
                    && tds.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                {
                    bool typeExisted = context.GlobalContext.TryResolve(typeSymbol, out GeneratedType? genType);
                    if (!typeExisted)
                    {
                        genType = new GeneratedType(typeSymbol.ContainingNamespace.Name,
                            typeSymbol.CanBeReferencedByName ? typeSymbol.Name : typeSymbol.ToDisplayString(),
                            typeSymbol.ToDisplayString(),
                            typeSymbol.IsRecord,
                            typeSymbol.IsValueType,
                            CreateTypeParameters(typeSymbol),
                            Array.Empty<TypeParameterConstraintClause>(),
                            new(),
                            new(),
                            null,
                            context.GlobalContext.Resolve(typeSymbol.ContainingType));

                        context.GlobalContext.Add(typeSymbol, genType);
                    }

                    GeneratedMethod? genMethod =
                        genType.Methods.FirstOrDefault(x => x.Name == "Deserialize" /*&& x.Typestuff*/);
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
                            "Deserialize",
                            new(),
                            new() { Modifiers.Public, Modifiers.Static },
                            typeParams,
                            typeConstraints,
                            new(),
                            $"Deserializes the properties of a <see cref=\"{typeSymbol.ToSummaryName()}\"/> type.");
                        genType.Methods.Add(genMethod);

                        string typeName = context.ReaderTypeName ?? Consts.GenericParameterReaderName;
                        genMethod.Parameters.Add(new GeneratedMethodParameter(typeName, readerName, new(), null));
                        foreach (GeneratedSerializerCode item in propCodes)
                        {
                            GeneratedSerializerCode.SerializerVariable variable = item.VariableDeclarations[0];
                            string outVarName = variable.UniqueName; // + "_out";
                            genMethod.Parameters.Add(new GeneratedMethodParameter(
                                variable.TypeSyntax.ToFullString(),
                                outVarName,
                                new() { ParameterModifiers.Out },
                                $"The deserialized instance of the property <see cref=\"{variable.OriginalMember.Name}\"/>."));

                            ExpressionSyntax valueToAssign = variable.InitialValue?.Value
                                                             ?? variable.Declaration.Declaration.Variables
                                                                 .FirstOrDefault()
                                                                 ?.Initializer?.Value
                                                             ?? variable.CreateDefaultValue().Value;

                            genMethod.Body.Statements.Add(Statement.Declaration.Assign(outVarName, valueToAssign));
                            genMethod.Body.Statements.AddRange(item.Statements);
                        }
                    }

                    List<ArgumentSyntax> arguments = new()
                        {SyntaxFactory.Argument(SyntaxFactory.IdentifierName(readerName))};
                    foreach (GeneratedSerializerCode item in propCodes)
                    {
                        string variableName = item.VariableDeclarations[0].UniqueName;
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
                declerationNames.AddRange(statements.Statements
                    .OfType<LocalDeclarationStatementSyntax>()
                    .Concat(
                        statements.Statements
                            .OfType<BlockSyntax>()
                            .SelectMany(x => x.Statements.OfType<LocalDeclarationStatementSyntax>())
                    )
                    .SelectMany(declaration => declaration.Declaration.Variables)
                    .Select(variable => variable.Identifier.Text));
                foreach (GeneratedSerializerCode item in propCodes)
                {
                    statements.Statements.AddRange(item.ToMergedBlock());
                }
            }
        }
    }
}