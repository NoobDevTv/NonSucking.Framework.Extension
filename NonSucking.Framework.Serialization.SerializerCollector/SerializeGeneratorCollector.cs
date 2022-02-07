using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NonSucking.Framework.Serialization.SerializerCollector;
[Generator]
public class SerializeGeneratorCollector : IIncrementalGenerator
{
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } };
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
            transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)).Where(static c => c is not null);
        var compilationAndClasses 
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, 
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }
    private const string StaticSerializerAttribute = "NonSucking.Framework.Serialization.Serializers.StaticSerializerAttribute";
    static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        foreach (var attrList in classDeclaration.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attr).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == StaticSerializerAttribute)
                {
                    return classDeclaration;
                }
            }
        }

        return null;
    }

    private static int GetPriority(Compilation compilation, ClassDeclarationSyntax classDeclaration)
    {
        foreach (var attrList in classDeclaration.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (compilation.GetSemanticModel(attr.SyntaxTree).GetSymbolInfo(attr).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == StaticSerializerAttribute)
                {
                    return attr.ArgumentList is { Arguments.Count: > 0 }
                        ? int.Parse(attr.ArgumentList.Arguments[0].ToFullString())
                        : 0;
                }
            }
        }

        throw new NotImplementedException();
    }
    
    const string IdPrefix = "NSG";
    internal static void AddDiagnostic(SourceProductionContext context, string id, string title, string message, Location location, DiagnosticSeverity severity, string? helpLinkurl = null, params string[] customTags)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                $"{IdPrefix}{id}",
                title,
                message,
                nameof(SerializeGeneratorCollector),
                severity,
                true,
                helpLinkUri: helpLinkurl,
                customTags: customTags),
            location));
    }

    private static void WriteSerialize(IndentedTextWriter tw, (ClassDeclarationSyntax, int)[] serializers)
    {
        tw.WriteLine("internal static GeneratedSerializerCode CreateStatementForSerializing(MemberInfo property, NoosonGeneratorContext context, string writerName, SerializerMask includedSerializers = SerializerMask.All, SerializerMask excludedSerializers = SerializerMask.None)");
        WriteBody(tw, serializers, true);
    }

    private static void WriteDeserialize(IndentedTextWriter tw, (ClassDeclarationSyntax, int)[] serializers)
    {
        tw.WriteLine("internal static GeneratedSerializerCode CreateStatementForDeserializing(MemberInfo property, NoosonGeneratorContext context, string readerName, SerializerMask includedSerializers = SerializerMask.All, SerializerMask excludedSerializers = SerializerMask.None)");
        WriteBody(tw, serializers, false);
    }

    private static void WriteBody(IndentedTextWriter tw, (ClassDeclarationSyntax declaration, int priority)[] serializers, bool isSerializer)
    {
        string methodName = "TryDeserialize";
        string paramName = "readerName";
        if (isSerializer)
        {
            methodName = "TrySerialize";
            paramName = "writerName";
        }
        tw.WriteLine("{");
        tw.Indent++;
        tw.WriteLine("GeneratedSerializerCode statements = new();");
        
        tw.WriteLine("includedSerializers &= ~excludedSerializers;");

        tw.Write("_ = ");
        tw.Indent++;

        for (int i = 0; i < serializers.Length; i++)
        {
            if (i > 0)
            {
                tw.Write("|| ");
            }
            var s = serializers[i];
            var serializerName = s.declaration.Identifier.ToFullString().Trim();
            tw.WriteLine($"(includedSerializers & SerializerMask.{serializerName}) != SerializerMask.None && {serializerName}.{methodName}(property, context, {paramName}, statements)");
        }
        tw.WriteLine(";");

        tw.Indent--;
        tw.WriteLine("return statements;");
        tw.Indent--;
        tw.WriteLine("}");
    }

    private static void WriteEnum(IndentedTextWriter tw, (ClassDeclarationSyntax declaration, int priority)[] serializers)
    {
        tw.WriteLine("[Flags]");
        tw.WriteLine("public enum SerializerMask : ulong");
        tw.WriteLine("{");
        tw.Indent++;
        tw.WriteLine("None = 0,");

        var all = new StringBuilder();
        
        for (int i = 0; i < serializers.Length; i++)
        {
            var s = serializers[i];
            var serializerName = s.declaration.Identifier.ToFullString().Trim();
            tw.Write($"{serializerName} = {1 << i}");
            tw.WriteLine(",");

            if (i > 0)
                all.Append(" | ");
            all.Append(serializerName);
        }
        
        tw.WriteLine($"All = {all}");

        tw.Indent--;
        tw.WriteLine("}");
    }
    
    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classDeclarations, SourceProductionContext context)
    {
        if (classDeclarations.IsDefaultOrEmpty)
        {
            return;
        }

        var sortedSerializers
            = classDeclarations.OfType<ClassDeclarationSyntax>()
                .Select(x => (x, GetPriority(compilation, x!))).OrderBy(x => x.Item2).ToArray();

        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        using var tw = new IndentedTextWriter(sw);

        tw.WriteLine("using System;");
        tw.WriteLine("");
        tw.WriteLine("namespace NonSucking.Framework.Serialization;");
        tw.WriteLine("");
        
        
        tw.WriteLine("public partial class NoosonGenerator");
        tw.WriteLine("{");

        tw.Indent++;

        WriteEnum(tw, sortedSerializers);
        WriteSerialize(tw, sortedSerializers);
        WriteDeserialize(tw, sortedSerializers);

        tw.Indent--;
        
        tw.WriteLine("}");
        
        context.AddSource("NoosonGenerator.SerializeMethods.cs", sb.ToString());
    }
}