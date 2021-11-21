using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

using NonSucking.Framework.Extension.Generators.Attributes;
using NonSucking.Framework.Extension.Generators.Serializers;

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
using VaVare.Generators.Common;

namespace NonSucking.Framework.Extension.Generators
{
    public record NoosonGeneratorContext(SourceProductionContext GeneratorContext, string ReaderWriterName)
    {
        const string Category = "SerializationGenerator";
        const string IdPrefix = "NSG";
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


    [Generator]
    public class NoosonGenerator : IIncrementalGenerator
    {
        internal const string writerName = "writer";
        internal const string readerName = "reader";
        internal static readonly NoosonAttributeTemplate genSerializationAttribute = new();
        private static readonly string returnValue;

        static NoosonGenerator()
        {
            returnValue = Helper.GetRandomNameFor("returnValue");
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
                .FirstOrDefault(d => d?.AttributeClass.ToDisplayString() == genSerializationAttribute.FullName);

            if (attribute == default)
            {
                return VisitInfo.Empty;
            }

            List<TypeGroupInfo> properties
                = classDeclarationSyntax
                    .Members
                    .OfType<PropertyDeclarationSyntax>()
                    .GroupBy(x => x.Type)
                    .OrderBy(x => x.Key.ToString())
                    .Select(
                        group =>
                        {
                            MemberInfo[] propertyInfos
                            = group
                            .Select(p =>
                            {
                                var prop = syntaxContext.SemanticModel.GetDeclaredSymbol(p);
                                return new MemberInfo(prop.Type, prop, prop.Name);
                            }
                            )
                            .ToArray();

                            return new TypeGroupInfo(group.Key, syntaxContext.SemanticModel.GetSymbolInfo(group.Key), propertyInfos);

                        })
                    .ToList();

            return new VisitInfo(classDeclarationSyntax, typeSymbol, attribute, properties);
        }



        private static void InternalExecute(SourceProductionContext sourceProductionContext, (Compilation Compilation, ImmutableArray<VisitInfo> VisitInfos) source)
        {

            foreach (VisitInfo classToAugment in source.VisitInfos)
            {
                StringBuilder builder = new StringBuilder();
                NoosonGeneratorContext serializeContext = new(sourceProductionContext, writerName);
                NoosonGeneratorContext deserializeContext = new(sourceProductionContext, readerName);
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

                //rawSourceText += "You need a attribute for property XY, because it's a duplicate";
                string hintName
                    = $"{classToAugment.TypeSymbol.ToDisplayString()}";

                /*
                                            Schlachtplan
                0. ✓ IEnumerable => Not Supported (yet)
                5. ✓ Derserialize => Serialize Logik rückwärts aufrufen
                7. ✓ CleanUp and Refactor
                2. ✓ Ctor Analyzing => Get Only Props (Simples namematching von Parameter aufgrund von Namen), ReadOnlyProps ohne Ctor Parameter ignorieren
                8. ✓ Support for Dictionaries 
                6. ✓ Fehler/Warnings ausgeben
                9. ✓ Neue Generator API (v2)
                3.   Attributes => Überschreiben von Property Namen zu Ctor Parameter
                4.   Custom Type Serializer/Deserializer => Falls etwas not supported wird, wie IReadOnlySet, IEnumerable
                1.   Listen: IEnumerable => List, IReadOnlyCollection => ReadOnlyCollection, IReadOnlyDictionary => ReadOnlyDictionary

                *: ✓ Serialize bzw. Deserialize auf List Items aufrufen wenn möglich
                *: ✓ Randomize der count variable beim Deserialize
                *: ✓ Serialize Member access
                *: ✓ Randomize var item name
                *: ✓ List filter indexer
                *: ✓ item Accessor Dictionary serialize
                *: X Dictionary doesnt have add for KeyValuePair
               

                Unsere Attribute:
                1.   Ignore
                2.   Property name for ctor / Ctor Attribute for property name
                3.   PrefferedCtor
                4.   Custom Serialize/Deserialize

                Future:
                5.   BinaryWriter / Span Switch/Off/On
   
                */

                using var workspace = new AdhocWorkspace();
                var options = workspace.Options;
                var formattedText = Formatter.Format(sourceCode, workspace, options).ToFullString();

                sourceProductionContext.AddSource(hintName, formattedText);
            }

        }


        internal static BaseMethodDeclarationSyntax GenerateSerializeMethod(VisitInfo visitInfo, NoosonGeneratorContext context)
        {
            var parameter = SyntaxFactory.ParseParameterList($"System.IO.BinaryWriter {context.ReaderWriterName}");
            var body
                = CreateBlock(visitInfo, context, MethodType.Serialize);

            return new MethodBuilder("Serialize")
                .WithModifiers(Modifiers.Public)
                //.WithReturnType(null)
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
                = visitInfo
                .Properties
                .SelectMany(propertieTypeGroup => GenerateStatementsForProps(propertieTypeGroup.Properties, context, methodType))
                .Where(statement => statement is not null)
                .ToList();

            if (methodType == MethodType.Deserialize)
            {
                try
                {
                    var ret = CtorSerializer.CallCtorAndSetProps(visitInfo.TypeSymbol, statements, returnValue, DeclareOrAndAssign.DeclareAndAssign);
                    statements.Add(ret);
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



        internal static IEnumerable<StatementSyntax> GenerateStatementsForProps(ICollection<MemberInfo> properties, NoosonGeneratorContext context, MethodType methodType)
        {
            NoosonIgnoreAttributeTemplate ignore = new NoosonIgnoreAttributeTemplate();

            foreach (MemberInfo property in properties)
            {
                ITypeSymbol propertyType = property.TypeSymbol;

                string propertyName = property.Name;
                //int index = propertyName.IndexOf('.');

                //if (index >= 0)
                //{
                //    continue;
                //}

                if(!IsPropertySupported(property, context)
                    || property.Symbol.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == ignore.FullName))
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
                           "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                           property.Symbol,
                           DiagnosticSeverity.Error
                           );
                    return false;
                }
            }

            return true;
        }



        //Proudly stolen from https://github.com/mknejp/dotvariant/blob/c59599a079637e38c3471a13b6a0443e4e607058/src/dotVariant.Generator/Diagnose.cs#L234


        internal static StatementSyntax CreateStatementForSerializing(MemberInfo property, NoosonGeneratorContext context, string writerName)
        {
            StatementSyntax statement;
            var success
                        = SpecialTypeSerializer.TrySerialize(property, context, writerName, out statement)
                           || EnumSerializer.TrySerialize(property, context, writerName, out statement)
                           || MethodCallSerializer.TrySerialize(property, context, writerName, out statement)
                           || DictionarySerializer.TrySerialize(property, context, writerName, out statement)
                           || ListSerializer.TrySerialize(property, context, writerName, out statement)
                           || PublicPropertySerializer.TrySerialize(property, context, writerName, out statement)
                           ;


            return statement;
        }

        internal static StatementSyntax CreateStatementForDeserializing(MemberInfo property, NoosonGeneratorContext context, string readerName)
        {
            StatementSyntax statement;
            var success
                        = SpecialTypeSerializer.TryDeserialize(property, context, readerName, out statement)
                           || EnumSerializer.TryDeserialize(property, context, readerName, out statement)
                           || MethodCallSerializer.TryDeserialize(property, context, readerName, out statement)
                           || DictionarySerializer.TryDeserialize(property, context, readerName, out statement)
                           || ListSerializer.TryDeserialize(property, context, readerName, out statement)
                           || PublicPropertySerializer.TryDeserialize(property, context, readerName, out statement)
                           ;

            return statement;
        }

    }

    public enum MethodType
    {
        Serialize,
        Deserialize
    }

}
