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

namespace NonSucking.Framework.Serialization
{
    public record NoosonGeneratorContext(SourceProductionContext GeneratorContext, string ReaderWriterName, ISymbol MainSymbol)
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

        internal void AddDiagnostic(string id, string title, string message, Location location, DiagnosticSeverity severity, string helpLinkurl = null, params string[] customTags)
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
        internal void AddDiagnostic(string id, string title, string message, ISymbol symbolForLocation, DiagnosticSeverity severity, string helpLinkurl = null, params string[] customTags)
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



        public static AttributeData GetAttribute(this ISymbol symbol, Template attributeTemplate)
        {
            if (attributeTemplate is null)
                throw new ArgumentNullException(nameof(attributeTemplate));
            else if (attributeTemplate.Kind != Templates.TemplateKind.Attribute)
                throw new ArgumentException(nameof(attributeTemplate) + " is not attribute");

            return symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ToDisplayString() == attributeTemplate.FullName);
        }

        public static bool TryGetAttribute(this ISymbol symbol, Template attributeTemplate, out AttributeData attributeData)
        {
            if (attributeTemplate is null)
                throw new ArgumentNullException(nameof(attributeTemplate));
            else if (attributeTemplate.Kind != Templates.TemplateKind.Attribute)
                throw new ArgumentException(nameof(attributeTemplate) + " is not attribute");

            attributeData = symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ToDisplayString() == attributeTemplate.FullName);
            return attributeData is not null;
        }
    }

    [Generator]
    public partial class NoosonGenerator : IIncrementalGenerator
    {
        internal const string writerName = "writer";
        internal const string readerName = "reader";
        private static readonly string returnValue;
        private static readonly MemberInfo returnValueMember;
        internal const string ReturnValueBaseName = "ret";

        static NoosonGenerator()
        {
            returnValueMember = new MemberInfo(null, null, ReturnValueBaseName, "");
            returnValue = returnValueMember.CreateUniqueName();
        }

        public void Initialize(IncrementalGeneratorInitializationContext incrementalContext)
        {

            try
            {
                var visitInfos
                    = incrementalContext
                        .SyntaxProvider
                        .CreateSyntaxProvider(Predicate, Transform)
                        .Where(static visitInfo => visitInfo != VisitInfo.Empty);

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

        private static VisitInfo Transform(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            try
            {

                var typeDeclarationSyntax = syntaxContext.Node as TypeDeclarationSyntax;

                INamedTypeSymbol typeSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax);
                System.Collections.Immutable.ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
                var attribute
                    = attributes
                        .FirstOrDefault(d => d?.AttributeClass.ToDisplayString() == AttributeTemplates.GenSerializationAttribute.FullName);

                if (attribute == default)
                {
                    return VisitInfo.Empty;
                }

                var propEnumerable
                    = Helper
                        .GetMembersWithBase(typeSymbol);

                MemberInfo[] properties = propEnumerable.ToArray();

                return new VisitInfo(typeSymbol, attribute, properties);
            }
            catch (Exception e)
            {
                throw new Exception(syntaxContext.Node.SyntaxTree.FilePath + e.Message + "\n" + e.StackTrace);
            }
        }

        private static CompilationUnitSyntax CreateNesting(ITypeSymbol symbol, TypeDeclarationSyntax nestedType)
        {
            bool isNestedType = symbol.ContainingType != null;
            var containingNamespace = isNestedType ? null : symbol.ContainingNamespace.ToDisplayString();
            var nestedMember = new ClassBuildMember(nestedType);
            TypeDeclarationSyntax parentNestedType = null;
            CompilationUnitSyntax compilationUnitSyntax = null;
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
                return CreateNesting(symbol.ContainingType, parentNestedType);

            return compilationUnitSyntax;
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
            ImmutableArray<VisitInfo> VisitInfos) source, List<Template> templates)
        {
            foreach (Template template in templates)
            {
                if (template.Kind == TemplateKind.AdditionalSource && source.Compilation.GetTypeByMetadataName(template.FullName) is null)
                    sourceProductionContext.AddSource(template.Name, template.ToString());
            }
            foreach (VisitInfo typeToAugment in source.VisitInfos)
            {
                try
                {
                    if (typeToAugment.TypeSymbol.IsAbstract)
                    {
                        var location = typeToAugment.TypeSymbol.DeclaringSyntaxReferences.Length > 0
                            ? Location.Create(
                                typeToAugment.TypeSymbol.DeclaringSyntaxReferences[0].SyntaxTree,
                                typeToAugment.TypeSymbol.DeclaringSyntaxReferences[0].Span)
                            : Location.None;
                        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                $"{NoosonGeneratorContext.IdPrefix}0012",
                                "",
                                $"Abstract types are not supported for serializing/deserializing('{typeToAugment.TypeSymbol.ToDisplayString()}').",
                                nameof(NoosonGenerator),
                                DiagnosticSeverity.Error,
                                true),
                            location));
                        continue;
                    }
                    NoosonGeneratorContext serializeContext = new(sourceProductionContext, writerName, typeToAugment.TypeSymbol);
                    NoosonGeneratorContext deserializeContext = new(sourceProductionContext, readerName, typeToAugment.TypeSymbol);
                    var methods =
                        new[] {
                            GenerateSerializeMethod(typeToAugment, serializeContext),
                            GenerateDeserializeMethod(typeToAugment, deserializeContext)
                        };

                    var usings = new HashSet<string>();
                    usings.UnionWith(serializeContext.Usings);
                    usings.UnionWith(deserializeContext.Usings);

                    var usingsArray = usings.ToArray();

                    CompilationUnitSyntax sourceCode = null;
                    TypeDeclarationSyntax nestedType = null;

                    bool isNestedType = typeToAugment.TypeSymbol.ContainingType is not null;
                    var containingNamespace = isNestedType
                        ? null
                        : typeToAugment.TypeSymbol.ContainingNamespace.ToDisplayString();
                    if (typeToAugment.TypeSymbol.IsRecord)
                    {
                        var builder = new RecordBuilder(typeToAugment.TypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                containingNamespace,
                                typeToAugment.TypeSymbol.IsValueType)
                            .WithUsings(usingsArray)
                            .WithModifiers(Modifiers.Public, Modifiers.Partial)
                            .WithMethods(methods);
                        if (isNestedType)
                            nestedType = builder.BuildWithoutNamespace();
                        else
                            sourceCode = builder.Build();
                    }
                    else if (typeToAugment.TypeSymbol.IsValueType)
                    {
                        var builder = new StructBuilder(typeToAugment.TypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), containingNamespace)
                            .WithUsings(usingsArray)
                            .WithModifiers(Modifiers.Public, Modifiers.Partial)
                            .WithMethods(methods);
                        if (isNestedType)
                            nestedType = builder.BuildWithoutNamespace();
                        else
                            sourceCode = builder.Build();
                    }
                    else
                    {
                        var builder = new ClassBuilder(typeToAugment.TypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), containingNamespace)
                            .WithUsings(usingsArray)
                            .WithModifiers(Modifiers.Public, Modifiers.Partial)
                            .WithMethods(methods);
                        if (isNestedType)
                            nestedType = builder.BuildWithoutNamespace();
                        else
                            sourceCode = builder.Build();
                    }

                    if (isNestedType)
                        sourceCode = CreateNesting(typeToAugment.TypeSymbol.ContainingType, nestedType);

                    string hintName = TypeNameToSummaryName($"{typeToAugment.TypeSymbol.ToDisplayString()}.Nooson.cs");

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

                    sourceProductionContext.AddSource(hintName, sourceCode.NormalizeWhitespace().ToFullString() /*formattedText*/);
                }
                catch (ReflectionTypeLoadException loaderException)
                {
                    var exceptions = string.Join(" | ", loaderException.LoaderExceptions.Select(x => x.ToString()));

                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        $"{NoosonGeneratorContext.IdPrefix}0012",
                        "",
                        $"Missing dependencies for generation of serializer code for '{typeToAugment.TypeSymbol.ToDisplayString()}'. Amount: {loaderException.LoaderExceptions.Length}, {loaderException.Message}, {exceptions}",
                        nameof(NoosonGenerator),
                        DiagnosticSeverity.Error,
                        true),
                         Location.None));
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    var location = typeToAugment.TypeSymbol.DeclaringSyntaxReferences.Length > 0
                        ? Location.Create(
                            typeToAugment.TypeSymbol.DeclaringSyntaxReferences[0].SyntaxTree,
                            typeToAugment.TypeSymbol.DeclaringSyntaxReferences[0].Span)
                        : Location.None;


                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        $"{NoosonGeneratorContext.IdPrefix}0011",
                        "",
                        $"Error occured while trying to generate serializer code for '{typeToAugment.TypeSymbol.ToDisplayString()}' type: {e.Message}\n{e.StackTrace}",
                        nameof(NoosonGenerator),
                        DiagnosticSeverity.Error,
                        true),
                        location));
                }
            }
        }

        private static bool BaseHasNoosonAttribute(INamedTypeSymbol typeSymbol)
            => typeSymbol is not null
               && (typeSymbol.GetAttribute(AttributeTemplates.GenSerializationAttribute) is not null
                   || BaseHasNoosonAttribute(typeSymbol.BaseType));

        internal static BaseMethodDeclarationSyntax GenerateSerializeMethod(VisitInfo visitInfo, NoosonGeneratorContext context)
        {
            var parameter = SyntaxFactory.ParseParameterList($"System.IO.BinaryWriter {context.ReaderWriterName}");

            var body
                = CreateBlock(visitInfo with { Properties = visitInfo.Properties.Select(x => x with { Parent = "" }).ToArray() }, context, MethodType.Serialize);

            var modifiers = new List<Modifiers> { Modifiers.Public };

            if (!visitInfo.TypeSymbol.IsValueType)
            {
                if (BaseHasNoosonAttribute(visitInfo.TypeSymbol.BaseType))
                    modifiers.Add(Modifiers.Override);
                else
                    modifiers.Add(Modifiers.Virtual);
            }

            return new MethodBuilder("Serialize")
                .WithModifiers(modifiers.ToArray())
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        internal static BaseMethodDeclarationSyntax GenerateDeserializeMethod(VisitInfo visitInfo, NoosonGeneratorContext context)
        {
            var parameter = SyntaxFactory.ParseParameterList($"System.IO.BinaryReader {context.ReaderWriterName}");
            var body
                = CreateBlock(visitInfo, context, MethodType.Deserialize);

            var modifiers = new List<Modifiers> { Modifiers.Public };

            if (!visitInfo.TypeSymbol.IsValueType)
            {
                if (BaseHasNoosonAttribute(visitInfo.TypeSymbol.BaseType))
                    modifiers.Add(Modifiers.New);
            }
            modifiers.Add(Modifiers.Static);

            return new MethodBuilder("Deserialize")
                .WithModifiers(modifiers.ToArray())
                .WithReturnType(SyntaxFactory.ParseTypeName(visitInfo.TypeSymbol.Name))
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        internal static BlockSyntax CreateBlock(VisitInfo visitInfo, NoosonGeneratorContext context, MethodType methodType)
        {
            var statements
                = GenerateStatementsForProps(visitInfo.Properties, context, methodType)
                .Where(x => x.Statements.Count + x.VariableDeclarations.Count > 0)
                .SelectMany(x => x.ToMergedBlock())
                .ToList();

            if (methodType == MethodType.Deserialize)
            {
                try
                {
                    var ret = CtorSerializer.CallCtorAndSetProps(visitInfo.TypeSymbol, statements, returnValueMember, returnValue);
                    statements.AddRange(ret.ToMergedBlock());
                }
                catch (NotSupportedException)
                {
                    context.AddDiagnostic("0006",
                       "",
                       "No instance could be created with the constructors in this type. Add a custom ctor call, property mapping or a ctor with matching arguments.",
                       visitInfo.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
                }


                var returnStatement
                    = SyntaxFactory
                    .ParseStatement($"return {returnValue};");

                statements.Add(returnStatement);
            }
            return BodyGenerator.Create(statements.ToArray());
        }
        internal static IEnumerable<GeneratedSerializerCode> GenerateStatementsForProps(IReadOnlyCollection<MemberInfo> properties, NoosonGeneratorContext context, MethodType methodType)
        {
            var propsWithAttr = properties.Select(property => (property, attribute: property.Symbol.GetAttribute(AttributeTemplates.Order)));
            foreach (var propWithAttr in propsWithAttr.OrderBy(x => x.attribute is null ? int.MaxValue : (int)x.attribute.ConstructorArguments[0].Value))
            {
                var property = propWithAttr.property;
                ITypeSymbol propertyType = property.TypeSymbol;

                string propertyName = property.Name;

                if (!IsPropertySupported(property, context)
                    || property.Symbol.TryGetAttribute(AttributeTemplates.Ignore, out _))
                {
                    continue;
                }

                yield return methodType switch
                {
                    MethodType.Serialize => CreateStatementForSerializing(property, context, writerName),
                    MethodType.Deserialize => CreateStatementForDeserializing(property, context, readerName),
                    _ => throw new NotSupportedException($"{methodType} is not supported by Property generation")
                };

            }

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
