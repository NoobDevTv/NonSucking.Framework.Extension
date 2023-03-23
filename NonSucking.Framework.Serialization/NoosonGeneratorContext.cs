using Microsoft.CodeAnalysis;

using System.Collections.Generic;

using VaVare;

namespace NonSucking.Framework.Serialization
{
    /*
     TODOs:
    5. Maybe future factory class for instance creation in deserialize (For now you can pass an instance into the method)
    6. Resolver Table for dynamic types, for runtime type serialization
     */
    public record NoosonGeneratorContext(GlobalContext GlobalContext, SourceProductionContext GeneratorContext, GeneratedFile GeneratedFile, GeneratedType DefaultGeneratedType, string ReaderWriterName, ISymbol MainSymbol, bool UseAdvancedTypes, MethodType MethodType, string MethodName, string? WriterTypeName = null, string? ReaderTypeName = null)
    {
        public List<Modifiers> Modifiers { get; } = new() { VaVare.Modifiers.Public };

        //Proudly stolen from https://github.com/mknejp/dotvariant/blob/c59599a079637e38c3471a13b6a0443e4e607058/src/dotVariant.Generator/Diagnose.cs#L234
        public void AddDiagnostic(int id, LocalizableString message, DiagnosticSeverity severity, int warningLevel, Location location)
        {
            GeneratorContext.AddDiagnostic(id, message, severity, warningLevel, location);
        }

        internal void AddDiagnostic(int id, string title, string message, Location location, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
        {
            GeneratorContext.AddDiagnostic(id, title, message, location, severity, helpLinkUrl, customTags);
        }
        internal void AddDiagnostic(int id, string title, string message, ISymbol symbolForLocation, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
        {
            GeneratorContext.AddDiagnostic(id, title, message, symbolForLocation, severity, helpLinkUrl, customTags);
        }
        internal void AddDiagnostic(Diagnostics.DiagnosticInfo diagnosticInfo, Location location, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
        {
            GeneratorContext.AddDiagnostic(diagnosticInfo, location, severity, helpLinkUrl, customTags);
        }
        internal void AddDiagnostic(Diagnostics.DiagnosticInfo diagnosticInfo, ISymbol symbolForLocation, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
        {
            GeneratorContext.AddDiagnostic(diagnosticInfo, symbolForLocation, severity, helpLinkUrl, customTags);
        }
    }

}
