using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using NonSucking.Framework.Serialization.Attributes;
using NonSucking.Framework.Serialization.Serializers;
using NonSucking.Framework.Serialization.Templates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using VaVare;
using VaVare.Builders;
using VaVare.Builders.Base;
using VaVare.Builders.BuildMembers;
using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Models;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    [Generator]
    public partial class NoosonGenerator : IIncrementalGenerator
    {
        internal const string writerName = "writer";
        internal const string readerName = "reader";
        internal const string ReturnValueBaseName = "ret";

        public void Initialize(IncrementalGeneratorInitializationContext incrementalContext)
        {
            try
            {
                var visitInfos
                    = incrementalContext
                        .SyntaxProvider
                        .CreateSyntaxProvider(Predicate, Transform)
                        .Where(static typeSymbol => typeSymbol is not null);

                List<Template> templates
                    = Assembly
                        .GetAssembly(typeof(Template))
                        .GetTypes()
                        .Where(t => typeof(Template).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                        .Select(t => (Template) Activator.CreateInstance(t))
                        .ToList();

                var compilationVisitInfos
                    = incrementalContext
                        .CompilationProvider
                        .Combine(visitInfos.Collect());
                var gc = new GlobalContext(new());
                incrementalContext.RegisterSourceOutput(compilationVisitInfos,
                    (context, tuple) => InternalExecute(context, tuple, templates, gc));

                incrementalContext.RegisterPostInitializationOutput(i =>
                {
                    foreach (Template template in templates)
                    {
                        if (template.Kind != TemplateKind.AdditionalSource)
                            i.AddSource(template.Name, template.ToString());
                    }
                });
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }

        private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is ClassDeclarationSyntax {AttributeLists: {Count: > 0}}
                or RecordDeclarationSyntax {AttributeLists: {Count: > 0}}
                or StructDeclarationSyntax {AttributeLists: {Count: > 0}};
        }

        private static ITypeSymbol? Transform(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            try
            {
                if (syntaxContext.Node is not TypeDeclarationSyntax typeDeclarationSyntax)
                    return null;


                INamedTypeSymbol? typeSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax);
                if (typeSymbol == default)
                    return null;

                System.Collections.Immutable.ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
                var attribute
                    = attributes
                        .FirstOrDefault(d =>
                            d.AttributeClass?.ToDisplayString() ==
                            AttributeTemplates.GenSerializationAttribute.FullName);

                if (attribute == default)
                    return null;

                return typeSymbol;
            }
            catch (Exception e)
            {
                throw new Exception(syntaxContext.Node.SyntaxTree.FilePath + e.Message + "\n" + e.StackTrace);
            }
        }

        private static CompilationUnitSyntax CreateNesting(GeneratedType generatedType, TypeDeclarationSyntax? nestedType, HashSet<string> usings)
        {
            while (true)
            {
                bool isNestedType = generatedType.ContainingType is not null;
                var containingNamespace = isNestedType ? null : generatedType.Namespace;
                usings.UnionWith(generatedType.Usings);
                var usingsArray = usings.ToArray();
                var nestedMember = new ClassBuildMember(nestedType);
                TypeDeclarationSyntax? parentNestedType = null;
                CompilationUnitSyntax? compilationUnitSyntax = null;


                if (generatedType.IsRecord)
                {
                    var builder = new RecordBuilder(generatedType.Name, containingNamespace, generatedType.IsValueType).WithUsings(usingsArray)
                        .WithModifiers(Modifiers.Partial)
                        .With(nestedMember)
                        .WithTypeParameters(generatedType.TypeParameters)
                        .WithTypeConstraintClauses(generatedType.TypeParameterConstraint);

                    if (isNestedType)
                        parentNestedType = builder.BuildWithoutNamespace();
                    else
                        compilationUnitSyntax = builder.Build();
                }
                else if (generatedType.IsValueType)
                {
                    var builder = new StructBuilder(generatedType.Name, containingNamespace).WithUsings(usingsArray)
                        .WithModifiers(Modifiers.Partial)
                        .With(nestedMember)
                        .WithTypeParameters(generatedType.TypeParameters)
                        .WithTypeConstraintClauses(generatedType.TypeParameterConstraint);

                    if (isNestedType)
                        parentNestedType = builder.BuildWithoutNamespace();
                    else
                        compilationUnitSyntax = builder.Build();
                }
                else
                {
                    var builder = new ClassBuilder(generatedType.Name, containingNamespace).WithUsings(usingsArray)
                        .WithModifiers(Modifiers.Partial)
                        .With(nestedMember)
                        .WithTypeParameters(generatedType.TypeParameters)
                        .WithTypeConstraintClauses(generatedType.TypeParameterConstraint);

                    if (isNestedType)
                        parentNestedType = builder.BuildWithoutNamespace();
                    else
                        compilationUnitSyntax = builder.Build();
                }

                if (isNestedType)
                {
                    generatedType = generatedType.ContainingType!;
                    nestedType = parentNestedType;
                    continue;
                }

                return compilationUnitSyntax!;
                break;
            }
        }

        private static string TypeNameToSummaryName(string typeName)
        {
            Span<char> summaryName = stackalloc char[typeName.Length];
            for (int i = 0; i < summaryName.Length; i++)
            {
                var character = typeName[i];
                if (character == '<')
                    character = '{';
                else if (character == '>')
                    character = '}';
                summaryName[i] = character;
            }

            return summaryName.ToString();
        }

        private static void InternalExecute(SourceProductionContext sourceProductionContext, (Compilation Compilation,
            ImmutableArray<ITypeSymbol?> VisitInfos) source, List<Template> templates, GlobalContext gc)
        {
            bool useAdvancedTypes =
                source.Compilation.GetTypeByMetadataName(
                    "NonSucking.Framework.Serialization.IBinaryReader") is not null;
            foreach (Template template in templates)
            {
                if (template.Kind == TemplateKind.AdditionalSource &&
                    source.Compilation.GetTypeByMetadataName(template.FullName) is null)
                    sourceProductionContext.AddSource(template.Name, template.ToString());
            }

            //TODO: When caching somewhere somehow than this bad
            gc.Clean();
            foreach (var typeSymbol in source.VisitInfos.OfType<ITypeSymbol>())
            {
                try
                {
                    if (typeSymbol.IsAbstract)
                    {
                        var location = typeSymbol.DeclaringSyntaxReferences.Length > 0
                            ? Location.Create(
                                typeSymbol.DeclaringSyntaxReferences[0].SyntaxTree,
                                typeSymbol.DeclaringSyntaxReferences[0].Span)
                            : Location.None;
                        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                $"{NoosonGeneratorContext.IdPrefix}0012",
                                "",
                                $"Abstract types are not supported for serializing/deserializing('{typeSymbol.ToDisplayString()}').",
                                nameof(NoosonGenerator),
                                DiagnosticSeverity.Error,
                                true),
                            location));
                        continue;
                    }

                    var generatedType = GetGeneratedTypeFor(gc, typeSymbol);

                    var attributeData = typeSymbol.GetAttribute(AttributeTemplates.GenSerializationAttribute)!;

                    Helper.GetGenAttributeData(attributeData, out var generateDefaultReader,
                        out var generateDefaultWriter,
                        out var directReaders, out var directWriters);

                    NoosonGeneratorContext serializeContext = new(gc, sourceProductionContext, generatedType,
                        writerName, typeSymbol,
                        useAdvancedTypes);
                    NoosonGeneratorContext deserializeContext = new(gc, sourceProductionContext, generatedType,
                        readerName, typeSymbol,
                        useAdvancedTypes);


                    const string binaryWriterName = "System.IO.BinaryWriter";
                    const string binaryReaderName = "System.IO.BinaryReader";

                    if (useAdvancedTypes)
                    {
                        if (generateDefaultReader)
                            AddSerializeMethods(generatedType, typeSymbol,
                                serializeContext with {WriterTypeName = null});
                        if (generateDefaultWriter)
                            AddDeserializeMethods(generatedType, typeSymbol,
                                deserializeContext with {ReaderTypeName = null});
                    }
                    else
                    {
                        if (generateDefaultReader)
                            AddSerializeMethods(generatedType, typeSymbol,
                                serializeContext with {WriterTypeName = binaryWriterName});
                        if (generateDefaultWriter)
                            AddDeserializeMethods(generatedType, typeSymbol,
                                deserializeContext with {ReaderTypeName = binaryReaderName});
                    }

                    foreach (var directWriter in directWriters)
                    {
                        var directWriterName = directWriter?.ToDisplayString();
                        if (directWriterName == binaryWriterName && !useAdvancedTypes && generateDefaultWriter)
                            continue;
                        AddSerializeMethods(generatedType, typeSymbol,
                            serializeContext with {WriterTypeName = directWriterName});
                    }

                    foreach (var directReader in directReaders)
                    {
                        var directReaderName = directReader?.ToDisplayString();
                        if (directReaderName == binaryReaderName && !useAdvancedTypes && generateDefaultReader)
                            continue;
                        AddDeserializeMethods(generatedType, typeSymbol,
                            deserializeContext with {ReaderTypeName = directReaderName});
                    }
                }
                catch (ReflectionTypeLoadException loaderException)
                {
                    var exceptions = string.Join(" | ", loaderException.LoaderExceptions.Select(x => x.ToString()));

                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            $"{NoosonGeneratorContext.IdPrefix}0012",
                            "",
                            $"Missing dependencies for generation of serializer code for '{typeSymbol.ToDisplayString()}'. Amount: {loaderException.LoaderExceptions.Length}, {loaderException.Message}, {exceptions}",
                            nameof(NoosonGenerator),
                            DiagnosticSeverity.Error,
                            true),
                        Location.None));
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    var location = typeSymbol.DeclaringSyntaxReferences.Length > 0
                        ? Location.Create(
                            typeSymbol.DeclaringSyntaxReferences[0].SyntaxTree,
                            typeSymbol.DeclaringSyntaxReferences[0].Span)
                        : Location.None;


                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            $"{NoosonGeneratorContext.IdPrefix}0011",
                            "",
                            $"Error occured while trying to generate serializer code for '{typeSymbol.ToDisplayString()}' type: {e.Message}\n{e.StackTrace}",
                            nameof(NoosonGenerator),
                            DiagnosticSeverity.Error,
                            true),
                        location));
                }
            }

            foreach (var genTypeKV in gc.GeneratedTypes)
            {
                var genType = genTypeKV.Value;
                var usings = new HashSet<string>(genType.Usings);

                var usingsArray = usings.ToArray();

                var methods = new List<BaseMethodDeclarationSyntax>();

                static SyntaxKind ConvertParameterModifier(ParameterModifiers modifiers)
                {
                    return modifiers switch
                    {
                        ParameterModifiers.Out => SyntaxKind.OutKeyword,
                        ParameterModifiers.Ref => SyntaxKind.RefKeyword,
                        ParameterModifiers.This => SyntaxKind.ThisKeyword,
                        _ => SyntaxKind.None
                    };
                }

                foreach (var generatedMethod in genType.Methods)
                {
                    var mb = new MethodBuilder(generatedMethod.Name)
                        .WithModifiers(generatedMethod.Modifier.ToArray())
                        .WithParameters(generatedMethod.Parameters.Select(p =>
                        {
                            var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.List<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(
                                    p.Modifier.Select(modifier =>
                                        SyntaxFactory.Token(ConvertParameterModifier(modifier))).ToArray()),
                                SyntaxFactory.ParseTypeName(p.Type), SyntaxFactory.Identifier(p.Name), null);
                            return (parameterSyntax, p.Summary);
                        }).ToArray())
                        .WithTypeParameters(generatedMethod.TypeParameters)
                        .WithTypeConstraintClauses(generatedMethod.TypeParameterConstraints)
                        .WithBody(BodyGenerator.Create(generatedMethod.Body.ToMergedBlock().ToArray()));

                    if (generatedMethod.ReturnType is not null)
                        mb = mb.WithReturnType(SyntaxFactory.ParseTypeName(generatedMethod.ReturnType.Type),
                            generatedMethod.ReturnType.Summary);

                    if (generatedMethod.Summary is not null)
                        mb = mb.WithSummary(generatedMethod.Summary);

                    methods.Add(mb.Build());
                }

                var methodsArray = methods.ToArray();

                CompilationUnitSyntax? sourceCode = null;
                TypeDeclarationSyntax? nestedType = null;

                bool isNestedType = genType.ContainingType is not null;
                var containingNamespace = isNestedType
                    ? null
                    : genType.Namespace;


                if (genType.IsRecord)
                {
                    var builder = new RecordBuilder(genType.Name,
                        containingNamespace,
                        genType.IsValueType);

                    BuildSourceCode(builder, genType, usingsArray, methodsArray, ref sourceCode, ref nestedType,
                        isNestedType);
                }
                else if (genType.IsValueType)
                {
                    var builder = new StructBuilder(genType.Name, containingNamespace);

                    BuildSourceCode(builder, genType, usingsArray, methodsArray, ref sourceCode, ref nestedType,
                        isNestedType);
                }
                else
                {
                    var builder = new ClassBuilder(genType.Name, containingNamespace);

                    BuildSourceCode(builder, genType, usingsArray, methodsArray, ref sourceCode, ref nestedType,
                        isNestedType);
                }

                if (isNestedType)
                {
                    sourceCode = CreateNesting(genType.ContainingType!, nestedType, usings);
                }

                string hintName = TypeNameToSummaryName($"{genTypeKV.Key}.Nooson.g.cs");

                string autoGeneratedComment =
                    "//---------------------- // <auto-generated> // Nooson // </auto-generated> //----------------------" +
                    Environment.NewLine;
                string nullableEnablement = "#nullable enable" + Environment.NewLine;

                sourceProductionContext.AddSource(hintName,
                    $"{autoGeneratedComment}{nullableEnablement}{sourceCode!.NormalizeWhitespace().ToFullString()}");
            }
        }

        internal static TypeParameter[] CreateTypeParameters(ITypeSymbol symbol)
        {
            if (symbol is not INamedTypeSymbol namedType)
                return Array.Empty<TypeParameter>();
            return namedType.TypeParameters.Select(x =>
            {
                var v = Variance.None;
                switch (x.Variance)
                {
                    case VarianceKind.None:
                        v = Variance.None;
                        break;
                    case VarianceKind.Out:
                        v = Variance.Out;
                        break;
                    case VarianceKind.In:
                        v = Variance.In;
                        break;
                    default:
                        break;
                }

                return new TypeParameter(x.Name, v, null);
            }).ToArray();
        }

        internal static GeneratedType? CreatePseudoNestedTypes(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol is null)
                return null;
            return new GeneratedType(typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.CanBeReferencedByName ? typeSymbol.Name : typeSymbol.ToDisplayString(),
                typeSymbol.ToDisplayString(),
                typeSymbol.IsRecord,
                typeSymbol.IsValueType, CreateTypeParameters(typeSymbol), Array.Empty<TypeParameterConstraintClause>(), new(),
                new(), null, CreatePseudoNestedTypes(typeSymbol.ContainingType));
        }


        internal static GeneratedType GetGeneratedTypeFor(GlobalContext gc, ITypeSymbol typeSymbol)
        {
            if (!gc.TryResolve(typeSymbol, out var generatedType))
            {
                generatedType = new GeneratedType(typeSymbol.ContainingNamespace?.ToDisplayString(),
                    typeSymbol.CanBeReferencedByName ? typeSymbol.Name : typeSymbol.ToDisplayString(),
                    typeSymbol.ToDisplayString(),
                    typeSymbol.IsRecord, typeSymbol.IsValueType,
                    CreateTypeParameters(typeSymbol),
                    Array.Empty<TypeParameterConstraintClause>(),
                    new(), new(), null,
                    CreatePseudoNestedTypes(typeSymbol.ContainingType));
                gc.Add(typeSymbol, generatedType);
            }

            return generatedType;
        }

        private static void BuildSourceCode<T>(T builder, GeneratedType genType, string[] usingsArray,
            BaseMethodDeclarationSyntax[] methodsArray, ref CompilationUnitSyntax? sourceCode,
            ref TypeDeclarationSyntax? nestedType, bool isNestedType)
            where T : TypeBuilderBase<T>
        {
            builder
                .WithUsings(usingsArray)
                .WithModifiers(Modifiers.Partial)
                .WithMethods(methodsArray)
                .WithTypeParameters(genType.TypeParameters)
                .WithTypeConstraintClauses(genType.TypeParameterConstraint);


            if (isNestedType)
                nestedType = builder.BuildWithoutNamespace();
            else
                sourceCode = builder.Build();
        }

        internal static void AddSerializeMethods(GeneratedType generatedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {
            GenerateSerializeMethod(generatedType, typeSymbol, context);
            GenerateSerializeMethodNonStatic(generatedType, typeSymbol, context);
        }

        internal static void AddDeserializeMethods(GeneratedType generatedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {
            GenerateDeserializeMethod(generatedType, typeSymbol, context);
        }

        internal static bool BaseHasNoosonAttribute(INamedTypeSymbol? typeSymbol)
            => typeSymbol is not null
               && (typeSymbol.GetAttribute(AttributeTemplates.GenSerializationAttribute) is not null
                   || BaseHasNoosonAttribute(typeSymbol.BaseType));

        internal static void GenerateSerializeMethodNonStatic(GeneratedType generatedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {
            var member = new MemberInfo(typeSymbol, typeSymbol, "this");

            var generateGeneric = context.WriterTypeName is null;
            var generateConstraint = generateGeneric;

            var body
                = CreateBlock(member, context, MethodType.Serialize);

            if (!typeSymbol.IsValueType)
            {
                if (BaseHasNoosonAttribute(typeSymbol.BaseType))
                {
                    generateConstraint = false;
                }
            }

            GenerateSerialize(generatedType, context.Modifiers, context, member, generateGeneric, generateConstraint,
                body, null);
        }

        internal static void GenerateSerializeMethod(GeneratedType generatedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {
            var member = new MemberInfo(typeSymbol, typeSymbol, "that");

            var generateGeneric = context.WriterTypeName is null;

            var modifiers = context.Modifiers.Append(Modifiers.Static).ToList();

            var body = new GeneratedSerializerCode();
            body.Statements.Add(Statement
                .Expression
                .Invoke("that", "Serialize", arguments: new[] {new ValueArgument((object) writerName)})
                .AsStatement());

            var additionalParameter =
                new GeneratedMethodParameter(typeSymbol.ToDisplayString(), "that", new(), "The instance to serialize.");


            GenerateSerialize(generatedType, modifiers, context, member, generateGeneric, generateGeneric, body,
                additionalParameter);
        }

        private static void GenerateSerialize(GeneratedType generatedType, List<Modifiers> modifiers,
            NoosonGeneratorContext context,
            MemberInfo member, bool generateGeneric, bool generateConstraint, GeneratedSerializerCode body,
            GeneratedMethodParameter? additionalParameter)
        {
            var typeName = context.WriterTypeName ?? Consts.GenericParameterWriterName;

            var parameters = new List<GeneratedMethodParameter>();

            if (additionalParameter is not null)
                parameters.Add(additionalParameter);
            parameters.Add(new GeneratedMethodParameter(typeName, context.ReaderWriterName, new(),
                $"The <see cref=\"{typeName}\"/> to serialize to."));

            generatedType.Methods.Add(new GeneratedMethod(
                null,
                "Serialize",
                parameters,
                modifiers,
                generateGeneric
                    ? new[]
                    {
                        new TypeParameter(Consts.GenericParameterWriterName,
                            xmlDocumentation: "The type of the instance to serialize.")
                    }
                    : Array.Empty<TypeParameter>(), generateConstraint
                    ? new[]
                    {
                        new TypeParameterConstraintClause(Consts.GenericParameterWriterName,
                            new TypeParameterConstraint("NonSucking.Framework.Serialization.IBinaryWriter"))
                    }
                    : Array.Empty<TypeParameterConstraintClause>(),
                body,
                additionalParameter is null
                    ? "Serializes this instance."
                    : $"Serializes the given <see cref=\"{member.TypeSymbol.ToSummaryName()}\"/> instance."));
        }

        internal static void GenerateDeserializeMethod(GeneratedType generatedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {

            var generateGeneric = context.ReaderTypeName is null;
            var typeName = context.ReaderTypeName ?? Consts.GenericParameterReaderName;
            var member = new MemberInfo(typeSymbol, typeSymbol, ReturnValueBaseName);
            var parameter = new List<GeneratedMethodParameter>()
                {new GeneratedMethodParameter(typeName, context.ReaderWriterName, new(), null)};

            var body
                = CreateBlock(member, context, MethodType.Deserialize);

            var modifiers = new List<Modifiers> {Modifiers.Public, Modifiers.Static};

            if (!typeSymbol.IsValueType)
            {
                if (BaseHasNoosonAttribute(typeSymbol.BaseType))
                    modifiers.Add(Modifiers.New);
            }

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

            generatedType.Methods.Add(new GeneratedMethod(new(typeSymbol.ToDisplayString(), "", new(), "The deserialized instance."),
                "Deserialize", parameter, modifiers, typeParams, typeConstraints, body,
                $"Deserializes a <see cref=\"{typeSymbol.ToSummaryName()}\"/> instance."));
        }

        internal static GeneratedSerializerCode CreateBlock(MemberInfo member, NoosonGeneratorContext context,
            MethodType methodType)
        {
            var body = new GeneratedSerializerCode();
            try
            {
                var excludedSerializers = SerializerMask.MethodCallSerializer | SerializerMask.DynamicTypeSerializer |
                                          SerializerMask.NullableSerializer;

                var propCode = GenerateStatementsForMember(member, context, methodType, excludedSerializers);
                if (propCode == null)
                    return body; // TODO: fail?
                var statements = propCode.ToMergedBlock().ToList();

                if (methodType == MethodType.Deserialize)
                {
                    var retVar = propCode.VariableDeclarations.Single().UniqueName;
                    var returnStatement
                        = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(retVar));

                    statements.Add(returnStatement);
                }

                body.Statements.AddRange(statements);
                return body;
            }
            catch (NotSupportedException)
            {
                context.AddDiagnostic("0006",
                    "",
                    "No instance could be created with the constructors in this type. Add a custom ctor call, property mapping or a ctor with matching arguments.",
                    member.TypeSymbol,
                    DiagnosticSeverity.Error
                );
            }

            return body;
        }

        internal static GeneratedSerializerCode? GenerateStatementsForMember(MemberInfo property,
            NoosonGeneratorContext context, MethodType methodType,
            SerializerMask excludedSerializers = SerializerMask.None)
        {
            if (!IsPropertySupported(property, context)
                || property.Symbol.TryGetAttribute(AttributeTemplates.Ignore, out _))
            {
                return null;
            }

            var st = new StackTrace();
            if (st.FrameCount > 512)
            {
                context.AddDiagnostic("0020",
                    "",
                    $"The call stack has reached it's limit, check for recursion on type {property.TypeSymbol.ToDisplayString()}.",
                    property.TypeSymbol,
                    DiagnosticSeverity.Error
                );
                return null;
            }

            return methodType switch
            {
                MethodType.Serialize => CreateStatementForSerializing(property, context, writerName,
                    excludedSerializers: excludedSerializers),
                MethodType.Deserialize => CreateStatementForDeserializing(property, context, readerName,
                    excludedSerializers: excludedSerializers),
                _ => throw new NotSupportedException($"{methodType} is not supported by Property generation")
            };
        }

        private static bool IsPropertySupported(MemberInfo property, NoosonGeneratorContext context)
        {
            if (property.Symbol is IPropertySymbol propSymbol)
            {
                if (propSymbol.IsWriteOnly)
                {
                    context.AddDiagnostic("0007",
                        "",
                        "Properties that are write only are not supported. Implemented a custom serializer method or ignore this property.",
                        property.Symbol,
                        DiagnosticSeverity.Error
                    );
                    return false;
                }
            }

            return true;
        }
    }

    public enum MethodType
    {
        Serialize,
        Deserialize
    }
}