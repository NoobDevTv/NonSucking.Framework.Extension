using Microsoft.CodeAnalysis;

namespace NonSucking.Framework.Serialization;

public static class GeneratorContextExtensions
{
    internal const string Category = "SerializationGenerator";
    internal const string IdPrefix = "NSG";

    public static void AddDiagnostic(this SourceProductionContext that, int id, LocalizableString message, DiagnosticSeverity severity, int warningLevel, Location location)
    {
        that.ReportDiagnostic(
            Diagnostic.Create(
                $"{IdPrefix}{id:0000}",
                Category,
                message,
                severity,
                severity,
                true,
                warningLevel,
                location: location));
    }

    internal static void AddDiagnostic(this SourceProductionContext that, int id, string title, string message, Location location, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
    {
        that.ReportDiagnostic(Diagnostic.Create(
             new DiagnosticDescriptor(
                 $"{IdPrefix}{id:0000}",
                 title,
                 message,
                 nameof(NoosonGenerator),
                 severity,
                 true,
                 helpLinkUri: helpLinkUrl,
                 customTags: customTags),
             location));
    }
    internal static void AddDiagnostic(this SourceProductionContext that, int id, string title, string message, ISymbol symbolForLocation, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
    {
        var loc = symbolForLocation.GetLocation() ?? Location.None;
        that.AddDiagnostic(
            id,
            title,
            message,
            loc,
            severity,
            helpLinkUrl,
            customTags);
    }
    internal static void AddDiagnostic(this SourceProductionContext that, Diagnostics.DiagnosticInfo diagnosticInfo, Location location, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
    {
        that.AddDiagnostic(diagnosticInfo.Id, diagnosticInfo.Title, diagnosticInfo.FormatString, location, severity, helpLinkUrl, customTags);
    }
    internal static void AddDiagnostic(this SourceProductionContext that, Diagnostics.DiagnosticInfo diagnosticInfo, ISymbol symbolForLocation, DiagnosticSeverity severity, string? helpLinkUrl = null, params string[] customTags)
    {
        that.AddDiagnostic(diagnosticInfo.Id, diagnosticInfo.Title, diagnosticInfo.FormatString, symbolForLocation, severity, helpLinkUrl, customTags);
    }

    internal static Location GetLocation(this SyntaxReference reference)
    {
        return Location.Create(reference.SyntaxTree, reference.Span);
    }
    internal static Location? GetLocation(this ISymbol symbol)
    {
        if (symbol.DeclaringSyntaxReferences.Length == 0)
            return null;
        return symbol.DeclaringSyntaxReferences[0].GetLocation();
    }
}