using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
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

    private static StaticSerializerInfo GetStaticSerializerInfo(Compilation compilation, ClassDeclarationSyntax classDeclaration)
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
                    bool isFinalizer = false;

                    static int GetPriorityFromArgument(AttributeArgumentSyntax argument)
                    {
                        return int.Parse(argument.ToFullString());
                    }

                    var list = attr.ArgumentList?.Arguments.ToList();
                    if (list is null)
                        return new(0, 0, false);
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var attrItem = list[i];
                        if (attrItem.NameEquals is { } nameEquals)
                        {
                            list.RemoveAt(i);
                            if (nameEquals.Name.Identifier.Text == "IsFinalizer")
                                isFinalizer = bool.Parse(attrItem.Expression.ToString());
                        }
                    }
                    if (list is { Count: 1 })
                    {
                        var serializerPriority = GetPriorityFromArgument(list[0]);
                        return new(serializerPriority, serializerPriority, isFinalizer);
                    }

                    if (list is { Count: 2 })
                    {
                        var serializerPriority = GetPriorityFromArgument(list[0]);
                        var deserializerPriority = GetPriorityFromArgument(list[1]);
                        return new(serializerPriority, deserializerPriority, isFinalizer);
                    }
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

    private static void WriteSerialize(IndentedTextWriter tw, (ClassDeclarationSyntax, StaticSerializerInfo)[] serializers)
    {
        tw.WriteLine("internal static GeneratedSerializerCode CreateStatementForSerializing(MemberInfo property, NoosonGeneratorContext context, string writerName, SerializerMask includedSerializers = SerializerMask.All, SerializerMask excludedSerializers = SerializerMask.None)");
        WriteBodyHead(tw);
        WriteBody(tw, serializers.Where(x => !x.Item2.IsFinalizer), true);
        WriteBody(tw, serializers.Where(x => x.Item2.IsFinalizer), true);
        WriteBodyTail(tw);
    }

    private static void WriteDeserialize(IndentedTextWriter tw, (ClassDeclarationSyntax, StaticSerializerInfo)[] serializers)
    {
        tw.WriteLine("internal static GeneratedSerializerCode CreateStatementForDeserializing(MemberInfo property, NoosonGeneratorContext context, string readerName, SerializerMask includedSerializers = SerializerMask.All, SerializerMask excludedSerializers = SerializerMask.None)");
        WriteBodyHead(tw);
        WriteBody(tw, serializers.Where(x => !x.Item2.IsFinalizer), false);
        WriteBody(tw, serializers.Where(x => x.Item2.IsFinalizer), false);
        WriteBodyTail(tw);
    }

    private static void WriteBodyHead(IndentedTextWriter tw)
    {
        tw.WriteLine("{");
        tw.Indent++;
        tw.WriteLine("GeneratedSerializerCode statements = new();");
        
        tw.WriteLine("includedSerializers &= ~excludedSerializers;");
        
        tw.WriteLine("bool cont = false;");
        tw.WriteLine("bool done = false;");
    }

    private static void WriteBodyTail(IndentedTextWriter tw)
    {
        tw.WriteLine("return statements;");
        tw.Indent--;
        tw.WriteLine("}");
    }
    private static void WriteBody(IndentedTextWriter tw, IEnumerable<(ClassDeclarationSyntax declaration, StaticSerializerInfo serializerInfo)> serializers, bool isSerializer)
    {
        string methodName = "TryDeserialize";
        string paramName = "readerName";
        if (isSerializer)
        {
            methodName = "TrySerialize";
            paramName = "writerName";
        }
        tw.WriteLine("do");
        tw.WriteLine("{");
        tw.Indent++;
        tw.WriteLine("cont = false;");
        foreach (var s in serializers)
        {
            var serializerName = s.declaration.Identifier.ToFullString().Trim();
            tw.WriteLine($"if ((includedSerializers & SerializerMask.{serializerName}) != SerializerMask.None)");
            tw.WriteLine("{");
            tw.Indent++;
            tw.WriteLine($"switch ({serializerName}.{methodName}(ref property, context, {paramName}, statements, ref includedSerializers))");
            tw.WriteLine("{");
            tw.Indent++;
            tw.WriteLine("case Continuation.Done:");
            tw.Indent++;
            tw.WriteLine("done = true;");
            tw.WriteLine("continue;");
            tw.Indent--;
            tw.WriteLine("case Continuation.NotExecuted:");
            tw.WriteLine("case Continuation.Continue:");
            tw.Indent++;
            tw.WriteLine("break;");
            tw.Indent--;
            tw.WriteLine("case Continuation.Retry:");
            tw.Indent++;
            tw.WriteLine("cont = true;");
            tw.WriteLine("continue;");
            tw.Indent--;
            tw.Indent--;
            
            tw.WriteLine("}");
            tw.Indent--;
            tw.WriteLine("}");
        }
        tw.Indent--;
        tw.WriteLine("} while(cont);");
        tw.WriteLine("if (!done)");
        tw.Indent++;
        tw.WriteLine("return statements;");
        tw.Indent--;
    }

    private static void WriteSerializerContinuationEnum(IndentedTextWriter tw)
    {
        tw.WriteLine("public enum Continuation");
        tw.WriteLine("{");
        tw.Indent++;
        
        tw.WriteLine("NotExecuted,");
        tw.WriteLine("Continue,");
        tw.WriteLine("Retry,");
        tw.WriteLine("Done");
        
        tw.Indent--;
        tw.WriteLine("}");
    }
    private static void WriteSerializerMaskEnum(IndentedTextWriter tw, (ClassDeclarationSyntax declaration, StaticSerializerInfo serializerInfo)[] serializers)
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
                .Select(x => (x, GetStaticSerializerInfo(compilation, x!))).OrderBy(x => x.Item2.SerializerPriority).ToArray();
        var sortedDeserializers = sortedSerializers.OrderBy(x => x.Item2.DeserializerPriority).ToArray();

        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        using var tw = new IndentedTextWriter(sw);

        tw.WriteLine("using System;");
        tw.WriteLine("");
        tw.WriteLine("namespace NonSucking.Framework.Serialization;");
        tw.WriteLine("");

        WriteSerializerContinuationEnum(tw);
        WriteSerializerMaskEnum(tw, sortedSerializers);
        
        tw.WriteLine("public partial class NoosonGenerator");
        tw.WriteLine("{");

        tw.Indent++;
        WriteSerialize(tw, sortedSerializers);
        WriteDeserialize(tw, sortedDeserializers);

        tw.Indent--;
        
        tw.WriteLine("}");
        
        context.AddSource("NoosonGenerator.SerializeMethods.cs", sb.ToString());
    }
}