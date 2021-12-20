using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

using NonSucking.Framework.Serialization.Attributes;
using NonSucking.Framework.Serialization.Serializers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using VaVare;
using VaVare.Builders;
using VaVare.Generators.Common;

namespace NonSucking.Framework.Serialization
{
    public record NoosonGeneratorContext(SourceProductionContext GeneratorContext, string ReaderWriterName, ISymbol MainSymbol)
    {
        const string Category = "SerializationGenerator";
        const string IdPrefix = "NSG";

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
            AddDiagnostic(
                id,
                title,
                message,
                Location.Create(
                    symbolForLocation.DeclaringSyntaxReferences[0].SyntaxTree,
                    symbolForLocation.DeclaringSyntaxReferences[0].Span),
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
            if (attributeTemplate == null)
                throw new ArgumentNullException(nameof(attributeTemplate));
            else if (attributeTemplate.Kind != Templates.TemplateKind.Attribute)
                throw new ArgumentException(nameof(attributeTemplate) + " is not attribute");

            return symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ToDisplayString() == attributeTemplate.FullName);
        }

        public static bool TryGetAttribute(this ISymbol symbol, Template attributeTemplate, out AttributeData attributeData)
        {
            if (attributeTemplate == null)
                throw new ArgumentNullException(nameof(attributeTemplate));
            else if (attributeTemplate.Kind != Templates.TemplateKind.Attribute)
                throw new ArgumentException(nameof(attributeTemplate) + " is not attribute");

            attributeData = symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ToDisplayString() == attributeTemplate.FullName);
            return attributeData != null;
        }
    }

    [Generator]
    public class NoosonGenerator : IIncrementalGenerator
    {
        internal const string writerName = "writer";
        internal const string readerName = "reader";
        private static readonly string returnValue;
        internal const string ReturnValueBaseName = "ret";

        static NoosonGenerator()
        {
            returnValue = Helper.GetRandomNameFor(ReturnValueBaseName, "");
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

                var compilationVisitInfos
                    = incrementalContext
                        .CompilationProvider
                        .Combine(visitInfos.Collect());

                incrementalContext.RegisterSourceOutput(compilationVisitInfos, InternalExecute);

                List<Template> templates
                    = Assembly
                        .GetAssembly(typeof(Template))
                        .GetTypes()
                        .Where(t => typeof(Template).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                        .Select(t => (Template)Activator.CreateInstance(t))
                        .ToList();


                incrementalContext.RegisterPostInitializationOutput(i =>
                {
                    foreach (Template template in templates)
                    {
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
            return syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.AttributeLists.Count > 0;
        }

        private static VisitInfo Transform(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            var classDeclarationSyntax = syntaxContext.Node as ClassDeclarationSyntax;

            INamedTypeSymbol typeSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            System.Collections.Immutable.ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
            var attribute
                = attributes
                .FirstOrDefault(d => d?.AttributeClass.ToDisplayString() == AttributeTemplates.GenSerializationAttribute.FullName);

            if (attribute == default)
            {
                return VisitInfo.Empty;
            }
            MemberInfo[] properties
                = Helper
                    .GetMembersWithBase(typeSymbol)
                    .Where(x=>!x.Symbol.ContainingType.IsAbstract)
                    .ToArray();

            return new VisitInfo(typeSymbol, attribute, properties);
        }

        private static void InternalExecute(SourceProductionContext sourceProductionContext, (Compilation Compilation, ImmutableArray<VisitInfo> VisitInfos) source)
        {

            foreach (VisitInfo classToAugment in source.VisitInfos)
            {
                NoosonGeneratorContext serializeContext = new(sourceProductionContext, writerName, classToAugment.TypeSymbol);
                NoosonGeneratorContext deserializeContext = new(sourceProductionContext, readerName, classToAugment.TypeSymbol);
                var methods =
                    new[] {
                        GenerateSerializeMethod(classToAugment, serializeContext),
                        GenerateDeserializeMethod(classToAugment, deserializeContext)
                    };

                var sourceCode
                    = new ClassBuilder(classToAugment.TypeSymbol.Name, classToAugment.TypeSymbol.ContainingNamespace.ToDisplayString())
                    .WithUsings()
                    .WithModifiers(Modifiers.Public, Modifiers.Partial)
                    .WithMethods(methods)
                    .Build();

                string hintName = $"{classToAugment.TypeSymbol.ToDisplayString()}.Nooson.cs";

                using var workspace = new AdhocWorkspace() { };
                var options = workspace.Options
                    .WithChangedOption(CSharpFormattingOptions.NewLineForElse, true)
                    .WithChangedOption(CSharpFormattingOptions.NewLineForFinally, true)
                    .WithChangedOption(CSharpFormattingOptions.NewLineForCatch, true)
                    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true)
                    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, true)
                    .WithChangedOption(CSharpFormattingOptions.IndentBlock, false)
                    ;
                var formattedText = Formatter.Format(sourceCode, workspace, options).NormalizeWhitespace().ToFullString();

                sourceProductionContext.AddSource(hintName, formattedText);
            }

        }


        internal static BaseMethodDeclarationSyntax GenerateSerializeMethod(VisitInfo visitInfo, NoosonGeneratorContext context)
        {
            var parameter = SyntaxFactory.ParseParameterList($"System.IO.BinaryWriter {context.ReaderWriterName}");

            var body
                = CreateBlock(visitInfo with { Properties = visitInfo.Properties.Select(x => x with { Parent = "" }).ToArray() }, context, MethodType.Serialize);

            return new MethodBuilder("Serialize")
                .WithModifiers(Modifiers.Public)
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        internal static BaseMethodDeclarationSyntax GenerateDeserializeMethod(VisitInfo visitInfo, NoosonGeneratorContext context)
        {
            var parameter = SyntaxFactory.ParseParameterList($"System.IO.BinaryReader {context.ReaderWriterName}");
            var body
                = CreateBlock(visitInfo, context, MethodType.Deserialize);

            return new MethodBuilder("Deserialize")
                .WithModifiers(Modifiers.Public, Modifiers.Static)
                .WithReturnType(SyntaxFactory.ParseTypeName(visitInfo.TypeSymbol.Name))
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        internal static BlockSyntax CreateBlock(VisitInfo visitInfo, NoosonGeneratorContext context, MethodType methodType)
        {
            var statements
                = GenerateStatementsForProps(visitInfo.Properties, context, methodType)
                .Where(x=>x.Count > 0)
                .SelectMany(x => x)
                .ToList();

            if (methodType == MethodType.Deserialize)
            {
                try
                {
                    var ret = CtorSerializer.CallCtorAndSetProps(visitInfo.TypeSymbol, statements, returnValue, DeclareOrAndAssign.DeclareAndAssign);
                    statements.AddRange(ret);
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
        internal static IEnumerable<List<StatementSyntax>> GenerateStatementsForProps(IReadOnlyCollection<MemberInfo> properties, NoosonGeneratorContext context, MethodType methodType)
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


        internal static List<StatementSyntax> CreateStatementForSerializing(MemberInfo property, NoosonGeneratorContext context, string writerName)
        {
            List<StatementSyntax> statements = new();
            _ = CustomMethodCallSerializer.TrySerialize(property, context, writerName, statements)
                           || SpecialTypeSerializer.TrySerialize(property, context, writerName, statements)
                           || EnumSerializer.TrySerialize(property, context, writerName, statements)
                           || MethodCallSerializer.TrySerialize(property, context, writerName, statements)
                           || DictionarySerializer.TrySerialize(property, context, writerName, statements)
                           || ListSerializer.TrySerialize(property, context, writerName, statements)
                           || PublicPropertySerializer.TrySerialize(property, context, writerName, statements)
                           ;

            return statements;
        }

        internal static List<StatementSyntax> CreateStatementForDeserializing(MemberInfo property, NoosonGeneratorContext context, string readerName)
        {
            List<StatementSyntax> statements = new();
            _ = CustomMethodCallSerializer.TryDeserialize(property, context, readerName, statements)
                           || SpecialTypeSerializer.TryDeserialize(property, context, readerName, statements)
                           || EnumSerializer.TryDeserialize(property, context, readerName, statements)
                           || MethodCallSerializer.TryDeserialize(property, context, readerName, statements)
                           || DictionarySerializer.TryDeserialize(property, context, readerName, statements)
                           || ListSerializer.TryDeserialize(property, context, readerName, statements)
                           || PublicPropertySerializer.TryDeserialize(property, context, readerName, statements)
                           ;

            return statements;
        }

    }

    public enum MethodType
    {
        Serialize,
        Deserialize
    }

}
