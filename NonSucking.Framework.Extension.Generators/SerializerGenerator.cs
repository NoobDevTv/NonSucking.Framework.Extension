//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Text;
//using System;
//using System.Linq;
//using System.Collections.Generic;
//using System.Text;

//namespace NonSucking.Framework.Extension.Generators
//{
//    [Generator]
//    public class SerializerGenerator : ISourceGenerator
//    {
//        private const string attributeText = @"
//using System;
//namespace NonSucking.Framework.Extension.Serialization
//{
//    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
//    [System.Diagnostics.Conditional(""SerializerGenerator_DEBUG"")]
//    public sealed class GenSerializationAttribute : Attribute
//    {
//        public GenSerializationAttribute()
//        {
//        }

//        public string PropertyName { get; set; }
//    }
//}
//";

//        public void Initialize(GeneratorInitializationContext context)
//        {
//            context.RegisterForPostInitialization(i => i.AddSource("GenSerializationAttribute", attributeText));
//            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

//        }

//        public void Execute(GeneratorExecutionContext context)
//        {
//            var builder = new StringBuilder();
//            //context.AddSource("GenSerializationAttribute", SourceText.From(attributeText, Encoding.UTF8));

//            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
//                return;

//            //var attributeSymbol
//            //    = context
//            //        .Compilation
//            //        .GetTypeByMetadataName("NonSucking.Framework.Extension.Serialization.GenSerializationAttribute");
//            int i = 0;
//            foreach (var classToAugment in receiver.ClassesToAugment)
//            {
//                //var a
//                //    = classToAugment
//                //        .Item2
//                //        .GetAttributes()
//                //        .FirstOrDefault(x => x.AttributeClass?.ContainingSymbol.Equals(attributeSymbol.ContainingSymbol, SymbolEqualityComparer.Default) ?? false);
//                //if (a != null)
//                    context.AddSource($"user_serializer{i++}.autogen.cs", SourceText.From($@"
//                namespace DEMO
//                {{

//                    public partial User
//                    {{
//                        public void Serialize(){{}}
//                    }}
//                }}
//                "));
//            }

//            //context.AddSource("GenSerializationAttribute", SourceText.From(attributeText, Encoding.UTF8));

//        }

//        private class SyntaxReceiver : ISyntaxContextReceiver
//        {
//            public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

//            public List<(ClassDeclarationSyntax, INamedTypeSymbol)> ClassesToAugment { get; } = new List<(ClassDeclarationSyntax, INamedTypeSymbol)>();

//            /// <summary>
//            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
//            /// </summary>
//            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
//            {
//                // any field with at least one attribute is a candidate for property generation
//                if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
//                    && classDeclarationSyntax.AttributeLists.Count > 0)
//                {
//                    var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
//                    //if (typeSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "NonSucking.Framework.Extension.Serialization.GenSerializationAttribute"))
//                    {
//                        ClassesToAugment.Add((classDeclarationSyntax, typeSymbol));
//                    }
//                    //foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
//                    //{
//                    //    // Get the symbol being declared by the field, and keep it if its annotated
//                    //    IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
//                    //    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"))
//                    //    {
//                    //        Fields.Add(fieldSymbol);
//                    //    }
//                    //}
//                }
//            }
//        }
//    }
//}
