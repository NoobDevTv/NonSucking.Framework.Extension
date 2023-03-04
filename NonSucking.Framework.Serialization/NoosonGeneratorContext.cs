using Microsoft.CodeAnalysis;

using System.Collections.Generic;

using VaVare;

namespace NonSucking.Framework.Serialization
{
    /*
     TODOs:
    2. Call base.serialize when base virtual, ignore the props of the base => How to ctor?
    5. Maybe future factory class for instance creation in deserialize
    6. Resolver Table for dynamic types, for runtime type serialization
     */
    public record NoosonGeneratorContext(GlobalContext GlobalContext, SourceProductionContext GeneratorContext, GeneratedType GeneratedType, string ReaderWriterName, ISymbol MainSymbol, bool UseAdvancedTypes, string? WriterTypeName = null, string? ReaderTypeName = null)
    {
        internal const string Category = "SerializationGenerator";
        internal const string IdPrefix = "NSG";

        public List<Modifiers> Modifiers { get; } = new() { VaVare.Modifiers.Public };

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
            var loc = LocationFromSymbol(symbolForLocation) ?? Location.None;
            AddDiagnostic(
                id,
                title,
                message,
                loc,
                severity,
                helpLinkurl,
                customTags);
        }

        internal static Location? LocationFromSymbol(ISymbol symbol)
        {
            if (symbol.DeclaringSyntaxReferences.Length == 0)
                return null;
            return Location.Create(
                    symbol.DeclaringSyntaxReferences[0].SyntaxTree,
                    symbol.DeclaringSyntaxReferences[0].Span);
        }

        internal static Location GetExistingFrom(params ISymbol[] symbols)
        {
            foreach (var symbol in symbols)
            {
                if (LocationFromSymbol(symbol) is { } loc)
                    return loc;
            }
            return Location.None;
        }
    }

}
