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
using System.Text;
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
                        .Select(t => (Template)Activator.CreateInstance(t))
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
            return syntaxNode is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } }
                or RecordDeclarationSyntax { AttributeLists: { Count: > 0 } }
                or StructDeclarationSyntax { AttributeLists: { Count: > 0 } };
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

        private static CompilationUnitSyntax CreateNesting(GeneratedFile genFile, GeneratedType generatedType, TypeDeclarationSyntax? nestedType, HashSet<string> usings)
        {
            while (true)
            {
                bool isNestedType = generatedType.ContainingType is not null;
                var containingNamespace = isNestedType ? null : genFile.Namespace;
                usings.UnionWith(genFile.Usings);
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

            static int CountBaseTypes(ITypeSymbol symbol, int counter = 0)
            {
                if (symbol.BaseType is ITypeSymbol ts)
                    return CountBaseTypes(ts, ++counter);
                return counter;
            }
            foreach (var typeSymbol in source.VisitInfos.OfType<ITypeSymbol>().OrderBy(x => CountBaseTypes(x)))
            {
                try
                {

                    var generatedFile = GetGeneratedTypeFor(gc, typeSymbol);
                    var generatedType = generatedFile.GeneratedTypes.First();
                    var attributeData = typeSymbol.GetAttribute(AttributeTemplates.GenSerializationAttribute)!;

                    Helper.GetGenAttributeData(attributeData, out var generateDefaultReader,
                        out var generateDefaultWriter,
                        out var directReaders, out var directWriters);

                    NoosonGeneratorContext serializeContext = new(gc, sourceProductionContext, generatedFile, generatedType,
                        writerName, typeSymbol,
                        useAdvancedTypes, MethodType.Serialize);
                    NoosonGeneratorContext deserializeContext = new(gc, sourceProductionContext, generatedFile, generatedType,
                        readerName, typeSymbol,
                        useAdvancedTypes, MethodType.DeserializeWithCtor);


                    const string binaryWriterName = "System.IO.BinaryWriter";
                    const string binaryReaderName = "System.IO.BinaryReader";

                    if (useAdvancedTypes)
                    {
                        if (generateDefaultReader)
                            AddSerializeMethods(generatedType, typeSymbol,
                                serializeContext with { WriterTypeName = null });
                        if (generateDefaultWriter)
                            AddDeserializeMethods(generatedType, typeSymbol,
                                deserializeContext with { ReaderTypeName = null });
                    }
                    else
                    {
                        if (generateDefaultReader)
                            AddSerializeMethods(generatedType, typeSymbol,
                                serializeContext with { WriterTypeName = binaryWriterName });
                        if (generateDefaultWriter)
                            AddDeserializeMethods(generatedType, typeSymbol,
                                deserializeContext with { ReaderTypeName = binaryReaderName });
                    }

                    foreach (var directWriter in directWriters)
                    {
                        var directWriterName = directWriter?.ToDisplayString();
                        if (directWriterName == binaryWriterName && !useAdvancedTypes && generateDefaultWriter)
                            continue;
                        AddSerializeMethods(generatedType, typeSymbol,
                            serializeContext with { WriterTypeName = directWriterName });
                    }

                    foreach (var directReader in directReaders)
                    {
                        var directReaderName = directReader?.ToDisplayString();
                        if (directReaderName == binaryReaderName && !useAdvancedTypes && generateDefaultReader)
                            continue;
                        AddDeserializeMethods(generatedType, typeSymbol,
                            deserializeContext with { ReaderTypeName = directReaderName });
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
                try
                {
                    var genFile = genTypeKV.Value;
                    var usings = new HashSet<string>(genFile.Usings);
                    var usingsArray = usings.ToArray();
                    bool generateUsings = true;
                    StringBuilder sb = new();
                    foreach (var genType in genFile.GeneratedTypes)
                    {


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

                        TypeDeclarationSyntax? nestedType = null;

                        CompilationUnitSyntax? sourceCode = null;
                        bool isNestedType = genType.ContainingType is not null;
                        var containingNamespace = isNestedType
                            ? null
                            : genFile.Namespace;

                        if (genType.IsRecord)
                        {
                            var builder = new RecordBuilder(genType.Name,
                                containingNamespace,
                                genType.IsValueType);

                            BuildSourceCode(builder, genType, usingsArray, methodsArray, ref sourceCode, ref nestedType,
                                isNestedType, generateUsings);
                        }
                        else if (genType.IsValueType)
                        {
                            var builder = new StructBuilder(genType.Name, containingNamespace);

                            BuildSourceCode(builder, genType, usingsArray, methodsArray, ref sourceCode, ref nestedType,
                                isNestedType, generateUsings);
                        }
                        else
                        {
                            var builder = new ClassBuilder(genType.Name, containingNamespace);

                            BuildSourceCode(builder, genType, usingsArray, methodsArray, ref sourceCode, ref nestedType,
                                isNestedType, generateUsings);
                        }

                        if (isNestedType)
                        {
                            sourceCode = CreateNesting(genFile, genType.ContainingType!, nestedType, usings);
                        }

                        generateUsings = false;
                        sb.AppendLine(sourceCode!.NormalizeWhitespace().ToFullString());
                    }

                    string hintName = TypeNameToSummaryName($"{genTypeKV.Key}.Nooson.g.cs");

                    string autoGeneratedComment =
                        "//---------------------- // <auto-generated> // Nooson // </auto-generated> // ----------------------" +
                        Environment.NewLine;
                    string nullableEnablement = "#nullable enable" + Environment.NewLine;

                    sourceProductionContext.AddSource(hintName, $"{autoGeneratedComment}{nullableEnablement}{sb}");
                }
                catch (ReflectionTypeLoadException loaderException)
                {
                    var exceptions = string.Join(" | ", loaderException.LoaderExceptions.Select(x => x.ToString()));

                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            $"{NoosonGeneratorContext.IdPrefix}0012",
                            "",
                            $"Missing dependencies for generation of serializer code for '{genTypeKV.Key}'. Amount: {loaderException.LoaderExceptions.Length}, {loaderException.Message}, {exceptions}",
                            nameof(NoosonGenerator),
                            DiagnosticSeverity.Error,
                            true),
                        Location.None));
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {


                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            $"{NoosonGeneratorContext.IdPrefix}0011",
                            "",
                            $"Error occured while trying to generate serializer code for '{genTypeKV.Key}' type: {e.Message}\n{e.StackTrace}",
                            nameof(NoosonGenerator),
                            DiagnosticSeverity.Error,
                            true),
                        Location.None));
                }
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
            return new GeneratedType(typeSymbol.CanBeReferencedByName ? typeSymbol.Name : typeSymbol.ToDisplayString(),
                typeSymbol.ToDisplayString(),
                typeSymbol.IsRecord,
                typeSymbol.IsValueType, CreateTypeParameters(typeSymbol), Array.Empty<TypeParameterConstraintClause>(),
                new(), new() { Modifiers.Partial }, null, CreatePseudoNestedTypes(typeSymbol.ContainingType));
        }


        internal static GeneratedFile GetGeneratedTypeFor(GlobalContext gc, ITypeSymbol typeSymbol)
        {
            if (!gc.TryResolve(typeSymbol, out var generatedFile))
            {
                var genTypes = new List<GeneratedType>();
                generatedFile = new(
                    typeSymbol.ContainingNamespace?.ToDisplayString(),
                    typeSymbol.ToDisplayString(),
                    genTypes,
                    new());

                genTypes.Add(new GeneratedType(typeSymbol.CanBeReferencedByName ? typeSymbol.Name : typeSymbol.ToDisplayString(),
                    typeSymbol.ToDisplayString(),
                    typeSymbol.IsRecord, typeSymbol.IsValueType,
                    CreateTypeParameters(typeSymbol),
                    Array.Empty<TypeParameterConstraintClause>(), new(), new() { Modifiers.Partial }, null,
                    CreatePseudoNestedTypes(typeSymbol.ContainingType)));
                gc.Add(typeSymbol, generatedFile);
            }

            return generatedFile;
        }

        private static void BuildSourceCode<T>(T builder, GeneratedType genType, string[] usingsArray,
            BaseMethodDeclarationSyntax[] methodsArray, ref CompilationUnitSyntax? sourceCode,
            ref TypeDeclarationSyntax? nestedType, bool isNestedType, bool generateUsings)
            where T : TypeBuilderBase<T>
        {
            builder
                .WithModifiers(genType.ClassModifiers.ToArray())
                .WithMethods(methodsArray)
                .WithTypeParameters(genType.TypeParameters)
                .WithTypeConstraintClauses(genType.TypeParameterConstraint);

            if (!string.IsNullOrWhiteSpace(genType.Summary))
                builder = builder.WithSummary(genType.Summary);

            if (generateUsings)
                builder = builder.WithUsings(usingsArray);

            if (isNestedType)
                nestedType = builder.BuildWithoutNamespace();
            else
                sourceCode = builder.Build();
        }

        internal static void AddSerializeMethods(GeneratedType defaultGeneratedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {
            GenerateSerializeMethod(defaultGeneratedType, typeSymbol, context);
            GenerateSerializeMethodNonStatic(defaultGeneratedType, typeSymbol, context);
        }

        internal static void AddDeserializeMethods(GeneratedType defaultGeneratedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {
            GenerateDeserializeMethod(defaultGeneratedType, typeSymbol, context);
            GenerateDeserializeMethod(defaultGeneratedType, typeSymbol, context with { MethodType = MethodType.DeserializeIntoInstance });
            GenerateDeserializeMethod(defaultGeneratedType, typeSymbol, context with { MethodType = MethodType.DeserializeSelf });

            GeneratedType extensionType = CreateExtensionType(defaultGeneratedType, typeSymbol, context);

            GenerateDeserializeMethod(extensionType, typeSymbol, context with { MethodType = MethodType.DeserializeOnInstance });
        }

        private static GeneratedType CreateExtensionType(GeneratedType defaultGeneratedType, ITypeSymbol typeSymbol, NoosonGeneratorContext context)
        {
            List<Modifiers> modifiers = new List<Modifiers>() { Modifiers.Static };
            switch (typeSymbol.DeclaredAccessibility)
            {
                case Accessibility.Private:
                    modifiers.Insert(0, Modifiers.Private);
                    break;
                case Accessibility.ProtectedAndInternal:
                    modifiers.Insert(0, Modifiers.Internal);
                    modifiers.Insert(0, Modifiers.Protected);
                    break;
                case Accessibility.Protected:
                    modifiers.Insert(0, Modifiers.Protected);
                    break;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    modifiers.Insert(0, Modifiers.Internal);
                    break;
                case Accessibility.Public:
                    modifiers.Insert(0, Modifiers.Public);
                    break;
            }

            var extensionType = defaultGeneratedType with
            {
                ContainingType = null,
                Name = defaultGeneratedType.Name + "Extension",
                Methods = new(),
                ClassModifiers = modifiers,
                IsRecord = false,
                IsValueType = false,
                TypeParameterConstraint = Array.Empty<TypeParameterConstraintClause>(),
                TypeParameters = Array.Empty<TypeParameter>(),
                Summary = $"Adds extensions methods for <see cref=\"{defaultGeneratedType.Name}\"/>",
            };
            context.GeneratedFile.GeneratedTypes.Add(extensionType);
            return extensionType;
        }

        internal static bool BaseHasNoosonAttribute(INamedTypeSymbol? typeSymbol)
            => typeSymbol is not null
               && (typeSymbol.GetAttribute(AttributeTemplates.GenSerializationAttribute) is not null
                   || BaseHasNoosonAttribute(typeSymbol.BaseType));

        internal static void GenerateSerializeMethodNonStatic(GeneratedType generatedType, ITypeSymbol typeSymbol,
            NoosonGeneratorContext context)
        {
            var member = new MemberInfo(typeSymbol, typeSymbol, Consts.ThisName);

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
            var member = new MemberInfo(typeSymbol, typeSymbol, Consts.InstanceParameterName);

            var generateGeneric = context.WriterTypeName is null;

            var modifiers = context.Modifiers.Append(Modifiers.Static).ToList();

            var body = new GeneratedSerializerCode();
            body.Statements.Add(Statement
                .Expression
                .Invoke(Consts.InstanceParameterName, Consts.Serialize, arguments: new[] { new ValueArgument((object)writerName) })
                .AsStatement());

            var additionalParameter =
                new GeneratedMethodParameter(typeSymbol.ToDisplayString(), Consts.InstanceParameterName, new(), "The instance to serialize.");

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
                Consts.Serialize,
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
            var methodType = context.MethodType;
            var generateGeneric = context.ReaderTypeName is null;
            var typeName = context.ReaderTypeName ?? Consts.GenericParameterReaderName;
            var member = new MemberInfo(typeSymbol, typeSymbol, ReturnValueBaseName);
            if (methodType == MethodType.DeserializeOnInstance)
                member = member with { Parent = Consts.ThisName };

            var parameter = new List<GeneratedMethodParameter>()
            {
                new GeneratedMethodParameter(typeName, context.ReaderWriterName, new(), $"The <see cref=\"{context.ReaderTypeName}\"/> to deserialize from.")
            };
            if (methodType == MethodType.DeserializeIntoInstance)
            {
                var parameterInstance = new GeneratedMethodParameter(typeSymbol.ToDisplayString(), Consts.InstanceParameterName, new(), "The instance to deserialize into.");
                if (typeSymbol.IsValueType)
                    parameterInstance.Modifier.Add(ParameterModifiers.Ref);
                parameter.Insert(0, parameterInstance);
            }

            if (methodType == MethodType.DeserializeOnInstance)
            {
                var parameterInstance = new GeneratedMethodParameter(typeSymbol.ToDisplayString(), Consts.InstanceParameterName, new() { ParameterModifiers.This }, "The instance to deserialize into.");
                if (typeSymbol.IsValueType)
                    parameterInstance.Modifier.Add(ParameterModifiers.Ref);
                parameter.Insert(0, parameterInstance);
            }

            GeneratedSerializerCode body;
            if (methodType == MethodType.DeserializeOnInstance)
                body = GenerateCallToStaticDeserialize(generatedType, typeSymbol, Consts.InstanceParameterName);
            else if (methodType == MethodType.DeserializeSelf)
                body = GenerateCallToStaticDeserialize(generatedType, typeSymbol, Consts.ThisName);
            else
                body = CreateBlock(member, context, methodType);


            List<Modifiers> modifiers;
            if (methodType == MethodType.DeserializeSelf)
            {
                modifiers = GetModifiersForSelfDeserialization(typeSymbol, context);
            }
            else
                modifiers = context.Modifiers.Append(Modifiers.Static).ToList();

            if (context.MethodType == MethodType.DeserializeWithCtor && !typeSymbol.IsValueType)
            {
                if (typeSymbol.BaseType is { } bs && !bs.IsAbstract && BaseHasNoosonAttribute(bs))
                    modifiers.Add(Modifiers.New);
            }


            List<TypeParameter> typeParams = generateGeneric
                ? new()
                {
                    new TypeParameter(Consts.GenericParameterReaderName,
                        xmlDocumentation: "The type of the instance to deserialize.")
                }
                : new();
            List<TypeParameterConstraintClause> typeConstraints = generateGeneric
                ? new()
                {
                    new TypeParameterConstraintClause(Consts.GenericParameterReaderName,
                        new TypeParameterConstraint("NonSucking.Framework.Serialization.IBinaryReader"))
                }
                : new();

            if (typeSymbol.IsAbstract && methodType == MethodType.DeserializeWithCtor)
                return;

            GeneratedMethodParameter? retType = null;

            if (methodType == MethodType.DeserializeOnInstance)
            {
                var typeParamsWithConstraints
                    = GetTypeParametersWithConstraints(generatedType, typeSymbol).ToList();
                foreach (var item in typeParamsWithConstraints)
                {
                    typeParams.Add(item.Item1);
                    if (item.Item2.Constraints.Count > 0)
                        typeConstraints.Add(item.Item2);
                }

            }

            if (methodType == MethodType.DeserializeWithCtor)
                retType = new(typeSymbol.ToDisplayString(), "", new(), "The deserialized instance.");

            string desName = Consts.Deserialize;
            if (methodType == MethodType.DeserializeSelf)
                desName = Consts.DeserializeSelf;

            generatedType.Methods.Add(new GeneratedMethod(retType,
            desName, parameter, modifiers, typeParams.ToArray(), typeConstraints.ToArray(), body,
            $"Deserializes {(methodType == MethodType.DeserializeWithCtor ? "a" : "into")} <see cref=\"{typeSymbol.ToSummaryName()}\"/> instance."));
        }

        private static List<Modifiers> GetModifiersForSelfDeserialization(ITypeSymbol typeSymbol, NoosonGeneratorContext context)
        {
            List<Modifiers> modifiers = context.Modifiers.ToList();
            if (typeSymbol.IsValueType)
                return modifiers;

            IMethodSymbol? baseSelfDeserialize = Helper.GetFirstMemberWithBase(typeSymbol.BaseType,
                   (s) => s is IMethodSymbol im
                          && im.Name == Consts.DeserializeSelf)
                    as IMethodSymbol;
            if (baseSelfDeserialize is not null)
            {
                if (!baseSelfDeserialize.IsAbstract && !baseSelfDeserialize.IsVirtual)
                {

                    context.AddDiagnostic("0014",
                        "",
                        "Base Serialize is neither virtual nor abstract and therefore a shadow serialize will be implemented, which might not be wanted. Please consult your doctor or apothecary.",
                        NoosonGeneratorContext.GetExistingFrom(baseSelfDeserialize, typeSymbol),
                        DiagnosticSeverity.Warning
                    );
                    modifiers.Add(Modifiers.New);
                }
                else
                {
                    modifiers.Add(Modifiers.Override);
                }
            }
            else
            {
                (GeneratedMethod? generatedMethod, _) = Helper.GetFirstMemberWithBase(context.GlobalContext, typeSymbol.BaseType,
                    (m) => m.Name == Consts.DeserializeSelf
                           && !m.IsStatic
                           && m.Parameters.Count == 1);
                if (generatedMethod is not null)
                {
                    if (!generatedMethod.IsAbstract && !generatedMethod.IsVirtual)
                    {

                        context.AddDiagnostic("0014",
                            "",
                            "Base Serialize is neither virtual nor abstract and therefore a shadow serialize will be implemented, which might not be wanted. Please consult your doctor or apothecary.",
                            NoosonGeneratorContext.GetExistingFrom(typeSymbol),
                            DiagnosticSeverity.Warning
                        );
                        modifiers.Add(Modifiers.New);
                    }
                    else
                    {
                        modifiers.Add(Modifiers.Override);
                    }
                }
                else
                    modifiers.Add(Modifiers.Virtual);
            }

            return modifiers;
        }

        private static IEnumerable<(TypeParameter, TypeParameterConstraintClause)> GetTypeParametersWithConstraints(GeneratedType? generatedType, ITypeSymbol typeSymbol)
        {
            Stack<(TypeParameter, TypeParameterConstraintClause)> stack = new();

            while (typeSymbol is INamedTypeSymbol nt && nt.IsGenericType)
            {
                var currentType = generatedType;
                typeSymbol = nt.ContainingType;
                generatedType = currentType?.ContainingType;


                var tp = nt.TypeParameters;
                if (tp.Length == 0)
                    continue;

                var constraints = tp.Select((x) =>
                {
                    var ourConstraints = generatedType?.TypeParameterConstraint.FirstOrDefault(y => y.Identifier == x.Name);

                    (bool hasClass, bool hasStruct, bool hasUnmanaged, bool hasNotNull, bool hasConstructor, List<TypeParameterConstraint> typeConstraints)
                    = ((ourConstraints?.Constraints) ?? Enumerable.Empty<TypeParameterConstraint>()).Aggregate((hasClass: false, hasStruct: false, hasUnmanaged: false, hasNotNull: false, hasConstructor: false, typeConstraints: new List<TypeParameterConstraint>()), (a, b) =>
                    {
                        a.hasClass |= b.Type == TypeParameterConstraint.ConstraintType.Class;
                        a.hasStruct |= b.Type == TypeParameterConstraint.ConstraintType.Struct;
                        a.hasConstructor |= b.Type == TypeParameterConstraint.ConstraintType.Constructor;
                        if (b.Type == TypeParameterConstraint.ConstraintType.Type)
                        {
                            if (b.ConstraintIdentifier == "unmanaged")
                                a.hasUnmanaged |= true;
                            else if (b.ConstraintIdentifier == "notnull")
                                a.hasNotNull |= true;
                            else
                                a.typeConstraints.Add(b);
                        }
                        return a;
                    });

                    var clause = new TypeParameterConstraintClause(x.Name);
                    if (hasClass || x.HasReferenceTypeConstraint)
                        clause.Constraints.Add(new(TypeParameterConstraint.ConstraintType.Class));
                    else if (hasStruct || x.HasValueTypeConstraint)
                        clause.Constraints.Add(new(TypeParameterConstraint.ConstraintType.Struct));
                    else if (hasUnmanaged || x.HasUnmanagedTypeConstraint)
                        clause.Constraints.Add(new("unmanaged"));

                    if (hasNotNull || x.HasNotNullConstraint)
                        clause.Constraints.Add(new("notnull"));

                    clause.Constraints.AddRange(x.ConstraintTypes
                        .Select(x => new TypeParameterConstraint(x.ToDisplayString()))
                        .Concat(typeConstraints)
                        .GroupBy(x => x.ConstraintIdentifier)
                        .Select(x => x.First()));

                    if (hasConstructor || x.HasConstructorConstraint)
                        clause.Constraints.Add(new(TypeParameterConstraint.ConstraintType.Constructor));
                    return (new TypeParameter(x.Name), clause);
                });

                foreach (var item in constraints)
                {
                    stack.Push(item);
                }
            }

            while (stack.Count > 0)
                yield return stack.Pop();

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

                if (methodType == MethodType.DeserializeWithCtor)
                {
                    var retVar = propCode.VariableDeclarations.Single().UniqueName;
                    var returnStatement
                        = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(retVar));

                    statements.Add(returnStatement);
                }

                if (methodType == MethodType.DeserializeIntoInstance)
                {
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


        internal static GeneratedSerializerCode GenerateCallToStaticDeserialize(GeneratedType generatedType, ITypeSymbol typeSymbol, string name)
        {
            var body = new GeneratedSerializerCode();
            body.Statements.Add(Statement
                .Expression
                .Invoke(
                    generatedType.DisplayName,
                    Consts.Deserialize,
                    arguments: new[]
                        {
                            new ValueArgument((object)$"{(typeSymbol.IsValueType ? "ref " :"")}{name}"),
                            new ValueArgument((object)readerName)
                        })
                .AsStatement());

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
                MethodType.DeserializeWithCtor
                    or MethodType.DeserializeIntoInstance
                    or MethodType.DeserializeOnInstance => CreateStatementForDeserializing(property, context, readerName,
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
        DeserializeWithCtor,
        DeserializeIntoInstance,
        DeserializeSelf,
        DeserializeOnInstance
    }
}