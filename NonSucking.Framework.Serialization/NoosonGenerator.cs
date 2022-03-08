using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

using NonSucking.Framework.Serialization.Attributes;
using NonSucking.Framework.Serialization.Serializers;
using NonSucking.Framework.Serialization.Templates;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using VaVare;
using VaVare.Builders;
using VaVare.Builders.BuildMembers;
using VaVare.Generators.Common;
using VaVare.Models;

namespace NonSucking.Framework.Serialization
{
    public record NoosonGeneratorContext(SourceProductionContext GeneratorContext, string ReaderWriterName, ISymbol MainSymbol, bool UseAdvancedTypes, string? WriterTypeName = null, string? ReaderTypeName = null)
    {
        public HashSet<string> Usings { get; } = new();
        internal const string Category = "SerializationGenerator";
        internal const string IdPrefix = "NSG";

        //Proudly stolen from https://github.com/mknejp/dotvariant/blob/c59599a079637e38c3471a13b6a0443e4e607058/src/dotVariant.Generator/Diagnose.cs#L234
        public void AddDiagnostic(string id, LocalizableString message, DiagnosticSeverity severity, int warningLevel, Location location)
        {
            GeneratorContext.ReportDiagnostic(
                Diagnostic.Create(
                    $"{IdPrefix}{id}",
                    Category,
                    message,
                    severity,
                    severity,
                    true,
                    warningLevel,
                    location: location));
        }

        internal void AddDiagnostic(string id, string title, string message, Location location, DiagnosticSeverity severity, string? helpLinkurl = null, params string[] customTags)
        {
            GeneratorContext.ReportDiagnostic(Diagnostic.Create(
                 new DiagnosticDescriptor(
                     $"{IdPrefix}{id}",
                     title,
                     message,
                     nameof(NoosonGenerator),
                     severity,
                     true,
                     helpLinkUri: helpLinkurl,
                     customTags: customTags),
                 location));
        }
        internal void AddDiagnostic(string id, string title, string message, ISymbol symbolForLocation, DiagnosticSeverity severity, string? helpLinkurl = null, params string[] customTags)
        {
            var loc = symbolForLocation.DeclaringSyntaxReferences.Length == 0
                ? Location.None
                : Location.Create(
                    symbolForLocation.DeclaringSyntaxReferences[0].SyntaxTree,
                    symbolForLocation.DeclaringSyntaxReferences[0].Span);
            AddDiagnostic(
                id,
                title,
                message,
                loc,
                severity,
                helpLinkurl,
                customTags);
        }
    }

    public static class AttributeTemplates
    {
        internal static readonly NoosonIgnoreAttributeTemplate Ignore = new();
        internal static readonly NoosonPreferredCtorAttributeTemplate PreferredCtor = new();
        internal static readonly NoosonParameterAttributeTemplate Parameter = new();
        internal static readonly NoosonCustomAttributeTemplate Custom = new();
        internal static readonly NoosonAttributeTemplate GenSerializationAttribute = new();
        internal static readonly NoosonOrderAttributeTemplate Order = new();
        internal static readonly NoosonIncludeAttributeTemplate Include = new();



        public static AttributeData? GetAttribute(this ISymbol symbol, Template attributeTemplate)
        {
            if (attributeTemplate is null)
                throw new ArgumentNullException(nameof(attributeTemplate));
            else if (attributeTemplate.Kind != TemplateKind.Attribute)
                throw new ArgumentException(nameof(attributeTemplate) + " is not attribute");

            return symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == attributeTemplate.FullName);
        }

        public static bool TryGetAttribute(this ISymbol symbol, Template attributeTemplate, out AttributeData? attributeData)
        {
            attributeData = GetAttribute(symbol, attributeTemplate);
            return attributeData is not null;
        }
    }

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

                incrementalContext.RegisterSourceOutput(compilationVisitInfos,
                    (context, tuple) => InternalExecute(context, tuple, templates));

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
                        .FirstOrDefault(d => d.AttributeClass?.ToDisplayString() == AttributeTemplates.GenSerializationAttribute.FullName);

                if (attribute == default)
                    return null;

                return typeSymbol;
            }
            catch (Exception e)
            {
                throw new Exception(syntaxContext.Node.SyntaxTree.FilePath + e.Message + "\n" + e.StackTrace);
            }
        }

        private static CompilationUnitSyntax CreateNesting(ITypeSymbol symbol, TypeDeclarationSyntax? nestedType)
        {
            bool isNestedType = symbol.ContainingType is not null;
            var containingNamespace = isNestedType ? null : symbol.ContainingNamespace.ToDisplayString();
            var nestedMember = new ClassBuildMember(nestedType);
            TypeDeclarationSyntax? parentNestedType = null;
            CompilationUnitSyntax? compilationUnitSyntax = null;
            if (symbol.IsRecord)
            {
                var builder = new RecordBuilder(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        containingNamespace,
                        symbol.IsValueType)
                    .WithUsings()
                    .WithModifiers(Modifiers.Public, Modifiers.Partial)
                    .With(nestedMember);
                if (isNestedType)
                    parentNestedType = builder.BuildWithoutNamespace();
                else
                    compilationUnitSyntax = builder.Build();
            }
            else if (symbol.IsValueType)
            {
                var builder = new StructBuilder(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), containingNamespace)
                    .WithUsings()
                    .WithModifiers(Modifiers.Public, Modifiers.Partial)
                    .With(nestedMember);
                if (isNestedType)
                    parentNestedType = builder.BuildWithoutNamespace();
                else
                    compilationUnitSyntax = builder.Build();
            }
            else
            {
                var builder = new ClassBuilder(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), containingNamespace)
                    .WithUsings()
                    .WithModifiers(Modifiers.Public, Modifiers.Partial)
                    .With(nestedMember);
                if (isNestedType)
                    parentNestedType = builder.BuildWithoutNamespace();
                else
                    compilationUnitSyntax = builder.Build();
            }

            if (isNestedType)
                return CreateNesting(symbol.ContainingType!, parentNestedType);

            return compilationUnitSyntax!;
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
            ImmutableArray<ITypeSymbol?> VisitInfos) source, List<Template> templates)
        {
            bool useAdvancedTypes = source.Compilation.GetTypeByMetadataName("NonSucking.Framework.Serialization.IBinaryReader") is not null;
            foreach (Template template in templates)
            {
                if (template.Kind == TemplateKind.AdditionalSource && source.Compilation.GetTypeByMetadataName(template.FullName) is null)
                    sourceProductionContext.AddSource(template.Name, template.ToString());
            }
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

                    var attributeData = typeSymbol.GetAttribute(AttributeTemplates.GenSerializationAttribute)
                                        ?? throw new Exception();
                    
                    Helper.GetGenAttributeData(attributeData, out var generateDefaultReader, out var generateDefaultWriter,
                        out var directReaders, out var directWriters);
                    
                    NoosonGeneratorContext serializeContext = new(sourceProductionContext, writerName, typeSymbol, useAdvancedTypes);
                    NoosonGeneratorContext deserializeContext = new(sourceProductionContext, readerName, typeSymbol, useAdvancedTypes);


                    const string binaryWriterName = "System.IO.BinaryWriter";
                    const string binaryReaderName = "System.IO.BinaryReader";
                    
                    
                    var methods = new List<BaseMethodDeclarationSyntax>();
                    if (useAdvancedTypes)
                    {
                        if (generateDefaultReader)
                            methods.Add(GenerateSerializeMethod(typeSymbol,
                                serializeContext with { WriterTypeName = null }));
                        if (generateDefaultWriter)
                            methods.Add(GenerateDeserializeMethod(typeSymbol,
                                deserializeContext with { ReaderTypeName = null }));
                    }
                    else
                    {
                        if (generateDefaultReader)
                            methods.Add(GenerateSerializeMethod(typeSymbol,
                                serializeContext with { WriterTypeName = binaryWriterName}));
                        if (generateDefaultWriter)
                            methods.Add(GenerateDeserializeMethod(typeSymbol,
                                deserializeContext with { ReaderTypeName = binaryReaderName}));
                    }

                    foreach (var directWriter in directWriters)
                    {
                        var directWriterName = directWriter?.ToDisplayString();
                        if (directWriterName == binaryWriterName && !useAdvancedTypes && generateDefaultWriter)
                            continue;
                        methods.Add(GenerateSerializeMethod(typeSymbol,
                            serializeContext with { WriterTypeName =  directWriterName}));
                    }
                    
                    foreach (var directReader in directReaders)
                    {
                        var directReaderName = directReader?.ToDisplayString();
                        if (directReaderName == binaryReaderName && !useAdvancedTypes && generateDefaultReader)
                            continue;
                        methods.Add(GenerateDeserializeMethod(typeSymbol,
                            deserializeContext with { ReaderTypeName = directReaderName }));
                    }

                    var usings = new HashSet<string>();
                    usings.UnionWith(serializeContext.Usings);
                    usings.UnionWith(deserializeContext.Usings);

                    var usingsArray = usings.ToArray();
                    var methodsArray = methods.ToArray();

                    CompilationUnitSyntax? sourceCode = null;
                    TypeDeclarationSyntax? nestedType = null;

                    bool isNestedType = typeSymbol.ContainingType is not null;
                    var containingNamespace = isNestedType
                        ? null
                        : typeSymbol.ContainingNamespace.ToDisplayString();


                    if (typeSymbol.IsRecord)
                    {
                        var builder = new RecordBuilder(typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                containingNamespace,
                                typeSymbol.IsValueType)
                            .WithUsings(usingsArray)
                            .WithModifiers(Modifiers.Public, Modifiers.Partial)
                            .WithMethods(methodsArray);


                        if (isNestedType)
                            nestedType = builder.BuildWithoutNamespace();
                        else
                            sourceCode = builder.Build();
                    }
                    else if (typeSymbol.IsValueType)
                    {
                        var builder = new StructBuilder(typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), containingNamespace)
                            .WithUsings(usingsArray)
                            .WithModifiers(Modifiers.Public, Modifiers.Partial)
                            .WithMethods(methodsArray);
                        if (isNestedType)
                            nestedType = builder.BuildWithoutNamespace();
                        else
                            sourceCode = builder.Build();
                    }
                    else
                    {
                        var builder = new ClassBuilder(typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), containingNamespace)
                            .WithUsings(usingsArray)
                            .WithModifiers(Modifiers.Public, Modifiers.Partial)
                            .WithMethods(methodsArray);
                        if (isNestedType)
                            nestedType = builder.BuildWithoutNamespace();
                        else
                            sourceCode = builder.Build();
                    }

                    if (isNestedType)
                    {
                        sourceCode = CreateNesting(typeSymbol.ContainingType!, nestedType);
                    }

                    string hintName = TypeNameToSummaryName($"{typeSymbol.ToDisplayString()}.Nooson.g.cs");

                    //using var workspace = new AdhocWorkspace() { };
                    //var options = workspace.Options
                    //    .WithChangedOption(CSharpFormattingOptions.NewLineForElse, true)
                    //    .WithChangedOption(CSharpFormattingOptions.NewLineForFinally, true)
                    //    .WithChangedOption(CSharpFormattingOptions.NewLineForCatch, true)
                    //    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true)
                    //    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, true)
                    //    .WithChangedOption(CSharpFormattingOptions.IndentBlock, false)
                    //    ;
                    //var formattedText = Formatter.Format(sourceCode, workspace, options).NormalizeWhitespace().ToFullString();

                    string autoGeneratedComment = "//---------------------- // <auto-generated> // Nooson // </auto-generated> //----------------------" + Environment.NewLine;

                    sourceProductionContext.AddSource(hintName, $"{autoGeneratedComment}{sourceCode!.NormalizeWhitespace().ToFullString()}" /*formattedText*/);
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
        }

        private static bool BaseHasNoosonAttribute(INamedTypeSymbol? typeSymbol)
            => typeSymbol is not null
               && (typeSymbol.GetAttribute(AttributeTemplates.GenSerializationAttribute) is not null
                   || BaseHasNoosonAttribute(typeSymbol.BaseType));

        internal static BaseMethodDeclarationSyntax GenerateSerializeMethod(ITypeSymbol typeSymbol, NoosonGeneratorContext context)
        {
            const string genericParameterName = "TNonSuckingWriter";
            var generateGeneric = context.WriterTypeName is null;
            var generateConstraint = generateGeneric;
            var typeName = context.WriterTypeName ?? genericParameterName;
            var parameter = SyntaxFactory.ParseParameterList($"${typeName} {context.ReaderWriterName}");

            var member = new MemberInfo(typeSymbol, typeSymbol, "this");
            var body
                = CreateBlock(member, context, MethodType.Serialize);

            var modifiers = new List<Modifiers> { Modifiers.Public };

            if (!typeSymbol.IsValueType)
            {
                if (BaseHasNoosonAttribute(typeSymbol.BaseType))
                {
                    modifiers.Add(Modifiers.Override);
                    generateConstraint = false;
                }
                else
                    modifiers.Add(Modifiers.Virtual);
            }

            return new MethodBuilder("Serialize")
                .WithModifiers(modifiers.ToArray())
                .WithTypeParameters(generateGeneric ? new []{ new TypeParameter(genericParameterName) }  : Array.Empty<TypeParameter>())
                .WithTypeConstraintClauses(generateConstraint
                    ? new[] { new TypeParameterConstraintClause(genericParameterName, new TypeParameterConstraint("NonSucking.Framework.Serialization.IBinaryWriter")) }
                    : Array.Empty<TypeParameterConstraintClause>())
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        internal static BaseMethodDeclarationSyntax GenerateDeserializeMethod(ITypeSymbol typeSymbol, NoosonGeneratorContext context)
        {
            const string genericParameterName = "TNonSuckingReader";
            
            var generateGeneric = context.ReaderTypeName is null;
            var typeName = context.ReaderTypeName ?? genericParameterName;
            var member = new MemberInfo(typeSymbol, typeSymbol, ReturnValueBaseName);
            var parameter = SyntaxFactory.ParseParameterList($"{typeName} {context.ReaderWriterName}");
            var body
                = CreateBlock(member, context, MethodType.Deserialize);

            var modifiers = new List<Modifiers> { Modifiers.Public };

            if (!typeSymbol.IsValueType)
            {
                if (BaseHasNoosonAttribute(typeSymbol.BaseType))
                    modifiers.Add(Modifiers.New);
            }
            modifiers.Add(Modifiers.Static);

            return new MethodBuilder("Deserialize")
                .WithModifiers(modifiers.ToArray())
                .WithReturnType(SyntaxFactory.ParseTypeName(typeSymbol.Name))
                .WithTypeParameters(generateGeneric ? new []{ new TypeParameter(genericParameterName) }  : Array.Empty<TypeParameter>())
                .WithTypeConstraintClauses(generateGeneric
                    ? new[] { new TypeParameterConstraintClause(genericParameterName, new TypeParameterConstraint("NonSucking.Framework.Serialization.IBinaryReader")) }
                    : Array.Empty<TypeParameterConstraintClause>())
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        internal static BlockSyntax CreateBlock(MemberInfo member, NoosonGeneratorContext context, MethodType methodType)
        {
            try
            {
                var excludedSerializers = SerializerMask.MethodCallSerializer | SerializerMask.NullableSerializer;

                var propCode = GenerateStatementsForMember(member, context, methodType, excludedSerializers);
                if (propCode == null)
                    return BodyGenerator.Create(); // TODO: fail?
                var statements = propCode.ToMergedBlock().ToList();

                if (methodType == MethodType.Deserialize)
                {
                    var retVar = propCode.VariableDeclarations.Single().UniqueName;
                    var returnStatement
                        = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(retVar));

                    statements.Add(returnStatement);
                }

                return BodyGenerator.Create(statements.ToArray());
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
            
            return BodyGenerator.Create();
        }

        internal static GeneratedSerializerCode? GenerateStatementsForMember(MemberInfo property,
            NoosonGeneratorContext context, MethodType methodType, SerializerMask excludedSerializers = SerializerMask.None)
        {
            if (!IsPropertySupported(property, context)
                || property.Symbol.TryGetAttribute(AttributeTemplates.Ignore, out _))
            {
                return null;
            }
            
            return methodType switch
            {
                MethodType.Serialize => CreateStatementForSerializing(property, context, writerName, excludedSerializers: excludedSerializers),
                MethodType.Deserialize => CreateStatementForDeserializing(property, context, readerName, excludedSerializers: excludedSerializers),
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
